using System.Collections;
using CNNFeatureMaps;
using ModelDesignScene;
using Tablet;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FactoryEnvironment
{
    // Owns only this factory session. Lessons retain their own calculations and input.
    public sealed class FactoryLearningFlowManager : MonoBehaviour
    {
        [SerializeField] private Transform[] _contentAnchors;
        [SerializeField] private Font _font;
        private readonly string[] _scenes = { "CNNPixelLab", "CNNFeatureMapLab", "CNNModelDesignLab" };
        private FactoryFlowPortalController _portal;
        private Scene _lesson;
        private CNNPixelLabManager _pixels;
        private CNNFeatureMapGuide _maps;
        private int _stage;
        private bool _loading;
        private string _error;
        private TabletManager _tablet;
        private TabletMapData _mapData;

        private void OnEnable()
        {
            if (_mapData != null)
            {
                PublishMap();
            }
        }

        private void Start()
        {
            PublishMap();
            _portal = new GameObject("Factory learning navigation").AddComponent<FactoryFlowPortalController>();
            _portal.transform.SetParent(transform, false);
            _portal.Initialize(_font, Advance);
            _portal.transform.position = new Vector3(0, 1.4f, -17);
            MovePlayer(new Vector3(0, 0, -20));
            SceneManager.SetActiveScene(gameObject.scene);
        }

        private void Update()
        {
            if (_portal == null) { return; }
            bool ready = _stage == 0 || (_stage == 1 && _pixels != null && _pixels.ConvolutionCompleted)
                || (_stage == 2 && _maps != null && _maps.ReadyForModelDesign);
            string text = _stage == 0 ? "필터 체험 시작" : _stage == 1 ? "피처맵 체험으로" : "CNN 모델 제작으로";
            string hint = _stage == 0 ? "필터 → 피처맵 → CNN 모델 제작\n오른손 레이로 가리키고 Trigger를 누르세요."
                : _stage == 1 ? "최종 RGB 합 계산을 완료하면 다음 체험으로 이동해요.\n피처맵 체험에서는 별도의 8×8 예제 이미지를 사용해요."
                : "4단계에서 입력 픽셀을 바꾸고 피처맵의 변화를 관찰하세요.";
            if (_stage == 3) { text = "CNN 모델 제작"; hint = "블록을 배치하고 모델을 학습시켜 결과를 확인하세요."; }
            _portal.Show(_loading ? "체험 불러오는 중…" : text, _error ?? hint, ready && !_loading);
        }

        private void Advance()
        {
            if (_loading || _stage >= _scenes.Length) { return; }
            if (_stage == 1 && (_pixels == null || !_pixels.ConvolutionCompleted)) { return; }
            if (_stage == 2 && (_maps == null || !_maps.ReadyForModelDesign)) { return; }
            // Do not destroy a held object or move a player while they are manipulating it.
            foreach (XRBaseInteractor interactor in FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
            {
                if (interactor.hasSelection) { _error = "잡은 물체를 놓고 다시 눌러 주세요."; return; }
            }
            _loading = true;
            _error = null;
            StartCoroutine(LoadLesson());
        }

        private IEnumerator LoadLesson()
        {
            yield return null; // Finish the Trigger callback before unloading its scene.
            string next = _scenes[_stage];
            if (_contentAnchors == null || _contentAnchors.Length <= _stage || _contentAnchors[_stage] == null
                || !Application.CanStreamedLevelBeLoaded(next))
            {
                Fail("체험을 준비하지 못했습니다. 담당자에게 알려 주세요.");
                yield break;
            }
            if (SceneManager.GetSceneByName(next).isLoaded)
            {
                Fail("해당 체험이 이미 열려 있습니다. 공장 씬에서 다시 시작해 주세요.");
                yield break;
            }
            AsyncOperation load = SceneManager.LoadSceneAsync(next, LoadSceneMode.Additive);
            if (load == null) { Fail("체험을 열지 못했습니다. 다시 눌러 주세요."); yield break; }
            yield return load;
            Scene incoming = SceneManager.GetSceneByName(next);
            GameObject content = FindContent(incoming);
            if (content == null)
            {
                yield return SceneManager.UnloadSceneAsync(incoming);
                Fail("체험 루트를 찾지 못했습니다. 이전 체험은 유지됩니다.");
                yield break;
            }
            // Preserve the factory lighting and floor; discard only the lesson's standalone surroundings.
            foreach (GameObject root in incoming.GetRootGameObjects())
            {
                if (root != content) { root.SetActive(false); }
            }
            foreach (Light light in content.GetComponentsInChildren<Light>(true)) { light.enabled = false; }
            Transform anchor = _contentAnchors[_stage];
            Vector3 offset = anchor.position - Vector3.forward * 1.5f;
            content.transform.position += offset;
            foreach (Rigidbody body in content.GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
            CNNPixelLabManager nextPixels = content.GetComponentInChildren<CNNPixelLabManager>(true);
            if (nextPixels != null) { nextPixels.RebaseDock(offset); }
            CNNFeatureMapGuide nextMaps = content.GetComponentInChildren<CNNFeatureMapGuide>(true);
            if (_pixels != null) { _pixels.CloseDetails(); }
            if (_lesson.IsValid() && _lesson.isLoaded) { yield return SceneManager.UnloadSceneAsync(_lesson); }
            _lesson = incoming;
            _pixels = nextPixels;
            _maps = nextMaps;
            SceneManager.SetActiveScene(gameObject.scene);
            _stage++;
            // Keep navigation beside the participant, outside the lesson's front-facing panels.
            _portal.transform.position = anchor.position + new Vector3(-2.6f, 1.25f, -2.5f);
            _portal.transform.rotation = Quaternion.Euler(0, -75f, 0);
            MovePlayer(anchor.position + Vector3.back * 3);
            _loading = false;
        }

        private static GameObject FindContent(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<CNNPixelLabManager>(true) != null
                    || root.GetComponentInChildren<CNNFeatureMapLabManager>(true) != null
                    || root.GetComponentInChildren<CNNModelDesignLabManager>(true) != null) { return root; }
            }
            return null;
        }

        private static void MovePlayer(Vector3 floorPosition)
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            if (rig == null || rig.Camera == null) { return; }
            float height = rig.Camera.transform.position.y - rig.Origin.transform.position.y;
#if UNITY_EDITOR
            // The keyboard simulator starts its floor-tracked headset at zero height.
            // Real headset tracking must keep the user's measured height.
            if (!UnityEngine.XR.XRSettings.isDeviceActive
                && FindFirstObjectByType<XR.XREditorSimulator>() != null && height < .5f)
            {
                Vector3 offset = rig.CameraFloorOffsetObject.transform.localPosition;
                offset.y += rig.CameraYOffset - height;
                rig.CameraFloorOffsetObject.transform.localPosition = offset;
                height = rig.CameraYOffset;
            }
#endif
            rig.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
            rig.MoveCameraToWorldLocation(floorPosition + Vector3.up * height);
        }

        private void Fail(string message) { _error = message; _loading = false; Debug.LogError(message, this); }

        private void PublishMap()
        {
            _mapData = FactoryMapReader.Read(gameObject);
            _tablet = FindFirstObjectByType<TabletManager>();
            if (_tablet != null)
            {
                _tablet.SetMapData(_mapData);
            }
        }

        private void OnDisable()
        {
            if (_tablet != null)
            {
                _tablet.ClearMapData(_mapData);
            }
        }

        private void OnDestroy()
        {
            if (_lesson.IsValid() && _lesson.isLoaded && Application.isPlaying)
            {
                SceneManager.UnloadSceneAsync(_lesson);
            }
        }
    }
}

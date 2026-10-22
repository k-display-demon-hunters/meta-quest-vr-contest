using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using XR;

namespace FactoryTutorial
{
    public sealed class FactoryTutorialManager : MonoBehaviour
    {
        private static readonly string[] Titles = { "이동과 둘러보기", "선택", "잡기", "정면 복원", "태블릿" };
        private static readonly string[] Instructions =
        {
            "고개를 돌려 주변을 둘러보세요.\n왼손 스틱으로 이동하고 오른손 스틱으로 시점을 회전합니다.\n이 안내에서는 동작을 수행하지 않아도 다음으로 넘어갈 수 있습니다.",
            "오른손 ray로 버튼을 가리키면 테두리가 강조됩니다.\n오른손 Trigger를 눌러 선택하세요.\n흐리게 표시된 버튼은 아직 사용할 수 없습니다.",
            "오른손 ray로 물체를 가리키고 Grip을 누른 채 잡습니다.\n잡은 물체는 시선을 따라 이동하고 Grip을 놓으면 해제됩니다.\n물체를 잡는 동안 일부 화면 버튼은 사용할 수 없습니다.",
            "현재 위치에서 정면을 다시 맞추려면 오른손 A를 누르세요.\n오른손 B로 ray를 켜거나 끌 수 있습니다.\nray를 끄면 잡고 있던 물체도 놓습니다.",
            "왼손 태블릿에서 지도와 수집한 양피지를 확인할 수 있습니다.\n왼손 Trigger + 스틱으로 항목 이동, X로 선택, Y로 뒤로 갑니다.\nMenu로 설정을 엽니다. 안내를 마치고 Pixel Lab으로 이동하세요."
        };
        [SerializeField] private FactoryTutorialPanelController _panel;
        [SerializeField] private Font _font;

        private XRPlayerInputController _playerInput;
        private bool _initialized;
        private bool _ownsRay;
        private bool _previousForcedRay;
        private int _page;
        private bool _finishing;

        public bool IsComplete { get; private set; }

        private void OnEnable()
        {
            if (_initialized && !IsComplete)
            {
                HoldGuideRay();
            }
        }

        public bool Initialize(XROrigin rig, Transform entrance)
        {
            if (_initialized)
            {
                return true;
            }
            if (rig == null || rig.Camera == null || entrance == null
                || _panel == null || _font == null)
            {
                Debug.LogError("[FactoryTutorial] XR 리그와 공장 입구·안내 화면 연결을 확인해 주세요.", this);
                return false;
            }

            _playerInput = rig.GetComponent<XRPlayerInputController>();
            if (_playerInput == null)
            {
                Debug.LogError("[FactoryTutorial] 공통 XR 리그의 입력 Controller를 확인해 주세요.", this);
                return false;
            }

            _panel.transform.SetPositionAndRotation(
                entrance.position + entrance.forward * 3f + Vector3.up * 1.65f, entrance.rotation);
            _panel.Initialize(_font, rig.Camera);
            _panel.OnPrevious += Previous;
            _panel.OnNext += Next;
            _panel.OnFinish += Finish;
            _initialized = true;
            HoldGuideRay();
            Refresh();
            return true;
        }

        private void Previous()
        {
            if (_finishing || IsComplete)
            {
                return;
            }
            _page = Mathf.Max(0, _page - 1);
            Refresh();
        }

        private void Next()
        {
            if (_finishing || IsComplete)
            {
                return;
            }
            if (_page == Titles.Length - 1)
            {
                Finish();
                return;
            }
            _page++;
            Refresh();
        }

        private void Refresh()
        {
            _panel.ShowPage(_page, Titles.Length, Titles[_page], Instructions[_page]);
        }

        private void Finish()
        {
            if (_finishing || IsComplete)
            {
                return;
            }
            _finishing = true;
            StartCoroutine(FinishAfterClick());
        }

        private IEnumerator FinishAfterClick()
        {
            // Let the XR UI callback finish before the factory unloads this scene.
            yield return null;
            ReleaseGuideRay();
            _panel.gameObject.SetActive(false);
            IsComplete = true;
        }

        private void HoldGuideRay()
        {
            if (_ownsRay || _playerInput == null)
            {
                return;
            }
            _playerInput.EnsureRayReady();
            _previousForcedRay = _playerInput.TutorialRayForcedOn;
            _playerInput.TutorialRayForcedOn = true;
            _ownsRay = true;
        }

        private void ReleaseGuideRay()
        {
            if (!_ownsRay)
            {
                return;
            }
            if (_playerInput != null)
            {
                _playerInput.TutorialRayForcedOn = _previousForcedRay;
            }
            _ownsRay = false;
        }

        private void OnDisable()
        {
            ReleaseGuideRay();
            if (_finishing && !IsComplete)
            {
                StopAllCoroutines();
                _finishing = false;
            }
        }

        private void OnDestroy()
        {
            if (_panel != null)
            {
                _panel.OnPrevious -= Previous;
                _panel.OnNext -= Next;
                _panel.OnFinish -= Finish;
            }
        }
    }
}

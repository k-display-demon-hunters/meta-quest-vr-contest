using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdySuccessController : MonoBehaviour
    {
        [SerializeField] private Renderer[] _machineLights = new Renderer[5];
        [SerializeField] private Transform _factorySign;
        [SerializeField] private KdyLessonGrabController _signGrab;
        [SerializeField] private Renderer _signSurface;
        [SerializeField] private Text _status;
        [SerializeField] private Text _scoreLabel;

        private const int SignWidth = 16;
        private const int SignHeight = 12;
        private const float LampInterval = .35f;
        private KdyLessonProgressController _progress;
        private MaterialPropertyBlock _lightProperties;
        private Material _originalSignMaterial;
        private Material _signMaterial;
        private Texture2D _signTexture;
        private bool _initialized;
        private bool _passed;
        private bool _celebrating;
        private bool _hasPickedUpSign;

        public event Action Changed;
        public bool HasPassedResult => _initialized && _passed;
        public bool IsSignGrabbed => _signGrab != null && _signGrab.IsGrabbed;
        public bool CanRetry => _initialized && !_celebrating && !IsSignGrabbed &&
            _progress != null && _progress.HasTrainingResult && _progress.TrainingAttempts > 0 &&
            _progress.LastTrainedNetwork.IsComplete;

        public string Hint
        {
            get
            {
                if (!_initialized || !_passed)
                {
                    return "통과한 학습 기록이 필요해요. 설계실로 돌아가 설계를 완성하고 목표 개선율에 도달해 보세요.";
                }
                if (_celebrating)
                {
                    return "마지막 학습 결과가 목표에 도달했어요. 공장 장치 다섯 개가 차례대로 복구됩니다.";
                }
                if (_hasPickedUpSign)
                {
                    return "간판 준비 완료. 다음 구역에서 간판을 입력 슬롯에 넣고 복원 결과를 비교해 보세요.";
                }
                return "오른손 레이를 새 간판 손잡이에 맞추고 Grip을 눌러 잡으세요. 노트북에서는 G를 계속 누릅니다.";
            }
        }

        public void Initialize(Camera viewCamera, KdyLessonProgressController progress)
        {
            if (_initialized)
            {
                return;
            }
            if (!HasRequiredReferences())
            {
                Debug.LogError("[KdyLearning] Scene 15 needs five machine lamps, a grabbable factory sign, its surface and result labels.", this);
                return;
            }

            _progress = progress;
            _passed = progress != null && progress.HasTrainingResult && progress.TrainingAttempts > 0 &&
                progress.LastTrainedNetwork.IsComplete && progress.LastImprovement >= progress.TargetImprovement;
            _lightProperties = new MaterialPropertyBlock();
            _signGrab.SetInteractionEnabled(false);
            CreateSignSurface();
            foreach (Renderer lamp in _machineLights)
            {
                SetMachineLight(lamp, false);
            }
            _signGrab.GrabbedChanged += HandleSignGrabbed;
            _initialized = true;
            UpdateScoreLabel();
            if (!_passed)
            {
                _status.text = "통과 조건을 충족한 기록이 없어요.\n먼저 설계실에서 신경망을 만들고 학습해 보세요.\n교육용 학습 시뮬레이션";
                Changed?.Invoke();
                return;
            }

            _celebrating = true;
            Changed?.Invoke();
            StartCoroutine(RestoreMachines());
        }

        private IEnumerator RestoreMachines()
        {
            for (int i = 0; i < _machineLights.Length; i++)
            {
                SetMachineLight(_machineLights[i], true);
                _status.text = $"목표 달성 · 공장 장치 복구 {i + 1} / {_machineLights.Length}\n교육용 학습 시뮬레이션의 마지막 기록을 확인했습니다.";
                yield return new WaitForSeconds(LampInterval);
            }
            _celebrating = false;
            _signGrab.SetInteractionEnabled(true);
            _status.text = "목표 달성! 공장 장치가 복구됐어요.\n새 간판은 아직 흐립니다. 오른손 Grip으로 손잡이를 잡으세요.\n앞서 학습한 간판과 다른 다음 작업용 입력입니다.\n교육용 학습 시뮬레이션";
            Changed?.Invoke();
        }

        private void HandleSignGrabbed(bool grabbed)
        {
            if (!_initialized || !_passed || !isActiveAndEnabled)
            {
                return;
            }
            if (grabbed && _factorySign.gameObject.activeInHierarchy)
            {
                _hasPickedUpSign = true;
                _status.text = "새 공장 간판을 집었어요.\n간판 준비 완료 · 다음 체험은 16번 구역에서 이어집니다.\n교육용 학습 시뮬레이션";
            }
            Changed?.Invoke();
        }

        private void UpdateScoreLabel()
        {
            if (_progress == null || !_progress.HasTrainingResult)
            {
                _scoreLabel.text = "학습 기록 없음\n교육용 학습 시뮬레이션";
                return;
            }
            string score = _progress.LastImprovement.ToString("0.0", CultureInfo.InvariantCulture);
            string target = _progress.TargetImprovement.ToString("0.0", CultureInfo.InvariantCulture);
            _scoreLabel.text = $"마지막 개선율 {score}% / 목표 {target}%\n{(_passed ? "통과" : "목표 미달")} · 교육용 학습 시뮬레이션";
        }

        private void SetMachineLight(Renderer lamp, bool restored)
        {
            Color color = restored ? new Color(.16f, .9f, .65f) : new Color(.035f, .065f, .08f);
            _lightProperties.Clear();
            lamp.GetPropertyBlock(_lightProperties);
            _lightProperties.SetColor("_BaseColor", color);
            _lightProperties.SetColor("_Color", color);
            _lightProperties.SetColor("_EmissionColor", restored ? color * 2.5f : Color.black);
            lamp.SetPropertyBlock(_lightProperties);
        }

        private void CreateSignSurface()
        {
            Color[] topRows = KdyFactorySignExampleData.CreateBlurredImage();
            Color[] pixels = new Color[topRows.Length];
            for (int y = 0; y < SignHeight; y++)
            {
                System.Array.Copy(topRows, y * SignWidth, pixels, (SignHeight - 1 - y) * SignWidth, SignWidth);
            }
            _signTexture = new Texture2D(SignWidth, SignHeight, TextureFormat.RGBA32, false)
            {
                name = "Kdy New Factory Sign VR",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _signTexture.SetPixels(pixels);
            _signTexture.Apply(false, true);
            _originalSignMaterial = _signSurface.sharedMaterial;
            _signMaterial = new Material(_originalSignMaterial)
            {
                name = "Kdy Success Factory Sign Material",
                mainTexture = _signTexture
            };
            if (_signMaterial.HasProperty("_BaseMap"))
            {
                _signMaterial.SetTexture("_BaseMap", _signTexture);
            }
            if (_signMaterial.HasProperty("_MainTex"))
            {
                _signMaterial.SetTexture("_MainTex", _signTexture);
            }
            if (_signMaterial.HasProperty("_BaseColor"))
            {
                _signMaterial.SetColor("_BaseColor", Color.white);
            }
            if (_signMaterial.HasProperty("_Color"))
            {
                _signMaterial.SetColor("_Color", Color.white);
            }
            _signSurface.sharedMaterial = _signMaterial;
        }

        private bool HasRequiredReferences()
        {
            if (_machineLights == null || _machineLights.Length != 5 || _factorySign == null || _signGrab == null ||
                _signSurface == null || _signSurface.sharedMaterial == null || _status == null || _scoreLabel == null)
            {
                return false;
            }
            foreach (Renderer lamp in _machineLights)
            {
                if (lamp == null)
                {
                    return false;
                }
            }
            return true;
        }

        private void OnDestroy()
        {
            if (_signGrab != null)
            {
                _signGrab.GrabbedChanged -= HandleSignGrabbed;
            }
            if (_signSurface != null && _signSurface.sharedMaterial == _signMaterial)
            {
                _signSurface.sharedMaterial = _originalSignMaterial;
            }
            if (_signMaterial != null)
            {
                Destroy(_signMaterial);
            }
            if (_signTexture != null)
            {
                Destroy(_signTexture);
            }
        }
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyQuestUnlockController : MonoBehaviour
    {
        [SerializeField] private Renderer[] _machineLights = new Renderer[5];
        [SerializeField] private Renderer[] _scrollSeals = new Renderer[5];
        [SerializeField] private Transform _designBench;
        [SerializeField] private Text _status;

        private const int RequiredScrollCount = 5;
        private const float LightDuration = .10f;
        private const float BenchDuration = 1.5f;
        private const float BenchLowering = 1.0f;
        private const string ParameterHint = "층 깊이·채널 수·커널 크기는 결과에 영향을 주지만, 크게 할수록 항상 좋아지지는 않아요.";
        private static readonly Color DarkLight = new Color(.035f, .075f, .10f);
        private static readonly Color CyanLight = new Color(.10f, .90f, 1.0f);
        private static readonly Color CollectedSeal = new Color(1.0f, .72f, .24f);
        private static readonly Color MissingSeal = new Color(.12f, .16f, .20f);

        private KdyLessonProgressController _progress;
        private MaterialPropertyBlock _properties;
        private Collider[] _benchColliders;
        private bool[] _benchColliderStates;
        private Vector3 _benchHome;
        private Vector3 _benchLowered;
        private Coroutine _reveal;
        private string _goalText;
        private bool _readyToDesign;
        private bool _initialized;

        public event Action Changed;
        public bool ReadyToDesign => _readyToDesign && HasAllScrolls();

        public string Hint
        {
            get
            {
                if (!_initialized)
                {
                    return "복구 장치와 양피지를 확인하고 있어요.";
                }

                if (!HasAllScrolls())
                {
                    return $"양피지 {_progress.ScrollCount}/5 · 먼저 CNN실에서 다섯 장을 모으세요.\n{ParameterHint}";
                }

                if (!_readyToDesign)
                {
                    return "양피지 5/5 · 복구 회로와 설계대를 준비하고 있어요.\n" + ParameterHint;
                }

                return _goalText + "\n" + ParameterHint;
            }
        }

        public void Initialize(Camera camera, KdyLessonProgressController progress)
        {
            if (_initialized)
            {
                return;
            }

            if (progress == null || !HasRequiredReferences())
            {
                Debug.LogError("[Kdy Quest] Session progress, design bench and five lights/seals must be assigned.", this);
                return;
            }

            _progress = progress;
            _properties = new MaterialPropertyBlock();
            _goalText = $"흐린 입력 대비 RGB 평균제곱오차 {_progress.TargetImprovement:0.#}% 이상 감소";
            _benchHome = _designBench.localPosition;
            _benchLowered = _benchHome + Vector3.down * BenchLowering;
            _benchColliders = _designBench.GetComponentsInChildren<Collider>(true);
            _benchColliderStates = new bool[_benchColliders.Length];
            for (int i = 0; i < _benchColliders.Length; i++)
            {
                _benchColliderStates[i] = _benchColliders[i].enabled;
                _benchColliders[i].enabled = false;
            }

            _designBench.localPosition = _benchLowered;
            _initialized = true;
            RefreshSeals();
            SetAllLightsOff();
            if (HasAllScrolls())
            {
                _reveal = StartCoroutine(RevealDesignBench());
            }
            else
            {
                SetStatus($"양피지 {_progress.ScrollCount}/5 · 먼저 CNN실에서 다섯 장을 모으세요.");
                Changed?.Invoke();
            }
        }

        private IEnumerator RevealDesignBench()
        {
            for (int i = 0; i < RequiredScrollCount; i++)
            {
                SetStatus($"양피지 5/5 · 복구 회로 연결 중 {i + 1}/5");
                Changed?.Invoke();
                float elapsed = 0.0f;
                while (elapsed < LightDuration)
                {
                    elapsed += Time.deltaTime;
                    SetLight(_machineLights[i], Mathf.Clamp01(elapsed / LightDuration));
                    yield return null;
                }

                SetLight(_machineLights[i], 1.0f);
            }

            SetStatus("양피지 5/5 · 모델 설계대가 열리고 있어요.");
            Changed?.Invoke();
            float benchElapsed = 0.0f;
            while (benchElapsed < BenchDuration)
            {
                benchElapsed += Time.deltaTime;
                float amount = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(benchElapsed / BenchDuration));
                _designBench.localPosition = Vector3.Lerp(_benchLowered, _benchHome, amount);
                yield return null;
            }

            _designBench.localPosition = _benchHome;
            for (int i = 0; i < _benchColliders.Length; i++)
            {
                _benchColliders[i].enabled = _benchColliderStates[i];
            }

            _readyToDesign = true;
            _reveal = null;
            SetStatus("복구 준비 완료 · ‘설계 시작’을 선택하세요.");
            Changed?.Invoke();
        }

        private void RefreshSeals()
        {
            for (int i = 0; i < RequiredScrollCount; i++)
            {
                bool collected = _progress.HasScroll(i);
                SetColor(_scrollSeals[i], collected ? CollectedSeal : MissingSeal, collected ? .4f : 0.0f);
            }
        }

        private void SetAllLightsOff()
        {
            foreach (Renderer light in _machineLights)
            {
                SetLight(light, 0.0f);
            }
        }

        private void SetLight(Renderer surface, float amount)
        {
            SetColor(surface, Color.Lerp(DarkLight, CyanLight, amount), amount * 1.8f);
        }

        private void SetColor(Renderer surface, Color color, float emission)
        {
            surface.GetPropertyBlock(_properties);
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            if (surface.sharedMaterial != null && surface.sharedMaterial.HasProperty("_EmissionColor"))
            {
                _properties.SetColor("_EmissionColor", color * emission);
            }

            surface.SetPropertyBlock(_properties);
        }

        private void SetStatus(string stage)
        {
            if (_status != null)
            {
                _status.text = stage + "\n목표: " + _goalText + "\n교육용 시뮬레이션";
            }
        }

        private bool HasAllScrolls()
        {
            if (_progress == null)
            {
                return false;
            }

            for (int i = 0; i < RequiredScrollCount; i++)
            {
                if (!_progress.HasScroll(i))
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasRequiredReferences()
        {
            if (_designBench == null || _machineLights.Length != RequiredScrollCount ||
                _scrollSeals.Length != RequiredScrollCount)
            {
                return false;
            }

            for (int i = 0; i < RequiredScrollCount; i++)
            {
                if (_machineLights[i] == null || _scrollSeals[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnDisable()
        {
            if (_reveal != null)
            {
                StopCoroutine(_reveal);
                _reveal = null;
            }
        }
    }
}

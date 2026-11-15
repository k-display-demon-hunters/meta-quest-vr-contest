using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModelDesignScene.ControlPanel
{
    public sealed class ModelRealityEntryGuideController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject _card;
        [SerializeField] private Text _title;
        [SerializeField] private Text _description;
        [SerializeField] private Text _instruction;
        [SerializeField] private GameObject _completionMark;
        [SerializeField] private GameObject _completionHint;
        [SerializeField] private RectTransform _arrow;
        [SerializeField] private GameObject _buttonOutline;
        [SerializeField] private Image[] _accents;
        [SerializeField, Min(0.1f)] private float _pulseDuration = 1.2f;

        private static readonly Color Amber = new Color32(255, 178, 74, 255);
        private static readonly Color HoverMint = new Color32(127, 245, 255, 255);
        private static readonly Color Muted = new Color32(170, 185, 192, 255);
        private static readonly Color CompleteGreen = new Color32(64, 214, 176, 255);
        private bool _visible;
        private bool _canEnter;
        private bool _hovered;
        private bool _completed;

        private void Update()
        {
            if (_visible && _canEnter && !_completed)
            {
                RefreshAccent();
            }
        }

        internal void Show(bool visible, bool canEnter, bool completed)
        {
            _card.SetActive(visible);
            _arrow.gameObject.SetActive(visible && canEnter && !completed);
            _buttonOutline.SetActive(visible && canEnter && !completed);
            _completionMark.SetActive(visible && completed);
            _completionHint.SetActive(visible && !completed);
            if (_visible == visible && _canEnter == canEnter && _completed == completed) { return; }
            _visible = visible;
            _canEnter = canEnter;
            _completed = completed;
            if (!visible || !canEnter) { _hovered = false; }
            RefreshInstruction();
            RefreshAccent();
        }

        private void RefreshInstruction()
        {
            _title.text = _completed ? "체험 완료" : "다음은 AR 체험입니다";
            _title.color = _completed ? CompleteGreen : Amber;
            _description.text = _completed ? "학습과 AR 룸 방문을\n모두 마쳤습니다."
                : "학습한 모델로 현실의\n숫자 0~3을 읽어 보세요.";
            _instruction.text = !_canEnter ? "잡고 있는 블록을 놓으면\nAR 룸에 입장할 수 있습니다."
                : _completed ? "위 버튼으로 AR 룸에\n다시 입장할 수 있습니다."
                : _hovered ? "오른손 Trigger를 눌러\nAR 룸에 입장해 주세요."
                : "오른손 레이로\n[AR 룸 입장]을 가리켜 주세요.";
            _instruction.color = !_canEnter ? Muted : _completed ? CompleteGreen : _hovered ? HoverMint : Amber;
        }

        private void RefreshAccent()
        {
            float pulse = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * Mathf.PI * 2f / _pulseDuration);
            Color color = _completed ? CompleteGreen : !_canEnter ? Muted : _hovered ? HoverMint : Amber;
            color.a = _canEnter && !_hovered && !_completed ? Mathf.Lerp(0.55f, 1f, pulse) : 1f;
            foreach (Image accent in _accents) { accent.color = color; }
            _arrow.localScale = Vector3.one * (_hovered ? 1.12f : 1f + 0.08f * pulse);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (!_visible || !_canEnter) { return; }
            _hovered = true;
            RefreshInstruction();
            RefreshAccent();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            RefreshInstruction();
            RefreshAccent();
        }

        private void OnDisable()
        {
            Show(false, false, false);
        }
    }
}

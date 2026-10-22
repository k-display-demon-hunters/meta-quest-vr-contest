using UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryIntro
{
    public sealed class FactoryIntroPresentationController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _instruction;
        [SerializeField] private Text _continueLabel;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Image _background;

        private CanvasGroup _canvasGroup;
        private Vector3 _titlePosition;
        private Vector3 _titleScale;
        private Vector2 _backgroundSize;
        private Vector2 _instructionPosition;
        private Vector2 _instructionSize;
        private Color _backgroundColor;
        private int _instructionFontSize;
        private int _captionIndex = -1;
        private bool _isCinematic;

        private void Awake()
        {
            _canvasGroup = _canvas.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
            }
            _titlePosition = _canvas.transform.localPosition;
            _titleScale = _canvas.transform.localScale;
            _backgroundSize = _background.rectTransform.sizeDelta;
            _backgroundColor = _background.color;
            _instructionPosition = _instruction.rectTransform.anchoredPosition;
            _instructionSize = _instruction.rectTransform.sizeDelta;
            _instructionFontSize = _instruction.fontSize;
        }

        public void BindPlayer(XROrigin rig)
        {
            _canvas.worldCamera = rig.Camera;
            SceneUIStyle.ApplyButtons(_canvas.transform);
        }

        public void Show(string heading, string instruction, string button, bool canContinue)
        {
            RestoreTitleLayout();
            _heading.text = heading;
            _instruction.text = instruction;
            _continueLabel.text = button;
            _continueButton.gameObject.SetActive(!string.IsNullOrEmpty(button));
            _continueButton.interactable = canContinue;
        }

        public void BeginCinematic()
        {
            _isCinematic = true;
            _captionIndex = -1;
            _continueButton.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        public void ShowCinematicCaption(float elapsed)
        {
            if (elapsed < 0.5f)
            {
                _canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / 0.5f);
                return;
            }
            if (_heading.gameObject.activeSelf)
            {
                _heading.gameObject.SetActive(false);
                _continueButton.gameObject.SetActive(false);
                _canvas.transform.localPosition = new Vector3(0f, 0.65f, 3.8f);
                _canvas.transform.localScale = Vector3.one * 0.0025f;
                _background.rectTransform.sizeDelta = new Vector2(1260f, 126f);
                _background.color = new Color(0.025f, 0.04f, 0.065f, 0.78f);
                _instruction.rectTransform.anchoredPosition = Vector2.zero;
                _instruction.rectTransform.sizeDelta = new Vector2(1200f, 108f);
                _instruction.fontSize = 34;
                _instruction.color = Color.white;
            }

            int index;
            float start;
            float end;
            string caption;
            if (elapsed < 4f)
            {
                index = 0;
                start = 0.9f;
                end = 4f;
                caption = "마을의 선명한 사진과 영상은 이 공장에서 만들어집니다.";
            }
            else if (elapsed < 10.5f)
            {
                // Leave the arrival and impact unobstructed by explanatory text.
                index = 1;
                start = 4f;
                end = 4f;
                caption = "";
            }
            else if (elapsed < 14.2f)
            {
                index = 2;
                start = 10.5f;
                end = 14.2f;
                caption = "공장이 멈추자, 마을의 모든 화면이 흐려졌습니다.";
            }
            else
            {
                index = 3;
                start = 14.5f;
                end = 19f;
                caption = "헌터스, 공장을 복구해 선명한 세상을 되찾아 주세요.";
            }
            if (_captionIndex != index)
            {
                _captionIndex = index;
                _instruction.text = caption;
            }
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, start + 0.4f, elapsed));
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(end - 0.4f, end, elapsed));
            _canvasGroup.alpha = string.IsNullOrEmpty(caption) ? 0f : fadeIn * fadeOut;
        }

        private void RestoreTitleLayout()
        {
            if (_isCinematic)
            {
                _canvas.transform.localPosition = _titlePosition;
                _canvas.transform.localScale = _titleScale;
                _background.rectTransform.sizeDelta = _backgroundSize;
                _background.color = _backgroundColor;
                _instruction.rectTransform.anchoredPosition = _instructionPosition;
                _instruction.rectTransform.sizeDelta = _instructionSize;
                _instruction.fontSize = _instructionFontSize;
                _instruction.color = SceneUIStyle.Muted;
                _isCinematic = false;
            }
            _heading.gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public void ReleasePlayer()
        {
            if (_canvas != null)
            {
                _canvas.worldCamera = null;
            }
        }

        private void OnDisable()
        {
            ReleasePlayer();
        }
    }
}

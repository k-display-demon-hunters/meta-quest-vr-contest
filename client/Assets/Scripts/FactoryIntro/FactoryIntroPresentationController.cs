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

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = _canvas.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void BindPlayer(XROrigin rig)
        {
            _canvas.worldCamera = rig.Camera;
            SceneUIStyle.ApplyButtons(_canvas.transform);
        }

        public void Show(string heading, string instruction, string button, bool canContinue)
        {
            _heading.text = heading;
            _instruction.text = instruction;
            _continueLabel.text = button;
            _continueButton.gameObject.SetActive(!string.IsNullOrEmpty(button));
            _continueButton.interactable = canContinue;
            _canvas.enabled = true;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public void BeginCinematic()
        {
            // Hide the whole guide immediately, including its invisible raycast targets.
            _continueButton.interactable = false;
            _continueButton.gameObject.SetActive(false);
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _canvasGroup.alpha = 0f;
            _canvas.enabled = false;
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

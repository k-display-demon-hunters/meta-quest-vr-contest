using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    [DisallowMultipleComponent]
    public sealed class WorldUIPopupController : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Text _body;
        [SerializeField] private Button _primaryButton;
        [SerializeField] private Text _primaryLabel;
        [SerializeField] private Button _secondaryButton;
        [SerializeField] private Text _secondaryLabel;

        [Header("Actions")]
        [SerializeField] private UnityEvent _onConfirm = new UnityEvent();
        [SerializeField] private UnityEvent _onCancel = new UnityEvent();
        private int _openedFrame;

        private void OnEnable()
        {
            _openedFrame = Time.frameCount;
            _primaryButton.onClick.AddListener(HandlePrimary);
            _secondaryButton.onClick.AddListener(HandleSecondary);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        public void SetText(string title, string body, string primaryLabel, string secondaryLabel)
        {
            _title.text = title;
            _body.text = body;
            _primaryLabel.text = primaryLabel;
            _secondaryLabel.text = secondaryLabel;
        }

        private void HandlePrimary()
        {
            if (!isActiveAndEnabled || Time.frameCount == _openedFrame) { return; }
            Hide();
            _onConfirm.Invoke();
        }

        private void HandleSecondary()
        {
            if (!isActiveAndEnabled || !_secondaryButton.gameObject.activeSelf || Time.frameCount == _openedFrame) { return; }
            Hide();
            _onCancel.Invoke();
        }

        private void OnDisable()
        {
            _primaryButton.onClick.RemoveListener(HandlePrimary);
            _secondaryButton.onClick.RemoveListener(HandleSecondary);
        }
    }
}

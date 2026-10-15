using System;
using UnityEngine;

namespace ModelDesignScene
{
    public sealed class ModelSettingsController : MonoBehaviour
    {
        [SerializeField] private BlockSettingsPopup _popupPrefab;

        private BlockSettingsPopup _popup;
        private ModelBlockController _settingsBlock;

        public event Action<ModelBlockController> OnDeleteRequested;

        private void Awake()
        {
            _popup = Instantiate(_popupPrefab, transform);
            _popup.Hide();
            _popup.OnDeleteRequested += HandleDeleteRequested;
        }

        public void Open(SlotSettingsController control)
        {
            if (!isActiveAndEnabled || control.Block == null)
            {
                return;
            }
            _settingsBlock = control.Block;
            Transform anchor = control.PopupAnchor;
            // The device owns the popup pose. Head position and viewing direction are irrelevant.
            _popup.transform.SetParent(anchor, false);
            _popup.Show(_settingsBlock.Layer, anchor.position, anchor.rotation);
        }

        public void Close()
        {
            _settingsBlock = null;
            if (_popup != null)
            {
                _popup.Hide();
            }
        }

        private void HandleDeleteRequested()
        {
            if (isActiveAndEnabled && _settingsBlock != null && !_settingsBlock.IsFixed)
            {
                OnDeleteRequested?.Invoke(_settingsBlock);
            }
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            if (_popup != null)
            {
                _popup.OnDeleteRequested -= HandleDeleteRequested;
                // The popup may be parented to a slot outside this controller's hierarchy.
                Destroy(_popup.gameObject);
            }
        }
    }
}

using UnityEngine;

namespace KdyLearning
{
    public enum KdyNetworkPartKind
    {
        Layer,
        Kernel3,
        Kernel9,
        Relu,
        Sigmoid
    }

    public sealed class KdyLayerBlockController : MonoBehaviour
    {
        [SerializeField] private KdyNetworkPartKind _kind;
        [SerializeField] private KdyLessonGrabController _grab;
        [SerializeField] private Transform[] _featurePanels = new Transform[32];
        private KdyNetworkDesignerController _designer;
        private Transform _homeParent;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Vector3 _homeScale;
        private Rigidbody _body;
        private bool _initialized;

        public KdyNetworkPartKind Kind => _kind;
        public bool IsGrabbed => _grab != null && _grab.IsGrabbed;

        public void Initialize(KdyNetworkDesignerController designer)
        {
            _designer = designer;
            if (_initialized)
            {
                return;
            }
            if (_grab == null)
            {
                _grab = GetComponent<KdyLessonGrabController>();
            }
            if (_grab == null)
            {
                Debug.LogError("[KdyLearning] A network part needs its own lesson grab controller.", this);
                return;
            }

            _homeParent = transform.parent;
            _homePosition = transform.localPosition;
            _homeRotation = transform.localRotation;
            _homeScale = transform.localScale;
            _body = GetComponent<Rigidbody>();
            _grab.GrabbedChanged += HandleGrabbed;
            _initialized = true;
        }

        public void SetFeatureMaps(int featureMaps)
        {
            if (_kind != KdyNetworkPartKind.Layer)
            {
                return;
            }
            int count = featureMaps >= 32 ? 32 : 4;
            for (int i = 0; i < _featurePanels.Length; i++)
            {
                if (_featurePanels[i] != null)
                {
                    _featurePanels[i].gameObject.SetActive(i < count);
                }
            }
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (_grab != null)
            {
                _grab.SetInteractionEnabled(enabled);
            }
        }

        public void SnapTo(Transform socket)
        {
            if (!_initialized || IsGrabbed || socket == null)
            {
                return;
            }
            transform.SetParent(socket, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = _homeScale;
            SynchronizeBody();
        }

        public void ReturnHome()
        {
            if (!_initialized || IsGrabbed)
            {
                return;
            }
            transform.SetParent(_homeParent, false);
            transform.SetLocalPositionAndRotation(_homePosition, _homeRotation);
            transform.localScale = _homeScale;
            SynchronizeBody();
        }

        private void SynchronizeBody()
        {
            if (_body != null)
            {
                _body.position = transform.position;
                _body.rotation = transform.rotation;
            }
        }

        private void HandleGrabbed(bool grabbed)
        {
            _designer?.HandlePartGrabbed(this, grabbed);
        }

        private void OnDestroy()
        {
            if (_grab != null)
            {
                _grab.GrabbedChanged -= HandleGrabbed;
            }
        }
    }
}

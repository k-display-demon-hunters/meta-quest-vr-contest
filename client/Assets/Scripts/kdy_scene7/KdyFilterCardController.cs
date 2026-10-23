using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    [RequireComponent(typeof(KdyLessonGrabController))]
    public sealed class KdyFilterCardController : MonoBehaviour
    {
        [SerializeField, Range(0, 2)] private int _filterIndex;
        [SerializeField] private Transform _slot;
        [SerializeField] private Renderer _previewSurface;
        [SerializeField] private Text _weights;
        [SerializeField] private KdyLessonGrabController _grab;
        [SerializeField] private float _dockDistance = .65f;

        private Rigidbody _body;
        private Transform _homeParent;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Vector3 _homeWorldPosition;
        private Quaternion _homeWorldRotation;
        private Material _originalMaterial;
        private Material _previewMaterial;
        private Texture2D _previewTexture;
        private bool _initialized;
        private bool _interactionEnabled = true;
        private bool _suppressRelease;

        public int FilterIndex => _filterIndex;
        public bool IsDocked { get; private set; }
        public event Action<int> Docked;

        private void Awake()
        {
            if (_grab == null)
            {
                _grab = GetComponent<KdyLessonGrabController>();
            }
            _body = GetComponent<Rigidbody>();
            _homeParent = transform.parent;
            _homePosition = transform.localPosition;
            _homeRotation = transform.localRotation;
            _homeWorldPosition = transform.position;
            _homeWorldRotation = transform.rotation;
        }

        private void OnEnable()
        {
            if (_grab != null)
            {
                _grab.GrabbedChanged += HandleGrabbedChanged;
            }
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            if (_slot == null || _grab == null || _previewSurface == null ||
                _previewSurface.sharedMaterial == null || _weights == null)
            {
                Debug.LogError("[KdyLearning] A filter card needs its slot, grab controller, preview material and weight text.", this);
                return;
            }
            _filterIndex = Mathf.Clamp(_filterIndex, 0, 2);
            _originalMaterial = _previewSurface.sharedMaterial;
            _previewTexture = CreatePreview();
            _previewMaterial = new Material(_originalMaterial);
            _previewMaterial.name = "Kdy Filter Card Preview " + _filterIndex;
            _previewMaterial.mainTexture = _previewTexture;
            SetWhiteTint(_previewMaterial);
            _previewSurface.sharedMaterial = _previewMaterial;
            ShowWeights();
            _initialized = true;
        }

        public bool TryDock()
        {
            if (!_initialized || !isActiveAndEnabled || !_interactionEnabled ||
                _suppressRelease || IsDocked || _grab.IsGrabbed ||
                Vector3.Distance(transform.position, _slot.position) > _dockDistance)
            {
                return false;
            }
            _suppressRelease = true;
            _grab.SetInteractionEnabled(false);
            IsDocked = true;
            SetPose(_slot.position, _slot.rotation);
            _suppressRelease = false;
            Docked?.Invoke(_filterIndex);
            return true;
        }

        public void ResetPose()
        {
            if (!_initialized)
            {
                return;
            }
            _suppressRelease = true;
            _grab.SetInteractionEnabled(false);
            IsDocked = false;
            // XRI detaches a grabbed object from its scene parent, so reset from the captured parent frame.
            Vector3 homePosition = _homeParent != null ? _homeParent.TransformPoint(_homePosition) : _homeWorldPosition;
            Quaternion homeRotation = _homeParent != null ? _homeParent.rotation * _homeRotation : _homeWorldRotation;
            SetPose(homePosition, homeRotation);
            _suppressRelease = false;
            SetInteractionEnabled(true);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
            _suppressRelease = true;
            _grab.SetInteractionEnabled(enabled && !IsDocked);
            _suppressRelease = false;
        }

        private void HandleGrabbedChanged(bool isGrabbed)
        {
            if (!_initialized || !isActiveAndEnabled || isGrabbed ||
                _suppressRelease || !_interactionEnabled || IsDocked)
            {
                return;
            }
            if (!TryDock())
            {
                ResetPose();
            }
        }

        private void SetPose(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            if (_body != null)
            {
                _body.position = position;
                _body.rotation = rotation;
            }
        }

        private Texture2D CreatePreview()
        {
            int width = KdySignExampleData.Width;
            int height = KdySignExampleData.Height;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Kdy Filter Preview " + _filterIndex;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            float maximum = 0f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    maximum = Mathf.Max(maximum, Mathf.Abs(KdySignExampleData.Filter(_filterIndex, x, y)));
                }
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float response = KdySignExampleData.Filter(_filterIndex, x, y);
                    texture.SetPixel(x, height - 1 - y, KdyFilterComparisonController.ResponseColor(response, maximum));
                }
            }
            texture.Apply(false, false);
            return texture;
        }

        private void ShowWeights()
        {
            StringBuilder matrix = new StringBuilder();
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    if (x > 0)
                    {
                        matrix.Append("   ");
                    }
                    matrix.Append(KdySignExampleData.Weight(_filterIndex, x, y));
                }
                matrix.Append('\n');
            }
            matrix.Append("\nR×0.2126 + G×0.7152 + B×0.0722");
            _weights.text = matrix.ToString();
        }

        private void SetWhiteTint(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }
        }

        private void OnDisable()
        {
            if (_grab != null)
            {
                _grab.GrabbedChanged -= HandleGrabbedChanged;
            }
        }

        private void OnDestroy()
        {
            if (_previewSurface != null && _previewMaterial != null &&
                _previewSurface.sharedMaterial == _previewMaterial)
            {
                _previewSurface.sharedMaterial = _originalMaterial;
            }
            if (_previewMaterial != null)
            {
                Destroy(_previewMaterial);
            }
            if (_previewTexture != null)
            {
                Destroy(_previewTexture);
            }
        }
    }
}

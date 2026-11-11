using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CNNConvolution
{
    // Workbench implementation: physical input and presentation, without lesson-stage state.
    internal sealed class WorkbenchKernelController : IDisposable
    {
        private readonly Transform _root;
        private readonly Transform _kernel;
        private readonly Text _kernelTitle;
        private readonly Text[] _weights = new Text[9];
        private readonly Transform[] _slices = new Transform[3];
        private readonly Text[,] _channelWeights = new Text[3, 9];
        private readonly MeshRenderer[] _gripAccents;
        private readonly Transform _home;
        private readonly Transform _target;
        private readonly WorkbenchPrimitiveFactory _factory;
        private readonly Material _placementPreviewMaterial;
        private readonly XRGrabInteractable _grab;
        private readonly Rigidbody _body;
        private readonly MaterialPropertyBlock _handleProperties = new MaterialPropertyBlock();
        private bool _cancellingSelection;
        private bool _constrainedMove;
        private Transform _grabAttach;
        private Vector3 _handAtGrab;
        private Vector3 _kernelAtGrab;

        public bool IsSelected => _grab.isSelected;
        public bool IsHovered => _grab.isHovered;
        public event Action Grabbed;
        public event Action Released;

        public WorkbenchKernelController(WorkbenchPrimitiveFactory factory, Transform root, Transform home, Transform target,
            Material placementPreviewMaterial)
        {
            _root = root;
            _factory = factory;
            _home = home;
            _target = target;
            _placementPreviewMaterial = placementPreviewMaterial;
            _kernel = factory.Root("Fixed weight slice");
            _gripAccents = FilterFrame(factory, _kernel);
            _slices[0] = _kernel;
            _kernelTitle = factory.Caption(_kernel, "필터 하나 · RGB 세 층", new Vector3(0, -0.36f, 0), 0.95f);
            _kernelTitle.fontSize = 34;
            for (int i = 0; i < 9; i++)
            {
                Transform tile = CreateWeightTile(_kernel, i);
                _weights[i] = factory.Caption(tile, "", new Vector3(0, 0, -0.018f), 0.045f);
                _weights[i].fontSize = 32;
                _channelWeights[0, i] = _weights[i];
            }
            for (int channel = 1; channel < 3; channel++)
            {
                Transform slice = CreateLinkedSlice(_kernel, channel);
                _slices[channel] = slice;
                for (int index = 0; index < 9; index++)
                {
                    Transform tile = CreateWeightTile(slice, index);
                    _channelWeights[channel, index] = factory.Caption(tile, "", new Vector3(0, 0, -0.018f), 0.045f);
                    _channelWeights[channel, index].fontSize = 32;
                }
            }
            BoxCollider collider = _kernel.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.60f, 0.54f, 0.04f);
            _body = _kernel.gameObject.AddComponent<Rigidbody>();
            _body.useGravity = false;
            _body.isKinematic = true;
            _grab = _kernel.gameObject.AddComponent<XRGrabInteractable>();
            _grab.colliders.Clear();
            _grab.colliders.Add(collider);
            _grab.throwOnDetach = false;
            _grab.retainTransformParent = false;
            _grab.trackRotation = false;
            _grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            BoxCollider handle = _kernel.gameObject.AddComponent<BoxCollider>();
            handle.center = new Vector3(0, 0.32f, 0);
            handle.size = new Vector3(0.26f, 0.10f, 0.09f);
            _grab.colliders.Add(handle);
            _grab.selectEntered.AddListener(HandleGrabbed);
            _grab.selectExited.AddListener(HandleReleased);
        }

        public Pose Pose => new Pose(_kernel.position, _kernel.rotation);
        public Pose HomePose => new Pose(_home.position, _home.rotation);
        public Pose TargetPose => new Pose(_target.position, _target.rotation);
        public Vector3 CellPosition(int index) => _factory.LocalPoint(_weights[index].transform.position);
        public Vector3 LocalPosition => _factory.LocalPoint(_kernel.position);
        public Vector3 ChannelPosition(int channel) => _factory.LocalPoint(_slices[channel].position);

        public bool IsWithinTarget(float radius) =>
            _target.InverseTransformPoint(_kernel.position).sqrMagnitude <= radius * radius;

        public void SetTitleVisible(bool visible) => _kernelTitle.gameObject.SetActive(visible);

        public void SetWeights(ConvolutionCalculation calculation)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                for (int index = 0; index < 9; index++)
                {
                    _channelWeights[channel, index].text = calculation.Weight(index).ToString();
                }
            }
            ShowChannel(0);
            _kernel.gameObject.SetActive(true);
            _kernelTitle.text = "필터 하나 · RGB 세 층";
        }

        public void ShowChannel(int channel)
        {
            for (int other = 0; other < 3; other++)
            {
                for (int index = 0; index < 9; index++)
                {
                    _channelWeights[other, index].transform.parent.parent.gameObject.SetActive(other == channel);
                }
            }
        }

        public void ConstrainToSegment(Pose start, Pose target)
        {
            Vector3 segment = target.position - start.position;
            Vector3 requested = _kernelAtGrab + _grabAttach.position - _handAtGrab;
            float amount = Mathf.Clamp01(Vector3.Dot(requested - start.position, segment) / segment.sqrMagnitude);
            SetPose(new Pose(start.position + segment * amount, target.rotation));
        }

        public void SetConstrainedMove(bool constrained)
        {
            _constrainedMove = constrained;
            // During the one-cell exercise this controller owns position. XR still owns selection,
            // but its Dynamic/OnBeforeRender passes must not overwrite the constrained pose.
            _grab.trackPosition = !constrained;
        }

        public float DistanceTo(Pose target) => _root.InverseTransformVector(_kernel.position - target.position).magnitude;

        public void AttachHighlight(Transform highlight, int index)
        {
            highlight.SetParent(_kernel, false);
            highlight.localPosition = WorkbenchPrimitiveFactory.GridCellPosition(index, 3) + new Vector3(0.052f, 0.052f, -0.026f);
            highlight.gameObject.SetActive(true);
        }

        public void AttachGripArrow(Transform arrow)
        {
            arrow.SetParent(_kernel, false);
            arrow.localScale = Vector3.one * 0.45f;
            arrow.localPosition = new Vector3(0, 0.41f, -0.04f);
        }

        public Transform CreatePlacementPreview()
        {
            Transform preview = _factory.Root("Filter placement preview");
            FilterFrame(_factory, preview);
            for (int i = 0; i < _weights.Length; i++) { CreateWeightTile(preview, i); }
            for (int channel = 1; channel < 3; channel++) { CreateLinkedSlice(preview, channel); }
            foreach (MeshRenderer renderer in preview.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = _placementPreviewMaterial;
                renderer.SetPropertyBlock(null);
            }
            preview.gameObject.SetActive(false);
            return preview;
        }

        private Transform CreateLinkedSlice(Transform parent, int channel)
        {
            Transform slice = _factory.Root("RGB filter slice " + channel);
            slice.SetParent(parent, false);
            slice.localPosition = InputGridController.ChannelOffset(channel);
            _factory.Frame(slice, 0.57f, 0.51f, 0.026f, WorkbenchColors.Channel(channel), 0.012f);
            // One connected filter, with only the front frame carrying a grab handle.
            Vector3 offset = InputGridController.ChannelOffset(1);
            Transform link = _factory.Tile(parent,
                InputGridController.ChannelOffset(channel - 1) + offset * 0.5f + new Vector3(-0.285f, 0.255f, 0),
                new Vector3(0.009f, 0.009f, offset.magnitude), new Color(0.47f, 0.38f, 0.24f));
            link.localRotation = Quaternion.LookRotation(offset);
            return slice;
        }

        private Transform CreateWeightTile(Transform parent, int index)
        {
            Vector3 position = WorkbenchPrimitiveFactory.GridCellPosition(index, 3) + new Vector3(0.052f, 0.052f, 0);
            return _factory.Tile(parent, position, new Vector3(0.045f, 0.045f, 0.025f), new Color(0.11f, 0.15f, 0.17f));
        }

        private MeshRenderer[] FilterFrame(WorkbenchPrimitiveFactory factory, Transform parent)
        {
            Color metal = new Color(0.47f, 0.38f, 0.24f);
            // Leave the input numerals visible when the offset filter lies over them.
            factory.Frame(parent, 0.57f, 0.51f, 0.026f, metal, 0.012f);
            for (int side = -1; side <= 1; side += 2)
            {
                factory.Tile(parent, new Vector3(side * 0.075f, 0.282f, 0), new Vector3(0.018f, 0.048f, 0.035f), metal);
            }
            factory.Tile(parent, new Vector3(0, 0.32f, 0), new Vector3(0.22f, 0.05f, 0.05f), new Color(0.08f, 0.10f, 0.11f));
            MeshRenderer[] accents = new MeshRenderer[5];
            for (int i = -2; i <= 2; i++)
            {
                Transform groove = factory.Tile(parent, new Vector3(i * 0.035f, 0.32f, -0.028f), new Vector3(0.006f, 0.035f, 0.008f), metal);
                accents[i + 2] = groove.Find("Tile mesh").GetComponent<MeshRenderer>();
            }
            return accents;
        }

        public void RestoreParent()
        {
            // XR detaches the object while held. Keep its world scale when returning it.
            _kernel.SetParent(_root, true);
        }

        public void SetPose(Pose pose)
        {
            _kernel.SetPositionAndRotation(pose.position, pose.rotation);
            _body.position = pose.position;
            _body.rotation = pose.rotation;
        }

        public void SetGrabEnabled(bool enabled)
        {
            _grab.enabled = enabled;
            foreach (Collider collider in _grab.colliders) { collider.enabled = enabled; }
        }

        public void CancelGrab()
        {
            if (_grab == null) { return; }
            if (_grab.isSelected && _grab.interactionManager != null)
            {
                // Assistance and shutdown are not user releases, regardless of OnDisable order.
                _cancellingSelection = true;
                _grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grab);
                _cancellingSelection = false;
            }
            if (_grab.transform.parent != _root)
            {
                if (_root.gameObject.activeInHierarchy) { RestoreParent(); }
                else { _grab.gameObject.SetActive(false); }
            }
        }

        public void ReleaseInteractions()
        {
            CancelGrab();
            SetGrabEnabled(false);
        }

        public void Dispose()
        {
            // Destruction can follow child destruction; do not repaint the workbench here.
            CancelGrab();
            _grab.selectEntered.RemoveListener(HandleGrabbed);
            _grab.selectExited.RemoveListener(HandleReleased);
            if (!_grab.transform.IsChildOf(_root)) { UnityEngine.Object.Destroy(_grab.gameObject); }
        }

        public void TintHandle(Color color, float emission)
        {
            _handleProperties.SetColor("_BaseColor", color);
            _handleProperties.SetColor("_EmissionColor", color * emission);
            foreach (MeshRenderer groove in _gripAccents) { groove.SetPropertyBlock(_handleProperties); }
        }

        private void HandleGrabbed(SelectEnterEventArgs args)
        {
            if (_constrainedMove)
            {
                _grabAttach = args.interactorObject.GetAttachTransform(_grab);
                _handAtGrab = _grabAttach.position;
                _kernelAtGrab = _kernel.position;
            }
            Grabbed?.Invoke();
        }
        private void HandleReleased(SelectExitEventArgs args)
        {
            if (!_cancellingSelection) { Released?.Invoke(); }
        }
    }
}

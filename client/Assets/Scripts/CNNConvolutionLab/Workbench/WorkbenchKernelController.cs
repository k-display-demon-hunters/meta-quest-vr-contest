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
        private const float FrameWidth = 0.57f;
        private const float FrameHeight = 0.51f;
        private static readonly Vector3 LayerStep = new Vector3(-0.035f, 0.065f, 0.05f);
        private static readonly Vector3 CellSize = new Vector3(0.17f, 0.15f, 0.002f);
        // Weight labels sit in the upper right of each cell so the picture stays visible under the filter.
        private static readonly Vector3 WeightOffset = new Vector3(0.035f, 0.04f, 0f);

        private readonly Transform _root;
        // The filter rests in the input group so it is shown, hidden and moved together with the picture.
        private readonly Transform _dock;
        private readonly Transform _kernel;
        private readonly Transform _kernelTitlePlate;
        private readonly Text[] _weights = new Text[9];
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
        internal float PlacementGhostPeriod { get; }
        // The filter is built at a fixed size and scaled so its frame covers exactly 3×3 picture cells.
        internal Vector3 Scale { get; }
        public event Action Grabbed;
        public event Action Released;

        public WorkbenchKernelController(WorkbenchPrimitiveFactory factory, Transform root, Transform dock, Transform home, Transform target,
            float windowSize, Material placementPreviewMaterial, float placementGhostPeriod)
        {
            _root = root;
            _dock = dock;
            _factory = factory;
            _home = home;
            _target = target;
            _placementPreviewMaterial = placementPreviewMaterial;
            PlacementGhostPeriod = placementGhostPeriod;
            Scale = new Vector3(windowSize / FrameWidth, windowSize / FrameHeight, windowSize / FrameWidth);
            _kernel = factory.Root("Filter");
            _kernel.SetParent(dock, false);
            _kernel.localScale = Scale;
            _gripAccents = FilterFrame(factory, _kernel);
            _kernelTitlePlate = factory.FormulaPlate("Filter title", 0.62f);
            _kernelTitlePlate.SetParent(_home, false);
            _kernelTitlePlate.localPosition = new Vector3(0f, 0.27f, -0.025f);
            Text title = factory.Caption(_kernelTitlePlate, "필터 · 빨강 층 1/9", new Vector3(0, 0, -0.027f), 0.6f);
            title.fontSize = WorkbenchColors.HeadingFontSize;
            // Only the red layer carries weights; the green and blue layers behind it weigh nothing.
            for (int index = 0; index < _weights.Length; index++)
            {
                Transform tile = factory.Tile(_kernel, WorkbenchPrimitiveFactory.GridCellPosition(index, 3) + WeightOffset,
                    new Vector3(0.09f, 0.06f, 0.025f), WorkbenchColors.PanelFace);
                _weights[index] = factory.Caption(tile, "", new Vector3(0, 0, -0.018f), 0.09f);
                _weights[index].fontSize = 40;
            }
            for (int channel = 1; channel < 3; channel++) { CreateLinkedSlice(channel); }

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
        public Vector3 CellPosition(int index) => _factory.LocalPoint(
            _kernel.TransformPoint(WorkbenchPrimitiveFactory.GridCellPosition(index, 3) + Vector3.back * 0.03f));

        // While a constrained move is held: where the hand asks the filter to be. The caller decides
        // which position the filter actually takes.
        public Vector3 RequestedPosition => _kernelAtGrab + _grabAttach.position - _handAtGrab;

        // Depth is ignored: a filter held in front of the target counts as being over it.
        public bool IsWithinTarget(float radius)
        {
            Vector3 offset = _target.InverseTransformPoint(_kernel.position);
            offset.z = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        public void SetTarget(Pose pose) => _target.SetPositionAndRotation(pose.position, pose.rotation);

        public void SetTitleVisible(bool visible) => _kernelTitlePlate.gameObject.SetActive(visible);

        public void SetWeights(PictureConvolution convolution)
        {
            foreach (Text weight in _weights) { weight.text = convolution.WeightLabel(0); }
        }

        public void ResetToHome()
        {
            // A cancelled grab can leave the filter detached and switched off.
            _kernel.gameObject.SetActive(true);
            RestoreParent();
            SetPose(HomePose);
        }

        public void SetConstrainedMove(bool constrained)
        {
            _constrainedMove = constrained;
            // During a constrained move the lesson owns the position. XR still owns selection,
            // but its Dynamic/OnBeforeRender passes must not overwrite the constrained pose.
            _grab.trackPosition = !constrained;
        }

        public void AttachHighlight(Transform highlight, int index)
        {
            highlight.SetParent(_kernel, false);
            highlight.localPosition = WorkbenchPrimitiveFactory.GridCellPosition(index, 3) + Vector3.back * 0.026f;
            highlight.gameObject.SetActive(true);
        }

        public void AttachGripArrow(Transform arrow)
        {
            arrow.SetParent(_kernel, false);
            arrow.localPosition = new Vector3(0, 0.50f, -0.04f);
        }

        internal Transform CreatePlacementGhost()
        {
            Transform ghost = _factory.Root("Filter placement ghost");
            ghost.SetParent(_target, false);
            ghost.localScale = Scale;
            // Keep the overlay behind the filter once it is placed.
            ghost.localPosition = Vector3.forward * 0.025f;
            for (int index = 0; index < _weights.Length; index++)
            {
                Transform tile = _factory.Tile(ghost, WorkbenchPrimitiveFactory.GridCellPosition(index, 3),
                    CellSize, WorkbenchColors.PlacementMint);
                MeshRenderer renderer = tile.GetComponentInChildren<MeshRenderer>();
                renderer.sharedMaterial = _placementPreviewMaterial;
                renderer.SetPropertyBlock(null);
            }
            ghost.gameObject.SetActive(false);
            return ghost;
        }

        // The green and blue layers are frames behind the red one, joined to it at the top corners.
        private void CreateLinkedSlice(int channel)
        {
            Transform slice = _factory.Root("RGB filter slice " + channel);
            slice.SetParent(_kernel, false);
            slice.localPosition = LayerStep * channel;
            _factory.Frame(slice, FrameWidth, FrameHeight, 0.026f, WorkbenchColors.Channel(channel), 0.012f);
            Quaternion linkRotation = Quaternion.LookRotation(LayerStep);
            Vector3 linkSize = new Vector3(0.009f, 0.009f, LayerStep.magnitude);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 frameCorner = new Vector3(side * FrameWidth * 0.5f, FrameHeight * 0.5f, 0f);
                Transform mesh = _factory.Tile(_kernel, Vector3.zero, Vector3.one, WorkbenchColors.PanelFrame).GetChild(0);
                mesh.SetLocalPositionAndRotation(frameCorner + LayerStep * (channel - 0.5f), linkRotation);
                mesh.localScale = linkSize;
            }
        }

        private MeshRenderer[] FilterFrame(WorkbenchPrimitiveFactory factory, Transform parent)
        {
            Color metal = WorkbenchColors.PanelFrame;
            // Leave the picture visible when the filter lies over it.
            factory.Frame(parent, FrameWidth, FrameHeight, 0.026f, metal, 0.012f);
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
            _kernel.SetParent(_dock, true);
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
            if (_grab.transform.parent != _dock)
            {
                if (_dock.gameObject.activeInHierarchy) { RestoreParent(); }
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

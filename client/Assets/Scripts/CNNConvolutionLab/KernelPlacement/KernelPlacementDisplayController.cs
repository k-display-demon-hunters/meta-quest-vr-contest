using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    internal sealed class KernelPlacementDisplayController
    {
        private readonly WorkbenchKernelController _kernel;
        private readonly Text _hint;
        private readonly Transform _gripArrow;
        private readonly Transform _dropTarget;
        private readonly Transform _placementDashed;
        private readonly Transform _placementSolid;
        private readonly Transform _placementCheck;

        public KernelPlacementDisplayController(WorkbenchPrimitiveFactory factory, WorkbenchKernelController kernel, Transform target, Text hint)
        {
            _kernel = kernel;
            _hint = hint;
            _gripArrow = factory.Root("Grip arrow");
            kernel.AttachGripArrow(_gripArrow);
            factory.Tile(_gripArrow, Vector3.zero, new Vector3(0.035f, 0.18f, 0.025f), Color.cyan);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform wing = factory.Tile(_gripArrow, new Vector3(side * 0.045f, -0.07f, 0), new Vector3(0.035f, 0.13f, 0.025f), Color.cyan);
                wing.localRotation = Quaternion.Euler(0, 0, -side * 45);
            }
            _gripArrow.gameObject.SetActive(false);
            _dropTarget = factory.Root("Weight placement outline");
            _placementDashed = factory.Root("Waiting placement silhouette");
            _placementDashed.SetParent(_dropTarget, false);
            _placementSolid = factory.Root("Ready placement silhouette");
            _placementSolid.SetParent(_dropTarget, false);
            _placementCheck = factory.Root("Placement complete check");
            _placementCheck.SetParent(_dropTarget, false);
            Color waiting = new Color(0.35f, 0.78f, 0.85f);
            Color ready = new Color(1f, 0.86f, 0.57f);
            // A fixed silhouette shows the actual destination; it never pulses in size.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int segment = -2; segment <= 2; segment++)
                {
                    factory.Tile(_placementDashed, new Vector3(segment * 0.11f, side * 0.27f, -0.035f),
                        new Vector3(0.060f, 0.008f, 0.010f), waiting);
                    factory.Tile(_placementDashed, new Vector3(side * 0.30f, segment * 0.10f, -0.035f),
                        new Vector3(0.008f, 0.055f, 0.010f), waiting);
                }
            }
            factory.Frame(_placementSolid, 0.60f, 0.54f, 0.014f, ready, 0.008f);
            _placementSolid.localPosition = new Vector3(0, 0, -0.035f);
            Transform shortStroke = factory.Tile(_placementCheck, new Vector3(0.40f, -0.28f, -0.05f),
                new Vector3(0.022f, 0.075f, 0.015f), ready);
            shortStroke.localRotation = Quaternion.Euler(0, 0, 40);
            Transform longStroke = factory.Tile(_placementCheck, new Vector3(0.455f, -0.25f, -0.05f),
                new Vector3(0.022f, 0.15f, 0.015f), ready);
            longStroke.localRotation = Quaternion.Euler(0, 0, -40);
            _placementSolid.gameObject.SetActive(false);
            _placementCheck.gameObject.SetActive(false);
            _dropTarget.SetPositionAndRotation(target.position, target.rotation);
            _dropTarget.gameObject.SetActive(false);
        }

        public void ShowPlacement(string message, bool ready, bool completed, bool showGrip, bool emphasize)
        {
            _dropTarget.gameObject.SetActive(true);
            _placementDashed.gameObject.SetActive(!ready);
            _placementSolid.gameObject.SetActive(ready);
            _placementCheck.gameObject.SetActive(completed);
            _gripArrow.gameObject.SetActive(showGrip);
            _kernel.SetTitleVisible(showGrip);
            _hint.gameObject.SetActive(true);
            _hint.text = message;
            Color accent = ready ? new Color(1f, 0.86f, 0.57f) : new Color(0.35f, 0.78f, 0.85f);
            _hint.color = ready ? accent : Color.white;
            _kernel.TintHandle(accent, emphasize ? 0.35f : 0.12f);
        }

        public void HidePlacement()
        {
            _dropTarget.gameObject.SetActive(false);
            _gripArrow.gameObject.SetActive(false);
            _hint.gameObject.SetActive(false);
            _kernel.TintHandle(new Color(0.47f, 0.38f, 0.24f), 0);
        }
    }
}

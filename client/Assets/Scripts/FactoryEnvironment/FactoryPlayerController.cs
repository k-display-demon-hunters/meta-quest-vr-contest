using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FactoryEnvironment
{
    public sealed class FactoryPlayerController : MonoBehaviour
    {
        private FactoryLayoutData _layoutData;
        private XRBaseInteractor[] _interactors;

        public XROrigin Rig { get; private set; }
        public Transform CameraTransform => Rig.Camera.transform;
        private void Awake()
        {
            _layoutData = GetComponent<FactoryLayoutData>();
        }

        public void PlaceAtEntrance(XROrigin rig)
        {
            Transform entrance = _layoutData.Entrance;
            Rig = rig;
            _interactors = Rig.GetComponentsInChildren<XRBaseInteractor>(true);
            float height = rig.Camera.transform.position.y - rig.Origin.transform.position.y;
#if UNITY_EDITOR
            // Keep the editor simulator at eye level without changing real headset tracking.
            if (!UnityEngine.XR.XRSettings.isDeviceActive
                && FindFirstObjectByType<XR.XREditorSimulator>() != null && height < 0.5f)
            {
                Vector3 offset = rig.CameraFloorOffsetObject.transform.localPosition;
                offset.y += rig.CameraYOffset - height;
                rig.CameraFloorOffsetObject.transform.localPosition = offset;
                height = rig.CameraYOffset;
            }
#endif
            rig.MatchOriginUpCameraForward(Vector3.up, entrance.forward);
            rig.MoveCameraToWorldLocation(entrance.position + Vector3.up * height);
        }

        public void MoveTo(Transform arrival)
        {
            float height = CameraTransform.position.y - Rig.Origin.transform.position.y;
            Rig.MatchOriginUpCameraForward(Vector3.up, arrival.forward);
            Rig.MoveCameraToWorldLocation(arrival.position + Vector3.up * height);
        }

        public void ReleaseSceneObjects(Scene scene)
        {
            foreach (XRBaseInteractor interactor in _interactors)
            {
                if (interactor == null)
                {
                    continue;
                }
                for (int i = interactor.interactablesSelected.Count - 1; i >= 0; i--)
                {
                    IXRSelectInteractable selected = interactor.interactablesSelected[i];
                    if (selected.transform.gameObject.scene == scene)
                    {
                        interactor.interactionManager.SelectExit(interactor, selected);
                    }
                }
            }
        }

        public void PrepareRayInput()
        {
            // Keep the UI input module in the factory scene when the guide scene is unloaded.
            Rig.GetComponent<XR.XRPlayerInputController>().EnsureRayReady();
        }
    }
}

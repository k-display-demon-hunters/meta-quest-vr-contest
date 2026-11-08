using System;
using System.Collections;
using Learning;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FactoryEnvironment
{
    public sealed class FactoryPlayerController : MonoBehaviour
    {
        [Serializable]
        private sealed class StageArrival
        {
            public LearningStageId Stage;
            public Transform Arrival;
        }

        [SerializeField] private FactorySceneLoadController _sceneLoader;
        [SerializeField] private FactoryAreaController _areaController;
        [SerializeField] private StageArrival[] _stageArrivals;
        private FactoryLayoutData _layoutData;
        private XRBaseInteractor[] _interactors;

        public XROrigin Rig { get; private set; }
        public Transform CameraTransform => Rig.Camera.transform;
        private void Awake()
        {
            _layoutData = GetComponent<FactoryLayoutData>();
            Rig = FindFirstObjectByType<XROrigin>();
            _interactors = Rig.GetComponentsInChildren<XRBaseInteractor>(true);
        }

        private void OnEnable()
        {
            _sceneLoader.ContentUnloading += HandleContentUnloading;
            _areaController.AreaChanged += HandleAreaChanged;
        }

        private void OnDisable()
        {
            _sceneLoader.ContentUnloading -= HandleContentUnloading;
            _areaController.AreaChanged -= HandleAreaChanged;
        }

        private void Start()
        {
            Transform entrance = _layoutData.Entrance;
            float height = CameraTransform.position.y - Rig.Origin.transform.position.y;
#if UNITY_EDITOR
            // Keep the editor simulator at eye level without changing real headset tracking.
            if (!UnityEngine.XR.XRSettings.isDeviceActive
                && FindFirstObjectByType<XR.XREditorSimulator>() != null && height < 0.5f)
            {
                Vector3 offset = Rig.CameraFloorOffsetObject.transform.localPosition;
                offset.y += Rig.CameraYOffset - height;
                Rig.CameraFloorOffsetObject.transform.localPosition = offset;
                height = Rig.CameraYOffset;
            }
#endif
            Rig.MatchOriginUpCameraForward(Vector3.up, entrance.forward);
            Rig.MoveCameraToWorldLocation(entrance.position + Vector3.up * height);
            // Use the persistent rig's UI input module across content scene loads.
            Rig.GetComponent<XR.XRPlayerInputController>().EnsureRayReady();
        }

        public void MoveToStage(LearningStageId stage)
        {
            foreach (StageArrival arrival in _stageArrivals)
            {
                if (arrival.Stage != stage) { continue; }
                MoveTo(arrival.Arrival);
                return;
            }
            throw new InvalidOperationException("No arrival for " + stage);
        }

        private void MoveTo(Transform arrival)
        {
            float height = CameraTransform.position.y - Rig.Origin.transform.position.y;
            Rig.MatchOriginUpCameraForward(Vector3.up, arrival.forward);
            Rig.MoveCameraToWorldLocation(arrival.position + Vector3.up * height);
        }

        private void HandleAreaChanged(FactoryLayoutData.Area area)
        {
            if (area.Stage != LearningStageId.Exit) { return; }
            foreach (FactoryLayoutData.StageSpace space in _layoutData.StageSpaces)
            {
                if (_sceneLoader.IsLoaded(space.Id))
                    ReleaseSceneObjects(SceneManager.GetSceneByName(LearningStageCatalog.GetSceneName(space.Id)));
            }
        }

        private IEnumerator HandleContentUnloading(LearningStageId stage, Scene scene)
        {
            ReleaseSceneObjects(scene);
            yield break;
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

    }
}

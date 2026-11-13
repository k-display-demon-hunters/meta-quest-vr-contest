using System;
using System.Collections;
using Learning;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
#if UNITY_EDITOR
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
#endif

namespace FactoryEnvironment
{
    public sealed class FactoryPlayerController : MonoBehaviour
    {
        private const float FloorDescentSpeed = 3f;
        private const float FloorContactTolerance = 0.06f;

        [Serializable]
        private sealed class StageArrival
        {
            public LearningStageId Stage;
            public Transform Arrival;
        }

        [SerializeField] private FactorySceneLoadController _sceneLoader;
        [SerializeField] private FactoryAreaController _areaController;
        [SerializeField] private BoxCollider _modelDesignRoomFloor;
        [SerializeField] private MeshCollider _modelDesignWalkSurface;
        [SerializeField] private StageArrival[] _stageArrivals;
        private FactoryLayoutData _layoutData;
        private XRBodyTransformer _bodyTransformer;
        private XRBaseInteractor[] _interactors;
#if UNITY_EDITOR
        private bool _usesEditorSimulator;
        private XROriginMovement _editorMovement;
#endif

        public XROrigin Rig { get; private set; }
        public Transform CameraTransform => Rig.Camera.transform;
        private void Awake()
        {
            _layoutData = GetComponent<FactoryLayoutData>();
            Rig = FindFirstObjectByType<XROrigin>();
            _bodyTransformer = Rig.GetComponent<XRBodyTransformer>();
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
#if UNITY_EDITOR
            if (_usesEditorSimulator)
            {
                _editorMovement.forceUnconstrained = false;
            }
#endif
        }

        private void Start()
        {
            Transform entrance = _layoutData.Entrance;
            float height = CameraTransform.position.y - Rig.Origin.transform.position.y;
#if UNITY_EDITOR
            // Keep the editor simulator at eye level without changing real headset tracking.
            _usesEditorSimulator = !UnityEngine.XR.XRSettings.isDeviceActive
                && FindFirstObjectByType<XR.XREditorSimulator>() != null;
            if (_usesEditorSimulator)
            {
                _editorMovement = Rig.GetComponent<ContinuousMoveProvider>().transformation;
                if (height < 0.5f)
                {
                    Vector3 offset = Rig.CameraFloorOffsetObject.transform.localPosition;
                    offset.y += Rig.CameraYOffset - height;
                    Rig.CameraFloorOffsetObject.transform.localPosition = offset;
                    height = Rig.CameraYOffset;
                }
            }
#endif
            Rig.MatchOriginUpCameraForward(Vector3.up, entrance.forward);
            Rig.MoveCameraToWorldLocation(entrance.position + Vector3.up * height);
            // Use the persistent rig's UI input module across content scene loads.
            Rig.GetComponent<XR.XRPlayerInputController>().EnsureRayReady();
        }

        private void Update()
        {
            FactoryAreaId area = _areaController.CurrentArea.Id;
#if UNITY_EDITOR
            if (_usesEditorSimulator)
            {
                bool freeMovement = area == FactoryAreaId.FeatureMapRoom;
                _editorMovement.forceUnconstrained = freeMovement;
                if (freeMovement)
                {
                    // Keep the height set with Q/E while inspecting the platforms.
                    return;
                }
            }
#endif
            if (area == FactoryAreaId.ModelDesignRoom)
            {
                FollowModelDesignFloor();
            }
            else if (area == FactoryAreaId.FeatureMapRoom || area == FactoryAreaId.FeatureMapRoomToModelDesignRoom)
            {
                // Follow the lower treads even when the shared rig has no thumbstick input.
                Vector3 descent = Vector3.down * (FloorDescentSpeed * Time.deltaTime);
                _bodyTransformer.constrainedBodyManipulator.MoveBody(descent);
            }
        }

        private void FollowModelDesignFloor()
        {
            if (!_modelDesignWalkSurface.enabled) { return; }

            Vector3 feet = _bodyTransformer.bodyPositionEvaluator.GetBodyGroundWorldPosition(Rig);
            Bounds surface = _modelDesignWalkSurface.bounds;
            float roomFloorY = _modelDesignRoomFloor.bounds.max.y;
#if UNITY_EDITOR
            if (_usesEditorSimulator)
            {
                // WASD changes the simulated tracked pose without going through the character controller.
                float editorFloorY = GetModelDesignSupportHeight(feet, surface.max.y + FloorContactTolerance, roomFloorY);
                Rig.Origin.transform.position += Vector3.up * (editorFloorY - feet.y);
                return;
            }
#endif
            if (feet.y < roomFloorY - FloorContactTolerance)
            {
                // Recover an origin that entered the room below the structural floor.
                Rig.MoveCameraToWorldLocation(CameraTransform.position + Vector3.up * (roomFloorY - feet.y));
                return;
            }

            float supportY = GetModelDesignSupportHeight(feet, feet.y + FloorContactTolerance, roomFloorY);
            float distance = Mathf.Min(feet.y - supportY, FloorDescentSpeed * Time.deltaTime);
            if (distance > 0f)
            {
                _bodyTransformer.constrainedBodyManipulator.MoveBody(Vector3.down * distance);
            }
        }

        private float GetModelDesignSupportHeight(Vector3 feet, float castHeight, float roomFloorY)
        {
            feet.y = castHeight;
            Ray ray = new Ray(feet, Vector3.down);
            float range = castHeight - roomFloorY + FloorContactTolerance;
            return _modelDesignWalkSurface.Raycast(ray, out RaycastHit hit, range) ? hit.point.y : roomFloorY;
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

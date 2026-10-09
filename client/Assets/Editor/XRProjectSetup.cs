using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Unity.XR.CoreUtils;

namespace XR.Editor
{
    public static class XRProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/QuestMain.unity";
        private const string RigPath = "Assets/Prefabs/XR/XRPlayerRig.prefab";
        private const string StartupSceneScheduledKey = "XR.QuestMainStartupSceneScheduled";

        [InitializeOnLoadMethod]
        private static void ScheduleStartupScene()
        {
            if (Application.isBatchMode || SessionState.GetBool(StartupSceneScheduledKey, false))
            {
                return;
            }

            // SessionState survives script reloads, so this runs only once per editor session.
            SessionState.SetBool(StartupSceneScheduledKey, true);
            EditorApplication.delayCall += OpenStartupScene;
        }

        private static void OpenStartupScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OpenStartupScene;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1 ||
                !File.Exists(ScenePath))
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || !string.IsNullOrEmpty(scene.path) ||
                scene.isDirty || scene.rootCount != 0)
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/XR/Configure Project")]
        public static void ConfigureProject()
        {
            Directory.CreateDirectory("Assets/XR");
            AssetDatabase.Refresh();
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                    out XRGeneralSettingsPerBuildTarget settings))
            {
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, "Assets/XR/XRGeneralSettings.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, settings, true);
            }

            ConfigureLoader(settings, BuildTargetGroup.Android);
            ConfigureLoader(settings, BuildTargetGroup.Standalone);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.kdisplaydemonhunters.metaquestvrcontest");
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                "Assets/Settings/Mobile_RPAsset.asset");
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 4;
            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureLoader(XRGeneralSettingsPerBuildTarget settings, BuildTargetGroup target)
        {
            if (!settings.HasManagerSettingsForBuildTarget(target))
            {
                settings.CreateDefaultManagerSettingsForBuildTarget(target);
            }

            XRGeneralSettings general = settings.SettingsForBuildTarget(target);
            general.InitManagerOnStart = true;
            if (!XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", target))
            {
                throw new InvalidOperationException($"Cannot assign OpenXR loader for {target}.");
            }

            OpenXRSettings openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            FeatureHelpers.RefreshFeatures(target);
            openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            EnableFeature<OculusTouchControllerProfile>(openXR);
            if (target == BuildTargetGroup.Android)
            {
                EnableFeature<MetaQuestFeature>(openXR);
            }
            EditorUtility.SetDirty(openXR);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.Manager);
        }

        private static void EnableFeature<T>(OpenXRSettings settings) where T : OpenXRFeature
        {
            T feature = settings.GetFeature<T>();
            if (feature == null)
            {
                throw new InvalidOperationException($"OpenXR feature {typeof(T).Name} is unavailable.");
            }
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }

        // Explicit command only: opening the project never overwrites a scene or prefab.
        [MenuItem("Tools/XR/Create Initial VR Scene")]
        public static void CreateInitialScene()
        {
            if (File.Exists(ScenePath) || File.Exists(RigPath))
            {
                throw new InvalidOperationException("XR scene or rig already exists. Edit the existing assets instead.");
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureProject();
            Directory.CreateDirectory("Assets/Prefabs/XR");
            Directory.CreateDirectory("Assets/XR/Materials");
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material floorMaterial = CreateMaterial("Floor", new Color(0.18f, 0.22f, 0.28f));
            Material rayMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(rayMaterial, "Assets/XR/Materials/ControllerRay.mat");

            GameObject rig = new GameObject("XR Player Rig");
            rig.AddComponent<PersistentXRPlayerRig>();
            XROrigin origin = rig.AddComponent<XROrigin>();
            GameObject offset = CreateChild("Camera Offset", rig.transform);
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = 1.6f;
            GameObject cameraObject = CreateChild("Main Camera", offset.transform);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.05f, 0.085f, 1f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            origin.Camera = camera;
            AddPoseDriver(cameraObject, "<XRHMD>", "centerEyePosition", "centerEyeRotation");
            XRInteractionManager manager = rig.AddComponent<XRInteractionManager>();
            CreateController(offset.transform, manager, "LeftHand", InteractorHandedness.Left, rayMaterial);
            CreateController(offset.transform, manager, "RightHand", InteractorHandedness.Right, rayMaterial);
            CreateHeldDevices(offset.transform);
            ConfigureMovement(rig);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rig, RigPath, InteractionMode.AutomatedAction);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.2f, 8f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.gray;

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            ValidateRig();
        }

        [MenuItem("Tools/XR/Add Left Stick Movement")]
        public static void AddLeftStickMovement()
        {
            GameObject rig = PrefabUtility.LoadPrefabContents(RigPath);
            try
            {
                ConfigureMovement(rig);
                PrefabUtility.SaveAsPrefabAsset(rig, RigPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rig);
            }
            ValidateRig();
        }

        private static void ConfigureMovement(GameObject rig)
        {
            // Re-running this command preserves the user's existing movement tuning.
            if (rig.GetComponent<ContinuousMoveProvider>() != null)
            {
                return;
            }
            XRBodyTransformer transformer = rig.GetComponent<XRBodyTransformer>();
            if (transformer == null)
            {
                transformer = rig.AddComponent<XRBodyTransformer>();
            }
            transformer.xrOrigin = rig.GetComponent<XROrigin>();
            transformer.useCharacterControllerIfExists = false;
            LocomotionMediator mediator = rig.GetComponent<LocomotionMediator>();
            if (mediator == null)
            {
                mediator = rig.AddComponent<LocomotionMediator>();
            }
            ContinuousMoveProvider movement = rig.AddComponent<ContinuousMoveProvider>();
            movement.mediator = mediator;
            movement.moveSpeed = 1.5f;
            movement.enableStrafe = true;
            movement.enableFly = false;
            // XRI still has a legacy gravity fallback when no GravityProvider exists.
#pragma warning disable CS0618
            movement.useGravity = false;
#pragma warning restore CS0618
            movement.forwardSource = transformer.xrOrigin.Camera.transform;
            movement.leftHandMoveInput = new XRInputValueReader<Vector2>("Left Stick Move",
                XRInputValueReader.InputSourceMode.InputAction)
            {
                inputAction = new InputAction("Left Stick Move", InputActionType.Value,
                    "<XRController>{LeftHand}/primary2DAxis", processors: "stickDeadzone(min=0.15,max=0.95)",
                    expectedControlType: "Vector2")
            };
            // A deadzone filters small resting-stick values from the OpenXR Vector2 control.
            movement.rightHandMoveInput = new XRInputValueReader<Vector2>("Right Move Disabled",
                XRInputValueReader.InputSourceMode.Unused);
        }

        private static void CreateController(Transform parent, XRInteractionManager manager, string hand,
            InteractorHandedness handedness, Material rayMaterial)
        {
            GameObject controller = CreateChild(hand + " Controller", parent);
            string device = "<XRController>{" + hand + "}";
            // Prefer OpenXR's aim pose; simulated controllers only expose the device pose.
            AddPoseDriver(controller, device, "pointerPosition", "pointerRotation", fallbackToDevicePose: true);
            XRRayInteractor ray = controller.AddComponent<XRRayInteractor>();
            ray.interactionManager = manager;
            ray.handedness = handedness;
            ray.enableUIInteraction = false;
            ray.maxRaycastDistance = 5f;
            ray.raycastTriggerInteraction = QueryTriggerInteraction.Collide;
            if (handedness == InteractorHandedness.Right)
            {
                ray.translateManipulationInput = new XRInputValueReader<Vector2>("Right Object Distance",
                    XRInputValueReader.InputSourceMode.InputAction)
                {
                    inputAction = new InputAction("Right Object Distance", InputActionType.Value,
                        "<XRController>{RightHand}/primary2DAxis", processors: "stickDeadzone(min=0.15,max=0.95)",
                        expectedControlType: "Vector2")
                };
            }
            ray.selectInput = CreateButton(hand + " Select", device + "/gripPressed", device + "/grip");
            ray.activateInput = CreateButton(hand + " Activate", device + "/triggerPressed", device + "/trigger");
            LineRenderer line = controller.AddComponent<LineRenderer>();
            line.sharedMaterial = rayMaterial;
            line.startWidth = 0.005f;
            line.endWidth = 0.005f;
            controller.AddComponent<XRInteractorLineVisual>();
            if (handedness == InteractorHandedness.Left)
            {
                ray.enabled = false;
                line.enabled = false;
                controller.GetComponent<XRInteractorLineVisual>().enabled = false;
            }
        }

        private static void CreateHeldDevices(Transform parent)
        {
            Material body = CreateMaterial("DeviceBody", new Color(0.72f, 0.76f, 0.82f));
            Material screen = CreateMaterial("DeviceScreen", new Color(0.015f, 0.025f, 0.04f));
            Material tabletBody = CreateMaterial("TabletBody", new Color(0.72f, 0.76f, 0.82f),
                "Universal Render Pipeline/Unlit");
            GameObject left = CreateChild("LeftHand Grip Visual", parent);
            AddPoseDriver(left, "<XRController>{LeftHand}", "devicePosition", "deviceRotation");
            GameObject tablet = CreateChild("Tablet", left.transform);
            tablet.transform.localPosition = new Vector3(0f, 0.12f, 0.04f);
            CreateDevicePart("Tablet Body", tablet.transform, Vector3.zero, new Vector3(0.28f, 0.20f, 0.012f), tabletBody);
            Text message = CreateTabletCanvas(tablet.transform);
            LeftHandTablet display = tablet.AddComponent<LeftHandTablet>();
            SerializedObject serializedDisplay = new SerializedObject(display);
            serializedDisplay.FindProperty("_message").objectReferenceValue = message;
            serializedDisplay.ApplyModifiedPropertiesWithoutUndo();

            GameObject right = CreateChild("RightHand Grip Visual", parent);
            AddPoseDriver(right, "<XRController>{RightHand}", "devicePosition", "deviceRotation");
            CreateDevicePart("Controller Handle", right.transform, new Vector3(0f, -0.035f, 0f), new Vector3(0.035f, 0.11f, 0.04f), body);
            CreateDevicePart("Controller Head", right.transform, new Vector3(0f, 0.027f, 0.01f), new Vector3(0.072f, 0.025f, 0.075f), body);
            CreateDevicePart("Controller Stick", right.transform, new Vector3(-0.016f, 0.046f, 0.018f), new Vector3(0.016f, 0.015f, 0.016f), screen);
            CreateDevicePart("Controller A", right.transform, new Vector3(0.017f, 0.043f, 0.002f), new Vector3(0.011f, 0.007f, 0.011f), screen);
            CreateDevicePart("Controller B", right.transform, new Vector3(0.017f, 0.043f, 0.022f), new Vector3(0.011f, 0.007f, 0.011f), screen);
            CreateDevicePart("Controller Trigger", right.transform, new Vector3(0f, 0.009f, 0.054f), new Vector3(0.025f, 0.027f, 0.012f), screen);
        }

        private static Text CreateTabletCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Tablet Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.transform.SetParent(parent, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0f, -0.0075f);
            // UI pixels map to 0.5 mm in world space, matching the tablet screen.
            canvasObject.transform.localScale = Vector3.one * 0.0005f;
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 340f);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.layer = canvasObject.layer;
            background.transform.SetParent(canvasObject.transform, false);
            // Preserve the old 26 x 18 cm screen, with text 0.5 mm in front.
            background.transform.localPosition = new Vector3(0f, 0f, 1f);
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 360f);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.015f, 0.025f, 0.04f);
            backgroundImage.raycastTarget = false;

            GameObject label = new GameObject("Button Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.layer = canvasObject.layer;
            label.transform.SetParent(canvasObject.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text message = label.GetComponent<Text>();
            message.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            message.fontSize = 32;
            message.alignment = TextAnchor.MiddleCenter;
            message.color = Color.white;
            message.raycastTarget = false;
            message.text = "TABLET READY\n\nX / Y / TRIGGER";
            return message;
        }

        private static void CreateDevicePart(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
            part.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static XRInputButtonReader CreateButton(string name, string button, string value)
        {
            return new XRInputButtonReader(name, name + " Value", true, XRInputButtonReader.InputSourceMode.InputAction)
            {
                inputActionPerformed = new InputAction(name, InputActionType.Button, button),
                inputActionValue = new InputAction(name + " Value", InputActionType.Value, value)
            };
        }

        private static void AddPoseDriver(GameObject target, string device, string position, string rotation,
            bool fallbackToDevicePose = false)
        {
            InputAction positionAction = new InputAction("Position", InputActionType.Value,
                expectedControlType: "Vector3");
            InputAction rotationAction = new InputAction("Rotation", InputActionType.Value,
                expectedControlType: "Quaternion");
            if (fallbackToDevicePose)
            {
                // XRI's fallback composites keep aim priority even when its value is zero.
                positionAction.AddCompositeBinding("Vector3Fallback")
                    .With("first", device + "/" + position)
                    .With("second", device + "/devicePosition");
                rotationAction.AddCompositeBinding("QuaternionFallback")
                    .With("first", device + "/" + rotation)
                    .With("second", device + "/deviceRotation");
            }
            else
            {
                positionAction.AddBinding(device + "/" + position);
                rotationAction.AddBinding(device + "/" + rotation);
            }

            TrackedPoseDriver driver = target.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(positionAction);
            driver.rotationInput = new InputActionProperty(rotationAction);
            driver.trackingStateInput = new InputActionProperty(new InputAction("Tracking State", InputActionType.Value,
                device + "/trackingState", expectedControlType: "Integer"));
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Material CreateMaterial(string name, Color color, string shaderName = "Universal Render Pipeline/Lit")
        {
            Material material = new Material(Shader.Find(shaderName));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, $"Assets/XR/Materials/{name}.mat");
            return material;
        }

        [MenuItem("Tools/XR/Validate Project")]
        public static void ValidateProject()
        {
            // Meta's rules use the active platform's OpenXRSettings.Instance internally.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                throw new BuildFailedException("Switch the active build profile to Android / Meta Quest before validation.");
            }
            XRGeneralSettings general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (general == null || !general.InitManagerOnStart || general.Manager == null ||
                !XRPackageMetadataStore.IsLoaderAssigned("UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android))
            {
                throw new BuildFailedException("Android OpenXR must be assigned and initialized on startup.");
            }
            OpenXRSettings openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXR.GetFeature<MetaQuestFeature>()?.enabled != true ||
                openXR.GetFeature<OculusTouchControllerProfile>()?.enabled != true)
            {
                throw new BuildFailedException("Enable Meta Quest Support and the Oculus Touch interaction profile.");
            }
            if (!File.Exists(ScenePath))
            {
                throw new BuildFailedException("QuestMain scene is missing.");
            }
            List<OpenXRFeature.ValidationRule> issues = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
            foreach (OpenXRFeature.ValidationRule issue in issues)
            {
                if (issue.error)
                {
                    throw new BuildFailedException(issue.message);
                }
                Debug.LogWarning("[XR] " + issue.message);
            }
            ValidateRig();
            Debug.Log("[XR] Project validation passed.");
        }

        private static void ValidateRig()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
            if (prefab == null || prefab.GetComponent<PersistentXRPlayerRig>() == null ||
                prefab.GetComponent<XROrigin>()?.Camera == null ||
                prefab.GetComponentsInChildren<Camera>(true).Length != 1 ||
                prefab.GetComponentsInChildren<XRRayInteractor>(true).Length != 2 ||
                prefab.GetComponentsInChildren<TrackedPoseDriver>(true).Length != 5 ||
                prefab.GetComponentInChildren<LeftHandTablet>(true) == null)
            {
                throw new BuildFailedException("XR rig camera/controller references are incomplete.");
            }
            foreach (XRRayInteractor ray in prefab.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (ray.interactionManager == null ||
                    ray.selectInput.inputSourceMode != XRInputButtonReader.InputSourceMode.InputAction ||
                    ray.selectInput.inputActionPerformed.bindings.Count == 0 ||
                    ray.activateInput.inputActionPerformed.bindings.Count == 0)
                {
                    throw new BuildFailedException("XR controller manager or input binding is missing.");
                }
            }
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                {
                    throw new BuildFailedException("XR rig has a missing script.");
                }
            }
            Debug.Log("[XR] Rig validation passed.");
        }

    }
}

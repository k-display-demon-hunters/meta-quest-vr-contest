using Tablet;
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
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
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
        private const string StartupScenePath = "Assets/Scenes/FactoryIntro.unity";
        private const string RigPath = "Assets/Prefabs/XR/XRPlayerRig.prefab";
        private const string StartupSceneScheduledKey = "XR.FactoryIntroStartupSceneScheduled";

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
                !File.Exists(StartupScenePath))
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || !string.IsNullOrEmpty(scene.path) ||
                scene.isDirty || scene.rootCount != 0)
            {
                return;
            }

            EditorSceneManager.OpenScene(StartupScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/XR/Configure Project")]
        public static void ConfigureProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before configuring XR.");
            }
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
            // Desktop development uses simulated input devices without an installed OpenXR runtime.
            // Quest Link requires enabling Standalone initialization explicitly in Project Settings.
            general.InitManagerOnStart = target == BuildTargetGroup.Android;
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
                EnableFeature<MetaQuestTouchPlusControllerProfile>(openXR);
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
            movement.moveSpeed = 5f;
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
            if (!File.Exists(StartupScenePath))
            {
                throw new BuildFailedException("FactoryIntro scene is missing.");
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
                prefab.GetComponentInChildren<TabletManager>(true) == null)
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

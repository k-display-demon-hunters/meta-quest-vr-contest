using UnityEngine;
#if UNITY_EDITOR
using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
#endif

namespace KdyIntro
{
    // Lives with the persistent XR rig so virtual hands keep their poses between chapters.
    public sealed class KdyDesktopController : MonoBehaviour
    {
#if UNITY_EDITOR
        private XR.XREditorSimulator _wrapper;
        private XRInteractionSimulator _simulation;
        private XRInputValueReader<float> _previousX;
        private XRInputValueReader<float> _previousY;
        private XRInputValueReader<float> _previousZ;
        private XRInputValueReader<float> _bodyMovement;
        private bool _ownsWrapper;
        private bool _wrapperWasEnabled;
        private bool _simulationWasEnabled;

        private void Awake()
        {
            XROrigin origin = GetComponentInParent<XROrigin>();
            _wrapper = origin.GetComponentInChildren<XR.XREditorSimulator>(true);
            if (_wrapper != null)
            {
                _wrapperWasEnabled = _wrapper.enabled;
                _wrapper.enabled = true;
                _simulation = _wrapper.GetComponentInChildren<XRInteractionSimulator>(true);
            }
            else
            {
                // Reuse an existing SDK simulator rather than creating a duplicate singleton.
                _simulation = XRInteractionSimulator.instance;
                if (_simulation == null)
                {
                    _ownsWrapper = true;
                    _wrapper = gameObject.AddComponent<XR.XREditorSimulator>();
                    _simulation = _wrapper.GetComponentInChildren<XRInteractionSimulator>(true);
                }
            }
            if (_simulation == null)
            {
                Debug.LogError("[KdyIntro] Desktop XR simulator could not be initialized.", this);
                return;
            }
            _simulationWasEnabled = _simulation.enabled;
            _simulation.enabled = true;
            _previousX = _simulation.translateXInput;
            _previousY = _simulation.translateYInput;
            _previousZ = _simulation.translateZInput;
            _bodyMovement = new XRInputValueReader<float>("Kdy Body Movement",
                XRInputValueReader.InputSourceMode.ManualValue);
            // KdyRigController owns WASD/QE, even while aiming a virtual hand.
            _simulation.translateXInput = _bodyMovement;
            _simulation.translateYInput = _bodyMovement;
            _simulation.translateZInput = _bodyMovement;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private IEnumerator Start()
        {
            // Let the newly created HMD publish its first tracked pose before placing the user.
            yield return null;
            if (_ownsWrapper)
            {
                XROrigin origin = GetComponentInParent<XROrigin>();
                if (origin != null && origin.Camera != null)
                {
                    Vector3 position = origin.Camera.transform.position;
                    position.y = 1.65f;
                    origin.MoveCameraToWorldLocation(position);
                }
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.StartsWith("kdy_scene", StringComparison.Ordinal))
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (!_ownsWrapper && _simulation != null && _bodyMovement != null)
            {
                _simulation.translateXInput = _previousX;
                _simulation.translateYInput = _previousY;
                _simulation.translateZInput = _previousZ;
                _simulation.enabled = _simulationWasEnabled;
            }
            if (!_ownsWrapper && _wrapper != null)
            {
                _wrapper.enabled = _wrapperWasEnabled;
            }
            _bodyMovement?.inputAction?.Dispose();
        }
#endif
    }
}

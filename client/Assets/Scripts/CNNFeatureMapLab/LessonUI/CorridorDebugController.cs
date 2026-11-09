using System;
using UnityEngine;

namespace FeatureMapCorridor
{
    [Serializable]
    internal sealed class CorridorDebugController
    {
        [SerializeField] private bool _showDebug;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private TMPro.TextMeshPro _hud;
        private Transform _leftHand;
        private string _text = "";
        private string _lastState = "";
        private string _lastButton = "—";
        private bool _visible;
#endif
        internal void RecordButton(string caption)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _lastButton = caption;
            Debug.Log("[Corridor] UI: "+caption);
#endif
        }

        internal void Show(string state, string hover, string cell, string click, Camera camera, TMPro.TMP_FontAsset font)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_lastState != state)
            {
                Debug.Log("[Corridor] State: "+state);
                _lastState = state;
            }
            if (!_showDebug)
            {
                Hide();
                return;
            }
            if (_leftHand == null)
            {
                var rig = camera.GetComponentInParent<XR.PersistentXRPlayerRig>();
                if (rig == null)
                {
                    return;
                }
                foreach (var ray in rig.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true))
                if (ray.handedness == UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Left)
                {
                    _leftHand = ray.transform;
                }
            }
            if (_hud == null)
            {
                _hud = new GameObject("CorridorDebugHUD").AddComponent<TMPro.TextMeshPro>();
                _hud.transform.SetParent(_leftHand, false);
                _hud.transform.localPosition = new Vector3(0, .2f, .08f);
                _hud.transform.localScale = Vector3.one*.001f;
                _hud.rectTransform.sizeDelta = new Vector2(700, 180);
                _hud.font = font;
                _hud.fontSize = 26;
                _hud.raycastTarget = false;
            }
            _visible = true;
            _text = state+"\nHover: "+hover+" "+cell+"\nClick: "+click+"\nUI: "+_lastButton;
            _hud.gameObject.SetActive(true);
            _hud.transform.rotation = Quaternion.LookRotation(_hud.transform.position-camera.transform.position);
            _hud.text = _text;
#endif
        }

        internal void Draw()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_visible)
            {
                GUI.Box(new Rect(12, 12, 520, 125), "[Corridor] "+_text);
            }
#endif
        }

        internal void Hide()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _visible = false;
            if (_hud != null)
            {
                _hud.gameObject.SetActive(false);
            }
#endif
        }

        internal void Dispose()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_hud != null)
            {
                UnityEngine.Object.Destroy(_hud.gameObject);
            }
#endif
        }
    }
}

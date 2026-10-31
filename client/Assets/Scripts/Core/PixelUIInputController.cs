using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace UI
{
    public sealed class PixelUIInputController : IDisposable
    {
        private GameObject _ownedEventSystem;
        private XRUIInputModule _addedInputModule;

        public void Enable(Transform parent)
        {
            if (EventSystem.current == null)
            {
                _ownedEventSystem = new GameObject("XR UI Input",
                    typeof(EventSystem), typeof(XRUIInputModule));
                _ownedEventSystem.transform.SetParent(parent, false);
            }
            else if (EventSystem.current.GetComponent<XRUIInputModule>() == null)
            {
                _addedInputModule = EventSystem.current.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        public void Dispose()
        {
            if (_ownedEventSystem != null)
            {
                _ownedEventSystem.SetActive(false);
                UnityEngine.Object.Destroy(_ownedEventSystem);
                _ownedEventSystem = null;
            }
            if (_addedInputModule != null)
            {
                UnityEngine.Object.Destroy(_addedInputModule);
                _addedInputModule = null;
            }
        }
    }
}

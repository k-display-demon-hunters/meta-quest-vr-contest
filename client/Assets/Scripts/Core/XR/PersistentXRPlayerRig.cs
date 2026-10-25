using UnityEngine;

namespace XR
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class PersistentXRPlayerRig : MonoBehaviour
    {
        private static PersistentXRPlayerRig _instance;
        [SerializeField] private GameObject _leftControllerModel;
        [SerializeField] private GameObject _rightControllerModel;
        [SerializeField] private RuntimeAnimatorController _leftControllerAnimation;
        [SerializeField] private RuntimeAnimatorController _rightControllerAnimation;

        private void Start()
        {
            foreach (Transform target in GetComponentsInChildren<Transform>(true))
            {
                if (target.name != "LeftHand Grip Visual" && target.name != "RightHand Grip Visual") { continue; }
                bool left = target.name.StartsWith("Left");
                if ((left ? _leftControllerModel : _rightControllerModel) == null) { continue; }
                foreach (Transform child in target)
                {
                    if (child.name.StartsWith("Controller ")) { child.gameObject.SetActive(false); }
                }
                CreateControllerVisual(target, left);
            }
        }

        public TouchPlusHandController CreateControllerVisual(Transform parent, bool left)
        {
            GameObject model = left ? _leftControllerModel : _rightControllerModel;
            if (model == null) { return null; }
            GameObject visual = Instantiate(model, parent, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            TouchPlusHandController controller = visual.AddComponent<TouchPlusHandController>();
            controller.Initialize(left, left ? _leftControllerAnimation : _rightControllerAnimation);
            return controller;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            // Reset when entering Play mode even when domain reload is disabled.
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Disable immediately so duplicate cameras and inputs never become active.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            if (GetComponent<XRPlayerInputController>() == null)
            {
                gameObject.AddComponent<XRPlayerInputController>();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

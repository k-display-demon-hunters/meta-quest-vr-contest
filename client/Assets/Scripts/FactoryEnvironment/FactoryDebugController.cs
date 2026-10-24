using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace FactoryEnvironment
{
    public sealed class FactoryDebugController : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private FactoryLearningFlowManager _flow;

        public void Initialize(FactoryLearningFlowManager flow)
        {
            _flow = flow;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.ctrlKey.isPressed
                || !keyboard.shiftKey.isPressed || !keyboard.f8Key.wasPressedThisFrame)
            {
                return;
            }
            _flow.TrySkipCurrentLesson();
        }
#endif
    }
}

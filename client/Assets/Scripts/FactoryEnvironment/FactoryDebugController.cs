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
        private Font _font;
        private GUIStyle _noticeStyle;
        private float _noticeUntil;

        public void Initialize(FactoryLearningFlowManager flow, Font font)
        {
            _flow = flow;
            _font = font;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _flow == null || !keyboard.ctrlKey.isPressed
                || !keyboard.shiftKey.isPressed || !keyboard.f8Key.wasPressedThisFrame)
            {
                return;
            }
            if (_flow.TrySkipCurrentLesson())
            {
                _noticeUntil = Time.unscaledTime + 3f;
            }
        }

        private void OnGUI()
        {
            if (Time.unscaledTime >= _noticeUntil)
            {
                return;
            }
            if (_noticeStyle == null)
            {
                _noticeStyle = new GUIStyle(GUI.skin.box)
                {
                    font = _font,
                    fontSize = 22,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
            }
            GUI.Box(new Rect(20f, 20f, Mathf.Min(640f, Screen.width - 40f), 80f),
                "개발자 건너뛰기 적용\n안내선을 따라 다음 방 또는 출구로 이동하세요.", _noticeStyle);
        }

        private void OnDisable()
        {
            _noticeUntil = 0f;
        }
#endif
    }
}

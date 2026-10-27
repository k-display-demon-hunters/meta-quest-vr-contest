using UnityEngine;
using UnityEngine.UI;

namespace CNNFeatureMaps.Lesson
{
    public enum LessonAction { Previous, Next, Scan, Filter, Spread, Restore, Enlarge, Shrink }

    [RequireComponent(typeof(Button))]
    public sealed class LessonButton : MonoBehaviour
    {
        [SerializeField] private LessonAction _action;
        [SerializeField] private Text _label;
        private Button _button;
        private float _lastPress = -1f;

        public LessonAction Action => _action;

        public event System.Action<LessonAction> OnActionRequested;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(Press);
        }

        public void SetLabel(string label)
        {
            _label.text = label;
        }

        public void SetAvailable(bool available)
        {
            _button.interactable = available;
        }

        public void Press()
        {
            if (!_button.IsInteractable() || Time.unscaledTime - _lastPress < .35f)
            {
                return;
            }
            _lastPress = Time.unscaledTime;
            OnActionRequested?.Invoke(_action);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(Press);
        }
    }
}

using UnityEngine;

namespace CNNFeatureMaps.Lesson
{
    public sealed class FilterCardController : MonoBehaviour
    {
        [SerializeField] private TextMesh _filterName;
        [SerializeField] private TextMesh _weights;

        public void Show(int channel, bool visible)
        {
            _filterName.text = channel == 0 ? LessonContent.HorizontalFilterName : LessonContent.VerticalFilterName;
            _weights.text = channel == 0 ? LessonContent.HorizontalFilterWeights : LessonContent.VerticalFilterWeights;
            gameObject.SetActive(visible);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyScrollBeaconController : MonoBehaviour
    {
        [SerializeField] private Renderer _seal;
        [SerializeField] private Text _label;
        [SerializeField] private string _scrollName;
        private Vector3 _home;
        private bool _available;
        private MaterialPropertyBlock _properties;

        private void Awake()
        {
            _home = transform.localPosition;
            _properties = new MaterialPropertyBlock();
        }

        public void Configure(bool available, bool collected)
        {
            _available = available;
            _label.text = _scrollName + (collected ? " · 획득" : (available ? " · 선택" : " · 잠김"));
            Color tint = available ? new Color(.1f, .85f, 1f) : new Color(.18f, .22f, .28f);
            _properties.SetColor("_BaseColor", tint);
            _properties.SetColor("_EmissionColor", available ? tint * 2f : Color.black);
            _seal.SetPropertyBlock(_properties);
        }

        public void ShowQuestDestination()
        {
            _label.text = "신경망 설계 구역 · 선택";
        }

        private void Update()
        {
            if (_available)
            {
                transform.localPosition = _home + Vector3.up * (Mathf.Sin(Time.time * 1.7f) * .045f);
            }
        }
    }
}

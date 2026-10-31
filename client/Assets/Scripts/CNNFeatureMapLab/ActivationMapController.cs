using System;
using System.Collections;
using UnityEngine;

namespace CNNFeatureMaps.Activation
{
    public sealed class ActivationMapController : MonoBehaviour
    {
        private const int Size = 8;
        [SerializeField] private ActivationCellController[] _beforeCells;
        [SerializeField] private ActivationCellController[] _afterCells;
        [SerializeField] private Renderer[] _movingCells;
        [SerializeField] private Transform _beforeSelection;
        [SerializeField] private Transform _afterSelection;
        [SerializeField] private TextMesh _selectedValue;
        [SerializeField] private TextMesh _afterTitle;
        [SerializeField] private Transform _gate;
        private readonly bool[] _revealed = new bool[Size * Size];
        private float[,] _before;
        private float[,] _after;
        private float _scale;
        private MaterialPropertyBlock _properties;
        private Coroutine _animation;
        public bool IsAnimating => _animation != null;
        public event Action OnApplied;

        private void Awake() { _properties = new MaterialPropertyBlock(); }

        private void OnEnable()
        {
            foreach (ActivationCellController cell in _beforeCells) { cell.OnSelected += SelectCell; }
            foreach (ActivationCellController cell in _afterCells) { cell.OnSelected += SelectCell; }
        }

        private void OnDisable()
        {
            foreach (ActivationCellController cell in _beforeCells) { cell.OnSelected -= SelectCell; }
            foreach (ActivationCellController cell in _afterCells) { cell.OnSelected -= SelectCell; }
            if (_animation != null) { StopCoroutine(_animation); _animation = null; }
            foreach (Renderer cell in _movingCells) { cell.gameObject.SetActive(false); }
        }

        public void ShowMap(float[,] before, float[,] after)
        {
            _before = before;
            _after = after;
            _scale = .001f;
            foreach (float value in before) { _scale = Mathf.Max(_scale, Mathf.Abs(value)); }
            Array.Clear(_revealed, 0, _revealed.Length);
            for (int index = 0; index < _beforeCells.Length; index++)
            {
                _beforeCells[index].Show(before[index / Size, index % Size], _scale, true);
                _afterCells[index].Show(0f, _scale, false);
            }
            _beforeSelection.gameObject.SetActive(false);
            _afterSelection.gameObject.SetActive(false);
            _selectedValue.text = "칸을 눌러 전후 값 비교";
            _afterTitle.text = "적용 후 · 대기";
        }

        public void Apply()
        {
            if (IsAnimating) { return; }
            ShowMap(_before, _after);
            _animation = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            _afterTitle.text = "적용 후 · 처리 중";
            for (int row = 0; row < Size; row++)
            {
                float elapsed = 0f;
                while (elapsed < .7f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .7f));
                    for (int column = 0; column < Size; column++)
                    {
                        int index = row * Size + column;
                        Renderer moving = _movingCells[column];
                        moving.gameObject.SetActive(true);
                        Vector3 from = _beforeCells[index].transform.localPosition;
                        Vector3 to = _afterCells[index].transform.localPosition;
                        Vector3 position = Vector3.Lerp(from, to, amount);
                        position.z = -.22f - Mathf.Sin(amount * Mathf.PI) * .16f;
                        moving.transform.localPosition = position;
                        float distance = Vector3.Dot(_gate.right, moving.transform.position - _gate.position);
                        float value = distance < 0f ? _before[row, column] : _after[row, column];
                        moving.transform.localScale = new Vector3(.11f, .11f, ActivationCellController.Depth(value, _scale));
                        Color color = ActivationCellController.Tint(value, _scale);
                        _properties.SetColor("_BaseColor", color);
                        _properties.SetColor("_Color", color);
                        moving.SetPropertyBlock(_properties);
                    }
                    yield return null;
                }
                for (int column = 0; column < Size; column++)
                {
                    int index = row * Size + column;
                    _revealed[index] = true;
                    _afterCells[index].Show(_after[row, column], _scale, true);
                    _movingCells[column].gameObject.SetActive(false);
                }
            }
            _afterTitle.text = "적용 후";
            _animation = null;
            // Start with an actual negative value so the zeroing operation is immediately inspectable.
            int example = 0;
            for (int index = 0; index < _beforeCells.Length; index++)
            {
                if (_before[index / Size, index % Size] < 0f) { example = index; break; }
            }
            SelectCell(example);
            OnApplied?.Invoke();
        }

        private void SelectCell(int index)
        {
            if (_before == null || IsAnimating) { return; }
            _beforeSelection.gameObject.SetActive(true);
            _afterSelection.gameObject.SetActive(true);
            PlaceSelection(_beforeSelection, _beforeCells[index].transform);
            PlaceSelection(_afterSelection, _afterCells[index].transform);
            float before = _before[index / Size, index % Size];
            string result = _revealed[index] ? _after[index / Size, index % Size].ToString("0.000") : "적용 대기";
            _selectedValue.text = $"{index / Size + 1}행 {index % Size + 1}열\n{before:0.000}  →  {result}";
        }

        private static void PlaceSelection(Transform selection, Transform cell)
        {
            selection.localPosition = cell.localPosition - Vector3.forward * (cell.localScale.z * .5f + .01f);
        }
    }
}

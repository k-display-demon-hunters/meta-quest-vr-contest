using System;
using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    internal sealed class ResultGridController
    {
        private readonly Transform _result;
        private readonly Text _resultTitle;
        private readonly Text[] _products = new Text[9];
        private readonly Transform[] _productMarks = new Transform[9];
        private readonly Transform _highlight;
        private readonly MeshRenderer[] _highlightEdges;
        private readonly MaterialPropertyBlock _highlightProperties = new MaterialPropertyBlock();
        private readonly WorkbenchPrimitiveFactory _factory;

        public ResultGridController(WorkbenchPrimitiveFactory factory, Transform anchor, string title)
        {
            _factory = factory;
            _result = factory.Root(title);
            _result.localPosition = new Vector3(0.45f, 0, 0.1f);
            factory.Frame(_result, 0.64f, 0.57f, 0.08f, new Color(0.28f, 0.31f, 0.32f));
            factory.Tile(_result, new Vector3(0, 0, 0.065f), new Vector3(0.60f, 0.53f, 0.035f), new Color(0.055f, 0.065f, 0.075f));
            Transform resultTitlePlate = factory.FormulaPlate("Result title plate", 0.82f);
            resultTitlePlate.SetParent(_result, false);
            resultTitlePlate.localPosition = new Vector3(0, 0.38f, -0.04f);
            _resultTitle = factory.Caption(resultTitlePlate, title, new Vector3(0, 0, -0.027f), 0.76f);
            _resultTitle.fontSize = 38;
            for (int i = 0; i < 9; i++)
            {
                Transform productTile = factory.Tile(_result, WorkbenchPrimitiveFactory.GridCellPosition(i, 3), new Vector3(0.17f, 0.15f, 0.12f), new Color(0.16f, 0.18f, 0.22f));
                _products[i] = factory.Caption(productTile, "·", new Vector3(0, 0, -0.066f), 0.17f);
                _products[i].color = WorkbenchColors.PendingValue;
                Transform mark = factory.Root("Recorded product " + i);
                mark.SetParent(productTile, false);
                mark.localPosition = new Vector3(0.058f, -0.05f, -0.075f);
                Transform shortStroke = factory.Tile(mark, new Vector3(-0.007f, -0.003f, 0),
                    new Vector3(0.005f, 0.016f, 0.006f), WorkbenchColors.CalculationAccent);
                shortStroke.localRotation = Quaternion.Euler(0, 0, 40);
                Transform longStroke = factory.Tile(mark, new Vector3(0.005f, 0.003f, 0),
                    new Vector3(0.005f, 0.03f, 0.006f), WorkbenchColors.CalculationAccent);
                longStroke.localRotation = Quaternion.Euler(0, 0, -40);
                mark.gameObject.SetActive(false);
                _productMarks[i] = mark;
            }
            _result.SetPositionAndRotation(anchor.position, anchor.rotation);
            _highlight = factory.Highlight("Active product cell", 0.178f, 0.158f);
            _highlightEdges = _highlight.GetComponentsInChildren<MeshRenderer>(true);
        }

        public Vector3 CellPosition(int index) => _factory.LocalPoint(_products[index].transform.position);
        public void SetVisible(bool visible) => _result.gameObject.SetActive(visible);
        public bool HasRecordedValue(int index) => _productMarks[index].gameObject.activeSelf;

        // Bind the selection overlay during construction so it keeps the original canvas and hit plane.
        public void AttachSelectionSurface(int index, Action<RectTransform> bind)
        {
            bind((RectTransform)_products[index].transform.parent);
        }

        public void Clear()
        {
            SetVisible(true);
            for (int i = 0; i < _products.Length; i++)
            {
                ClearCell(i);
            }
            HideHighlight();
        }

        public void ClearCell(int index)
        {
            _products[index].text = "·";
            _products[index].color = WorkbenchColors.PendingValue;
            _products[index].enabled = true;
            _productMarks[index].gameObject.SetActive(false);
        }

        public void Record(int index, int value)
        {
            _products[index].text = value.ToString();
            _products[index].color = Color.white;
            _productMarks[index].gameObject.SetActive(true);
        }

        public void SetValueVisible(int index, bool visible) => _products[index].enabled = visible;
        public void SetValueColor(int index, Color color) => _products[index].color = color;

        public void Highlight(int index)
        {
            _highlight.SetParent(_result, false);
            _highlight.localPosition = WorkbenchPrimitiveFactory.GridCellPosition(index, 3) + Vector3.back * 0.075f;
            _highlight.gameObject.SetActive(true);
        }

        public void SetHighlightGlow(bool glowing)
        {
            Color emission = glowing ? WorkbenchColors.CalculationAccent * 0.65f : Color.black;
            foreach (MeshRenderer edge in _highlightEdges)
            {
                edge.GetPropertyBlock(_highlightProperties);
                _highlightProperties.SetColor("_EmissionColor", emission);
                edge.SetPropertyBlock(_highlightProperties);
            }
        }

        public void HideHighlight() => _highlight.gameObject.SetActive(false);

    }
}

using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    internal sealed class EquationDisplayController
    {
        private readonly Text _sumLabel;
        private readonly Text _equation;
        private readonly Text _sumEquation;
        private readonly GameObject _equationPlate;
        private readonly GameObject _sumEquationPlate;
        private readonly WorkbenchPrimitiveFactory _factory;

        public EquationDisplayController(WorkbenchPrimitiveFactory factory, Transform sum, Transform equation, Transform sumEquation)
        {
            _factory = factory;
            Transform slot = factory.Root("Answer destination");
            factory.Tile(slot, Vector3.zero, new Vector3(0.64f, 0.16f, 0.045f), WorkbenchColors.PanelFace);
            factory.Frame(slot, 0.67f, 0.19f, 0.05f, WorkbenchColors.PanelFrame);
            _sumLabel = factory.Caption(slot, "합: —", new Vector3(0, 0, -0.03f), 0.61f);
            _sumLabel.fontSize = 42;
            Transform equationPlate = factory.FormulaPlate("One cell multiplication plate", 0.82f);
            _equation = factory.Caption(equationPlate, "", new Vector3(0, 0, -0.027f), 0.78f);
            _equation.fontSize = 48;
            Transform sumEquationPlate = factory.FormulaPlate("Channel and RGB sum plate", 1.04f);
            _sumEquation = factory.Caption(sumEquationPlate, "", new Vector3(0, 0, -0.027f), 1.0f);
            _sumEquation.fontSize = 42;
            slot.SetParent(sum, false);
            equationPlate.SetParent(equation, false);
            sumEquationPlate.SetParent(sumEquation, false);
            _equationPlate = equationPlate.gameObject;
            _sumEquationPlate = sumEquationPlate.gameObject;
        }

        public Vector3 EquationPosition => _factory.LocalPoint(_equation.transform.position);
        public Vector3 AnswerPosition => _factory.LocalPoint(_sumLabel.transform.position);
        public void SetAnswerVisible(bool visible) => _sumLabel.enabled = visible;

        public void ShowProduct(int channel, int input, int weight, int product)
        {
            _equation.color = WorkbenchColors.Channel(channel);
            _equation.text = $"{input} × {weight} = {product}";
            _equationPlate.SetActive(true);
        }

        public void ClearProduct() => _equation.text = "";

        public void Clear()
        {
            // Empty plates would float as blank bars in scenes without the products backing panel.
            ClearProduct();
            _equationPlate.SetActive(false);
            _sumEquationPlate.SetActive(false);
            _sumLabel.fontSize = 60;
            _sumLabel.color = Color.white;
            _sumLabel.text = "합계 —";
        }

        public void ShowChannelSum(int sum, string equation)
        {
            _sumLabel.text = "합계 " + sum;
            ShowSumEquation(equation, WorkbenchColors.CalculationAccent);
        }

        public void ShowRgbSum(int output, string equation)
        {
            ShowSumEquation(equation, Color.white);
            _sumLabel.text = "출력 " + output;
        }

        public void ShowSumValue(int sum) => _sumLabel.text = "합계 " + sum;

        private void ShowSumEquation(string equation, Color color)
        {
            _sumEquation.color = color;
            _sumEquation.text = equation;
            _sumEquationPlate.SetActive(true);
        }
    }
}

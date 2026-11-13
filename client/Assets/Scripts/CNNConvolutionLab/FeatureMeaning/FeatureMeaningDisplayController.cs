using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    // Closing scene display: a letter picture, the filter window sweeping it, one bar map per filter
    // and the next-stage box the maps travel into.
    internal sealed class FeatureMeaningDisplayController
    {
        private const float Pitch = 0.13f;
        private const float FlatHeight = 0.02f;
        private const float MapTilt = 50f;
        private const int MapCells = FeatureMeaningExample.MapSize * FeatureMeaningExample.MapSize;
        private static readonly Vector3 PicturePosition = new Vector3(-1.15f, 0.02f, 0f);
        private static readonly Vector3[] MapPositions = { new Vector3(-0.2f, -0.05f, 0f), new Vector3(0.6f, -0.05f, 0f) };
        private static readonly Vector3 NextStagePosition = new Vector3(1.38f, 0f, 0f);
        private static readonly Color StrokeColor = new Color(0.90f, 0.93f, 0.95f);

        private readonly Transform _window;
        private readonly MeshRenderer[] _filterCells = new MeshRenderer[9];
        private readonly Text _filterName;
        private readonly Transform[] _maps = new Transform[FeatureMeaningExample.FilterCount];
        private readonly GameObject[] _mapLabels = new GameObject[FeatureMeaningExample.FilterCount];
        private readonly Transform[,] _bars = new Transform[FeatureMeaningExample.FilterCount, MapCells];
        private readonly MeshRenderer[,] _barFaces = new MeshRenderer[FeatureMeaningExample.FilterCount, MapCells];
        private readonly Text _verdict;
        private readonly Text _verdictNote;

        public FeatureMeaningDisplayController(WorkbenchPrimitiveFactory factory, Transform anchor)
        {
            Transform picture = factory.Root("Pattern picture");
            picture.SetParent(anchor, false);
            picture.localPosition = PicturePosition;
            float pictureSide = Pitch * FeatureMeaningExample.PictureSize;
            factory.Frame(picture, pictureSide + 0.04f, pictureSide + 0.04f, 0.04f, WorkbenchColors.PanelFrame, 0.012f);
            for (int row = 0; row < FeatureMeaningExample.PictureSize; row++)
            {
                for (int column = 0; column < FeatureMeaningExample.PictureSize; column++)
                {
                    Color color = FeatureMeaningExample.IsStroke(row, column) ? StrokeColor : WorkbenchColors.PanelFace;
                    factory.Tile(picture, Cell(row, column, FeatureMeaningExample.PictureSize), new Vector3(0.12f, 0.12f, 0.03f), color);
                }
            }
            _window = factory.Highlight("Filter sweep window", Pitch * 3f, Pitch * 3f);
            _window.SetParent(picture, false);

            float cardHeight = pictureSide * 0.5f + 0.17f;
            Transform card = factory.Root("Filter card");
            card.SetParent(picture, false);
            card.localPosition = new Vector3(-0.28f, cardHeight, 0f);
            for (int index = 0; index < _filterCells.Length; index++)
            {
                Transform tile = factory.Tile(card, new Vector3((index % 3 - 1) * 0.075f, (1 - index / 3) * 0.075f, 0f),
                    new Vector3(0.068f, 0.068f, 0.02f), WorkbenchColors.PanelFace);
                _filterCells[index] = tile.Find("Tile mesh").GetComponent<MeshRenderer>();
            }
            _filterName = factory.Caption(picture, "", new Vector3(0.16f, cardHeight, 0f), 0.6f);
            _filterName.fontSize = 44;

            float mapSide = Pitch * FeatureMeaningExample.MapSize;
            for (int filter = 0; filter < _maps.Length; filter++)
            {
                Transform map = factory.Root("Feature map " + filter);
                map.SetParent(anchor, false);
                factory.Frame(map, mapSide + 0.04f, mapSide + 0.04f, 0.03f, WorkbenchColors.PanelFrame, 0.01f);
                for (int index = 0; index < MapCells; index++)
                {
                    Transform tile = factory.Tile(map,
                        Cell(index / FeatureMeaningExample.MapSize, index % FeatureMeaningExample.MapSize, FeatureMeaningExample.MapSize),
                        new Vector3(0.11f, 0.11f, FlatHeight), WorkbenchColors.PanelFace);
                    _bars[filter, index] = tile.Find("Tile mesh");
                    _barFaces[filter, index] = _bars[filter, index].GetComponent<MeshRenderer>();
                }
                _maps[filter] = map;
                Text label = factory.Caption(anchor, FeatureMeaningExample.FilterName(filter) + "의 출력",
                    MapPositions[filter] + new Vector3(0f, 0.52f, 0f), 0.75f);
                label.fontSize = 36;
                _mapLabels[filter] = label.transform.parent.gameObject;
            }

            Transform next = factory.Root("Next stage");
            next.SetParent(anchor, false);
            next.localPosition = NextStagePosition;
            factory.Tile(next, Vector3.zero, new Vector3(0.44f, 0.44f, 0.05f), WorkbenchColors.PanelFace);
            factory.Frame(next, 0.46f, 0.46f, 0.06f, WorkbenchColors.PanelFrame, 0.012f);
            Text heading = factory.Caption(next, "다음 단계", new Vector3(0f, 0.34f, 0f), 0.6f);
            heading.fontSize = 38;
            _verdict = factory.Caption(next, "", new Vector3(0f, 0f, -0.04f), 0.44f);
            _verdict.rectTransform.sizeDelta = new Vector2(440f, 220f);
            _verdict.fontSize = 150;
            _verdictNote = factory.Caption(next, "", new Vector3(0f, -0.34f, 0f), 0.7f);
            _verdictNote.fontSize = 36;
        }

        public void Prepare()
        {
            _window.gameObject.SetActive(false);
            for (int filter = 0; filter < _maps.Length; filter++)
            {
                _maps[filter].localPosition = MapPositions[filter];
                _maps[filter].localRotation = Quaternion.Euler(MapTilt, 0f, 0f);
                _maps[filter].localScale = Vector3.one;
                _maps[filter].gameObject.SetActive(false);
                _mapLabels[filter].SetActive(false);
                for (int index = 0; index < MapCells; index++) { RaiseBar(filter, index, 0); }
            }
            _verdict.text = "?";
            _verdict.color = WorkbenchColors.PendingValue;
            _verdictNote.text = "";
            ShowFilter(0);
        }

        // Shows the filter's pattern on the card and reveals the bar map it will fill.
        public void ShowFilter(int filter)
        {
            for (int index = 0; index < _filterCells.Length; index++)
            {
                bool marked = FeatureMeaningExample.IsFilterCell(filter, index / 3, index % 3);
                WorkbenchPrimitiveFactory.Tint(_filterCells[index], marked ? WorkbenchColors.PlacementMint : WorkbenchColors.PanelFace);
            }
            _filterName.text = FeatureMeaningExample.FilterName(filter);
            _maps[filter].gameObject.SetActive(true);
            _mapLabels[filter].SetActive(true);
        }

        public void MoveWindow(int row, int column)
        {
            _window.localPosition = Cell(row + 1, column + 1, FeatureMeaningExample.PictureSize) + Vector3.back * 0.03f;
            _window.gameObject.SetActive(true);
        }

        public void HideWindow() => _window.gameObject.SetActive(false);

        // A stronger response is a taller, brighter bar rising out of the tilted map.
        public void RaiseBar(int filter, int index, int response)
        {
            float height = FlatHeight + 0.03f * response * response;
            Transform bar = _bars[filter, index];
            bar.localScale = new Vector3(0.11f, 0.11f, height);
            bar.localPosition = new Vector3(0f, 0f, -height * 0.5f);
            WorkbenchPrimitiveFactory.Tint(_barFaces[filter, index],
                Color.Lerp(WorkbenchColors.PanelFace, WorkbenchColors.CalculationAccent, response / 3f));
        }

        public void SendMapsToNextStage(float progress)
        {
            for (int filter = 0; filter < _maps.Length; filter++)
            {
                _maps[filter].localPosition = Vector3.Lerp(MapPositions[filter], NextStagePosition, progress);
                _maps[filter].localScale = Vector3.one * Mathf.Lerp(1f, 0.12f, progress);
            }
        }

        public void ShowVerdict()
        {
            for (int filter = 0; filter < _maps.Length; filter++)
            {
                _maps[filter].gameObject.SetActive(false);
                _mapLabels[filter].SetActive(false);
            }
            _verdict.text = "N";
            _verdict.color = WorkbenchColors.CalculationAccent;
            _verdictNote.text = "이 그림은 N";
        }

        private static Vector3 Cell(int row, int column, int size) =>
            new Vector3((column - size / 2) * Pitch, (size / 2 - row) * Pitch, 0f);
    }
}

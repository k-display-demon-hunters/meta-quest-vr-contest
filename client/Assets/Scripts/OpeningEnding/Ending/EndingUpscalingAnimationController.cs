using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace OpeningEnding.Ending
{
    /// <summary>Spatial explanation of CNN upscaling using the village's frozen news image.</summary>
    public sealed class EndingUpscalingAnimationController : MonoBehaviour
    {
        private const int Columns = 16;
        private const int Rows = 9;
        private const int MapColumns = Columns - 2;
        private const int MapRows = Rows - 2;
        private const int ExampleColumn = 3;
        private const int ExampleRow = 2;
        private static readonly Vector3 ScreenCenter = new Vector3(0f, 3.2f, 5.762f);
        private static readonly Vector3 InputCenter = new Vector3(-1.25f, 2.85f, 3.6f);
        private static readonly Vector3 FirstMapCenter = new Vector3(1.25f, 2.85f, 4f);
        private static readonly Vector3 FinalOutputCenter = new Vector3(0f, 3f, 4f);
        private static readonly Vector2 ScreenSize = new Vector2(5.04f, 2.835f);
        private static readonly Vector2 InputSize = new Vector2(2.35f, 1.321875f);
        private static readonly Color Teal = new Color(.24f, .95f, .82f);
        private static readonly Color Amber = new Color(1f, .7f, .22f);
        private static readonly Color[] MapColors = { Teal, new Color(.36f, .64f, 1f), new Color(1f, .58f, .28f) };

        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _stageLabel;
        [SerializeField] private Text _calculation;
        [SerializeField] private Material _broadcastMaterial;
        [SerializeField] private Material _tileMaterial;

        private Material _broadcast;
        private EndingPixelTileMesh _input;
        private EndingPixelTileMesh _output;
        private EndingPixelTileMesh _filter;
        private EndingPixelTileMesh _flow;
        private readonly EndingPixelTileMesh[] _maps = new EndingPixelTileMesh[3];
        private readonly float[][] _responses = new float[3][];
        private int _visibleStage = -1;
        private int _lastTerm = -1;

        private void Awake()
        {
            _broadcast = new Material(_broadcastMaterial) { name = "Ending frozen news tiles" };
            _broadcast.SetFloat("_PlaybackTime", 3f);
            _broadcast.SetFloat("_SignalTime", 3f);
            _broadcast.SetFloat("_Channel", 0f);
            _broadcast.SetFloat("_Phase", 0f);
            _broadcast.SetFloat("_Damage", 0f);
            _broadcast.SetFloat("_Interference", 0f);
            _broadcast.SetFloat("_UpscalingPreview", 0f);
            _input = new EndingPixelTileMesh(transform, "Image pixel tiles", Columns, Rows, _broadcast, _tileMaterial, true);
            _output = new EndingPixelTileMesh(transform, "High resolution image tiles", Columns * 2, Rows * 2, _broadcast, _tileMaterial, false);
            _filter = new EndingPixelTileMesh(transform, "Convolution filter frame", 8, 1, _tileMaterial, _tileMaterial, true);
            _flow = new EndingPixelTileMesh(transform, "Feature transfer", 12, 1, _tileMaterial, _tileMaterial, true);
            for (int map = 0; map < _maps.Length; map++)
            {
                _maps[map] = new EndingPixelTileMesh(transform, "Feature layer " + (map + 1), MapColumns, MapRows,
                    _tileMaterial, _tileMaterial, true);
                _responses[map] = EndingUpscalingSample.CalculateMap(map);
            }
            Hide();
        }

        public void BindCamera(Camera camera) => _canvas.worldCamera = camera;

        public void ShowExtraction(float progress)
        {
            ShowStage(0, string.Empty);
            _input.SetVisible(true);
            for (int tile = 0; tile < Columns * Rows; tile++)
            {
                float delay = (tile % Columns) * .008f + (tile / Columns) * .01f;
                float travel = Ease(Mathf.InverseLerp(delay, .78f + delay, progress));
                Vector3 source = GridPoint(tile, Columns, Rows, ScreenCenter, ScreenSize);
                Vector3 destination = GridPoint(tile, Columns, Rows, InputCenter, InputSize * 1.04f);
                Vector3 position = Vector3.Lerp(source, destination, travel);
                position.y += Mathf.Sin(travel * Mathf.PI) * .32f;
                position.z -= Mathf.Sin(travel * Mathf.PI) * .35f;
                Vector2 tileSize = new Vector2(Mathf.Lerp(ScreenSize.x / Columns, InputSize.x / Columns, travel),
                    Mathf.Lerp(ScreenSize.y / Rows, InputSize.y / Rows, travel));
                Quaternion rotation = Quaternion.Euler(Mathf.Sin(travel * Mathf.PI) * -12f,
                    Mathf.Sin(travel * Mathf.PI) * (tile % Columns - 7.5f) * 3f, 0f);
                _input.SetTile(tile, position, rotation, tileSize, Mathf.Lerp(.005f, .09f, travel), Color.white, Teal * .5f, .06f * travel);
            }
            _input.Upload();
        }

        public void ShowConvolution(float progress)
        {
            ShowStage(1, "합성곱");
            _filter.SetVisible(true);
            _maps[0].SetVisible(true);
            float scan = Mathf.InverseLerp(.56f, 1f, progress) * (MapColumns * MapRows - 1);
            int scanCell = Mathf.FloorToInt(scan);
            int nextCell = Mathf.Min(scanCell + 1, MapColumns * MapRows - 1);
            bool demonstrate = progress < .56f;
            Vector2 patch = demonstrate ? new Vector2(ExampleColumn, ExampleRow)
                : Vector2.Lerp(new Vector2(scanCell % MapColumns, scanCell / MapColumns),
                    new Vector2(nextCell % MapColumns, nextCell / MapColumns), Ease(scan - scanCell));
            if (progress >= .49f && progress < .56f)
            {
                patch = Vector2.Lerp(new Vector2(ExampleColumn, ExampleRow), Vector2.zero,
                    Ease(Mathf.InverseLerp(.49f, .56f, progress)));
            }
            int term = Mathf.Min(8, Mathf.FloorToInt(Mathf.InverseLerp(.03f, .43f, progress) * 9f));
            float termPulse = Mathf.Sin(Mathf.Repeat(Mathf.InverseLerp(.03f, .43f, progress) * 9f, 1f) * Mathf.PI);
            DrawInput(InputCenter, InputSize, 1f, demonstrate ? term : -1, termPulse);
            DrawFilter(patch, demonstrate && progress < .49f ? 1f : .85f);
            DrawMap(0, FirstMapCenter, new Vector2(1.95f, 1.06f), Quaternion.Euler(0f, -8f, 0f),
                demonstrate ? (progress >= .43f ? 1f : 0f) : scan + 1f, demonstrate);
            DrawContributions(progress);
            if (progress < .49f)
            {
                int calculationStep = progress >= .43f ? 9 : term;
                if (_lastTerm != calculationStep)
                {
                    _lastTerm = calculationStep;
                    float sum = EndingUpscalingSample.SumTerms(ExampleColumn, ExampleRow, Mathf.Min(calculationStep + 1, 9));
                    if (calculationStep < 9)
                    {
                        float value = EndingUpscalingSample.Value(ExampleColumn + term % 3, ExampleRow + term / 3);
                        float weight = EndingUpscalingSample.Weight(0, term);
                        _calculation.text = string.Format(CultureInfo.InvariantCulture,
                            "{0:0.00} × ({1:0.00}) = {2:0.000}\n곱한 값의 합  {3:0.000}", value, weight, value * weight, sum);
                    }
                    else
                    {
                        _calculation.text = string.Format(CultureInfo.InvariantCulture,
                            "Σ (픽셀 × 가중치) = {0:0.000}", sum);
                    }
                }
                _calculation.color = new Color(1f, .91f, .7f, Mathf.InverseLerp(0f, .04f, progress));
            }
            else
            {
                _calculation.color = new Color(1f, .91f, .7f, 1f - Mathf.InverseLerp(.49f, .56f, progress));
            }
        }

        public void ShowFeatureMaps(float progress)
        {
            ShowStage(2, "특징 맵");
            float spread = Ease(Mathf.InverseLerp(0f, .7f, progress));
            DrawInput(Vector3.Lerp(InputCenter, new Vector3(-2.1f, 3.85f, 5.1f), spread),
                Vector2.Lerp(InputSize, new Vector2(1.3f, .73125f), spread), 1f, -1);
            _filter.SetVisible(false);
            _flow.SetVisible(false);
            for (int map = 0; map < 3; map++)
            {
                _maps[map].SetVisible(true);
                float reveal = map == 0 ? 1f : Ease(Mathf.InverseLerp(.08f + map * .11f, .45f + map * .12f, progress));
                Vector3 position = Vector3.Lerp(FirstMapCenter, FanCenter(map), spread);
                position.z -= Mathf.Sin(spread * Mathf.PI) * map * .18f;
                Quaternion rotation = Quaternion.Slerp(Quaternion.Euler(0f, -8f, 0f), FanRotation(map), spread);
                DrawMap(map, position, Vector2.Lerp(new Vector2(1.95f, 1.06f), new Vector2(1.6f, .9f), spread) * reveal,
                    rotation, MapColumns * MapRows, false);
            }
        }

        public void ShowPrediction(float progress)
        {
            ShowStage(3, "고해상도 예측");
            _output.SetVisible(true);
            _flow.SetVisible(true);
            float gather = Ease(Mathf.InverseLerp(0f, .24f, progress));
            float finish = Ease(Mathf.InverseLerp(.82f, 1f, progress));
            DrawInput(new Vector3(-2.1f, 3.85f, 5.1f), new Vector2(1.3f, .73125f), 1f - gather, -1);
            for (int map = 0; map < 3; map++)
            {
                Vector3 position = Vector3.Lerp(FanCenter(map), new Vector3(-1.75f + map * .12f, 2.8f + map * .12f, 3.65f + map * .3f), gather);
                Quaternion rotation = Quaternion.Slerp(FanRotation(map), Quaternion.Euler(0f, -18f, 0f), gather);
                DrawMap(map, position, Vector2.Lerp(new Vector2(1.6f, .9f), new Vector2(1.25f, .7f), gather) * (1f - finish),
                    rotation, MapColumns * MapRows, false);
            }
            Vector3 outputCenter = Vector3.Lerp(new Vector3(1.02f, 2.85f, 3.65f), FinalOutputCenter, finish);
            Vector2 outputSize = Vector2.Lerp(new Vector2(2.6f, 1.4625f), new Vector2(3.2f, 1.8f), finish);
            for (int tile = 0; tile < Columns * Rows * 4; tile++)
            {
                float delay = (tile % 32) * .011f + (tile / 32) * .008f;
                float travel = Ease(Mathf.InverseLerp(.08f + delay, .4f + delay, progress));
                Vector3 source = new Vector3(-1.3f, 2.85f + Mathf.Sin(tile * .7f) * .24f, 3.8f);
                Vector3 destination = GridPoint(tile, 32, 18, outputCenter, outputSize);
                Vector3 position = Vector3.Lerp(source, destination, travel);
                position.y += Mathf.Sin(travel * Mathf.PI) * .36f;
                position.z -= Mathf.Sin(travel * Mathf.PI) * .35f;
                Vector2 tileSize = new Vector2(outputSize.x / 32f, outputSize.y / 18f) * Ease(Mathf.InverseLerp(0f, .3f, travel));
                _output.SetTile(tile, position, Quaternion.Euler(0f, (1f - travel) * -45f, 0f), tileSize,
                    .065f * travel, Color.white, Teal * .55f, .06f);
            }
            _output.Upload();
            for (int item = 0; item < 12; item++)
            {
                float travel = Mathf.Repeat(progress * 3f + item / 12f, 1f);
                Vector3 source = new Vector3(-1.25f, 2.85f + (item % 3 - 1) * .2f, 3.7f + item % 3 * .2f);
                Vector3 destination = new Vector3(.05f, 2.85f, 3.6f);
                Vector3 position = Vector3.Lerp(source, destination, Ease(travel));
                position.y += Mathf.Sin(travel * Mathf.PI) * (.15f + item % 3 * .08f);
                float envelope = Mathf.Sin(travel * Mathf.PI) * gather * (1f - finish);
                _flow.SetTile(item, position, Quaternion.identity, new Vector2(.1f, .025f) * envelope,
                    .02f * envelope, MapColors[item % 3], MapColors[item % 3]);
            }
            _flow.Upload();
        }

        public void ShowReturn(float progress)
        {
            ShowStage(4, string.Empty);
            _flow.SetVisible(false);
            _input.SetVisible(false);
            for (int map = 0; map < 3; map++)
            {
                _maps[map].SetVisible(false);
            }
            float wipe = Ease(progress);
            for (int tile = 0; tile < Columns * Rows * 4; tile++)
            {
                float column = (tile % 32 + .5f) / 32f;
                float travel = Ease(Mathf.InverseLerp(Mathf.Max(0f, column - .22f), column, wipe));
                // A tile docks just as the display's matching column is revealed, then disappears into it.
                float visible = 1f - Ease(Mathf.InverseLerp(column, column + .018f, wipe));
                if (progress >= 1f)
                {
                    visible = 0f;
                }
                Vector3 source = GridPoint(tile, 32, 18, FinalOutputCenter, new Vector2(3.2f, 1.8f));
                Vector3 destination = GridPoint(tile, 32, 18, ScreenCenter + Vector3.back * .004f, ScreenSize);
                Vector3 position = Vector3.Lerp(source, destination, travel);
                position.y += Mathf.Sin(travel * Mathf.PI) * .2f;
                position.z -= Mathf.Sin(travel * Mathf.PI) * .2f;
                Vector2 size = Vector2.Lerp(new Vector2(3.2f / 32f, 1.8f / 18f), new Vector2(ScreenSize.x / 32f, ScreenSize.y / 18f), travel) * visible;
                _output.SetTile(tile, position, Quaternion.identity, size, Mathf.Lerp(.065f, .001f, travel) * visible,
                    Color.white, Teal * .55f, .06f * (1f - travel));
            }
            _output.Upload();
        }

        public void Hide()
        {
            _canvas.enabled = false;
            _input.SetVisible(false);
            _output.SetVisible(false);
            _filter.SetVisible(false);
            _flow.SetVisible(false);
            for (int map = 0; map < 3; map++)
            {
                _maps[map].SetVisible(false);
            }
            _visibleStage = -1;
            _lastTerm = -1;
        }

        private void ShowStage(int stage, string label)
        {
            if (_visibleStage == stage)
            {
                return;
            }
            _visibleStage = stage;
            _canvas.enabled = true;
            _stageLabel.text = label;
            _calculation.text = string.Empty;
            _lastTerm = -1;
        }

        private void DrawInput(Vector3 center, Vector2 size, float scale, int highlightedTerm, float highlightStrength = 1f)
        {
            for (int tile = 0; tile < Columns * Rows; tile++)
            {
                Vector3 position = GridPoint(tile, Columns, Rows, center, size * 1.04f);
                bool highlight = highlightedTerm >= 0 && tile == (ExampleRow + highlightedTerm / 3) * Columns + ExampleColumn + highlightedTerm % 3;
                position.z -= highlight ? .12f * highlightStrength : 0f;
                _input.SetTile(tile, position, Quaternion.identity, new Vector2(size.x / Columns, size.y / Rows) * scale,
                    .09f * scale, Color.white, highlight ? Color.Lerp(Teal * .5f, Amber, highlightStrength) : Teal * .5f);
            }
            _input.Upload();
        }

        private void DrawFilter(Vector2 patch, float strength)
        {
            Vector2 step = new Vector2(InputSize.x * 1.04f / Columns, InputSize.y * 1.04f / Rows);
            Vector3 center = InputCenter + new Vector3((patch.x + 1.5f - Columns * .5f) * step.x,
                (Rows * .5f - patch.y - 1.5f) * step.y, -.19f);
            for (int line = 0; line < 8; line++)
            {
                bool vertical = line >= 4;
                int edge = line % 4;
                Vector3 position = center + (vertical ? Vector3.right * (edge - 1.5f) * step.x : Vector3.up * (edge - 1.5f) * step.y);
                Vector2 size = vertical ? new Vector2(.012f, step.y * 3f) : new Vector2(step.x * 3f, .012f);
                _filter.SetTile(line, position, Quaternion.identity, size, .024f, Amber * strength, Amber);
            }
            _filter.Upload();
        }

        private void DrawContributions(float progress)
        {
            _flow.SetVisible(progress < .49f);
            if (progress >= .49f)
            {
                return;
            }
            int exampleCell = ExampleRow * MapColumns + ExampleColumn;
            Vector3 destination = FirstMapCenter + Quaternion.Euler(0f, -8f, 0f)
                * (GridPoint(exampleCell, MapColumns, MapRows, Vector3.zero, new Vector2(1.95f, 1.06f))
                   + Vector3.back * Mathf.Abs(_responses[0][exampleCell]) * .2f);
            for (int term = 0; term < 12; term++)
            {
                float travel = Mathf.InverseLerp(.03f + term * .044f, .03f + (term + 1) * .044f, progress);
                int sample = (ExampleRow + term / 3) * Columns + ExampleColumn + term % 3;
                Vector3 source = GridPoint(sample, Columns, Rows, InputCenter, InputSize * 1.04f) + Vector3.back * .13f;
                Vector3 position = Vector3.Lerp(source, destination, Ease(travel));
                position.y += Mathf.Sin(travel * Mathf.PI) * .26f;
                position.z -= Mathf.Sin(travel * Mathf.PI) * .3f;
                float size = term < 9 ? Mathf.Sin(travel * Mathf.PI) * .085f : 0f;
                Color color = Color.Lerp(Amber, Teal, travel);
                _flow.SetTile(term, position, Quaternion.Euler(0f, travel * 90f, travel * 90f),
                    Vector2.one * size, size * .6f, color, color);
            }
            _flow.Upload();
        }

        private void DrawMap(int map, Vector3 center, Vector2 size, Quaternion rotation, float filled, bool onlyExample)
        {
            for (int cell = 0; cell < MapColumns * MapRows; cell++)
            {
                float reveal = onlyExample ? (cell == ExampleRow * MapColumns + ExampleColumn ? filled : 0f) : Mathf.Clamp01(filled - cell);
                if (!onlyExample && map == 0 && cell == ExampleRow * MapColumns + ExampleColumn)
                {
                    reveal = 1f;
                }
                float response = Mathf.Clamp01(Mathf.Abs(_responses[map][cell]) * 2.5f);
                Vector3 flat = GridPoint(cell, MapColumns, MapRows, Vector3.zero, size);
                Vector3 position = center + rotation * (flat + Vector3.back * response * .08f);
                Color face = Color.Lerp(new Color(.035f, .075f, .1f), MapColors[map], response);
                _maps[map].SetTile(cell, position, rotation, new Vector2(size.x / MapColumns, size.y / MapRows) * .93f * reveal,
                    .06f * reveal, face, MapColors[map] * .42f);
            }
            _maps[map].Upload();
        }

        private static Vector3 GridPoint(int tile, int columns, int rows, Vector3 center, Vector2 size)
            => center + new Vector3(((tile % columns + .5f) / columns - .5f) * size.x,
                (.5f - (tile / columns + .5f) / rows) * size.y, 0f);

        private static Vector3 FanCenter(int map) => new Vector3((map - 1) * 1.65f, 2.85f + map * .08f, 3.7f + map * .3f);
        private static Quaternion FanRotation(int map) => Quaternion.Euler(0f, (map - 1) * -16f, 0f);
        private static float Ease(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

        private void OnDestroy()
        {
            _input.Dispose();
            _output.Dispose();
            _filter.Dispose();
            _flow.Dispose();
            for (int map = 0; map < 3; map++)
            {
                _maps[map].Dispose();
            }
            Destroy(_broadcast);
        }
    }
}

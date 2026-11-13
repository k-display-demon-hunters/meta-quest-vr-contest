using UnityEngine;
using UnityEngine.UI;

namespace CNNConvolution
{
    internal sealed class InputGridController
    {
        internal static readonly Vector3 CellSize = new Vector3(0.17f, 0.15f, 0.12f);
        private static readonly string[] Names = { "R", "G", "B" };
        private readonly Transform[] _layers = new Transform[3];
        private readonly Text[] _headings = new Text[3];
        private readonly Text[,] _values = new Text[3, 25];
        private readonly MeshRenderer[,] _inputFaces = new MeshRenderer[3, 25];
        private readonly Transform[,] _tiles = new Transform[3, 25];
        private readonly Transform[] _windows = new Transform[3];
        private readonly Transform _anchor;
        private readonly WorkbenchPrimitiveFactory _factory;

        public InputGridController(WorkbenchPrimitiveFactory factory, Transform anchor)
        {
            _anchor = anchor;
            _factory = factory;
            Transform front = factory.Root(Names[0] + " input slice");
            Transform headingPlate = factory.PanelHeading(front);
            for (int c = 0; c < 3; c++)
            {
                _layers[c] = c == 0 ? front : factory.Root(Names[c] + " input slice");
                _layers[c].SetParent(anchor, false);
                _layers[c].localPosition = ChannelOffset(c);
                factory.Frame(_layers[c], 0.97f, 0.86f, 0.055f, WorkbenchColors.PanelFrame, 0.012f);
                // Open frames preserve the channel depth without a solid plate hiding the next layer.
                _windows[c] = factory.Highlight(Names[c] + " receptive field", 0.59f, 0.53f);
                _windows[c].SetParent(_layers[c], false);
                Text heading = factory.Caption(headingPlate, Names[c] + " 입력 · 5×5", new Vector3(0, 0, -0.027f), 0.88f);
                heading.color = WorkbenchColors.Channel(c);
                heading.fontSize = WorkbenchColors.HeadingFontSize;
                _headings[c] = heading;
                Text leftSide = factory.Caption(_layers[c], Names[c], new Vector3(-0.48f, 0.40f, 0), 0.16f);
                leftSide.transform.parent.localRotation = Quaternion.Euler(0, 90, 0);
                Text rightSide = factory.Caption(_layers[c], Names[c], new Vector3(0.48f, 0.40f, 0), 0.16f);
                rightSide.transform.parent.localRotation = Quaternion.Euler(0, -90, 0);
                for (int i = 0; i < 25; i++)
                {
                    Transform tile = factory.Tile(_layers[c], WorkbenchPrimitiveFactory.GridCellPosition(i, 5), CellSize, Color.gray);
                    _tiles[c, i] = tile;
                    WorkbenchPrimitiveFactory.Tint(tile.Find("Tile mesh").GetComponent<MeshRenderer>(), WorkbenchColors.PanelFace);
                    Transform face = factory.Tile(tile, new Vector3(0, 0, -0.063f), new Vector3(0.158f, 0.138f, 0.012f), Color.gray);
                    _inputFaces[c, i] = face.Find("Tile mesh").GetComponent<MeshRenderer>();
                    // A neutral number inset keeps zero and bright green equally readable.
                    factory.Tile(tile, new Vector3(0, -0.008f, -0.073f), new Vector3(0.095f, 0.105f, 0.008f), new Color(0.025f, 0.035f, 0.045f));
                    _values[c, i] = factory.Caption(tile, "", new Vector3(0, -0.008f, -0.08f), 0.095f);
                    _values[c, i].fontSize = 48;
                }
            }
        }

        public void Prepare(ConvolutionCalculation calculation)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 25; i++)
                {
                    _values[c, i].text = calculation.GridInput(c, i).ToString();
                    float strength = 0.30f + calculation.GridInput(c, i) / 3f * 0.35f;
                    Color color = Color.Lerp(WorkbenchColors.PanelFace, WorkbenchColors.Channel(c), strength);
                    WorkbenchPrimitiveFactory.Tint(_inputFaces[c, i], color);
                    _values[c, i].color = Color.white;
                }
            }
            ShowChannel(0);
            ShowWindow(0, 0);
        }

        internal static Vector3 ChannelOffset(int channel) => new Vector3(-0.34f * channel, 0.06f * channel, 0.30f * channel);

        public void ShowChannel(int channel)
        {
            for (int other = 0; other < _layers.Length; other++)
            {
                _headings[other].enabled = other == channel;
                // Inactive channels retain their side labels, frame and matching window; only the
                // active channel has solid number tiles, so front layers cannot occlude it.
                for (int index = 0; index < 25; index++)
                {
                    _tiles[other, index].gameObject.SetActive(other == channel);
                }
            }
        }

        public void ShowWindow(int row, int column)
        {
            foreach (Transform window in _windows)
            {
                window.localPosition = new Vector3((column - 1) * 0.19f, (1 - row) * 0.17f, -0.09f);
                window.gameObject.SetActive(true);
            }
        }

        public Vector3 CellPosition(int channel, int index) => _factory.LocalPoint(_values[channel, index].transform.position);

        public void AttachHighlight(Transform highlight, int channel, int index)
        {
            highlight.SetParent(_layers[channel], false);
            highlight.localPosition = WorkbenchPrimitiveFactory.GridCellPosition(index, 5) + Vector3.back * 0.083f;
            highlight.gameObject.SetActive(true);
        }

        internal static Vector3 WindowOffset(int row, int column) =>
            new Vector3((column - 1) * 0.19f, (1 - row) * 0.17f, -0.10f);

        public Pose WindowPose(int row, int column) =>
            new Pose(_anchor.TransformPoint(WindowOffset(row, column)), _anchor.rotation);

    }
}

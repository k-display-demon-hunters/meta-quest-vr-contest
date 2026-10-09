using UnityEngine;

namespace CNNLearning
{
    /// <summary>
    /// An isolated, four-step lesson over a fixed, fully connected 3 x 3 convolution demo.
    /// It visualizes arithmetic and structural receptive fields; it is not a trained classifier.
    /// </summary>
    public sealed class CNNLearningGuide : MonoBehaviour
    {
        [SerializeField] private Transform _inputRoot;
        [SerializeField] private Transform[] _layerRoots;
        [SerializeField] private Transform[] _outputRoots;
        [SerializeField] private Material _toolMaterial;
        [SerializeField] private Font _guideFont;
        private int _spreadLayer;
        private const float CellSpacing = .066f;
        private readonly bool[,,] _visited = new bool[2, 8, 8];
        private readonly CNNLearningNode[,,] _firstNodes = new CNNLearningNode[2, 8, 8];
        private readonly CNNLearningButton[] _buttons = new CNNLearningButton[6];
        private readonly TextMesh[] _buttonLabels = new TextMesh[6];
        private CNNLearningScene _scene;
        private CNNLearningNode[] _nodes;
        private CNNLearningPanel[] _panels;
        private CNNLearningLayer[] _layers;
        private CNNLearningGrabTarget _filter;
        private Rigidbody _filterBody;
        private LineRenderer _connection;
        private GameObject _controls;
        private GameObject _filterCard;
        private TextMesh _title;
        private TextMesh _instruction;
        private TextMesh _readout;
        private TextMesh _weights;
        private TextMesh _filterName;
        private Font _font;
        private Texture2D _originalImage;
        private int _step;
        private int _channel;
        private int _row = 3;
        private int _column = 3;
        private int _scanIndex;
        private bool _scanning;
        private bool _initialized;
        private float _nextScan;
        private int _editCount;

        public void Initialize(CNNLearningScene scene, CNNLearningNode[] nodes)
        {
            _scene = scene;
            _nodes = nodes;
            _panels = GetComponentsInChildren<CNNLearningPanel>(true);
            _layers = GetComponentsInChildren<CNNLearningLayer>(true);
            foreach (CNNLearningNode node in nodes)
            {
                if (node.Layer == 0)
                {
                    _firstNodes[node.Channel, node.Row, node.Column] = node;
                }
            }
            // Use the existing project's readable example asset, without changing it.
            // A private texture snapshot lets the input editing lesson return to its starting image.
            _originalImage = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[64];
            for (int row = 0; row < 8; row++)
            {
                for (int column = 0; column < 8; column++)
                {
                    float value = scene.Data.ReadValue(-1, 0, row, column);
                    pixels[(7 - row) * 8 + column] = new Color(value, value, value);
                }
            }
            _originalImage.SetPixels(pixels);
            _originalImage.Apply();
            _font = _guideFont != null ? _guideFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateFilter();
            CreateControls();
            _initialized = true;
            ApplyStep();
        }

        private void CreateFilter()
        {
            GameObject filter = new GameObject("Drag this 3 x 3 filter");
            filter.SetActive(false);
            filter.transform.SetParent(_inputRoot, false);
            filter.transform.localPosition = GridPosition(_row, _column, -.075f);
            float side = CellSpacing * 3f;
            for (int index = 0; index < 4; index++)
            {
                float offset = (index - 1.5f) * CellSpacing;
                CreateShape("Filter row", PrimitiveType.Cube, filter.transform,
                    new Vector3(0f, offset, 0f), new Vector3(side, .004f, .004f), new Color(.2f, .95f, .7f), false);
                CreateShape("Filter column", PrimitiveType.Cube, filter.transform,
                    new Vector3(offset, 0f, 0f), new Vector3(.004f, side, .004f), new Color(.2f, .95f, .7f), false);
            }
            GameObject handle = CreateShape("Mint filter handle", PrimitiveType.Sphere, filter.transform,
                new Vector3(-.14f, 0f, -.018f), Vector3.one * .055f, new Color(.2f, .95f, .7f), true);
            filter.AddComponent<Rigidbody>();
            _filter = filter.AddComponent<CNNLearningGrabTarget>();
            _filter.ConfigureFilter(_scene, handle.GetComponent<Collider>());
            filter.SetActive(true);
            _filterBody = filter.GetComponent<Rigidbody>();
            GameObject connection = new GameObject("Patch to output neuron");
            connection.transform.SetParent(transform, false);
            _connection = connection.AddComponent<LineRenderer>();
            _connection.sharedMaterial = _toolMaterial;
            _connection.positionCount = 2;
            _connection.useWorldSpace = true;
            _connection.startWidth = .006f;
            _connection.endWidth = .006f;
            _connection.startColor = _connection.endColor = new Color(1f, .83f, .22f);
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(1f, .83f, .22f));
            properties.SetColor("_EmissionColor", new Color(.4f, .3f, .02f));
            _connection.SetPropertyBlock(properties);
        }

        private void CreateControls()
        {
            _controls = new GameObject("Small lesson controls");
            _controls.transform.SetParent(transform, false);
            _title = CreateText("Lesson", _controls.transform, new Vector3(.5f, 1.02f, 2.72f), 2.8f, .07f);
            _instruction = CreateText("One instruction", _controls.transform, new Vector3(.5f, .88f, 2.72f), 2.8f, .12f);
            _readout = CreateText("Live result", _controls.transform, new Vector3(.5f, .71f, 2.72f), 2.8f, .11f);
            string[] labels = { "이전", "다음", "자동 이동", "필터 바꾸기", "펼치기", "처음 상태" };
            for (int index = 0; index < labels.Length; index++)
            {
                GameObject button = CreateShape(labels[index], PrimitiveType.Cube, _controls.transform,
                    new Vector3(-.6f + index * .44f, .48f, 2.72f), new Vector3(.38f, .12f, .04f), Color.white, true);
                button.SetActive(false);
                _buttons[index] = button.AddComponent<CNNLearningButton>();
                _buttons[index].Configure(this, (CNNLearningAction)index);
                button.SetActive(true);
                _buttonLabels[index] = CreateText(labels[index] + " label", _controls.transform,
                    new Vector3(-.6f + index * .44f, .48f, 2.695f), .31f, .042f);
                SetText(_buttonLabels[index], labels[index], .31f, .042f);
            }
            TextMesh help = CreateText("Minimal controls", _controls.transform, new Vector3(.5f, .29f, 2.72f), 2.8f, .08f);
            SetText(help, "PC: 클릭·드래그 / Shift+휠: 확대 / Alt+드래그: 회전\nVR: Grip: 잡기 / A·B: 크기 / 층 손잡이를 잡고 Trigger: 펼치기", 2.8f, .08f);
            _filterCard = new GameObject("Actual convolution weights");
            _filterCard.transform.SetParent(transform, false);
            _filterName = CreateText("Filter name", _filterCard.transform, new Vector3(-1.35f, .97f, 2.75f), .68f, .045f);
            _weights = CreateText("3 x 3 weights", _filterCard.transform, new Vector3(-1.35f, .78f, 2.75f), .68f, .17f);
            TextMesh padding = CreateText("Border rule", _filterCard.transform, new Vector3(-1.35f, .57f, 2.75f), .68f, .07f);
            SetText(padding, "이미지 밖은 0\n한 픽셀씩 이동", .68f, .07f);
            for (int layer = 0; layer < _layerRoots.Length; layer++)
            {
                TextMesh label = CreateText("Layer caption", _layerRoots[layer], new Vector3(0f, .36f, 0f), .7f, .06f);
                SetText(label, "합성곱 " + (layer + 1) + " + ReLU", .7f, .06f);
            }
            TextMesh inputLabel = CreateText("Input caption", _inputRoot, new Vector3(0f, .36f, 0f), .65f, .06f);
            SetText(inputLabel, "입력 / 8 × 8", .65f, .06f);
            for (int index = 0; index < _outputRoots.Length; index++)
            {
                TextMesh label = CreateText("Demo response caption", _outputRoots[index], new Vector3(0f, -.7f, 0f), .18f, .12f);
                SetText(label, "예시\n" + (index == 0 ? "A" : "B"), .18f, .12f);
            }
        }

        private GameObject CreateShape(string name, PrimitiveType type, Transform parent, Vector3 position,
            Vector3 scale, Color color, bool interactive)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            Renderer renderer = shape.GetComponent<Renderer>();
            renderer.sharedMaterial = _toolMaterial;
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_EmissionColor", color * .15f);
            renderer.SetPropertyBlock(properties);
            shape.GetComponent<Collider>().enabled = interactive;
            return shape;
        }

        private TextMesh CreateText(string name, Transform parent, Vector3 position, float width, float height)
        {
            GameObject label = new GameObject(name);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            TextMesh text = label.AddComponent<TextMesh>();
            text.font = _font;
            text.fontSize = 64;
            text.characterSize = .1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color(.85f, .95f, .96f);
            label.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            return text;
        }

        private static void SetText(TextMesh label, string content, float width, float height)
        {
            // Bound every caption in metres, avoiding the oversized labels in the early demo.
            Vector3 parentScale = label.transform.parent.lossyScale;
            Vector3 compensation = new Vector3(1f / Mathf.Max(Mathf.Abs(parentScale.x), .001f),
                1f / Mathf.Max(Mathf.Abs(parentScale.y), .001f), 1f / Mathf.Max(Mathf.Abs(parentScale.z), .001f));
            label.transform.localScale = compensation;
            label.text = content;
            Bounds bounds = label.GetComponent<Renderer>().localBounds;
            float scale = Mathf.Min(.3f, Mathf.Min(width / Mathf.Max(bounds.size.x, .001f), height / Mathf.Max(bounds.size.y, .001f)));
            label.transform.localScale = compensation * scale;
        }

        private static Vector3 GridPosition(int row, int column, float depth)
        {
            return new Vector3((column - 3.5f) * CellSpacing, (3.5f - row) * CellSpacing, depth);
        }

        private void LateUpdate()
        {
            if (!_initialized || _step >= 2)
            {
                return;
            }
            if (_filter.IsHeld)
            {
                if (_scanning)
                {
                    _scanning = false;
                    UpdateButtons();
                }
                Vector3 local = _inputRoot.InverseTransformPoint(_filter.transform.position);
                int row = Mathf.Clamp(Mathf.RoundToInt(3.5f - local.y / CellSpacing), 0, 7);
                int column = Mathf.Clamp(Mathf.RoundToInt(local.x / CellSpacing + 3.5f), 0, 7);
                if (row != _row || column != _column)
                {
                    ApplyPatch(row, column);
                }
            }
            if (_scanning && Time.unscaledTime >= _nextScan)
            {
                ApplyPatch(_scanIndex / 8, _scanIndex % 8);
                _scanIndex++;
                _nextScan = Time.unscaledTime + .1f;
                if (_scanIndex >= 64)
                {
                    _scanning = false;
                    UpdateButtons();
                }
            }
            // Constrain only the teaching filter to the image plane. The CNN itself stays freely manipulable.
            _filterBody.position = _inputRoot.TransformPoint(GridPosition(_row, _column, -.075f));
            _filterBody.rotation = _inputRoot.rotation;
            _filter.transform.SetPositionAndRotation(_filterBody.position, _filterBody.rotation);
            _connection.SetPosition(0, _filter.transform.position);
            _connection.SetPosition(1, _firstNodes[_channel, _row, _column].transform.position);
        }

        public bool HandleInputSelection(int row, int column)
        {
            if (!_initialized)
            {
                return false;
            }
            if (_step < 2)
            {
                _scanning = false;
                UpdateButtons();
                ApplyPatch(row, column);
                return true;
            }
            if (_step == 3)
            {
                _scene.Data.ToggleInput(row, column);
                _scene.Data.SelectPixel(row, column);
                _editCount++;
                _scene.RefreshLearningAppearance();
                SetText(_readout, "입력 픽셀을 " + _editCount + "회 바꿨습니다. 전체 CNN이 다시 계산됩니다.\nA·B는 학습된 분류 결과가 아니라 예시 반응입니다.", 2.8f, .11f);
                return true;
            }
            int first = Mathf.Min(7, row + 1) - Mathf.Max(0, row - 1) + 1;
            int second = Mathf.Min(7, column + 1) - Mathf.Max(0, column - 1) + 1;
            SetText(_readout, "선택 픽셀 (" + (row + 1) + ", " + (column + 1) + ") → 첫 feature map마다 " + (first * second) + "개 뉴런\n노란색: 이 픽셀이 수용 영역에 포함됨 / 계산값의 크기와는 다릅니다.", 2.8f, .11f);
            return false;
        }

        private void ApplyPatch(int row, int column)
        {
            _row = row;
            _column = column;
            _visited[_channel, row, column] = true;
            _scene.Data.SelectWindow(row, column);
            UpdateNodeStates();
            _scene.RefreshLearningAppearance();
            float sum = _scene.Data.ReadFirstLayerSum(_channel, row, column);
            float value = _scene.Data.ReadValue(0, _channel, row, column);
            int count = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    if (_visited[_channel, y, x])
                    {
                        count++;
                    }
                }
            }
            SetText(_readout, "입력 영역 (" + (row + 1) + ", " + (column + 1) + "): 곱한 값의 합 = " + sum.ToString("0.000") +
                " → ReLU = " + value.ToString("0.000") + "\n" + count + " / 64 위치 체험 / ReLU는 음수를 0으로 바꿉니다.", 2.8f, .11f);
        }

        private void UpdateNodeStates()
        {
            foreach (CNNLearningNode node in _nodes)
            {
                bool computed = _step != 0 || node.Layer != 0 || _visited[node.Channel, node.Row, node.Column];
                bool spotlight = _step < 2 && node.Layer == 0 && node.Channel == _channel && node.Row == _row && node.Column == _column;
                node.SetLearningState(computed, spotlight);
            }
        }

        private void ApplyStep()
        {
            _scanning = false;
            _spreadLayer = 0;
            foreach (CNNLearningLayer layer in _layers)
            {
                if (layer.IsExpanded)
                {
                    layer.ToggleSpread();
                }
            }
            _scene.Data.SelectPixel(-1, -1);
            for (int index = 0; index < _layerRoots.Length; index++)
            {
                _layerRoots[index].gameObject.SetActive(index == 0 || _step >= 2);
            }
            foreach (CNNLearningPanel panel in _panels)
            {
                if (panel.Layer == 0)
                {
                    panel.gameObject.SetActive(_step != 0 || panel.Channel == _channel);
                }
            }
            foreach (Transform output in _outputRoots)
            {
                output.gameObject.SetActive(_step == 3);
            }
            _filter.gameObject.SetActive(_step < 2);
            _filterCard.SetActive(_step < 2);
            _connection.enabled = _step < 2;
            if (_step == 1 && !_layers[0].IsExpanded)
            {
                _layers[0].ToggleSpread();
            }
            else if (_step != 1 && _layers[0].IsExpanded)
            {
                _layers[0].ToggleSpread();
            }
            string[] titles = { "1 / 4 — feature map 만들기", "2 / 4 — 같은 입력, 다른 필터", "3 / 4 — 한 픽셀의 영향 따라가기", "4 / 4 — 입력을 바꿔 보기" };
            string[] instructions =
            {
                "민트색 손잡이를 이미지 위에서 움직이거나, 입력 픽셀을 눌러 보세요.\n3×3 영역에서 출력 뉴런 하나가 만들어집니다. 자동 이동으로 전체를 둘러보세요.",
                "같은 이미지도 필터가 다르면 서로 다른 feature map이 됩니다.\n필터를 바꿔 비교하고, 맵의 테두리를 잡아 가까이 끌어와 보세요.",
                "입력 픽셀 하나를 누르고, 각 층의 노란 뉴런들을 따라가 보세요.\n깊은 층의 뉴런일수록 넓게 봅니다: 입력 기준 최대 3×3 → 5×5 → 7×7.",
                "입력 픽셀을 눌러 흑백을 바꾸고, feature map의 변화를 보세요.\n잡고·돌리고·확대하고, 신경망 사이를 걸어 다니며 살펴보세요."
            };
            SetText(_title, titles[_step], 2.8f, .07f);
            SetText(_instruction, instructions[_step], 2.8f, .12f);
            UpdateFilterCard();
            UpdateNodeStates();
            _scene.RefreshLearningAppearance();
            if (_step < 2)
            {
                ApplyPatch(_row, _column);
            }
            else
            {
                SetText(_readout, _step == 2 ? "노란색은 수용 영역을 표시하며, 가중치가 0인 위치도 포함합니다.\n맵의 밝기는 ReLU 계산값입니다. 뉴런을 움직여도 계산 연결은 유지됩니다." :
                    "고정된 예제 필터 / 한 픽셀씩 이동 / 바깥은 0 / ReLU 적용\n예시 반응은 고양이·강아지 인식이 아닙니다. 처음 상태로 입력을 복원할 수 있습니다.", 2.8f, .11f);
            }
            UpdateButtons();
        }

        private void UpdateFilterCard()
        {
            SetText(_filterName, _channel == 0 ? "좌우 변화 감지" : "위아래 변화 감지", .68f, .045f);
            string weights = _channel == 0 ? "-1/3    0    +1/3\n-1/3    0    +1/3\n-1/3    0    +1/3" :
                "-1/3   -1/3   -1/3\n  0       0       0\n+1/3  +1/3  +1/3";
            SetText(_weights, weights, .68f, .17f);
        }

        private void UpdateButtons()
        {
            _buttons[0].SetAvailable(_step > 0);
            _buttons[1].SetAvailable(_step < 3);
            _buttons[2].SetAvailable(_step < 2);
            _buttons[3].SetAvailable(_step < 2);
            _buttons[4].SetAvailable(_step > 0);
            _buttons[5].SetAvailable(true);
            SetText(_buttonLabels[4], "펼치기 " + (_spreadLayer + 1), .31f, .042f);
            SetText(_buttonLabels[2], _scanning ? "멈추기" : "자동 이동", .31f, .042f);
        }

        public void Perform(CNNLearningAction action)
        {
            if (!_initialized)
            {
                return;
            }
            if (_scene.HasHeldTargets)
            {
                SetText(_readout, "잡은 물체를 놓은 뒤 체험 단계를 바꿔 주세요.", 2.8f, .11f);
                return;
            }
            switch (action)
            {
                case CNNLearningAction.Previous:
                    _step = Mathf.Max(0, _step - 1);
                    ApplyStep();
                    break;
                case CNNLearningAction.Next:
                    _step = Mathf.Min(3, _step + 1);
                    ApplyStep();
                    break;
                case CNNLearningAction.Scan:
                    _scanning = !_scanning;
                    _scanIndex = 0;
                    _nextScan = Time.unscaledTime;
                    UpdateButtons();
                    break;
                case CNNLearningAction.Filter:
                    _channel = 1 - _channel;
                    ApplyStep();
                    break;
                case CNNLearningAction.Spread:
                    for (int index = 0; index < _layers.Length; index++)
                    {
                        bool expand = index == _spreadLayer;
                        if (_layers[index].gameObject.activeInHierarchy && _layers[index].IsExpanded != expand)
                        {
                            _layers[index].ToggleSpread();
                        }
                    }
                    _spreadLayer = (_spreadLayer + 1) % (_step == 1 ? 1 : _layers.Length);
                    UpdateButtons();
                    break;
                case CNNLearningAction.Restore:
                    System.Array.Clear(_visited, 0, _visited.Length);
                    _editCount = 0;
                    _scene.Data.RestoreInput(_originalImage);
                    _scene.ResetLayout();
                    break;
            }
        }

        public void RestoreCurrentLessonLayout()
        {
            if (_initialized)
            {
                ApplyStep();
            }
        }

        private void OnDestroy()
        {
            if (_originalImage != null)
            {
                Destroy(_originalImage);
            }
        }
    }
}

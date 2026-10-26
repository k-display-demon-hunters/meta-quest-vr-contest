using CNNFeatureMaps.Interaction;
using UnityEngine;

namespace CNNFeatureMaps.Lesson
{
    // 프리팹 참조는 유지하고, 콘텐츠 표시 제어에는 Manager를 통해 전달한다.
    public sealed class LessonGuide : MonoBehaviour
    {
        [SerializeField] private Transform _inputRoot;
        [SerializeField] private Transform[] _layerRoots;
        [SerializeField] private Transform[] _outputRoots;
        [SerializeField] private Material _toolMaterial;
        [SerializeField] private GrabTarget _filter;
        [SerializeField] private GameObject _filterCard;
        [SerializeField] private TextMesh _title;
        [SerializeField] private TextMesh _instruction;
        [SerializeField] private TextMesh _readout;
        [SerializeField] private TextMesh _weights;
        [SerializeField] private TextMesh _filterName;
        [SerializeField] private TextMesh _help;
        [SerializeField] private TextMesh _padding;
        [SerializeField] private TextMesh _inputCaption;
        [SerializeField] private LessonButton[] _buttons;
        [SerializeField] private TextMesh[] _buttonLabels;
        [SerializeField] private TextMesh[] _layerCaptions;
        [SerializeField] private TextMesh[] _outputCaptions;
        private CNNFeatureMapLabManager _scene;

        public Transform InputRoot => _inputRoot;
        public GrabTarget Filter => _filter;
        public Material ToolMaterial => _toolMaterial;
        public bool ReadyForModelDesign => _scene != null && _scene.ReadyForModelDesign;
        internal Transform[] LayerRoots => _layerRoots;
        internal Transform[] OutputRoots => _outputRoots;

        public void Initialize(CNNFeatureMapLabManager scene)
        {
            _scene = scene;
            InitializeVisuals();
        }

        private void InitializeVisuals()
        {
            foreach (TextMesh label in GetComponentsInChildren<TextMesh>(true))
            {
                label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            }
            for (int index = 0; index < _buttons.Length; index++)
            {
                _buttons[index].Configure(_scene, (LessonAction)index);
                Vector3 position = _buttons[index].transform.localPosition;
                position.x = -.69f + index * .34f;
                _buttons[index].transform.localPosition = position;
                _buttons[index].transform.localScale = new Vector3(.3f, .12f, .04f);
                Vector3 labelPosition = _buttonLabels[index].transform.localPosition;
                labelPosition.x = position.x;
                _buttonLabels[index].transform.localPosition = labelPosition;
                SetText(_buttonLabels[index], _buttonLabels[index].text, .27f, .042f);
            }
            CreateSizeButton("확대", LessonAction.Enlarge, 6);
            CreateSizeButton("축소", LessonAction.Shrink, 7);
            _help.color = UI.SceneUIStyle.Muted;
            SetText(_help, _help.text, 2.8f, .08f);
            SetText(_padding, _padding.text, .68f, .07f);
            SetText(_inputCaption, _inputCaption.text, .65f, .06f);
            foreach (TextMesh label in _layerCaptions)
            {
                SetText(label, label.text, .7f, .06f);
            }
            foreach (TextMesh label in _outputCaptions)
            {
                SetText(label, label.text, .18f, .12f);
            }
        }

        private void CreateSizeButton(string label, LessonAction action, int index)
        {
            Transform parent = _buttons[0].transform.parent;
            Vector3 position = _buttons[0].transform.localPosition;
            position.x = -.69f + index * .34f;
            GameObject buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonObject.name = label;
            buttonObject.transform.SetParent(parent, false);
            buttonObject.transform.localPosition = position;
            buttonObject.transform.localRotation = _buttons[0].transform.localRotation;
            buttonObject.transform.localScale = _buttons[0].transform.localScale;
            buttonObject.GetComponent<Renderer>().sharedMaterial = _buttons[0].GetComponent<Renderer>().sharedMaterial;
            LessonButton button = buttonObject.AddComponent<LessonButton>();
            button.Configure(_scene, action);
            button.SetAvailable(true);

            TextMesh text = Instantiate(_buttonLabels[0], parent);
            text.name = label + " label";
            position.z = _buttonLabels[0].transform.localPosition.z;
            text.transform.localPosition = position;
            SetText(text, label, .27f, .042f);
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

        public void ShowStep(LessonStep step, int channel)
        {
            bool isFilterStep = step == LessonStep.BuildFeatureMap || step == LessonStep.CompareFilters;
            _filterCard.SetActive(isFilterStep);
            string title;
            string instruction;
            switch (step)
            {
                case LessonStep.BuildFeatureMap:
                    title = "1 / 4 — feature map 만들기";
                    instruction = "민트색 손잡이를 이미지 위에서 움직이거나, 입력 픽셀을 눌러 보세요.\n3×3 영역에서 출력 뉴런 하나가 만들어집니다. 자동 이동으로 전체를 둘러보세요.";
                    break;
                case LessonStep.CompareFilters:
                    title = "2 / 4 — 같은 입력, 다른 필터";
                    instruction = "같은 이미지도 필터가 다르면 서로 다른 feature map이 됩니다.\n필터를 바꿔 비교하고, 맵의 테두리를 잡아 가까이 끌어와 보세요.";
                    break;
                case LessonStep.TraceReceptiveField:
                    title = "3 / 4 — 한 픽셀의 영향 따라가기";
                    instruction = "입력 픽셀 하나를 누르고, 각 층의 노란 뉴런들을 따라가 보세요.\n깊은 층의 뉴런일수록 넓게 봅니다: 입력 기준 최대 3×3 → 5×5 → 7×7.";
                    SetText(_readout, "노란색은 수용 영역을 표시하며, 가중치가 0인 위치도 포함합니다.\n맵의 밝기는 ReLU 계산값입니다. 뉴런을 움직여도 계산 연결은 유지됩니다.", 2.8f, .11f);
                    break;
                case LessonStep.EditInput:
                    title = "4 / 4 — 입력을 바꿔 보기";
                    instruction = "입력 픽셀을 눌러 흑백을 바꾸고, feature map의 변화를 보세요.\n잡고·돌리고·확대하고, 신경망 사이를 걸어 다니며 살펴보세요.";
                    SetText(_readout, "고정된 예제 필터 / 한 픽셀씩 이동 / 바깥은 0 / ReLU 적용\n예시 반응은 고양이·강아지 인식이 아닙니다. 처음 상태로 입력을 복원할 수 있습니다.", 2.8f, .11f);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(step), step, null);
            }
            SetText(_title, title, 2.8f, .07f);
            SetText(_instruction, instruction, 2.8f, .12f);
            UpdateFilterCard(channel);
        }

        private void UpdateFilterCard(int channel)
        {
            SetText(_filterName, channel == 0 ? "좌우 변화 감지" : "위아래 변화 감지", .68f, .045f);
            string weights = channel == 0 ? "-1/3    0    +1/3\n-1/3    0    +1/3\n-1/3    0    +1/3" :
                "-1/3   -1/3   -1/3\n  0       0       0\n+1/3  +1/3  +1/3";
            SetText(_weights, weights, .68f, .17f);
        }

        public void UpdateButtons(LessonStep step, int spreadLayer, bool scanning)
        {
            bool isFirstStep = step == LessonStep.BuildFeatureMap;
            bool isFilterStep = isFirstStep || step == LessonStep.CompareFilters;
            _buttons[0].SetAvailable(!isFirstStep);
            _buttons[1].SetAvailable(true);
            SetText(_buttonLabels[1], step == LessonStep.EditInput ? "단계 마치기" : "다음으로", .27f, .042f);
            _buttons[2].SetAvailable(isFilterStep);
            _buttons[3].SetAvailable(isFilterStep);
            _buttons[4].SetAvailable(!isFirstStep);
            _buttons[5].SetAvailable(true);
            SetText(_buttonLabels[4], "펼치기 " + (spreadLayer + 1), .27f, .042f);
            SetText(_buttonLabels[2], scanning ? "멈추기" : "자동 이동", .27f, .042f);
        }

        public void ShowPatch(int row, int column, float sum, float value, int count)
        {
            SetText(_readout, "입력 영역 (" + (row + 1) + ", " + (column + 1) + "): 곱한 값의 합 = " + sum.ToString("0.000") +
                " → ReLU = " + value.ToString("0.000") + "\n" + count + " / 64 위치 체험 / ReLU는 음수를 0으로 바꿉니다.", 2.8f, .11f);
        }

        public void ShowInputEdited(int editCount)
        {
            SetText(_readout, "입력 픽셀을 " + editCount + "회 바꿨습니다. 전체 CNN이 다시 계산됩니다.\nA·B는 학습된 분류 결과가 아니라 예시 반응입니다.", 2.8f, .11f);
        }

        public void ShowAffectedPixel(int row, int column, int affectedCount)
        {
            SetText(_readout, "선택 픽셀 (" + (row + 1) + ", " + (column + 1) + ") → 첫 feature map마다 " + affectedCount + "개 뉴런\n노란색: 이 픽셀이 수용 영역에 포함됨 / 계산값의 크기와는 다릅니다.", 2.8f, .11f);
        }

        public void ShowReleaseBeforeAction()
        {
            SetText(_readout, "잡은 물체를 놓은 뒤 체험 단계를 바꿔 주세요.", 2.8f, .11f);
        }

        public void ShowStepFinished(bool skipped)
        {
            SetText(_readout, (skipped ? "입력 변경 활동을 건너뛰었습니다. " : "현재 단계를 마쳤습니다. ") +
                "바닥 안내선을 따라 모델 제작 방으로 이동하세요.", 2.8f, .11f);
        }

        public void ShowSelectScaleTarget()
        {
            SetText(_readout, "뉴런·맵·층을 Trigger로 선택하거나 잡았다 놓은 뒤 확대·축소를 누르세요.", 2.8f, .11f);
        }
    }
}

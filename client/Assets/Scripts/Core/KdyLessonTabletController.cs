using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace KdyLearning
{
    public sealed class KdyLessonTabletController : MonoBehaviour
    {
        [SerializeField] private Text _heading;
        [SerializeField] private Text _content;
        [SerializeField] private GameObject _map;
        [SerializeField] private Image[] _mapMarkers;
        [SerializeField] private Text _mapTarget;
        [SerializeField] private RectTransform _playerDot;
        private readonly string[] _names = { "진행도", "공장 맵", "내 신경망", "양피지", "힌트" };
        private readonly string[] _scrollNames = { "① 픽셀 · RGB", "② 합성곱 · 패딩", "③ 필터", "④ 피쳐맵", "⑤ 활성화함수" };
        private KdyLessonProgressController _progress;
        private Camera _camera;
        private int _sceneNumber;
        private string _hint;
        private Color _headingColor;
        private float _hintEmphasisUntil;

        public void Initialize(KdyLessonProgressController progress, Camera viewCamera, int sceneNumber)
        {
            _progress = progress;
            _camera = viewCamera;
            _sceneNumber = sceneNumber;
            _headingColor = _heading.color;
            _content.resizeTextForBestFit = true;
            _content.resizeTextMinSize = 14;
            _content.resizeTextMaxSize = 20;
            Refresh();
        }

        private void Update()
        {
            if (_heading != null && _progress != null)
            {
                bool emphasize = _progress.Tab == 4 && Time.unscaledTime < _hintEmphasisUntil;
                _heading.color = emphasize ? Color.Lerp(_headingColor, new Color(1f, .72f, .24f),
                    Mathf.PingPong(Time.unscaledTime * 1.2f, 1f)) : _headingColor;
            }
            if (_map != null && _map.activeSelf && _playerDot != null && _camera != null)
            {
                Vector3 position = _camera.transform.position;
                _playerDot.anchoredPosition = new Vector2(Mathf.Clamp(position.x / 6f, -1f, 1f) * 150f,
                    Mathf.Lerp(-80f, 95f, Mathf.Clamp01(position.z / 22f)));
            }
        }

        public void ChangeTab(int direction)
        {
            if (_progress != null)
            {
                _progress.Tab = (_progress.Tab + direction + _names.Length) % _names.Length;
                Refresh();
            }
        }

        public void EmphasizeHints()
        {
            if (_progress == null)
            {
                return;
            }
            _progress.Tab = 4;
            _hintEmphasisUntil = Time.unscaledTime + 6f;
            Refresh();
        }

        public void SetHint(string hint)
        {
            _hint = hint;
            Refresh();
        }

        public void Refresh()
        {
            if (_progress == null)
            {
                return;
            }
            int tab = Mathf.Clamp(_progress.Tab, 0, _names.Length - 1);
            _heading.text = (tab + 1) + " / 5   " + _names[tab];
            _map.SetActive(tab == 1);
            _content.gameObject.SetActive(tab != 1);
            if (tab == 1)
            {
                int next = _progress.NextScroll;
                for (int i = 0; i < _mapMarkers.Length; i++)
                {
                    // Only the next destination is marked, even though all stations exist in the room.
                    _mapMarkers[i].gameObject.SetActive(i == next || (next == 5 && i == 4));
                }
                _mapTarget.text = next < _scrollNames.Length ? "다음: " + _scrollNames[next] : "⑤ 위치 → 신경망 설계 구역";
                return;
            }
            if (tab == 0)
            {
                string[] missions =
                {
                    "맵을 보고 다음 양피지 찾기",
                    "이미지를 펼치고 RGB 픽셀 살펴보기",
                    "입체 필터와 패딩 체험하기",
                    "필터 카드 두 종류 꽂아 보기",
                    "피쳐맵 세 장 선택하고 껐다 켜기",
                    "레버로 ReLU와 Sigmoid 비교하기",
                    "설계대 해금 · 목표 확인",
                    "부품으로 나만의 신경망 조립",
                    "손실 곡선과 복원 과정을 관찰",
                    "최근 결과를 보고 다시 설계하거나 학습",
                    "추천 힌트를 참고해 부품을 직접 바꾸기",
                    "통과 결과 확인 · 공장 간판 잡기",
                    "새 간판 투입 · 복원 전후 비교",
                    "공장 복구 · CNN 복습 · 체험 완료"
                };
                string mission = missions[Mathf.Clamp(_sceneNumber - 4, 0, missions.Length - 1)];
                _content.text = (_progress.IsGameComplete ? "체험 완료!\n" : "") + "양피지 " + _progress.ScrollCount + " / 5" +
                    (_progress.HasTrainingScroll ? " · ⑥ 학습 노트 획득" : "") + "\n\n현재 미션\n" + mission;
            }
            else if (tab == 2)
            {
                if (_sceneNumber < 10 && _progress.ScrollCount < 5)
                {
                    _content.text = "양피지 5개를 모으면\n신경망 설계가 열려요.\n\n지금은 CNN의 원리를\n하나씩 체험하는 단계예요.";
                }
                else
                {
                    bool resultView = _sceneNumber >= 13 && _progress.HasTrainingResult;
                    KdyNetworkConfiguration network = resultView ? _progress.LastTrainedNetwork : _progress.Network;
                    string layers = network.LayerCount > 0 ? network.LayerCount + "개" : "배치 전";
                    string maps = network.HasFeatureMaps ? network.FeatureMaps + "개" : "선택 전";
                    string kernel = network.HasKernel ? network.KernelSize + "×" + network.KernelSize : "선택 전";
                    string activation = network.HasActivation ? (network.UseSigmoid ? "Sigmoid" : "ReLU") : "선택 전";
                    string latest = _progress.HasTrainingResult ? _progress.LastImprovement.ToString("F1") + "%" : "기록 없음";
                    _content.text = (resultView ? "최근 학습 설계\n" : "") + "은닉층 " + layers + " / 피쳐맵 " + maps +
                        "\n커널 " + kernel + " / " + activation +
                        "\n목표: RGB 오차 " + _progress.TargetImprovement.ToString("F0") + "% 감소" +
                        "\n최근 개선율: " + latest + "\n학습 시도: " + _progress.TrainingAttempts +
                        (_sceneNumber == 14 && _progress.FailureCount >= 3 && _progress.HasTrainingResult &&
                            _progress.LastImprovement < _progress.TargetImprovement ? "\n추천: 3층 · 32장 · ReLU" : "\n교육용 가상 학습 결과");
                }
            }
            else if (tab == 3)
            {
                string[] descriptions =
                {
                    "① RGB: 픽셀마다 세 색의 값",
                    "② 합성곱: 곱하고 더해 출력 한 칸",
                    "③ 필터: 계수마다 다른 특징",
                    "④ 피쳐맵: 필터의 위치별 반응",
                    "⑤ 활성화: 값을 비선형으로 변환"
                };
                StringBuilder notes = new StringBuilder();
                for (int i = 0; i < descriptions.Length; i++)
                {
                    if (_progress.HasScroll(i))
                    {
                        if (notes.Length > 0)
                        {
                            notes.Append('\n');
                        }
                        notes.Append(descriptions[i]);
                    }
                }
                if (_progress.HasTrainingScroll)
                {
                    if (notes.Length > 0)
                    {
                        notes.Append('\n');
                    }
                    notes.Append("⑥ 학습: 오차·필터 보정·반복");
                }
                _content.text = notes.Length == 0 ? "아직 모은 양피지가 없어요.\n\n① 픽셀·RGB부터 찾아보세요." : notes.ToString();
            }
            else
            {
                _content.text = string.IsNullOrEmpty(_hint) ? "오른손 Trigger: 선택\n오른손 Grip: 잡기\n왼손 X / Y: 메뉴 넘기기\n\n시뮬레이터: T 선택 · G 잡기\nShift + I/K/J/L: 스틱 이동" : _hint;
            }
        }
    }
}

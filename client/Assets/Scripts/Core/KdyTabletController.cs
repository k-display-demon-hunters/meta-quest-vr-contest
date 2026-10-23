using System;
using UnityEngine;
using UnityEngine.UI;

namespace KdyIntro
{
    public sealed class KdyTabletController : MonoBehaviour
    {
        [SerializeField] private Text[] _headings;
        [SerializeField] private Text[] _contents;
        [SerializeField] private GameObject[] _maps;
        private readonly string[] _names = { "진행도", "공장 맵", "내 신경망", "양피지", "힌트" };
        private int _tab;
        private int _sceneNumber;
        public event Action TabChanged;

        public void Initialize(int sceneNumber)
        {
            _sceneNumber = sceneNumber;
            Refresh();
        }

        public void ChangeTab(int direction)
        {
            _tab = (_tab + direction + _names.Length) % _names.Length;
            Refresh();
            TabChanged?.Invoke();
        }

        private void Refresh()
        {
            string mission = _sceneNumber == 1 ? "임무 시작하기" : (_sceneNumber == 2 ? "공장의 긴급 상황 확인" : "조작 익히고 CNN실 입장");
            string text;
            switch (_tab)
            {
                case 0:
                    text = "양피지 0 / 5\n\n현재 미션\n" + mission;
                    break;
                case 1:
                    text = "공장 입구\n     ↓\nCNN실\n\n정면의 파란 출입문으로 이동하세요.";
                    break;
                case 2:
                    text = "아직 신경망이 없어요.\n\n양피지 5개를 모으면\n직접 설계할 수 있어요.";
                    break;
                case 3:
                    text = "아직 모은 양피지가 없어요.\n\n① 픽셀  ② 합성곱\n③ 필터  ④ 피쳐맵\n⑤ 활성화함수";
                    break;
                default:
                    text = "왼쪽 스틱: 이동\n오른쪽 Trigger: 선택\n오른쪽 Grip: 잡기\n왼손 X / Y: 태블릿 탭";
                    break;
            }
            foreach (GameObject map in _maps)
            {
                map.SetActive(_tab == 1);
            }
            foreach (Text heading in _headings)
            {
                heading.text = (_tab + 1) + " / 5   " + _names[_tab];
            }
            foreach (Text content in _contents)
            {
                content.text = text;
                content.gameObject.SetActive(_tab != 1);
            }
        }
    }
}

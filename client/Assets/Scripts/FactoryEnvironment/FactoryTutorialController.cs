using System;
using System.Collections;
using UnityEngine;

namespace FactoryEnvironment
{
    // Page navigation only: no device input or performance requirements.
    public sealed class FactoryTutorialController : MonoBehaviour
    {
        private static readonly string[] Titles = { "이동과 둘러보기", "선택", "잡기", "정면 복원", "태블릿" };
        private static readonly string[] Instructions =
        {
            "고개를 돌려 주변을 둘러보세요.\n왼손 스틱으로 이동하고 오른손 스틱으로 시점을 회전합니다.\n이 안내에서는 동작을 수행하지 않아도 다음으로 넘어갈 수 있습니다.",
            "오른손 ray로 버튼을 가리키면 테두리가 강조됩니다.\n오른손 Trigger를 눌러 선택하세요.\n흐리게 표시된 버튼은 아직 사용할 수 없습니다.",
            "오른손 ray로 물체를 가리키고 Grip을 누른 채 잡습니다.\n잡은 물체는 시선을 따라 이동하고 Grip을 놓으면 해제됩니다.\n물체를 잡는 동안 일부 화면 버튼은 사용할 수 없습니다.",
            "현재 위치에서 정면을 다시 맞추려면 오른손 A를 누르세요.\n오른손 B로 ray를 켜거나 끌 수 있습니다.\nray를 끄면 잡고 있던 물체도 놓습니다.",
            "왼손 태블릿에서 지도와 수집한 양피지를 확인할 수 있습니다.\n왼손 Trigger + 스틱으로 항목 이동, X로 선택, Y로 뒤로 갑니다.\nMenu로 설정을 엽니다. 안내를 마치고 Pixel Lab으로 이동하세요."
        };
        private FactoryTutorialPanelController _panel;
        private int _page;
        private bool _finishing;
        public bool IsComplete { get; private set; }
        public event Action OnCompleted;

        public void BeginGuide(FactoryTutorialPanelController panel)
        {
            if (_panel != null || IsComplete)
            {
                return;
            }
            _panel = panel;
            _panel.OnPrevious += Previous;
            _panel.OnNext += Next;
            _panel.OnFinish += Finish;
            Refresh();
        }

        private void Previous() { _page = Mathf.Max(0, _page - 1); Refresh(); }
        private void Next()
        {
            if (_page == Titles.Length - 1) { Finish(); return; }
            _page++;
            Refresh();
        }
        private void Refresh() => _panel.ShowPage(_page, Titles.Length, Titles[_page], Instructions[_page]);
        private void Finish()
        {
            if (IsComplete || _finishing)
            {
                return;
            }
            _finishing = true;
            StartCoroutine(FinishAfterClick());
        }

        private IEnumerator FinishAfterClick()
        {
            yield return null;
            IsComplete = true;
            _panel.gameObject.SetActive(false);
            OnCompleted?.Invoke();
        }
        private void OnDestroy()
        {
            if (_panel == null)
            {
                return;
            }
            _panel.OnPrevious -= Previous;
            _panel.OnNext -= Next;
            _panel.OnFinish -= Finish;
        }
    }
}

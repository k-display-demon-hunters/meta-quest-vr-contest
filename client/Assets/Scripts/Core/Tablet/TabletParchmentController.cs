using System;
using Parchment;
using UnityEngine;
namespace Tablet
{
    internal sealed class TabletParchmentController
    {
        private readonly TabletUIBuilder _view;
        private readonly Func<int, bool> _hasCollected;
        public int SelectedIndex { get; private set; }
        public event Action<int, Pose> OpenRequested;
        public TabletParchmentController(TabletUIBuilder view, Func<int, bool> collected) { _view = view; _hasCollected = collected; }
        public void RestoreSelection(int index) => SelectedIndex = Mathf.Clamp(index, 0, ParchmentCatalog.All.Count - 1);
        public void RefreshInventory()
        {
            int row = 0;
            for (int i = 0; i < _view.PaperButtons.Length; i++)
            {
                bool owned = _hasCollected(i);
                _view.PaperButtons[i].gameObject.SetActive(owned);
                if (!owned) { continue; }
                ParchmentLesson lesson = ParchmentCatalog.Get(i);
                _view.PaperTitles[i].text = lesson.Title;
                _view.PaperSubtitles[i].text = lesson.Topic;
                ((RectTransform)_view.PaperButtons[i].transform).anchoredPosition = new Vector2(0f, 180f - row * 103f);
                row++;
            }
            _view.PaperCount.text = row + "개 수집";
            _view.EmptyCollection.SetActive(row == 0);
        }
        public void SelectItem(int index)
        {
            if (!_hasCollected(index)) { return; }
            SelectedIndex = index;
            Transform origin = _view.PaperButtons[index].transform;
            OpenRequested?.Invoke(index, new Pose(origin.position, origin.rotation));
        }
    }
}

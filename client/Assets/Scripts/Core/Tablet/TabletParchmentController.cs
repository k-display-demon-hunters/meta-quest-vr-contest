using System;
using UnityEngine;
using static Tablet.TabletUIBuilder;

namespace Tablet
{
    internal sealed class TabletParchmentController
    {
        private readonly bool[] _collectedParchments = new bool[ParchmentLesson.All.Length];
        private readonly bool[] _featureMapVisible = { true, true, true };
        private string[] _parchmentPages;
        private int _parchmentPageIndex;
        private int _selectedItemIndex;

        private readonly TabletUIBuilder _ui;
        private readonly TabletUIBuilder.ParchmentView _view;
        private readonly TabletVisualResources _resources;
        private readonly Action<int> _resetFocus;
        private readonly Action<bool> _showFeedback;
        private readonly Action<string, string> _feedback;

        public int SelectedIndex => _selectedItemIndex;
        public bool IsOpen => _view.ParchmentReader.activeSelf;

        public TabletParchmentController(TabletUIBuilder ui, TabletVisualResources resources,
            Action<int> resetFocus, Action<bool> showFeedback, Action<string, string> feedback)
        {
            _ui = ui;
            _view = ui.Parchment;
            _resources = resources;
            _resetFocus = resetFocus;
            _showFeedback = showFeedback;
            _feedback = feedback;
        }

        public void RestoreSelection(int index)
        {
            _selectedItemIndex = Mathf.Clamp(index, 0, ParchmentLesson.All.Length - 1);
        }

        public static string ItemName(int index) => "양피지 " + (index + 1).ToString("00");
        public bool IsFeatureMapVisible(int index) => _featureMapVisible[index];

        private void RefreshParchmentPage()
        {
            ParchmentLesson lesson = ParchmentLesson.All[_selectedItemIndex];
            _view.ParchmentSource.text = ItemName(_selectedItemIndex) + "  /  " + lesson.Department;
            _view.ParchmentTitle.text = lesson.Title;
            _view.ParchmentBody.text = _parchmentPages[_parchmentPageIndex];
            _view.ParchmentPageLabel.text = (_parchmentPageIndex + 1) + " / " + _parchmentPages.Length;
            _view.PreviousParchmentPage.interactable = _parchmentPageIndex > 0;
            _view.NextParchmentPage.interactable = _parchmentPageIndex < _parchmentPages.Length - 1;
            bool illustrated = _parchmentPageIndex == _parchmentPages.Length - 1;
            _view.ParchmentBody.rectTransform.sizeDelta = new Vector2(850f, illustrated ? 220f : 306f);
            // ReferencePosition uses the centre of the old rectangle; keep the top edge fixed when its height changes.
            _view.ParchmentBody.rectTransform.anchoredPosition = _ui.ReferencePosition(_view.ParchmentReader.transform,
                770f, 383f + (illustrated ? 220f : 306f) * 0.5f);
            _ui.RebuildParchmentIllustration(illustrated);
        }

        public void ChangeParchmentPage(int delta)
        {
            if (_parchmentPages == null)
            {
                return;
            }
            _parchmentPageIndex = Mathf.Clamp(_parchmentPageIndex + delta, 0, _parchmentPages.Length - 1);
            RefreshParchmentPage();
        }

        public void CloseParchment()
        {
            _view.ParchmentReader.SetActive(false);
            _showFeedback(true);
            _resetFocus(_selectedItemIndex);
        }

        public void ToggleFeatureMap(int index)
        {
            _featureMapVisible[index] = !_featureMapVisible[index];
            _view.FeatureMapImages[index].enabled = _featureMapVisible[index];
            string[] captions = { "세로 경계", "가로 경계", "주변 경계" };
            _view.FeatureMapLabels[index].text = captions[index] + (_featureMapVisible[index] ? " · 켜짐" : " · 꺼짐");
        }

        public void RefreshInventory()
        {
            for (int index = 0; index < _view.InventoryGlows.Count; index++)
            {
                bool owned = HasParchment(index);
                bool selected = owned && index == _selectedItemIndex;
                _view.InventoryGlows[index].gameObject.SetActive(selected);
                _view.InventorySurfaces[index].sprite = selected ? _resources.SelectedSprite : _resources.PanelSprite;
                _view.InventorySurfaces[index].color = owned ? CardColor : new Color(0.9f, 0.92f, 0.94f);
                _view.InventoryButtons[index].interactable = owned;
                _view.InventoryLabels[index].text = ItemName(index) + (owned ? "" : " · 미발견");
                _view.InventoryLabels[index].fontSize = owned ? 21 : 18;
                _view.InventoryLabels[index].color = owned ? TextColor : MutedTextColor;
                _view.InventoryDescriptions[index].text = owned ? ParchmentLesson.All[index].Title : ParchmentLesson.All[index].Room;
                _view.InventoryDescriptions[index].fontSize = owned ? 18 : 16;
            }
        }

        public void SelectItem(int index)
        {
            if (!HasParchment(index))
            {
                return;
            }
            _selectedItemIndex = index;
            _parchmentPages = ParchmentLesson.All[index].Pages;
            _parchmentPageIndex = 0;
            RefreshInventory();
            _view.ParchmentReader.SetActive(true);
            _view.ParchmentReader.transform.SetAsLastSibling();
            _showFeedback(false);
            RefreshParchmentPage();
        }

        public bool HasParchment(int index)
        {
            return index >= 0 && index < _collectedParchments.Length && _collectedParchments[index];
        }

        public bool CollectParchment(int index)
        {
            if (index < 0 || index >= _collectedParchments.Length || HasParchment(index))
            {
                return false;
            }
            _collectedParchments[index] = true;
            RefreshInventory();
            _feedback(ItemName(index) + " 완료",
                ParchmentLesson.All[index].Title + "\n\n양피지가 태블릿에 보관되었습니다.\n오른쪽 아이템함에서 다시 읽을 수 있습니다.");
            return true;
        }
    }
}

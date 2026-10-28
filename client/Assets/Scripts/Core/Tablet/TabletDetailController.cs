using UnityEngine;
using UnityEngine.UI;

namespace Tablet
{
    internal sealed class TabletDetailController
    {
        private readonly TabletUIBuilder.ShellView _view;

        public bool IsOpen => _view.DetailOverlay.activeSelf;
        public bool IsQuitConfirmationOpen => IsOpen && _view.ConfirmQuitButton.gameObject.activeSelf;

        public TabletDetailController(TabletUIBuilder.ShellView view)
        {
            _view = view;
        }

        public void Show(string title, string body, bool showImage)
        {
            _view.ConfirmQuitButton.gameObject.SetActive(false);
            _view.DetailCloseButton.GetComponentInChildren<Text>().text = "완료";
            _view.FactoryMapView.SetActive(false);
            _view.DetailBody.gameObject.SetActive(true);
            _view.DetailTitle.text = title;
            _view.DetailBody.text = body;
            _view.DetailBody.fontSize = showImage ? 23 : 27;
            _view.DetailBody.rectTransform.anchoredPosition = new Vector2(0f, showImage ? 211f : 60f);
            _view.DetailBody.rectTransform.sizeDelta = new Vector2(700f, showImage ? 54f : 330f);
            _view.DetailImage.gameObject.SetActive(showImage);
            _view.DetailOverlay.SetActive(true);
        }

        public void ShowMap()
        {
            _view.DetailBody.gameObject.SetActive(false);
            _view.FactoryMapView.SetActive(true);
        }

        public void ShowQuitConfirmation()
        {
            _view.DetailCloseButton.GetComponentInChildren<Text>().text = "취소";
            _view.ConfirmQuitButton.gameObject.SetActive(true);
        }

        public void Close()
        {
            _view.DetailOverlay.SetActive(false);
        }
    }
}

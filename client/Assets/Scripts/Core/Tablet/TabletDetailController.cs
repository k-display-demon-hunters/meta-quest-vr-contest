namespace Tablet
{
    internal sealed class TabletDetailController
    {
        private readonly TabletUIBuilder _view;
        public bool IsOpen => _view.QuitPanel.activeSelf;
        public TabletDetailController(TabletUIBuilder view) { _view = view; }
        public void Show() => _view.QuitPanel.SetActive(true);
        public void Close() => _view.QuitPanel.SetActive(false);
    }
}

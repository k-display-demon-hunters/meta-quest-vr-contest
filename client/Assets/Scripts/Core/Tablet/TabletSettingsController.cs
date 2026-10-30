using UnityEngine;
namespace Tablet
{
    internal sealed class TabletSettingsController
    {
        private readonly TabletUIBuilder _view;
        private bool _vibration;
        public bool IsOpen => _view.SettingsPanel.activeSelf;
        public TabletSettingsController(TabletUIBuilder view) { _view = view; }
        public int Load()
        {
            float brightness = Mathf.Clamp01(PlayerPrefs.GetFloat("Tablet.Brightness", .8f));
            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Tablet.Volume", .49f));
            _vibration = PlayerPrefs.GetInt("Tablet.Vibration", 1) == 1;
            _view.Brightness.SetValueWithoutNotify(brightness);
            _view.Volume.SetValueWithoutNotify(volume);
            SetBrightness(brightness);
            SetVolume(volume);
            RefreshVibration();
            return PlayerPrefs.GetInt("Tablet.Inventory", 0);
        }
        public void SetVisible(bool visible) => _view.SettingsPanel.SetActive(visible);
        public void SetBrightness(float value)
        {
            Screen.brightness = value;
            _view.BrightnessValue.text = Mathf.RoundToInt(value * 100f) + "%";
            _view.SaveLabel.text = "설정 저장";
        }
        public void SetVolume(float value)
        {
            AudioListener.volume = value;
            _view.VolumeValue.text = Mathf.RoundToInt(value * 100f) + "%";
            _view.SaveLabel.text = "설정 저장";
        }
        public void ToggleVibration() { _vibration = !_vibration; RefreshVibration(); _view.SaveLabel.text = "설정 저장"; }
        private void RefreshVibration() => _view.VibrationValue.text = _vibration ? "켜짐" : "꺼짐";
        public void Save(int item)
        {
            PlayerPrefs.SetFloat("Tablet.Brightness", _view.Brightness.value);
            PlayerPrefs.SetFloat("Tablet.Volume", _view.Volume.value);
            PlayerPrefs.SetInt("Tablet.Vibration", _vibration ? 1 : 0);
            PlayerPrefs.SetInt("Tablet.Inventory", item);
            PlayerPrefs.Save();
            _view.SaveLabel.text = "저장 완료";
        }
    }
}

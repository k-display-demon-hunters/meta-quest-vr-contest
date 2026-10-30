using System;
using System.Collections;
using UnityEngine;
using static Tablet.TabletUIBuilder;

namespace Tablet
{
    internal sealed class TabletSettingsController
    {
        private const string SaveBrightnessKey = "Tablet.Brightness";
        private const string SaveVolumeKey = "Tablet.Volume";
        private const string SaveVibrationKey = "Tablet.Vibration";
        private const string SaveStageKey = "Tablet.Stage";
        private const string SaveInventoryKey = "Tablet.Inventory";
        private float _brightness = 0.8f;
        private float _volume = 0.49f;
        private bool _isVibrationEnabled = true;
        private Coroutine _settingsAnimation;

        private readonly MonoBehaviour _host;
        private readonly TabletUIBuilder.SettingsView _view;
        public event Action Saved;
        public event Action<string, string> FeedbackRequested;

        public float Brightness => _brightness;
        public float Volume => _volume;
        public bool IsOpen => _view.SettingsOverlay.activeSelf;

        public TabletSettingsController(MonoBehaviour host, TabletUIBuilder.SettingsView view)
        {
            _host = host;
            _view = view;
        }

        public void StopAnimation()
        {
            if (_settingsAnimation != null)
            {
                _host.StopCoroutine(_settingsAnimation);
                _settingsAnimation = null;
            }
        }

        public void LoadSettings(out int stageIndex, out int itemIndex)
        {
            _brightness = Mathf.Clamp01(PlayerPrefs.GetFloat(SaveBrightnessKey, _brightness));
            _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(SaveVolumeKey, _volume));
            _isVibrationEnabled = PlayerPrefs.GetInt(SaveVibrationKey, _isVibrationEnabled ? 1 : 0) == 1;
            stageIndex = PlayerPrefs.GetInt(SaveStageKey, 0);
            itemIndex = PlayerPrefs.GetInt(SaveInventoryKey, 0);
            AudioListener.volume = _volume;
            Screen.brightness = _brightness;
        }

        public void SetSettingsVisible(bool visible)
        {
            StopAnimation();
            _view.SettingsOverlay.SetActive(visible);
            _view.SettingsCanvasGroup.alpha = 1f;
            _view.SettingsSheet.anchoredPosition = Vector2.zero;
            if (visible)
            {
                _settingsAnimation = _host.StartCoroutine(AnimateSettingsOpen());
            }
        }

        private IEnumerator AnimateSettingsOpen()
        {
            const float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                _view.SettingsCanvasGroup.alpha = eased;
                _view.SettingsSheet.anchoredPosition = new Vector2(0f, -6f * (1f - eased));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _view.SettingsCanvasGroup.alpha = 1f;
            _view.SettingsSheet.anchoredPosition = Vector2.zero;
            _settingsAnimation = null;
        }

        public void RefreshSettingsValues()
        {
            if (_view.BrightnessValue != null)
            {
                _view.BrightnessValue.text = Mathf.RoundToInt(_brightness * 100f) + "%";
            }
            if (_view.VolumeValue != null)
            {
                _view.VolumeValue.text = Mathf.RoundToInt(_volume * 100f) + "%";
            }
            if (_view.VibrationValue != null)
            {
                _view.VibrationValue.text = _isVibrationEnabled ? "켜짐" : "꺼짐";
                _view.VibrationTrack.color = _isVibrationEnabled ? SuccessColor : ElevatedColor;
                _view.VibrationThumb.anchoredPosition = new Vector2(_isVibrationEnabled ? 15f : -15f, 0f);
            }
        }

        public void SetBrightness(float value)
        {
            _brightness = Mathf.Clamp01(value);
            Screen.brightness = _brightness;
            RefreshSettingsValues();
            RefreshSaveLabels("저장하기");
        }

        public void SetVolume(float value)
        {
            _volume = Mathf.Clamp01(value);
            AudioListener.volume = _volume;
            RefreshSettingsValues();
            RefreshSaveLabels("저장하기");
        }

        public void ToggleVibration()
        {
            _isVibrationEnabled = !_isVibrationEnabled;
            RefreshSettingsValues();
            RefreshSaveLabels("저장하기");
            FeedbackRequested?.Invoke("진동 " + (_isVibrationEnabled ? "사용" : "해제"),
                "피드백 진동 설정이 변경되었습니다.");
        }

        private void RefreshSaveLabels(string value)
        {
            _view.SaveLabel.text = value;
            _view.DashboardSaveLabel.text = value;
        }

        public void SaveSettings(int stageIndex, int itemIndex)
        {
            PlayerPrefs.SetFloat(SaveBrightnessKey, _brightness);
            PlayerPrefs.SetFloat(SaveVolumeKey, _volume);
            PlayerPrefs.SetInt(SaveVibrationKey, _isVibrationEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SaveStageKey, stageIndex);
            PlayerPrefs.SetInt(SaveInventoryKey, itemIndex);
            PlayerPrefs.Save();
            Saved?.Invoke();
            RefreshSaveLabels("저장 완료");
            FeedbackRequested?.Invoke("저장 완료",
                "현재 단계와 태블릿 설정을 저장했습니다.\n다음 접속 때 같은 상태로 복원됩니다.");
        }
    }
}

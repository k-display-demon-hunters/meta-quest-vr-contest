using System.Collections;
using UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryIntro
{
    public sealed class FactoryIntroPresentationController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _instruction;
        [SerializeField] private Text _continueLabel;
        [SerializeField] private Button _continueButton;
        [SerializeField] private FactoryWraithController _wraith;
        [SerializeField] private GameObject _corruption;
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _alarmClip;

        public void BindPlayer(XROrigin rig)
        {
            _canvas.worldCamera = rig.Camera;
            SceneUIStyle.ApplyButtons(_canvas.transform);
        }

        public void Show(string heading, string instruction, string button, bool canContinue)
        {
            _heading.text = heading;
            _instruction.text = instruction;
            _continueLabel.text = button;
            _continueButton.gameObject.SetActive(!string.IsNullOrEmpty(button));
            _continueButton.interactable = canContinue;
        }

        public void ResetFactory()
        {
            _corruption.SetActive(false);
            _wraith.gameObject.SetActive(false);
        }

        public IEnumerator PlayAttack()
        {
            if (_alarmClip != null && _audio != null)
            {
                _audio.PlayOneShot(_alarmClip);
            }
            _canvas.transform.position += Vector3.down * 0.65f;
            _canvas.transform.localScale *= 0.65f;
            _wraith.gameObject.SetActive(true);
            _wraith.BindCorruption(_corruption);
            _wraith.Restart();
            while (!_wraith.IsComplete) yield return null;
            _wraith.gameObject.SetActive(false);
        }

        public void ReleasePlayer()
        {
            if (_canvas != null)
            {
                _canvas.worldCamera = null;
            }
        }

        private void OnDisable() => ReleasePlayer();
    }
}

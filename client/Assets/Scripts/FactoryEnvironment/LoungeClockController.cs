using System;
using UnityEngine;

namespace FactoryEnvironment
{
    [DisallowMultipleComponent]
    public sealed class LoungeClockController : MonoBehaviour
    {
        [SerializeField] private Transform _hourHand;
        [SerializeField] private Transform _minuteHand;
        [SerializeField] private Transform _secondHand;

        private int _displayedSecond;

        private void OnEnable()
        {
            _displayedSecond = -1;
            Update();
        }

        private void Update()
        {
            TimeSpan time = DateTime.Now.TimeOfDay;
            int secondOfDay = (int)time.TotalSeconds;
            if (secondOfDay == _displayedSecond)
                return;

            _displayedSecond = secondOfDay;
            _hourHand.localRotation = Quaternion.AngleAxis((float)(time.TotalHours % 12 * 30), Vector3.forward);
            _minuteHand.localRotation = Quaternion.AngleAxis((float)(time.TotalMinutes % 60 * 6), Vector3.forward);
            _secondHand.localRotation = Quaternion.AngleAxis(time.Seconds * 6f, Vector3.forward);
        }
    }
}

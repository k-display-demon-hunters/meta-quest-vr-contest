using UnityEngine;

namespace OpeningEnding.Intro
{
    public sealed class FactoryWraithController : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private Transform[] _shards;
        [SerializeField] private Material _attackMaterial;
        private Vector3[] _shardPositions;
        private Vector3 _bodyPosition;
        private GameObject _corruption;
        private float _started;
        private LineRenderer _attackRay;
        private bool _cinematicDriven;
        public bool IsComplete => Time.time - _started >= 8f;

        private void Awake()
        {
            _bodyPosition = _body.localPosition;
            _shardPositions = new Vector3[_shards.Length];
            for (int i = 0; i < _shards.Length; i++)
            {
                _shardPositions[i] = _shards[i].localPosition;
            }
        }

        private void EnsureAttackRay()
        {
            if (_attackRay != null)
            {
                return;
            }
            GameObject attack = new GameObject("Wraith Core Attack");
            attack.transform.SetParent(transform, false);
            _attackRay = attack.AddComponent<LineRenderer>();
            _attackRay.positionCount = 2;
            _attackRay.useWorldSpace = true;
            _attackRay.startWidth = .07f;
            _attackRay.endWidth = .18f;
            _attackRay.sharedMaterial = _attackMaterial;
            _attackRay.enabled = false;
        }

        private void OnEnable()
        {
            _started = Time.time;
        }

        private void Update()
        {
            if (_cinematicDriven)
            {
                return;
            }
            EnsureAttackRay();
            float age = Time.time - _started;
            float scale = age < 1f ? Mathf.SmoothStep(0f, 1f, age) : (age < 6f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (age - 6f) / 2f));
            AnimateBody(age, scale);
            if (_attackRay != null && _corruption != null)
            {
                _attackRay.enabled = age > 2.5f && age < 4.2f;
                _attackRay.SetPosition(0, _body.position + transform.right * .65f);
                _attackRay.SetPosition(1, _corruption.transform.position);
            }
            if (_corruption != null && age > 3.2f)
            {
                _corruption.SetActive(true);
            }
        }

        // The village opening samples one clock for the spirit, factory and displays.
        // Reference scenes retain their original automatic eight-second playback.
        public void SetCinematicFrame(float age, float visibility, Vector3 attackTarget, float attackStrength)
        {
            _cinematicDriven = true;
            EnsureAttackRay();
            AnimateBody(age, visibility);
            _attackRay.enabled = attackStrength > 0f;
            _attackRay.startWidth = 0.06f + attackStrength * 0.12f;
            _attackRay.endWidth = 0.12f + attackStrength * 0.28f;
            _attackRay.SetPosition(0, _body.position + transform.right * 0.65f);
            _attackRay.SetPosition(1, attackTarget);
        }

        private void AnimateBody(float age, float scale)
        {
            _body.localPosition = _bodyPosition + Vector3.up * (Mathf.Sin(age * 1.8f) * .12f);
            _body.localRotation = Quaternion.Euler(0f, Mathf.Sin(age * .8f) * 12f, 0f);
            _body.localScale = Vector3.one * scale;
            for (int i = 0; i < _shards.Length; i++)
            {
                Transform shard = _shards[i];
                Vector3 home = _shardPositions[i];
                shard.localPosition = Quaternion.Euler(0f, age * (i % 2 == 0 ? 18f : -12f), 0f) * home;
                shard.localScale = Vector3.one * (.08f + i % 4 * .035f) * scale;
                if (_cinematicDriven)
                {
                    shard.localRotation = Quaternion.Euler(age * 12f, age * 25f + i * 31f, age * 9f);
                }
                else
                {
                    shard.Rotate(12f * Time.deltaTime, 25f * Time.deltaTime, 9f * Time.deltaTime, Space.Self);
                }
            }
        }

        public void BindCorruption(GameObject corruption)
        {
            _corruption = corruption;
            if (_corruption != null)
            {
                _corruption.SetActive(false);
            }
        }

        public void Restart()
        {
            _cinematicDriven = false;
            _started = Time.time;
            if (_corruption != null)
            {
                _corruption.SetActive(false);
            }
        }
    }
}

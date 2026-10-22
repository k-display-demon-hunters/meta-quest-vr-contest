using System;
using UnityEngine;

namespace FactoryIntro
{
    public sealed class FactoryVillageController : MonoBehaviour
    {
        private const float ImpactTime = 8.2f;
        private const float WraithArrivalTime = 4.3f;
        private const float WalkStopDuration = .8f;
        private static readonly Vector3 WraithArrivalPosition = new Vector3(-4f, 7.4f, 16f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform _core;
        [SerializeField] private Renderer _coreSurface;
        [SerializeField] private Transform[] _rotors = Array.Empty<Transform>();
        [SerializeField] private Transform[] _clouds = Array.Empty<Transform>();
        [SerializeField] private Transform[] _citizens = Array.Empty<Transform>();
        [SerializeField] private Transform[] _heads = Array.Empty<Transform>();
        [SerializeField] private Transform[] _leftArms = Array.Empty<Transform>();
        [SerializeField] private Transform[] _rightArms = Array.Empty<Transform>();
        [SerializeField] private Transform[] _leftHips = Array.Empty<Transform>();
        [SerializeField] private Transform[] _rightHips = Array.Empty<Transform>();
        [SerializeField] private Transform[] _leftKnees = Array.Empty<Transform>();
        [SerializeField] private Transform[] _rightKnees = Array.Empty<Transform>();
        [SerializeField] private Transform[] _leftFeet = Array.Empty<Transform>();
        [SerializeField] private Transform[] _rightFeet = Array.Empty<Transform>();
        [SerializeField] private Transform[] _screenTargets = Array.Empty<Transform>();
        [SerializeField] private Transform[] _debris = Array.Empty<Transform>();
        [SerializeField] private Renderer[] _supplyLights = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] _factoryLights = Array.Empty<Renderer>();
        [SerializeField] private Transform[] _smoke = Array.Empty<Transform>();
        [SerializeField] private Transform _entranceBeacon;

        private LocalPose[] _rotorPoses;
        private LocalPose[] _cloudPoses;
        private LocalPose[] _citizenPoses;
        private LocalPose[] _headPoses;
        private LocalPose[] _leftArmPoses;
        private LocalPose[] _rightArmPoses;
        private LocalPose[] _leftHipPoses;
        private LocalPose[] _rightHipPoses;
        private LocalPose[] _leftKneePoses;
        private LocalPose[] _rightKneePoses;
        private LocalPose[] _leftFootPoses;
        private LocalPose[] _rightFootPoses;
        private LocalPose[] _debrisPoses;
        private LocalPose[] _smokePoses;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _beaconScale;
        private float _ambientStartTime;
        private bool _initialized;

        public Vector3 CorePosition => _core.position;

        private struct LocalPose
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        private void Awake()
        {
            Initialize();
            ResetVillage();
        }

        public void ResetVillage()
        {
            Initialize();
            _ambientStartTime = 0f;
            EvaluateIdle(0f);
            AnimateDamage(0f);
        }

        public void EvaluateIdle(float ambientTime)
        {
            Initialize();
            AnimateVillage(ambientTime, 0f, false);
            AnimateFactory(0f, ambientTime);
        }

        public void BeginStory(float ambientTime)
        {
            Initialize();
            // The title and story share one ambient clock, so clicking Start never resets a stride.
            _ambientStartTime = Mathf.Max(0f, ambientTime);
        }

        public void Evaluate(float elapsed)
        {
            Initialize();
            AnimateVillage(_ambientStartTime + elapsed, elapsed, true);
            AnimateFactory(elapsed, _ambientStartTime + Mathf.Min(elapsed, ImpactTime));
            AnimateDamage(elapsed);
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _rotorPoses = Capture(_rotors);
            _cloudPoses = Capture(_clouds);
            _citizenPoses = Capture(_citizens);
            _headPoses = Capture(_heads);
            _leftArmPoses = Capture(_leftArms);
            _rightArmPoses = Capture(_rightArms);
            _leftHipPoses = Capture(_leftHips);
            _rightHipPoses = Capture(_rightHips);
            _leftKneePoses = Capture(_leftKnees);
            _rightKneePoses = Capture(_rightKnees);
            _leftFootPoses = Capture(_leftFeet);
            _rightFootPoses = Capture(_rightFeet);
            _debrisPoses = Capture(_debris);
            _smokePoses = Capture(_smoke);
            _beaconScale = _entranceBeacon.localScale;
            _propertyBlock = new MaterialPropertyBlock();
            _initialized = true;
        }

        private static LocalPose[] Capture(Transform[] transforms)
        {
            LocalPose[] poses = new LocalPose[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform target = transforms[i];
                poses[i] = new LocalPose
                {
                    Position = target.localPosition,
                    Rotation = target.localRotation,
                    Scale = target.localScale
                };
            }
            return poses;
        }

        private void AnimateVillage(float ambientTime, float elapsed, bool isStory)
        {
            for (int i = 0; i < _clouds.Length; i++)
            {
                _clouds[i].localPosition = _cloudPoses[i].Position +
                    Vector3.right * (Mathf.Sin(ambientTime * .09f + i) - Mathf.Sin(i)) * 1.5f;
            }
            for (int i = 0; i < _citizens.Length; i++)
            {
                AnimateResident(i, ambientTime, elapsed, isStory);
            }
        }

        private void AnimateResident(int index, float ambientTime, float elapsed, bool isStory)
        {
            float noticedAt = WraithArrivalTime + index * .12f;
            float notice = isStory ? SmoothRange(noticedAt, noticedAt + 1.1f, elapsed) : 0f;
            float dismay = isStory ? SmoothRange(10.5f + index / 2 * .3f, 11.8f + index / 2 * .3f, elapsed) : 0f;
            float protect = isStory
                ? SmoothRange(ImpactTime - .02f, ImpactTime + .24f, elapsed) *
                    (1f - .65f * SmoothRange(9.1f, 10.2f, elapsed)) * (1f - dismay)
                : 0f;

            // Decelerate the path before freezing it, independently of the arm/head reaction.
            float movementTime = ambientTime;
            if (isStory && elapsed > noticedAt)
            {
                float stopAge = Mathf.Clamp(elapsed - noticedAt, 0f, WalkStopDuration);
                movementTime = _ambientStartTime + noticedAt + stopAge - stopAge * stopAge / (2f * WalkStopDuration);
            }
            bool strolls = index == 0 || index == 2 || index == 5;
            float stride = Mathf.Sin(movementTime * 4.8f + index * 1.3f);
            float walking = strolls ? 1f - notice : 0f;
            Vector3 position = _citizenPoses[index].Position;
            Quaternion rotation = _citizenPoses[index].Rotation;
            if (strolls)
            {
                float routePhase = movementTime * .62f + index * 1.3f;
                float initialPhase = index * 1.3f;
                Vector3 offset = new Vector3(
                    .68f * (Mathf.Sin(routePhase) - Mathf.Sin(initialPhase)),
                    0f,
                    .38f * (Mathf.Cos(routePhase) - Mathf.Cos(initialPhase)));
                Vector3 tangent = new Vector3(.68f * Mathf.Cos(routePhase), 0f, -.38f * Mathf.Sin(routePhase));
                position += rotation * offset;
                rotation *= Quaternion.LookRotation(tangent, Vector3.up);
            }

            Vector3 portalDirection = WraithArrivalPosition - position;
            portalDirection.y = 0f;
            position -= portalDirection.normalized * (notice * .62f);
            Quaternion portalFacing = Quaternion.LookRotation(portalDirection, Vector3.up);
            rotation = Quaternion.Slerp(rotation, portalFacing, notice);
            Vector3 screenPosition = transform.InverseTransformPoint(_screenTargets[index].position);
            Vector3 screenDirection = screenPosition - position;
            screenDirection.y = 0f;
            rotation = Quaternion.Slerp(rotation, Quaternion.LookRotation(screenDirection, Vector3.up), dismay);

            float idleSway = Mathf.Sin(ambientTime * 1.5f + index * 1.7f);
            float flinch = isStory && elapsed >= ImpactTime
                ? Mathf.Sin((elapsed - ImpactTime) * 24f) * Mathf.Exp(-(elapsed - ImpactTime) * 3.5f)
                : 0f;
            position.y += Mathf.Abs(stride) * .035f * walking - protect * .14f;
            position += rotation * Vector3.back * (flinch * .06f);
            rotation *= Quaternion.Euler(protect * 3f, idleSway * 2f * (1f - notice), idleSway * 1.5f * (1f - protect));
            _citizens[index].localPosition = position;
            _citizens[index].localRotation = rotation;

            Quaternion idleHead = _headPoses[index].Rotation * Quaternion.Euler(0f, idleSway * (strolls ? 10f : 18f), 0f);
            Vector3 headPosition = position + rotation * _headPoses[index].Position;
            Quaternion portalHead = Quaternion.Inverse(rotation) * Quaternion.LookRotation(WraithArrivalPosition - headPosition, Vector3.up);
            Quaternion screenHead = Quaternion.Inverse(rotation) * Quaternion.LookRotation(screenPosition - headPosition, Vector3.up);
            Quaternion headRotation = Quaternion.Slerp(idleHead, portalHead, notice);
            headRotation = Quaternion.Slerp(headRotation, Quaternion.Euler(28f, 0f, 0f), protect);
            headRotation = Quaternion.Slerp(headRotation, screenHead * Quaternion.Euler(9f, Mathf.Sin(ambientTime * 5f + index) * 13f, 0f), dismay);
            _heads[index].localRotation = headRotation;

            AnimateArms(index, stride, idleSway, walking, notice, protect, dismay, ambientTime);
            AnimateLegs(index, stride, walking, notice, protect, elapsed - noticedAt);
        }

        private void AnimateArms(int index, float stride, float idleSway, float walking, float notice, float protect, float dismay, float ambientTime)
        {
            float conversation = index % 2 == 1 ? Mathf.Max(0f, idleSway) : 0f;
            Quaternion left = Quaternion.Euler(stride * 25f * walking - conversation * 35f, 0f, -6f - conversation * 15f);
            Quaternion right = Quaternion.Euler(-stride * 25f * walking - conversation * 18f, 0f, 6f + conversation * 12f);
            // Alternating residents point at the apparition or throw both hands up in surprise.
            bool points = index % 2 == 0;
            left = Quaternion.Slerp(left, Quaternion.Euler(points ? -68f : -105f, -12f, points ? -20f : -38f), notice);
            right = Quaternion.Slerp(right, Quaternion.Euler(points ? -125f : -105f, 10f, points ? 7f : 38f), notice);
            left = Quaternion.Slerp(left, Quaternion.Euler(-154f, -12f, -23f), protect);
            right = Quaternion.Slerp(right, Quaternion.Euler(-154f, 12f, 23f), protect);
            float gesture = Mathf.Sin(ambientTime * 3.5f + index) * 14f;
            left = Quaternion.Slerp(left, Quaternion.Euler(-58f + gesture, -10f, -48f), dismay);
            right = Quaternion.Slerp(right, Quaternion.Euler(-58f - gesture, 10f, 48f), dismay);
            _leftArms[index].localRotation = _leftArmPoses[index].Rotation * left;
            _rightArms[index].localRotation = _rightArmPoses[index].Rotation * right;
        }

        private void AnimateLegs(int index, float stride, float walking, float notice, float protect, float noticeAge)
        {
            float retreatStep = noticeAge > 0f && noticeAge < 1.1f
                ? Mathf.Sin(noticeAge / 1.1f * Mathf.PI * 2f) * notice
                : 0f;
            float step = stride * walking - retreatStep * .8f;
            float leftHip = -step * 30f;
            float rightHip = step * 30f;
            float leftKnee = Mathf.Max(0f, step) * 42f;
            float rightKnee = Mathf.Max(0f, -step) * 42f;
            leftHip = Mathf.Lerp(leftHip, -52f, protect);
            rightHip = Mathf.Lerp(rightHip, -52f, protect);
            leftKnee = Mathf.Lerp(leftKnee, 100f, protect);
            rightKnee = Mathf.Lerp(rightKnee, 100f, protect);
            _leftHips[index].localRotation = _leftHipPoses[index].Rotation * Quaternion.Euler(leftHip, 0f, 0f);
            _rightHips[index].localRotation = _rightHipPoses[index].Rotation * Quaternion.Euler(rightHip, 0f, 0f);
            _leftKnees[index].localRotation = _leftKneePoses[index].Rotation * Quaternion.Euler(leftKnee, 0f, 0f);
            _rightKnees[index].localRotation = _rightKneePoses[index].Rotation * Quaternion.Euler(rightKnee, 0f, 0f);
            _leftFeet[index].localRotation = _leftFootPoses[index].Rotation * Quaternion.Euler(-leftHip - leftKnee, 0f, 0f);
            _rightFeet[index].localRotation = _rightFootPoses[index].Rotation * Quaternion.Euler(-rightHip - rightKnee, 0f, 0f);
        }

        private static float SmoothRange(float from, float to, float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, value));
        }

        private void AnimateFactory(float elapsed, float ambientTime)
        {
            float overload = Mathf.InverseLerp(6.8f, ImpactTime, elapsed);
            bool damaged = elapsed >= ImpactTime;
            Color cyan = new Color(.08f, .9f, .82f);
            Color warning = new Color(1f, .15f, .38f);
            Color coreColor = damaged ? new Color(.15f, .025f, .05f) : Color.Lerp(cyan, warning, overload);
            SetLightColor(_coreSurface, coreColor, damaged ? .15f : 1.4f + Mathf.Sin(ambientTime * (3f + overload * 8f)) * .3f);
            for (int i = 0; i < _rotors.Length; i++)
            {
                _rotors[i].localRotation = _rotorPoses[i].Rotation * Quaternion.Euler(0f, ambientTime * (65f + i * 16f), 0f);
                _rotors[i].localPosition = _rotorPoses[i].Position +
                    Vector3.right * (damaged ? .12f * Mathf.Sin((elapsed - ImpactTime) * 20f) * Mathf.Exp(-(elapsed - ImpactTime) * 2f) : 0f);
            }
            for (int i = 0; i < _factoryLights.Length; i++)
            {
                float flicker = damaged ? .06f : 1f + overload * Mathf.Sin(elapsed * 16f + i);
                SetLightColor(_factoryLights[i], damaged ? new Color(.2f, .035f, .025f) : Color.Lerp(cyan, warning, overload), Mathf.Max(.05f, flicker));
            }
            for (int i = 0; i < _supplyLights.Length; i++)
            {
                float failure = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8.6f + i * .19f, 9.1f + i * .19f, elapsed));
                SetLightColor(_supplyLights[i], Color.Lerp(cyan, new Color(.035f, .07f, .09f), failure), 1.2f * (1f - failure));
            }
        }

        private void AnimateDamage(float elapsed)
        {
            float age = Mathf.Max(0f, elapsed - ImpactTime);
            for (int i = 0; i < _debris.Length; i++)
            {
                float flight = Mathf.Clamp(age - i * .035f, 0f, 2.8f);
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 velocity = new Vector3(side * (1.7f + i % 4 * .5f), 3.7f + i % 3 * .7f, -1.5f - i % 3 * .7f);
                Vector3 position = _debrisPoses[i].Position + velocity * flight + Vector3.down * (3.6f * flight * flight);
                position.y = Mathf.Max(.4f, position.y);
                _debris[i].localPosition = position;
                _debris[i].localRotation = _debrisPoses[i].Rotation * Quaternion.Euler(flight * 70f, flight * side * 46f, flight * side * 90f);
            }
            for (int i = 0; i < _smoke.Length; i++)
            {
                Transform smoke = _smoke[i];
                bool visible = age > .2f;
                smoke.gameObject.SetActive(visible);
                if (visible)
                {
                    float growth = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 2.5f));
                    smoke.localPosition = _smokePoses[i].Position + Vector3.up * (Mathf.Sin(age * .7f + i) * .2f + growth * .6f);
                    smoke.localScale = _smokePoses[i].Scale * growth * (1f + Mathf.Sin(age * 1.3f + i) * .07f);
                }
            }
            bool invite = elapsed >= 14f;
            _entranceBeacon.gameObject.SetActive(invite);
            _entranceBeacon.localScale = _beaconScale * (1f + Mathf.Sin(elapsed * 2.4f) * .045f);
        }

        private void SetLightColor(Renderer target, Color color, float emission)
        {
            _propertyBlock.Clear();
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            _propertyBlock.SetColor(EmissionColorId, color * emission);
            target.SetPropertyBlock(_propertyBlock);
        }
    }
}

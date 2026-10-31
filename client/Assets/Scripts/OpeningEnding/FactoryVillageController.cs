using System;
using UnityEngine;

namespace OpeningEnding
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
        private static readonly float[] WalkArcDistances = BuildWalkArcDistances();

        [SerializeField] private VillageResidentMotionLibrary _residentMotions;
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
        private VillageResidentMotionController[] _residentControllers;
        private LocalPose[] _debrisPoses;
        private LocalPose[] _smokePoses;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _beaconScale;
        private float _ambientStartTime;
        private bool _initialized;
        private bool _residentThanksActive;
        private bool _residentDeparting;
        private Vector3 _thanksFromPosition;
        private Quaternion _thanksFromRotation;
        private float _thanksMotionTime;
        private float _thanksStartedAt;
        private float _departureMotionTime;
        private readonly Vector3 _thanksWorldPosition = new Vector3(-1.1f, 0f, 3.2f);
        private readonly Vector3 _asideWorldPosition = new Vector3(-2.6f, 0f, 4.2f);

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
            _residentControllers = new VillageResidentMotionController[_citizens.Length];
            for (int i = 0; i < _citizens.Length; i++)
            {
                VillageResidentMotionController resident = _citizens[i].gameObject.AddComponent<VillageResidentMotionController>();
                resident.Bind(_residentMotions, _heads[i], _leftArms[i], _rightArms[i],
                    _leftHips[i], _rightHips[i], _leftKnees[i], _rightKnees[i], _leftFeet[i], _rightFeet[i]);
                _residentControllers[i] = resident;
            }
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
            AnimateClouds(ambientTime);
            for (int i = 0; i < _citizens.Length; i++)
            {
                AnimateResident(i, ambientTime, elapsed, isStory);
            }
        }

        private void AnimateClouds(float ambientTime)
        {
            for (int i = 0; i < _clouds.Length; i++)
            {
                _clouds[i].localPosition = _cloudPoses[i].Position +
                    Vector3.right * (Mathf.Sin(ambientTime * .09f + i) - Mathf.Sin(i)) * 1.5f;
            }
        }

        private void AnimateResident(int index, float ambientTime, float elapsed, bool isStory)
        {
            float noticedAt = WraithArrivalTime + index * .12f;
            float age = elapsed - noticedAt;
            float notice = isStory ? SmoothRange(noticedAt, noticedAt + WalkStopDuration, elapsed) : 0f;
            float dismay = isStory ? SmoothRange(10.5f + index / 2 * .3f, 11.8f + index / 2 * .3f, elapsed) : 0f;
            float protect = isStory ? SmoothRange(ImpactTime, ImpactTime + .3f, elapsed) *
                (1f - SmoothRange(9.1f, 10.3f, elapsed)) : 0f;
            float movementTime = ambientTime;
            if (isStory && age > 0f)
            {
                float stopAge = Mathf.Clamp(age, 0f, WalkStopDuration);
                movementTime = _ambientStartTime + noticedAt + stopAge - stopAge * stopAge / (2f * WalkStopDuration);
            }
            GetResidentPath(index, movementTime, out Vector3 position, out Quaternion rotation);
            Vector3 portalDirection = WraithArrivalPosition - position;
            portalDirection.y = 0f;
            position -= portalDirection.normalized * notice * .42f;
            if (Strolls(index)) position.x = Mathf.Clamp(position.x, -2.85f, 2.85f);
            rotation = Quaternion.Slerp(rotation, Quaternion.LookRotation(portalDirection, Vector3.up), notice);
            Vector3 screenPosition = transform.InverseTransformPoint(_screenTargets[index].position);
            Vector3 screenDirection = screenPosition - position;
            screenDirection.y = 0f;
            rotation = Quaternion.Slerp(rotation, Quaternion.LookRotation(screenDirection, Vector3.up), dismay);

            VillageResidentMotionController resident = _residentControllers[index];
            resident.Sample(Strolls(index) ? VillageResidentMotionLibrary.Motion.Walk : VillageResidentMotionLibrary.Motion.Idle,
                Strolls(index) ? WalkSampleTime(index, movementTime) : movementTime + index * .31f);
            resident.Blend(VillageResidentMotionLibrary.Motion.Idle, ambientTime + index * .53f, notice);
            if (isStory && age > 0f)
            {
                // Standard has no dedicated Surprise clip. Its real chest recoil becomes the apparition reaction.
                float recoil = SmoothRange(0f, .12f, age) * (1f - SmoothRange(.6f, 1.35f, age));
                resident.Blend(VillageResidentMotionLibrary.Motion.HitChest, age * .36f, recoil, false);
            }
            resident.Blend(VillageResidentMotionLibrary.Motion.Crouch, ambientTime + index * .23f, protect);
            float impactAge = Mathf.Max(0f, elapsed - ImpactTime);
            float headHit = isStory ? SmoothRange(0f, .08f, impactAge) * (1f - SmoothRange(.45f, .9f, impactAge)) : 0f;
            resident.Blend(VillageResidentMotionLibrary.Motion.HitHead, impactAge * .65f, headHit * .65f, false);
            resident.Blend(VillageResidentMotionLibrary.Motion.Talking, ambientTime + index * .47f, dismay * .8f);
            Vector3 lookTarget = Vector3.Lerp(_screenTargets[index].position, transform.TransformPoint(WraithArrivalPosition), notice);
            lookTarget = Vector3.Lerp(lookTarget, _screenTargets[index].position, dismay);
            float watching = Strolls(index) ? 0f : (1f - notice) * .8f;
            float lookWeight = Mathf.Max(watching, Mathf.Max(notice * (1f - protect) * .8f, dismay * .8f));
            resident.Apply(position, rotation, lookTarget, lookWeight);
        }

        private static bool Strolls(int index) => index == 0 || index == 2 || index == 5;

        private static float[] BuildWalkArcDistances()
        {
            const int segments = 64;
            float[] distances = new float[segments + 1];
            Vector2 previous = new Vector2(0f, 1.1f);
            for (int i = 1; i <= segments; i++)
            {
                float phase = i * (2f * Mathf.PI / segments);
                Vector2 point = new Vector2(.38f * Mathf.Sin(phase), 1.1f * Mathf.Cos(phase));
                distances[i] = distances[i - 1] + Vector2.Distance(previous, point);
                previous = point;
            }
            return distances;
        }

        private static float RouteDistance(float phase)
        {
            float turns = phase / (2f * Mathf.PI);
            float completeTurns = Mathf.Floor(turns);
            float sample = (turns - completeTurns) * 64f;
            int first = Mathf.Min(63, Mathf.FloorToInt(sample));
            return completeTurns * WalkArcDistances[64] +
                Mathf.Lerp(WalkArcDistances[first], WalkArcDistances[first + 1], sample - first);
        }

        private static float WalkSampleTime(int index, float movementTime)
        {
            // Advance the authored gait by distance, so the narrow turns also slow the footsteps.
            float phase = index * 1.3f;
            float distance = RouteDistance(phase + movementTime * .55f) - RouteDistance(phase);
            return distance * (1.3333333f / .9f) + index * .31f;
        }

        private void GetResidentPath(int index, float time, out Vector3 position, out Quaternion rotation)
        {
            position = _citizenPoses[index].Position;
            rotation = _citizenPoses[index].Rotation;
            if (!Strolls(index)) return;
            // Narrow walking loops stay on the central paved paths and outside the viewing benches.
            float phase = time * .55f + index * 1.3f;
            position.x = Mathf.Clamp(position.x, -2.4f, 2.4f);
            position += new Vector3(.38f * Mathf.Sin(phase), 0f, 1.1f * Mathf.Cos(phase));
            Vector3 tangent = new Vector3(.38f * Mathf.Cos(phase), 0f, -1.1f * Mathf.Sin(phase));
            rotation = Quaternion.LookRotation(tangent, Vector3.up);
        }

        public void BeginRestoration()
        {
            Initialize();
            _ambientStartTime = 0f;
            EvaluateRestoration(0f);
        }

        public void EvaluateRestoration(float elapsed)
        {
            Initialize();
            AnimateRestoredFactory(elapsed);
            for (int i = 0; i < _citizens.Length; i++) AnimateRestoredResident(i, elapsed);
        }

        public void BeginEndingRepair()
        {
            Initialize();
            _residentThanksActive = false;
            _residentDeparting = false;
            _ambientStartTime = 0f;
            EvaluateEndingWaiting(0f);
        }

        public void EvaluateEndingWaiting(float factoryTime)
        {
            AnimateRestoredFactory(factoryTime);
            // The factory can run again while its low-resolution input still needs upscaling.
            // Keep everyone watching; celebration starts only after the screen comparison.
            for (int i = 0; i < _citizens.Length; i++)
            {
                GetDamagedResidentPose(i, out Vector3 position, out Quaternion facing);
                VillageResidentMotionController resident = _residentControllers[i];
                resident.Sample(VillageResidentMotionLibrary.Motion.Talking, factoryTime + i * .47f);
                resident.Blend(VillageResidentMotionLibrary.Motion.Idle, factoryTime + i * .53f,
                    SmoothRange(8f, 12f, factoryTime));
                resident.Apply(position, facing, _screenTargets[i].position, .8f);
            }
        }

        public void EvaluateEndingCelebration(float factoryTime, float celebrationTime)
        {
            AnimateRestoredFactory(factoryTime);
            for (int i = 0; i < _citizens.Length; i++)
            {
                if (_residentThanksActive && i == 0) continue;
                AnimateRestoredResident(i, 13f + celebrationTime);
            }
        }

        public void BeginResidentThanks(float celebrationTime, float ambientTime)
        {
            // Snapshot the currently displayed path position, rather than restarting a route.
            _residentThanksActive = true;
            _residentDeparting = false;
            _thanksFromPosition = _citizens[0].localPosition;
            _thanksFromRotation = _citizens[0].localRotation;
            _thanksMotionTime = WalkSampleTime(0, Mathf.Max(0f, celebrationTime - 6f));
            _thanksStartedAt = ambientTime;
        }

        public void EvaluateResidentThanks(float elapsed, bool departing, Vector3 playerPosition, float ambientTime)
        {
            if (departing && !_residentDeparting)
            {
                _residentDeparting = true;
                _thanksFromPosition = _citizens[0].localPosition;
                _thanksFromRotation = _citizens[0].localRotation;
                _departureMotionTime = _thanksMotionTime + ambientTime - _thanksStartedAt;
            }
            float duration = departing ? 2.5f : 3f;
            Vector3 target = transform.InverseTransformPoint(departing ? _asideWorldPosition : _thanksWorldPosition);
            float progress = SmoothRange(0f, duration, elapsed);
            Vector3 position = Vector3.Lerp(_thanksFromPosition, target, progress);
            Vector3 travel = target - _thanksFromPosition;
            travel.y = 0f;
            Quaternion walkingFacing = Quaternion.LookRotation(travel, Vector3.up);
            Quaternion facing = Quaternion.Slerp(_thanksFromRotation, walkingFacing, SmoothRange(0f, .45f, elapsed));
            Vector3 playerDirection = transform.InverseTransformPoint(playerPosition) - position;
            playerDirection.y = 0f;
            Quaternion playerFacing = Quaternion.LookRotation(playerDirection, Vector3.up);
            float arriving = SmoothRange(duration - .65f, duration, elapsed);
            facing = Quaternion.Slerp(facing, playerFacing, arriving);
            float distance = travel.magnitude * progress;
            VillageResidentMotionController resident = _residentControllers[0];
            // The walk advances by travelled distance, slowing smoothly at the destination.
            float walkingTime = (departing ? _departureMotionTime : _thanksMotionTime) + distance * (1.3333333f / .9f);
            resident.Sample(VillageResidentMotionLibrary.Motion.Walk, walkingTime);
            if (departing)
            {
                resident.Blend(VillageResidentMotionLibrary.Motion.Talking, _departureMotionTime + elapsed,
                    1f - SmoothRange(0f, .4f, elapsed));
            }
            // Dialogue pages may restart their local timer; keep this gesture clock continuous.
            resident.Blend(departing ? VillageResidentMotionLibrary.Motion.Idle : VillageResidentMotionLibrary.Motion.Talking,
                _thanksMotionTime + ambientTime - _thanksStartedAt, arriving);
            resident.Apply(position, facing, playerPosition, arriving * .65f);
        }

        public void EvaluateResidentAside(float ambientTime, Vector3 playerPosition)
        {
            Vector3 position = transform.InverseTransformPoint(_asideWorldPosition);
            Vector3 direction = transform.InverseTransformPoint(playerPosition) - position;
            direction.y = 0f;
            VillageResidentMotionController resident = _residentControllers[0];
            resident.Sample(VillageResidentMotionLibrary.Motion.Idle, _thanksMotionTime + ambientTime - _thanksStartedAt);
            resident.Apply(position, Quaternion.LookRotation(direction, Vector3.up), playerPosition, .65f);
        }

        private void AnimateRestoredFactory(float elapsed)
        {
            float coreRecovery = SmoothRange(3f, 8f, elapsed);
            AnimateClouds(elapsed);
            AnimateDamage(16f);
            _entranceBeacon.gameObject.SetActive(false);
            Color cyan = new Color(.08f, .9f, .82f);
            SetLightColor(_coreSurface, Color.Lerp(new Color(.15f, .025f, .05f), cyan, coreRecovery),
                Mathf.Lerp(.15f, 1.4f + Mathf.Sin(elapsed * 3f) * .2f, coreRecovery));
            for (int i = 0; i < _factoryLights.Length; i++)
            {
                float restored = SmoothRange(3.4f + i * .14f, 7.8f + i * .14f, elapsed);
                SetLightColor(_factoryLights[i], Color.Lerp(new Color(.2f, .035f, .025f), cyan, restored), Mathf.Lerp(.06f, 1.1f, restored));
            }
            for (int i = 0; i < _supplyLights.Length; i++)
            {
                float restored = SmoothRange(7f + i * .12f, 8.5f + i * .12f, elapsed);
                SetLightColor(_supplyLights[i], Color.Lerp(new Color(.035f, .07f, .09f), cyan, restored), 1.2f * restored);
            }
            // Integral of the acceleration curve keeps the restarted rotors continuous at both ends.
            float ramp = Mathf.Clamp01((elapsed - 3f) / 5f);
            float rotorTime = ImpactTime + 5f * (ramp * ramp * ramp - .5f * ramp * ramp * ramp * ramp) + Mathf.Max(0f, elapsed - 8f);
            for (int i = 0; i < _rotors.Length; i++)
            {
                _rotors[i].localPosition = _rotorPoses[i].Position;
                _rotors[i].localRotation = _rotorPoses[i].Rotation * Quaternion.Euler(0f, rotorTime * (65f + i * 16f), 0f);
            }
            for (int i = 0; i < _debris.Length; i++)
            {
                float repair = SmoothRange(3.2f + i * .08f, 6.2f + i * .08f, elapsed);
                _debris[i].localPosition = Vector3.Lerp(_debris[i].localPosition, _debrisPoses[i].Position, repair);
                _debris[i].localRotation = Quaternion.Slerp(_debris[i].localRotation, _debrisPoses[i].Rotation, repair);
            }
            for (int i = 0; i < _smoke.Length; i++)
            {
                float smokeFade = 1f - SmoothRange(3f, 7f, elapsed);
                _smoke[i].gameObject.SetActive(smokeFade > .001f);
                _smoke[i].localScale *= smokeFade;
            }
        }

        private void AnimateRestoredResident(int index, float elapsed)
        {
            float recovery = SmoothRange(10f + index * .15f, 13.4f + index * .15f, elapsed);
            float celebration = SmoothRange(13f + index * .14f, 14.4f + index * .14f, elapsed) *
                (1f - SmoothRange(18f, 20f, elapsed));
            float peaceful = SmoothRange(19f, 21f, elapsed);
            GetDamagedResidentPose(index, out Vector3 damagedPosition, out Quaternion screenFacing);
            GetResidentPath(index, Mathf.Max(0f, elapsed - 19f), out Vector3 peacefulPosition, out Quaternion peacefulFacing);
            Vector3 position = Vector3.Lerp(damagedPosition, peacefulPosition, peaceful);
            Quaternion facing = Quaternion.Slerp(screenFacing, peacefulFacing, peaceful);
            VillageResidentMotionController resident = _residentControllers[index];
            resident.Sample(VillageResidentMotionLibrary.Motion.Talking, elapsed + index * .47f);
            resident.Blend(VillageResidentMotionLibrary.Motion.Idle, elapsed + index * .53f, recovery);
            resident.Blend(VillageResidentMotionLibrary.Motion.Dance, (elapsed - 13f) * .85f + index * .17f, celebration);
            resident.Blend(Strolls(index) ? VillageResidentMotionLibrary.Motion.Walk : VillageResidentMotionLibrary.Motion.Idle,
                Strolls(index) ? WalkSampleTime(index, Mathf.Max(0f, elapsed - 19f)) : elapsed + index * .31f, peaceful);
            resident.Apply(position, facing, _screenTargets[index].position, (1f - peaceful) * (1f - celebration) * .8f);
        }

        private void GetDamagedResidentPose(int index, out Vector3 position, out Quaternion facing)
        {
            float noticedAt = WraithArrivalTime + index * .12f;
            GetResidentPath(index, noticedAt + WalkStopDuration * .5f, out position, out Quaternion ignored);
            Vector3 away = WraithArrivalPosition - position;
            away.y = 0f;
            position -= away.normalized * .42f;
            if (Strolls(index)) position.x = Mathf.Clamp(position.x, -2.85f, 2.85f);
            Vector3 screenDirection = transform.InverseTransformPoint(_screenTargets[index].position) - position;
            screenDirection.y = 0f;
            facing = Quaternion.LookRotation(screenDirection, Vector3.up);
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

using UnityEngine;

namespace OpeningEnding
{
    public sealed class VillageResidentMotionController : MonoBehaviour
    {
        private VillageResidentMotionLibrary _library;
        private readonly VillageResidentMotionLibrary.Frame _pose = VillageResidentMotionLibrary.NewFrame();
        private readonly VillageResidentMotionLibrary.Frame _blend = VillageResidentMotionLibrary.NewFrame();
        private Transform _body;
        private Transform _head;
        private Transform[] _joints;
        private Quaternion _headRest;
        private float _floorHeight;

        public void Bind(VillageResidentMotionLibrary library, Transform head, Transform leftArm, Transform rightArm,
            Transform leftHip, Transform rightHip, Transform leftKnee, Transform rightKnee, Transform leftFoot, Transform rightFoot)
        {
            _library = library;
            _head = head;
            _headRest = head.localRotation;
            _floorHeight = transform.localPosition.y + .06f;
            // Give the same block-built resident a waist and elbows. Meshes and materials remain shared.
            _body = new GameObject("Resident waist").transform;
            _body.SetParent(transform, false);
            _body.localPosition = new Vector3(0f, .74f, 0f);
            Transform jacket = transform.Find("Jacket");
            if (jacket != null) jacket.SetParent(_body, true);
            head.SetParent(_body, true);
            leftArm.SetParent(_body, true);
            rightArm.SetParent(_body, true);
            Transform leftElbow = MakeElbow(leftArm);
            Transform rightElbow = MakeElbow(rightArm);
            _joints = new[] { _body, head, leftArm, rightArm, leftElbow, rightElbow,
                leftHip, rightHip, leftKnee, rightKnee, leftFoot, rightFoot };
        }

        private static Transform MakeElbow(Transform shoulder)
        {
            Transform sleeve = shoulder.Find("Sleeve");
            Transform hand = shoulder.Find("Hand");
            Transform elbow = new GameObject("Elbow").transform;
            elbow.SetParent(shoulder, false);
            elbow.localPosition = new Vector3(0f, -.27f, 0f);
            if (sleeve != null)
            {
                sleeve.localPosition = new Vector3(0f, -.125f, 0f);
                sleeve.localScale = new Vector3(.19f, .27f, .22f);
                GameObject forearm = new GameObject("Forearm sleeve");
                forearm.transform.SetParent(elbow, false);
                forearm.transform.localPosition = new Vector3(0f, -.115f, 0f);
                forearm.transform.localScale = new Vector3(.19f, .25f, .22f);
                forearm.AddComponent<MeshFilter>().sharedMesh = sleeve.GetComponent<MeshFilter>().sharedMesh;
                MeshRenderer original = sleeve.GetComponent<MeshRenderer>();
                MeshRenderer renderer = forearm.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = original.sharedMaterials;
                renderer.shadowCastingMode = original.shadowCastingMode;
                renderer.receiveShadows = original.receiveShadows;
                renderer.lightProbeUsage = original.lightProbeUsage;
                renderer.reflectionProbeUsage = original.reflectionProbeUsage;
            }
            if (hand != null)
            {
                hand.SetParent(elbow, false);
                hand.localPosition = new Vector3(0f, -.26f, .03f);
            }
            return elbow;
        }

        public void Sample(VillageResidentMotionLibrary.Motion motion, float time, bool loop = true)
        {
            _library.Sample(motion, time, loop, _pose);
        }

        public void Blend(VillageResidentMotionLibrary.Motion motion, float time, float weight, bool loop = true)
        {
            if (weight <= 0f) return;
            _library.Sample(motion, time, loop, _blend);
            _pose.GroundClearance = Mathf.Lerp(_pose.GroundClearance, _blend.GroundClearance, weight);
            for (int i = 0; i < VillageResidentMotionLibrary.JointCount; i++)
                _pose.Rotations[i] = Quaternion.Slerp(_pose.Rotations[i], _blend.Rotations[i], weight);
        }

        public void Apply(Vector3 position, Quaternion facing, Vector3 lookAt, float lookWeight)
        {
            transform.localPosition = position;
            transform.localRotation = facing;
            Quaternion worldFacing = transform.rotation;
            // Curves are already in resident space. World assignment avoids double-applying a parent's bend.
            for (int i = 0; i < _joints.Length; i++)
                _joints[i].rotation = worldFacing * _pose.Rotations[i] * (i == 1 ? _headRest : Quaternion.identity);

            // Match the lowest shoe sole to the path, including bent-knee impacts and the dance bounce.
            float leftSole = LowestShoePoint(_joints[10]);
            float rightSole = LowestShoePoint(_joints[11]);
            float ground = transform.parent.TransformPoint(new Vector3(position.x, _floorHeight + _pose.GroundClearance, position.z)).y;
            transform.position += Vector3.up * (ground - Mathf.Min(leftSole, rightSole));

            if (lookWeight > 0f)
            {
                Vector3 direction = lookAt - _head.position;
                if (direction.sqrMagnitude > .01f)
                {
                    Quaternion desired = Quaternion.LookRotation(direction, transform.up);
                    _head.rotation = Quaternion.Slerp(_head.rotation, desired, lookWeight);
                }
            }
        }

        private static float LowestShoePoint(Transform shoe)
        {
            float lowest = float.PositiveInfinity;
            // The original shoe is a scaled unit cube with a forward toe.
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    lowest = Mathf.Min(lowest, shoe.TransformPoint(new Vector3(x * .5f, -.5f, z * .5f)).y);
            return lowest;
        }
    }
}

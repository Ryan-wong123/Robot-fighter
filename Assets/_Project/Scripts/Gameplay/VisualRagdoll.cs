using System.Collections.Generic;
using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    internal sealed class VisualRagdoll
    {
        private const float ReactionDuration = 0.48f;

        private const float InitialDeflectionDegrees = 40f;
        private const float ImpactAngularSpeed = 9f;

        private readonly List<Rigidbody> bodies = new List<Rigidbody>();
        private readonly List<Collider> shapes = new List<Collider>();
        private readonly Collider fighterCollider;
        private readonly Transform torso;
        private readonly Transform leg;
        private Rigidbody reactingBody;
        private Quaternion simulatedRotation;
        private Vector3 simulatedPosition;
        private float reactionTime;
        public bool Full { get; private set; }

        public VisualRagdoll(Transform visual)
        {
            fighterCollider = visual.parent.GetComponent<Collider>();
            foreach (var bone in visual.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "Spine")
                    torso = bone;
                else if (bone.name == "Upper_Leg.L")
                    leg = bone;
            }

            var physicalBones = new Dictionary<Transform, Rigidbody>();
            foreach (var bone in visual.GetComponentsInChildren<Transform>())
            {
                if (!IsPhysicalBone(bone.name))
                    continue;

                var body = bone.gameObject.AddComponent<Rigidbody>();
                body.mass = bone.name == "Pelvis" ? 3f : 1f;
                body.isKinematic = true;
                body.constraints = RigidbodyConstraints.FreezePositionZ;
                var shape = bone.gameObject.AddComponent<SphereCollider>();
                shape.radius = bone.name == "Spine" ? 0.25f : 0.13f;
                shape.enabled = false;
                shapes.Add(shape);
                bodies.Add(body);
                physicalBones.Add(bone, body);

                var ancestor = bone.parent;
                while (ancestor != null && !physicalBones.ContainsKey(ancestor))
                    ancestor = ancestor.parent;
                if (ancestor == null)
                    continue;

                var joint = bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = physicalBones[ancestor];
                joint.enableCollision = false;
            }
        }

        private static bool IsPhysicalBone(string name) =>
            name == "Pelvis" || name == "Spine" || name == "Head" ||
            name.StartsWith("Upper_Arm.") || name.StartsWith("Lower_Arm.") ||
            name.StartsWith("Upper_Leg.") || name.StartsWith("Lower_Leg.");

        public void React(Vector3 direction, HurtZone zone)
        {
            EndReaction();
            Transform bone = zone == HurtZone.Legs ? leg : torso;
            if (Full || bone == null || !bone.TryGetComponent(out reactingBody))
                return;

            reactionTime = ReactionDuration;
            reactingBody.useGravity = false;
            reactingBody.isKinematic = false;
            reactingBody.rotation = Quaternion.AngleAxis(-InitialDeflectionDegrees * Mathf.Sign(direction.x), Vector3.forward) * reactingBody.rotation;
            reactingBody.AddTorque(Vector3.forward * -Mathf.Sign(direction.x) * ImpactAngularSpeed,
                ForceMode.VelocityChange);
        }

        public void CaptureHitPose()
        {
            if (reactingBody == null)
                return;

            simulatedRotation = reactingBody.rotation;
            simulatedPosition = reactingBody.position;
        }

        public void BlendHit(float seconds)
        {
            if (reactingBody == null)
                return;

            reactionTime -= seconds;
            if (reactionTime <= 0f)
            {
                EndReaction();
                return;
            }

            float weight = Mathf.SmoothStep(0f, 1f, reactionTime / ReactionDuration);
            var bone = reactingBody.transform;
            bone.SetPositionAndRotation(Vector3.Lerp(bone.position, simulatedPosition, weight),
                Quaternion.Slerp(bone.rotation, simulatedRotation, weight));
        }

        private void EndReaction()
        {
            if (reactingBody == null)
                return;

            reactingBody.isKinematic = true;
            reactingBody.useGravity = true;
            reactingBody = null;
        }

        public void Launch(Vector3 direction)
        {
            if (bodies.Count == 0 || Full)
                return;

            EndReaction();
            Full = true;
            foreach (var shape in shapes)
                shape.enabled = true;
            for (int index = 0; index < shapes.Count; index++)
            {
                if (fighterCollider != null)
                    Physics.IgnoreCollision(shapes[index], fighterCollider);
                for (int previous = 0; previous < index; previous++)
                    Physics.IgnoreCollision(shapes[index], shapes[previous]);
            }
            foreach (var body in bodies)
            {
                body.isKinematic = false;
                body.AddForce(direction * 3f + Vector3.up * 2f, ForceMode.VelocityChange);
            }
        }

        internal void ApplyWaterGravity(float multiplier)
        {
            if (multiplier >= 1f)
                return;
            Vector3 lift = Physics.gravity * (multiplier - 1f);
            foreach (var body in bodies)
                if (!body.isKinematic)
                    body.AddForce(lift, ForceMode.Acceleration);
        }

        public void Reset()
        {
            EndReaction();
            foreach (var body in bodies)
                if (body != null)
                    body.isKinematic = true;
            foreach (var shape in shapes)
                if (shape != null)
                    shape.enabled = false;
            Full = false;
            reactionTime = 0f;
        }
    }
}

using System;
using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    /// <summary>Persistent damaged seams with bounded, one-shot cotton feedback.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class StuffingVisual : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer skin;
        [SerializeField] private ParticleSystem cotton;
        [SerializeField] private Transform[] anchors;
        [SerializeField] private AudioSource clothAudio;
        [SerializeField] private AudioSource winAudio;
        [SerializeField, Min(1)] private float revealSpeed = 500f;
        private Fighter owner;
        private readonly int[] indices = { -1, -1, -1 };
        private readonly float[] weights = new float[3];
        private int revealed;

        private void Awake()
        {
            owner = GetComponentInParent<Fighter>();
            string[] names = { "Stuffing75", "Stuffing50", "Stuffing25" };
            for (int shape = 0; shape < skin.sharedMesh.blendShapeCount; shape++)
                for (int stage = 0; stage < names.Length; stage++)
                    if (skin.sharedMesh.GetBlendShapeName(shape).EndsWith(names[stage], StringComparison.Ordinal))
                        indices[stage] = shape;
        }

        private void OnEnable()
        {
            if (owner == null) return;
            owner.VitalsChanged += UpdateStage;
            owner.PresentationReset += ResetFeedback;
            owner.VictoryCelebrated += Celebrate;
        }

        private void OnDisable()
        {
            if (owner != null)
            {
                owner.VitalsChanged -= UpdateStage;
                owner.PresentationReset -= ResetFeedback;
                owner.VictoryCelebrated -= Celebrate;
            }
            StopEffects();
        }

        private void UpdateStage(Fighter fighter)
        {
            int next = fighter.StuffingStage;
            if (next <= revealed) return;
            revealed = next;
            cotton.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            cotton.transform.position = anchors[next - 1].position;
            cotton.Play();
            cotton.Emit(16);
            if (clothAudio != null) clothAudio.Play();
        }

        private void ResetFeedback()
        {
            StopEffects();
            if (owner == null || owner.Health != owner.MaxHealth) return;
            revealed = 0;
            for (int stage = 0; stage < indices.Length; stage++)
            {
                weights[stage] = 0f;
                if (indices[stage] >= 0) skin.SetBlendShapeWeight(indices[stage], 0f);
            }
        }

        private void StopEffects()
        {
            if (cotton != null) cotton.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (clothAudio != null) clothAudio.Stop();
            if (winAudio != null) winAudio.Stop();
        }

        private void Celebrate()
        {
            if (winAudio != null) winAudio.Play();
        }

        private void LateUpdate()
        {
            if (owner == null || owner.IsHitStopped) return;
            for (int stage = 0; stage < indices.Length; stage++)
            {
                weights[stage] = Mathf.MoveTowards(weights[stage], revealed > stage ? 100f : 0f,
                    revealSpeed * Time.deltaTime);
                if (indices[stage] >= 0) skin.SetBlendShapeWeight(indices[stage], weights[stage]);
            }
        }
    }
}

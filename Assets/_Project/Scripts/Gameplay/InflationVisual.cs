using System;
using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    /// <summary>Blend-shape feedback for the frame-based inflation state.</summary>
    public sealed class InflationVisual : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer skin;
        [SerializeField] private ParticleSystem airBurst;
        [SerializeField] private AudioSource popAudio;
        [SerializeField, Min(1)] private float blendSpeed = 400f;
        [SerializeField] private Vector3 inflateSocketOffset;
        [SerializeField] private Vector3 deflateSocketOffset;
        private Fighter owner;
        private Transform socket;
        private Vector3 socketRest;
        private int inflate = -1, deflate = -1;
        private float inflateWeight, deflateWeight;

        private void Awake()
        {
            owner = GetComponentInParent<Fighter>();
            if (TryGetComponent<FighterVisual>(out var references))
            {
                socket = references.ItemSocket;
                if (socket != null) socketRest = socket.localPosition;
            }
            for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
            {
                string name = skin.sharedMesh.GetBlendShapeName(i);
                if (name.EndsWith("Inflate", StringComparison.Ordinal)) inflate = i;
                if (name.EndsWith("Deflate", StringComparison.Ordinal)) deflate = i;
            }
        }

        private void OnEnable()
        {
            if (owner != null)
            {
                owner.InflationPopped += Pop;
                owner.PresentationReset += ResetFeedback;
            }
        }

        private void OnDisable()
        {
            if (owner != null)
            {
                owner.InflationPopped -= Pop;
                owner.PresentationReset -= ResetFeedback;
            }
            ResetFeedback();
        }

        private void ResetFeedback()
        {
            inflateWeight = deflateWeight = 0f;
            if (socket != null) socket.localPosition = socketRest;
            if (inflate >= 0) skin.SetBlendShapeWeight(inflate, 0f);
            if (deflate >= 0) skin.SetBlendShapeWeight(deflate, 0f);
            if (airBurst != null) airBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (popAudio != null) popAudio.Stop();
        }

        private void Pop()
        {
            airBurst?.Play();
            if (popAudio != null) popAudio.Play();
        }

        private void LateUpdate()
        {
            if (owner == null || owner.IsHitStopped) return;
            inflateWeight = Mathf.MoveTowards(inflateWeight, owner.IsBellyBlimp ? 150f :
                owner.IsInflated ? 100f : 0f, blendSpeed * Time.deltaTime);
            deflateWeight = Mathf.MoveTowards(deflateWeight, owner.IsDeflated ? 100f : 0f,
                blendSpeed * Time.deltaTime);
            if (inflate >= 0) skin.SetBlendShapeWeight(inflate, inflateWeight);
            if (deflate >= 0) skin.SetBlendShapeWeight(deflate, deflateWeight);
            if (socket != null)
                socket.localPosition = socketRest + inflateSocketOffset * (inflateWeight / 100f) +
                    deflateSocketOffset * (deflateWeight / 100f);
        }
    }
}

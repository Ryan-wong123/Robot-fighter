using System;
using UnityEngine;
using WobbleBrawl.Gameplay;

namespace WobbleBrawl.UI
{
    public enum ImpactKind { Light, Heavy, Special, Block, GuardBreak, Knockout }

    [Serializable]
    public sealed class ImpactStyle
    {
        public string text;
        public string tint;
        public float lifetime;
        public float size;

        public Color Tint => ColorUtility.TryParseHtmlString(tint, out var color) ? color : Color.white;
    }

    [Serializable]
    public sealed class ImpactPalette
    {
        public int heavyDamage;
        public ImpactStyle[] styles;

        public ImpactKind Classify(DamageResult result, bool knockedOut, bool special)
        {
            if (knockedOut) return ImpactKind.Knockout;
            if (result.GuardBroken) return ImpactKind.GuardBreak;
            if (result.Blocked) return ImpactKind.Block;
            if (special) return ImpactKind.Special;
            return result.Damage >= heavyDamage ? ImpactKind.Heavy : ImpactKind.Light;
        }
    }
}

using System;

namespace WobbleBrawl.Core
{
    /// <summary>Side-effect-free geometry for the fairground's two hazards.</summary>
    public static class FairgroundRules
    {
        public static float Squeeze(float progress, float rampFraction)
        {
            if (rampFraction <= 0f || rampFraction > .5f)
            {
                throw new ArgumentOutOfRangeException(nameof(rampFraction));
            }
            return Math.Clamp(Math.Min(progress, 1f - progress) / rampFraction, 0f, 1f);
        }

        public static float HalfWidth(float normalHalfWidth, float reduction, float weight)
        {
            return normalHalfWidth * (1f - reduction * Math.Clamp(weight, 0f, 1f));
        }

        public static bool OnPad(float x, float feetHeight, float center, float halfWidth, float contactHeight)
        {
            return Math.Abs(x - center) <= halfWidth && feetHeight >= -.1f && feetHeight <= contactHeight;
        }
    }
}

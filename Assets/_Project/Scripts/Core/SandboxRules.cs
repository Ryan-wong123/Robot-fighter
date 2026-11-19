using System;

namespace WobbleBrawl.Core
{
    /// <summary>Pure swept contact and ground-patch geometry for Giant Sandbox.</summary>
    public static class SandboxRules
    {
        public static bool PawContact(float previous, float current, float position, float halfWidth,
            float feetHeight, float clearance, bool blocking)
        {
            return !blocking && feetHeight < clearance &&
                position >= Math.Min(previous, current) - halfWidth &&
                position <= Math.Max(previous, current) + halfWidth;
        }

        public static bool InSand(float position, float center, float halfWidth, float feetHeight, float contactHeight)
        {
            return Math.Abs(position - center) <= halfWidth && feetHeight >= -.1f && feetHeight <= contactHeight;
        }

        public static float PawPosition(float progress, float travelHalfWidth, bool fromLeft)
        {
            float value = travelHalfWidth * (Math.Clamp(progress, 0f, 1f) * 2f - 1f);
            return fromLeft ? value : -value;
        }
    }
}

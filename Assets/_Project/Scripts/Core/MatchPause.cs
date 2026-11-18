using UnityEngine;

namespace WobbleBrawl.Core
{
    public static class MatchPause
    {
        private static float resumeScale = 1f;
        public static bool IsPaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsPaused = false;
            resumeScale = 1f;
        }

        public static void Pause()
        {
            if (IsPaused)
                return;
            resumeScale = Time.timeScale;
            IsPaused = true;
            Time.timeScale = 0f;
        }

        public static void Resume()
        {
            if (!IsPaused)
                return;
            IsPaused = false;
            Time.timeScale = resumeScale;
        }

        public static void SetGameplayScale(float scale)
        {
            if (IsPaused)
                resumeScale = scale;
            else
                Time.timeScale = scale;
        }
    }
}

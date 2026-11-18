using System;
using UnityEngine;

namespace WobbleBrawl.Core
{
    public enum ControlLayout { Standard, Mirrored }
    public enum SoundBus { Music, Sfx }

    public static class GameSettings
    {
        public static float Master { get; private set; } = 1f;
        public static float Music { get; private set; } = 1f;
        public static float Sfx { get; private set; } = 1f;
        public static bool Haptics { get; private set; } = true;
        public static ControlLayout Layout { get; private set; }
        public static event Action Changed;

        public static void SetMaster(float value) { Master = Mathf.Clamp01(value); Apply(); }
        public static void SetMusic(float value) { Music = Mathf.Clamp01(value); Apply(); }
        public static void SetSfx(float value) { Sfx = Mathf.Clamp01(value); Apply(); }
        public static void SetHaptics(bool value) { Haptics = value; Apply(); }
        public static void SetLayout(ControlLayout value) { Layout = value; Apply(); }
        public static void SetQuality(int index)
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(index, 0, QualitySettings.names.Length - 1), true);
            Changed?.Invoke();
        }

        private static void Apply()
        {
            AudioListener.volume = Master;
            Changed?.Invoke();
        }

        public static void Vibrate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Haptics)
                Handheld.Vibrate();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Master = Music = Sfx = 1f;
            Haptics = true;
            Layout = ControlLayout.Standard;
            Changed = null;
        }
    }

}

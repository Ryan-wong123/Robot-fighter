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
        private static SaveService saves;
        private static SaveData saved;
        public static string SaveError => saves?.LastError;

        public static void SetMaster(float value) { Master = Mathf.Clamp01(value); Apply(); }
        public static void SetMusic(float value) { Music = Mathf.Clamp01(value); Apply(); }
        public static void SetSfx(float value) { Sfx = Mathf.Clamp01(value); Apply(); }
        public static void SetHaptics(bool value) { Haptics = value; Apply(); }
        public static void SetLayout(ControlLayout value) { Layout = value; Apply(); }
        public static void SetQuality(int index)
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(index, 0, QualitySettings.names.Length - 1), true);
            Persist();
            Changed?.Invoke();
        }

        private static void Apply()
        {
            AudioListener.volume = Master;
            Persist();
            Changed?.Invoke();
        }

        private static void Persist()
        {
            if (saves == null || saved == null)
                return;
            saved.Settings = new SavedSettings
            {
                Master = Master, Music = Music, Sfx = Sfx, Haptics = Haptics,
                Layout = Layout, Quality = QualitySettings.GetQualityLevel()
            };
            saves.Save(saved);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadSavedSettings()
        {
            saves = new SaveService(System.IO.Path.Combine(Application.persistentDataPath, "save.json"));
            saved = saves.Load();
            var settings = saved.Settings;
            Master = settings.Master;
            Music = settings.Music;
            Sfx = settings.Sfx;
            Haptics = settings.Haptics;
            Layout = settings.Layout;
            if (settings.Quality >= 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(settings.Quality, 0, QualitySettings.names.Length - 1), true);
            AudioListener.volume = Master;
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
            saves = null;
            saved = null;
        }
    }

}

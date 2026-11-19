using UnityEngine;
using UnityEngine.SceneManagement;

namespace WobbleBrawl.Core
{
    public enum AudioCue { Click, Hit, Heavy, Block, GuardBreak, Special, Knockout, Round, Win }

    public sealed class AudioManager : MonoBehaviour
    {
        private const int PoolSize = 8;
        private const float MusicGain = .35f;
        private const float SfxGain = .7f;
        private static AudioManager running;
        private readonly AudioSource[] voices = new AudioSource[PoolSize];
        private AudioClip[] clips;
        private AudioSource music;
        private AudioSource menuVoice;
        private AudioListener fallbackListener;
        private bool wasPaused;
        private float duckUntil;
        public int VoiceCapacity => voices.Length;
        public AudioSource MusicSource => music;

        public static void Ensure(Transform parent)
        {
            if (running == null)
                parent.gameObject.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            if (running != null && running != this)
            {
                Destroy(this);
                return;
            }
            running = this;
            fallbackListener = gameObject.AddComponent<AudioListener>();
            clips = new AudioClip[System.Enum.GetValues(typeof(AudioCue)).Length];
            for (int index = 0; index < clips.Length; index++)
                clips[index] = Resources.Load<AudioClip>("Audio/" + (AudioCue)index);
            music = NewSource("Music");
            music.loop = true;
            music.ignoreListenerPause = true;
            menuVoice = NewSource("Menu SFX");
            menuVoice.ignoreListenerPause = true;
            for (int index = 0; index < voices.Length; index++)
                voices[index] = NewSource("SFX " + index);
            SceneManager.sceneLoaded += SceneLoaded;
            GameSettings.Changed += ApplyLevels;
            PlayMusic(SceneManager.GetActiveScene().name);
            ApplyLevels();
            SelectListener(SceneManager.GetActiveScene());
        }

        private AudioSource NewSource(string label)
        {
            var node = new GameObject(label);
            node.transform.SetParent(transform, false);
            var source = node.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void OnDestroy()
        {
            if (running != this)
                return;
            SceneManager.sceneLoaded -= SceneLoaded;
            GameSettings.Changed -= ApplyLevels;
            running = null;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SelectListener(scene);
            foreach (var voice in voices)
                voice.Stop();
            duckUntil = 0f;
            PlayMusic(scene.name);
            ApplyLevels();
        }

        private void SelectListener(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var listener in root.GetComponentsInChildren<AudioListener>())
                    if (listener != fallbackListener && listener.isActiveAndEnabled)
                    {
                        fallbackListener.enabled = false;
                        return;
                    }
            fallbackListener.enabled = true;
        }

        private void PlayMusic(string scene)
        {
            string key = scene switch
            {
                "BathtubBrawl" => "BathtubMusic",
                "KitchenCounter" => "KitchenMusic",
                "BouncyCastleFair" => "FairMusic",
                "CombatSandbox" => "SandboxMusic",
                _ => "MenuMusic"
            };
            var clip = Resources.Load<AudioClip>("Audio/" + key);
            if (music.clip == clip && music.isPlaying)
                return;
            music.clip = clip;
            if (clip != null)
                music.Play();
        }

        private void ApplyLevels()
        {
            music.volume = MusicGain * GameSettings.Music * (Time.unscaledTime < duckUntil ? .4f : 1f);
            menuVoice.volume = SfxGain * GameSettings.Sfx;
            foreach (var voice in voices)
                voice.volume = SfxGain * GameSettings.Sfx;
        }

        private void Update()
        {
            if (MatchPause.IsPaused != wasPaused)
            {
                wasPaused = MatchPause.IsPaused;
                foreach (var voice in voices)
                {
                    if (wasPaused) voice.Pause();
                    else voice.UnPause();
                }
            }
            if (duckUntil > 0f && Time.unscaledTime >= duckUntil)
            {
                duckUntil = 0f;
                ApplyLevels();
            }
        }

        public static bool Play(AudioCue cue)
        {
            if (running == null)
                return false;
            var clip = running.clips[(int)cue];
            if (clip == null)
                return false;
            if (cue == AudioCue.Click)
            {
                running.menuVoice.clip = clip;
                running.menuVoice.Play();
                return true;
            }
            if (MatchPause.IsPaused)
                return false;
            foreach (var voice in running.voices)
            {
                if (voice.isPlaying)
                    continue;
                voice.clip = clip;
                voice.Play();
                if (cue == AudioCue.Knockout || cue == AudioCue.Win)
                {
                    running.duckUntil = Time.unscaledTime + clip.length;
                    running.ApplyLevels();
                }
                return true;
            }
            return false;
        }
    }
}

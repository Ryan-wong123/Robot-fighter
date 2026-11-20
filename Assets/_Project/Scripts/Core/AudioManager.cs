using UnityEngine;
using UnityEngine.SceneManagement;

namespace WobbleBrawl.Core
{
    public enum AudioCue { Click, Hit, Heavy, Block, GuardBreak, Special, Knockout, Round, Win,
        ItemPie, ItemBoomerang, ItemPogo, ItemBubble, ItemCar, ItemJack }

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
        private AudioSource bark;
        private AudioSource announcer;
        private AudioClip queuedAnnouncement;
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<AudioClip>> voiceClips =
            new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<AudioClip>>(System.StringComparer.Ordinal);
        public static float MusicDucking => running != null && Time.unscaledTime < running.duckUntil ? .4f : 1f;
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
            bark = NewSource("Fighter Voice");
            announcer = NewSource("Announcer");
            foreach (var clip in Resources.LoadAll<AudioClip>("Voices"))
            {
                int separator = clip.name.LastIndexOf("_v", System.StringComparison.Ordinal);
                if (separator < 0) continue;
                string key = clip.name.Substring(0, separator);
                if (!voiceClips.TryGetValue(key, out var variants))
                    voiceClips.Add(key, variants = new System.Collections.Generic.List<AudioClip>());
                variants.Add(clip);
            }
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
            bark.Stop();
            announcer.Stop();
            queuedAnnouncement = null;
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
                "GiantSandbox" => "SandMusic",
                "ToyChestShowdown" => "", // The stage controls its own tempo.
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
            music.volume = MusicGain * GameSettings.Music * MusicDucking;
            bark.volume = .75f * GameSettings.Voice;
            announcer.volume = .88f * GameSettings.Voice;
            menuVoice.volume = SfxGain * GameSettings.Sfx;
            foreach (var voice in voices)
                voice.volume = SfxGain * GameSettings.Sfx;
        }

        private void Update()
        {
            if (MatchPause.IsPaused != wasPaused)
            {
                wasPaused = MatchPause.IsPaused;
                if (wasPaused) { bark.Pause(); announcer.Pause(); }
                else { bark.UnPause(); announcer.UnPause(); }
                foreach (var voice in voices)
                {
                    if (wasPaused) voice.Pause();
                    else voice.UnPause();
                }
            }
            if (!wasPaused && queuedAnnouncement != null && !announcer.isPlaying)
            {
                var clip = queuedAnnouncement;
                queuedAnnouncement = null;
                if (GameSettings.Voice > 0f) StartAnnouncement(clip);
            }
            if (duckUntil > 0f && Time.unscaledTime >= duckUntil)
            {
                duckUntil = 0f;
                ApplyLevels();
            }
        }

        public static bool PlayVoice(string key, bool priority = false)
        {
            if (running == null || MatchPause.IsPaused || GameSettings.Voice <= 0f ||
                !running.voiceClips.TryGetValue(key, out var variants))
                return false;
            if (!priority && (running.announcer.isPlaying || running.bark.isPlaying))
                return false;
            var source = priority ? running.announcer : running.bark;

            int index = Random.Range(0, variants.Count);
            if (variants.Count > 1 && variants[index] == source.clip)
                index = (index + 1) % variants.Count;
            if (priority)
            {
                running.bark.Stop();
                if (running.announcer.isPlaying)
                    running.queuedAnnouncement = variants[index];
                else
                    running.StartAnnouncement(variants[index]);
            }
            else
            {
                source.clip = variants[index];
                source.Play();
            }
            return true;
        }

        private void StartAnnouncement(AudioClip clip)
        {
            announcer.clip = clip;
            announcer.Play();
            duckUntil = Mathf.Max(duckUntil, Time.unscaledTime + clip.length);
            ApplyLevels();
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
                voice.pitch = cue == AudioCue.Hit || cue == AudioCue.Heavy || cue == AudioCue.Block
                    ? Random.Range(.94f, 1.06f) : 1f;
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

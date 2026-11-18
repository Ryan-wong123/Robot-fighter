using UnityEngine;
namespace WobbleBrawl.Core
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SettingsAudioSource : MonoBehaviour
    {
        [SerializeField] private SoundBus bus = SoundBus.Sfx;
        private AudioSource source;
        private float originalVolume;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            originalVolume = source.volume;
        }
        private void OnEnable() { GameSettings.Changed += Apply; Apply(); }
        private void OnDisable() => GameSettings.Changed -= Apply;
        private void Apply() => source.volume = originalVolume * (bus == SoundBus.Music ? GameSettings.Music : GameSettings.Sfx);
    }
}

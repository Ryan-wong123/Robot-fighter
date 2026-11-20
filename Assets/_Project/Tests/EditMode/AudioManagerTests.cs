using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using WobbleBrawl.Core;

public sealed class AudioManagerTests
{
    [Test]
    public void FinalClipsArePresentAndNonempty()
    {
        foreach (string key in new[] { "MenuMusic", "BathtubMusic", "KitchenMusic", "TrainingMusic",
            "Click", "Hit", "Heavy", "Block", "GuardBreak", "Special", "Knockout", "Round", "Win" })
        {
            var clip = Resources.Load<AudioClip>("Audio/" + key);
            Assert.That(clip, Is.Not.Null, key);
            Assert.That(clip.samples, Is.GreaterThan(100), key);
        }
    }

    [UnityTest]
    public IEnumerator AudioManagerUsesVolumeSettingsAndReusesVoices()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        yield return ExerciseAudio();
        yield return new ExitPlayMode();
    }

    private static IEnumerator ExerciseAudio()
    {
        var manager = Object.FindFirstObjectByType<AudioManager>();
        Assert.That(manager, Is.Not.Null);
        Assert.That(manager.MusicSource.clip.name, Is.EqualTo("MenuMusic"));
        Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
            .Count(listener => listener.isActiveAndEnabled), Is.EqualTo(1));
        float music = GameSettings.Music;
        GameSettings.SetMusic(.25f);
        Assert.That(manager.MusicSource.volume, Is.EqualTo(.0875f).Within(.001f));
        int sources = manager.GetComponentsInChildren<AudioSource>().Length;
        Assert.That(sources, Is.EqualTo(manager.VoiceCapacity + 4));
        for (int index = 0; index < 30; index++)
            AudioManager.Play(AudioCue.Hit);
        Assert.That(manager.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(sources));
        float voice = GameSettings.Voice;
        try
        {
            GameSettings.SetVoice(0f);
            Assert.That(AudioManager.PlayVoice("announcer_fight", true), Is.False);
            GameSettings.SetVoice(1f);
            Assert.That(AudioManager.PlayVoice("announcer_fight", true), Is.True);
            Assert.That(AudioManager.PlayVoice("blobby_attack_light"), Is.False);
        }
        finally
        {
            GameSettings.SetVoice(voice);
        }
        MatchPause.Pause();
        Assert.That(AudioManager.Play(AudioCue.Hit), Is.False);
        Assert.That(AudioManager.Play(AudioCue.Click), Is.True);
        MatchPause.Resume();
        GameSettings.SetMusic(music);
        var loading = SceneManager.LoadSceneAsync("KitchenCounter");
        float deadline = Time.realtimeSinceStartup + 30f;
        while (!loading.isDone && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(loading.isDone, Is.True, "Kitchen Counter load timed out.");
        yield return null;
        Assert.That(manager.MusicSource.clip.name, Is.EqualTo("KitchenMusic"));
        Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
            .Count(listener => listener.isActiveAndEnabled), Is.EqualTo(1));
    }
}

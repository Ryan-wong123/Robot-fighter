using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace WobbleBrawl.Core
{
    [Serializable]
    public sealed class SavedSettings
    {
        public float Master = 1f;
        public float Music = 1f;
        public float Sfx = 1f;
        public bool Haptics = true;
        public ControlLayout Layout;
        public int Quality = -1;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int Version = SaveService.CurrentVersion;
        public SavedSettings Settings = new SavedSettings();
        public int Coins;
        public List<string> UnlockedFighters = new List<string> { "Blobby" };
    }

    public sealed class SaveService
    {
        public const int CurrentVersion = 2;
        private static readonly object FileGate = new object();
        private readonly string path;
        public bool IsReadOnly { get; private set; }
        public string LastError { get; private set; }

        [Serializable]
        private sealed class Header { public int Version; }
        [Serializable]
        private sealed class LegacySave
        {
            public int Version;
            public float Master = 1f;
            public float Music = 1f;
            public float Sfx = 1f;
            public bool Haptics = true;
            public ControlLayout Layout;
            public int Quality = -1;
            public int Coins;
        }

        public SaveService(string filePath)
        {
            path = Path.GetFullPath(filePath);
        }

        public SaveData Load()
        {
            lock (FileGate)
            {
                LastError = null;
                IsReadOnly = false;
                SaveData data = Read(path);
                if (data != null)
                    return data;
                data = Read(path + ".bak");
                return data ?? new SaveData();
            }
        }

        public bool Save(SaveData data)
        {
            lock (FileGate)
            {
                if (IsReadOnly)
                    return false;
                Normalize(data);
                string temporary = path + ".tmp";
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
                    using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush(true);
                    }

                    if (File.Exists(path))
                    {
                        // Keep the last readable save when recovering from a damaged primary file.
                        string backup = Read(path) != null && !IsReadOnly ? path + ".bak" : null;
                        if (IsReadOnly)
                            return false;
                        File.Replace(temporary, path, backup);
                    }
                    else
                        File.Move(temporary, path);
                    LastError = null;
                    return true;
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    LastError = error.Message;
                    return false;
                }
                finally
                {
                    try { if (File.Exists(temporary)) File.Delete(temporary); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        private SaveData Read(string file)
        {
            if (!File.Exists(file))
                return null;
            try
            {
                string json = File.ReadAllText(file, Encoding.UTF8).Trim();
                if (!json.StartsWith("{", StringComparison.Ordinal) || !json.EndsWith("}", StringComparison.Ordinal))
                    return null;
                var header = JsonUtility.FromJson<Header>(json);
                if (header == null || header.Version <= 0)
                    return null;
                if (header.Version > CurrentVersion)
                {
                    IsReadOnly = true;
                    LastError = "The save was created by a newer game version.";
                    return null;
                }
                SaveData data;
                if (header.Version == 1)
                {
                    var legacy = JsonUtility.FromJson<LegacySave>(json);
                    data = new SaveData
                    {
                        Coins = legacy.Coins,
                        Settings = new SavedSettings
                        {
                            Master = legacy.Master, Music = legacy.Music, Sfx = legacy.Sfx,
                            Haptics = legacy.Haptics, Layout = legacy.Layout, Quality = legacy.Quality
                        }
                    };
                }
                else
                    data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                    return null;
                Normalize(data);
                return data;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                LastError = error.Message;
                return null;
            }
        }

        private static void Normalize(SaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.Version = CurrentVersion;
            data.Settings ??= new SavedSettings();
            var settings = data.Settings;
            settings.Master = Volume(settings.Master);
            settings.Music = Volume(settings.Music);
            settings.Sfx = Volume(settings.Sfx);
            if (!Enum.IsDefined(typeof(ControlLayout), settings.Layout))
                settings.Layout = ControlLayout.Standard;
            settings.Quality = Math.Max(-1, settings.Quality);
            data.Coins = Math.Max(0, data.Coins);
            data.UnlockedFighters ??= new List<string>();
            if (!data.UnlockedFighters.Contains("Blobby"))
                data.UnlockedFighters.Add("Blobby");
        }

        private static float Volume(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
    }
}

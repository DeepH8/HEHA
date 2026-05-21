using UnityEngine;

namespace HEHA.Obby.Core
{
    public static class GameSettings
    {
        const string MusicVolumeKey = "heha_music_volume";
        const float DefaultMusicVolume = 0.5f;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
            set
            {
                PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }
    }
}

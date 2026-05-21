using UnityEngine;

namespace HEHA.Obby.Core
{
    public class BackgroundMusicController : MonoBehaviour
    {
        public static BackgroundMusicController Instance { get; private set; }

        const string ResourcesClipPath = "BackgroundMusic";

        [SerializeField] AudioClip musicClip;
        [SerializeField] AudioSource musicSource;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureMusicSource();
            StartMusic();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void EnsurePlaying()
        {
            if (Instance != null)
            {
                Instance.ApplyVolume();
                if (!Instance.musicSource.isPlaying)
                    Instance.musicSource.Play();
                return;
            }

            AudioClip clip = Resources.Load<AudioClip>(ResourcesClipPath);
            if (clip == null)
                return;

            GameObject go = new GameObject("BackgroundMusic");
            BackgroundMusicController controller = go.AddComponent<BackgroundMusicController>();
            controller.musicClip = clip;
        }

        public void SetMusicVolume(float volume)
        {
            GameSettings.MusicVolume = volume;
            ApplyVolume();
        }

        public void ApplyVolume()
        {
            if (musicSource != null)
                musicSource.volume = GameSettings.MusicVolume;
        }

        void EnsureMusicSource()
        {
            if (musicClip == null)
                musicClip = Resources.Load<AudioClip>(ResourcesClipPath);

            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
                if (musicSource == null)
                    musicSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.clip = musicClip;
        }

        void StartMusic()
        {
            ApplyVolume();
            if (musicClip == null || musicSource == null)
                return;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }
    }
}

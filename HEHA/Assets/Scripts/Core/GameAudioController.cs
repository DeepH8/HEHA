using UnityEngine;

namespace HEHA.Obby.Core
{
    public class GameAudioController : MonoBehaviour
    {
        public static GameAudioController Instance { get; private set; }

        [SerializeField] AudioClip jumpClip;
        [SerializeField] AudioClip deathClip;
        [SerializeField] AudioClip winClip;
        [SerializeField] AudioClip loseClip;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] float jumpVolume = 0.85f;
        [SerializeField] float deathVolume = 0.4f;
        [SerializeField] float winVolume = 0.9f;
        [SerializeField] float loseVolume = 0.9f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureAudioSource();
            EnsureSceneAudioListener();
        }

        void Start()
        {
            EnsureSceneAudioListener();
            BackgroundMusicController.EnsurePlaying();
        }

        public static void EnsureSceneAudioListener()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener listener in listeners)
            {
                if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                    return;
            }

            Camera camera = Camera.main;
            if (camera == null)
                return;

            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void EnsureAudioSource()
        {
            if (sfxSource != null)
                return;

            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
        }

        public void PlayJump() => PlayOneShot(jumpClip, jumpVolume);

        public void PlayDeath() => PlayOneShot(deathClip, deathVolume);

        public void PlayWin() => PlayOneShot(winClip, winVolume);

        public void PlayLose() => PlayOneShot(loseClip, loseVolume);

        void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || sfxSource == null)
                return;

            sfxSource.PlayOneShot(clip, volume);
        }
    }
}

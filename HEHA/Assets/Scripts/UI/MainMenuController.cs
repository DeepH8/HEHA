using HEHA.Obby.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HEHA.Obby.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string gameSceneName = "SampleScene";
        [SerializeField] GameObject menuPanel;
        [SerializeField] GameObject settingsPanel;
        [SerializeField] Button playButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button settingsBackButton;
        [SerializeField] Slider musicVolumeSlider;
        [SerializeField] Text musicVolumeValueText;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            EnsureMenuCanvasVisible();
            ShowMenuPanel();
        }

        void EnsureMenuCanvasVisible()
        {
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            if (canvases.Length == 0)
                canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;

                canvas.gameObject.SetActive(true);
                canvas.enabled = true;
                Vector3 scale = canvas.transform.localScale;
                if (scale.sqrMagnitude < 0.001f)
                    canvas.transform.localScale = Vector3.one;
            }
        }

        void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(StartGame);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(ShowSettingsPanel);

            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(ShowMenuPanel);

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                UpdateMusicVolumeLabel(musicVolumeSlider.value);
            }

            BackgroundMusicController.EnsurePlaying();
            BackgroundMusicController.Instance?.ApplyVolume();
        }

        void OnDestroy()
        {
            if (playButton != null)
                playButton.onClick.RemoveListener(StartGame);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(ShowSettingsPanel);

            if (settingsBackButton != null)
                settingsBackButton.onClick.RemoveListener(ShowMenuPanel);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }

        void OnMusicVolumeChanged(float value)
        {
            if (BackgroundMusicController.Instance != null)
                BackgroundMusicController.Instance.SetMusicVolume(value);
            else
                GameSettings.MusicVolume = value;

            UpdateMusicVolumeLabel(value);
        }

        void UpdateMusicVolumeLabel(float value)
        {
            if (musicVolumeValueText != null)
                musicVolumeValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        void ShowMenuPanel()
        {
            if (menuPanel != null)
                menuPanel.SetActive(true);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        public void ShowSettingsPanel()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(true);

            if (musicVolumeSlider != null)
            {
                float volume = GameSettings.MusicVolume;
                musicVolumeSlider.SetValueWithoutNotify(volume);
                UpdateMusicVolumeLabel(volume);
            }
        }

        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogError("MainMenuController: Game scene name is not set.");
                return;
            }

            BackgroundMusicController.EnsurePlaying();
            SceneManager.LoadScene(gameSceneName);
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HEHA.Obby.UI
{
    public class WinLoseScreenUI : MonoBehaviour
    {
        [SerializeField] string mainMenuSceneName = "MainMenu";
        [SerializeField] string gameSceneName = "SampleScene";
        [SerializeField] GameObject winPanel;
        [SerializeField] GameObject losePanel;
        [SerializeField] Text winMessageText;
        [SerializeField] Text loseMessageText;
        [SerializeField] Button winMainMenuButton;
        [SerializeField] Button winRestartButton;
        [SerializeField] Button loseMainMenuButton;
        [SerializeField] Button loseRestartButton;

        void Awake()
        {
            Hide();
            BindButton(winMainMenuButton, GoToMainMenu);
            BindButton(winRestartButton, RestartGame);
            BindButton(loseMainMenuButton, GoToMainMenu);
            BindButton(loseRestartButton, RestartGame);
        }

        static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        public void Show(bool won, int deathCount, int maxDeaths)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (winPanel != null)
                winPanel.SetActive(won);

            if (losePanel != null)
                losePanel.SetActive(!won);

            if (winMessageText != null)
                winMessageText.text = $"You reached the goal with {deathCount} / {maxDeaths} deaths.";

            if (loseMessageText != null)
                loseMessageText.text = $"You died {deathCount} times before reaching the goal.";
        }

        public void Hide()
        {
            if (winPanel != null)
                winPanel.SetActive(false);

            if (losePanel != null)
                losePanel.SetActive(false);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                Debug.LogError("WinLoseScreenUI: Main menu scene name is not set.");
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogError("WinLoseScreenUI: Game scene name is not set.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HEHA.Obby.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string gameSceneName = "SampleScene";
        [SerializeField] Button playButton;

        void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            EnsureMenuCanvasVisible();
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
        }

        void OnDestroy()
        {
            if (playButton != null)
                playButton.onClick.RemoveListener(StartGame);
        }

        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameSceneName))
            {
                Debug.LogError("MainMenuController: Game scene name is not set.");
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }
    }
}

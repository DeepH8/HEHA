#if UNITY_EDITOR
using System.IO;
using HEHA.Obby.Core;
using HEHA.Obby.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HEHA.Obby.Editor
{
    public static class MainMenuSceneBuilder
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string GameScenePath = "Assets/Scenes/SampleScene.unity";
        public const string GameSceneName = "SampleScene";
        const string BackgroundMusicPath = "Assets/Resources/BackgroundMusic.wav";

        [MenuItem("HEHA/Fix Main Menu Scene")]
        public static void FixMainMenuScene()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/MainMenu.unity")))
            {
                BuildMainMenuScene();
                return;
            }

            ApplyPlayModeAndBuildSettings();
            Debug.Log("HEHA: Main menu play-mode start scene and build order updated.");
        }

        [MenuItem("HEHA/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources"));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            }

            GameObject musicRoot = new GameObject("BackgroundMusic");
            BackgroundMusicController musicController = musicRoot.AddComponent<BackgroundMusicController>();
            SerializedObject musicSo = new SerializedObject(musicController);
            musicSo.FindProperty("musicClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(BackgroundMusicPath);
            musicSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject menuRoot = new GameObject("MainMenu");
            MainMenuController controller = menuRoot.AddComponent<MainMenuController>();
            WireMainMenuUi(controller);

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            ApplyPlayModeAndBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("HEHA: Main menu scene built with settings and background music.");
        }

        public static void ApplyPlayModeAndBuildSettings()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/MainMenu.unity")))
                return;

            var scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };

            EditorBuildSettings.scenes = scenes;

            SceneAsset mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (mainMenu != null)
                EditorSceneManager.playModeStartScene = mainMenu;
        }

        static void WireMainMenuUi(MainMenuController controller)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.transform.localScale = Vector3.one;

            RectTransform menuPanel = CreatePanel(canvasGo.transform, "MenuPanel");
            StretchFull(menuPanel);
            Image menuPanelImage = menuPanel.gameObject.AddComponent<Image>();
            menuPanelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            menuPanelImage.raycastTarget = true;

            CreateText(menuPanel, "Title", "HEHA", 84, new Vector2(0f, 160f), new Vector2(900f, 120f),
                new Color(0.95f, 0.97f, 1f));
            CreateText(menuPanel, "Subtitle", "Obby", 36, new Vector2(0f, 70f), new Vector2(700f, 60f),
                new Color(0.65f, 0.75f, 0.9f));

            Button playButton = CreateButton(menuPanel, "PlayButton", "Play", new Vector2(0f, -60f),
                new Vector2(360f, 88f), new Color(0.2f, 0.45f, 0.95f));
            Button settingsButton = CreateButton(menuPanel, "SettingsButton", "Settings", new Vector2(0f, -170f),
                new Vector2(360f, 88f), new Color(0.25f, 0.3f, 0.4f));

            RectTransform settingsPanel = CreatePanel(canvasGo.transform, "SettingsPanel");
            StretchFull(settingsPanel);
            Image settingsPanelImage = settingsPanel.gameObject.AddComponent<Image>();
            settingsPanelImage.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            settingsPanelImage.raycastTarget = true;
            settingsPanel.gameObject.SetActive(false);

            CreateText(settingsPanel, "SettingsTitle", "Settings", 64, new Vector2(0f, 180f), new Vector2(700f, 90f),
                new Color(0.95f, 0.97f, 1f));
            CreateText(settingsPanel, "MusicLabel", "Music Volume", 32, new Vector2(0f, 40f), new Vector2(500f, 50f),
                new Color(0.8f, 0.85f, 0.95f));

            Slider musicSlider = CreateVolumeSlider(settingsPanel, new Vector2(0f, -30f));
            Text musicValueText = CreateText(settingsPanel, "MusicVolumeValue", "50%", 28,
                new Vector2(0f, -100f), new Vector2(200f, 40f), new Color(0.75f, 0.8f, 0.9f));

            Button backButton = CreateButton(settingsPanel, "BackButton", "Back", new Vector2(0f, -200f),
                new Vector2(300f, 72f), new Color(0.25f, 0.3f, 0.4f));

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("gameSceneName").stringValue = GameSceneName;
            controllerSo.FindProperty("menuPanel").objectReferenceValue = menuPanel.gameObject;
            controllerSo.FindProperty("settingsPanel").objectReferenceValue = settingsPanel.gameObject;
            controllerSo.FindProperty("playButton").objectReferenceValue = playButton;
            controllerSo.FindProperty("settingsButton").objectReferenceValue = settingsButton;
            controllerSo.FindProperty("settingsBackButton").objectReferenceValue = backButton;
            controllerSo.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
            controllerSo.FindProperty("musicVolumeValueText").objectReferenceValue = musicValueText;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static Slider CreateVolumeSlider(RectTransform parent, Vector2 anchoredPos)
        {
            GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.name = "MusicVolumeSlider";
            sliderGo.transform.SetParent(parent, false);

            RectTransform rect = sliderGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 36f);
            rect.anchoredPosition = anchoredPos;

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = GameSettings.MusicVolume;
            slider.wholeNumbers = false;
            return slider;
        }

        static RectTransform CreatePanel(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Text CreateText(RectTransform parent, string name, string text, int fontSize, Vector2 anchoredPos,
            Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchoredPos, Vector2 size,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            StretchFull(textRect);

            Text text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 40;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            return button;
        }
    }
}
#endif

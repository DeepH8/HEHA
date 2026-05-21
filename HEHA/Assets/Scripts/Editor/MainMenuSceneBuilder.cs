#if UNITY_EDITOR
using System.IO;
using HEHA.Obby.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HEHA.Obby.Editor
{
    public static class MainMenuSceneBuilder
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string GameScenePath = "Assets/Scenes/SampleScene.unity";
        public const string GameSceneName = "SampleScene";

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

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            }

            GameObject menuRoot = new GameObject("MainMenu");
            MainMenuController controller = menuRoot.AddComponent<MainMenuController>();
            Button playButton = CreateMenuUi();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("playButton").objectReferenceValue = playButton;
            controllerSo.FindProperty("gameSceneName").stringValue = GameSceneName;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            ApplyPlayModeAndBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("HEHA: Main menu scene built. MainMenu is first in Build Settings.");
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

        static Button CreateMenuUi()
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

            RectTransform panel = CreatePanel(canvasGo.transform, "MenuPanel");
            StretchFull(panel);

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            panelImage.raycastTarget = true;

            CreateText(panel, "Title", "HEHA", 84, new Vector2(0f, 160f), new Vector2(900f, 120f),
                new Color(0.95f, 0.97f, 1f));
            CreateText(panel, "Subtitle", "Obby", 36, new Vector2(0f, 70f), new Vector2(700f, 60f),
                new Color(0.65f, 0.75f, 0.9f));

            Button playButton = CreateButton(panel, "PlayButton", "Play", new Vector2(0f, -80f),
                new Vector2(360f, 88f), new Color(0.2f, 0.45f, 0.95f));

            return playButton;
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

        static void CreateText(RectTransform parent, string name, string text, int fontSize, Vector2 anchoredPos,
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

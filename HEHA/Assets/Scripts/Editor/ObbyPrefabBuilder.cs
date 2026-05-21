#if UNITY_EDITOR
using System.IO;
using HEHA.Obby.Core;
using HEHA.Obby.Obstacles;
using HEHA.Obby.Player;
using HEHA.Obby.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HEHA.Obby.Editor
{
    public static class ObbyPrefabBuilder
    {
        const string PrefabRoot = "Assets/Prefabs";
        const string MaterialRoot = "Assets/Materials";
        const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("HEHA/Build Obby Prefabs And Wire Scene")]
        public static void BuildAll()
        {
            EnsureFolders();
            Material lavaMat = CreateColorMaterial("Lava", new Color(1f, 0.25f, 0.05f));
            Material checkpointMat = CreateColorMaterial("Checkpoint", new Color(0.2f, 0.8f, 1f));
            Material bridgeMat = CreateColorMaterial("Bridge", new Color(0.55f, 0.45f, 0.35f));
            Material floorMat = CreateColorMaterial("Floor", new Color(0.35f, 0.35f, 0.4f));
            Material finishMat = CreateColorMaterial("Finish", new Color(1f, 0.82f, 0.15f));
            Material playerMat = CreateColorMaterial("PlayerBody", new Color(0.2f, 0.45f, 0.95f));

            GameObject playerPrefab = BuildPlayerPrefab(playerMat);
            SavePrefab(playerPrefab, $"{PrefabRoot}/Player/PlayerR6.prefab");

            BuildObstaclePrefabs(lavaMat, checkpointMat, bridgeMat, floorMat, finishMat);
            WireSampleScene(playerPrefab);
            if (!File.Exists(MainMenuSceneBuilder.MainMenuScenePath))
                MainMenuSceneBuilder.BuildMainMenuScene();
            else
                MainMenuSceneBuilder.ApplyPlayModeAndBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("HEHA Obby: Prefabs built and SampleScene wired.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/Player"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/Obstacles"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Materials"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Models/Character"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Animations"));
        }

        static Material CreateColorMaterial(string name, Color color)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader) { color = color };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }

            return mat;
        }

        static GameObject BuildPlayerPrefab(Material bodyMat)
        {
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            GameObject root = new GameObject("PlayerR6");
            root.tag = "Player";
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                root.layer = playerLayer;

            CharacterController cc = root.AddComponent<CharacterController>();
            float cameraPivotHeight = 2.5f;
            if (CharacterModelSetup.IsAvailable)
            {
                cc.height = 1.85f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.925f, 0f);
                cameraPivotHeight = 1.4f;
            }
            else
            {
                cc.height = 5f;
                cc.radius = 1f;
                cc.center = new Vector3(0f, 2.5f, 0f);
            }

            PlayerInput playerInput = root.AddComponent<PlayerInput>();
            playerInput.actions = inputAsset;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            root.AddComponent<RobloxPlayerController>();
            root.AddComponent<RobloxCameraController>();
            root.AddComponent<PlayerDeathHandler>();
            root.AddComponent<LadderClimbState>();
            root.AddComponent<PlayerObstacleContact>();

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            RagdollDisassembler ragdoll = root.AddComponent<RagdollDisassembler>();

            bool usedCharacterModel = CharacterModelSetup.TryAttachToPlayer(root, cc, visualRoot.transform, ragdoll,
                out float modelCameraHeight);
            if (usedCharacterModel)
                cameraPivotHeight = modelCameraHeight;

            if (!usedCharacterModel)
            {
                Transform head = CreateLimb(visualRoot.transform, "Head", new Vector3(0f, 4.5f, 0f), new Vector3(1.2f, 1f, 1f), bodyMat);
                Transform torso = CreateLimb(visualRoot.transform, "Torso", new Vector3(0f, 3f, 0f), new Vector3(2f, 2f, 1f), bodyMat);
                Transform lArm = CreateLimb(visualRoot.transform, "LeftArm", new Vector3(-1.5f, 3f, 0f), new Vector3(1f, 2f, 1f), bodyMat);
                Transform rArm = CreateLimb(visualRoot.transform, "RightArm", new Vector3(1.5f, 3f, 0f), new Vector3(1f, 2f, 1f), bodyMat);
                Transform lLeg = CreateLimb(visualRoot.transform, "LeftLeg", new Vector3(-0.5f, 1f, 0f), new Vector3(1f, 2f, 1f), bodyMat);
                Transform rLeg = CreateLimb(visualRoot.transform, "RightLeg", new Vector3(0.5f, 1f, 0f), new Vector3(1f, 2f, 1f), bodyMat);

                SerializedObject so = new SerializedObject(ragdoll);
                so.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
                SerializedProperty limbs = so.FindProperty("limbs");
                limbs.arraySize = 6;
                SetLimb(limbs, 0, head);
                SetLimb(limbs, 1, torso);
                SetLimb(limbs, 2, lArm);
                SetLimb(limbs, 3, rArm);
                SetLimb(limbs, 4, lLeg);
                SetLimb(limbs, 5, rLeg);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject yawPivot = new GameObject("YawPivot");
            yawPivot.transform.SetParent(root.transform, false);
            yawPivot.transform.localPosition = new Vector3(0f, cameraPivotHeight, 0f);

            GameObject pitchPivot = new GameObject("PitchPivot");
            pitchPivot.transform.SetParent(yawPivot.transform, false);

            GameObject camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(pitchPivot.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.5f, -8f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            RobloxCameraController cameraController = root.GetComponent<RobloxCameraController>();
            SerializedObject camSo = new SerializedObject(cameraController);
            camSo.FindProperty("yawPivot").objectReferenceValue = yawPivot.transform;
            camSo.FindProperty("pitchPivot").objectReferenceValue = pitchPivot.transform;
            camSo.FindProperty("playerCamera").objectReferenceValue = cam;
            camSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        static Transform CreateLimb(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Collider col = go.GetComponent<Collider>();
            col.enabled = false;

            return go.transform;
        }

        static void SetLimb(SerializedProperty limbs, int index, Transform t)
        {
            SerializedProperty element = limbs.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("transform").objectReferenceValue = t;
            element.FindPropertyRelative("rigidbody").objectReferenceValue = t.GetComponent<Rigidbody>();
            element.FindPropertyRelative("collider").objectReferenceValue = t.GetComponent<Collider>();
        }

        static void BuildObstaclePrefabs(Material lavaMat, Material checkpointMat, Material bridgeMat, Material floorMat,
            Material finishMat)
        {
            GameObject lava = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lava.name = "LavaBlock";
            lava.tag = "Lava";
            lava.transform.localScale = new Vector3(4f, 0.5f, 4f);
            lava.GetComponent<Renderer>().sharedMaterial = lavaMat;
            lava.GetComponent<Collider>().isTrigger = true;
            lava.AddComponent<LavaBlock>();
            SavePrefab(lava, $"{PrefabRoot}/Obstacles/LavaBlock.prefab");

            GameObject killVol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            killVol.name = "KillVolume";
            killVol.tag = "KillZone";
            killVol.transform.localScale = new Vector3(50f, 1f, 50f);
            Object.DestroyImmediate(killVol.GetComponent<MeshRenderer>());
            killVol.GetComponent<Collider>().isTrigger = true;
            killVol.AddComponent<KillVolume>();
            SavePrefab(killVol, $"{PrefabRoot}/Obstacles/KillVolume.prefab");

            GameObject ladder = new GameObject("Ladder");
            BoxCollider ladderCol = ladder.AddComponent<BoxCollider>();
            ladderCol.isTrigger = true;
            ladderCol.size = new Vector3(2f, 6f, 1f);
            ladderCol.center = new Vector3(0f, 3f, 0f);
            ladder.AddComponent<LadderVolume>();
            SavePrefab(ladder, $"{PrefabRoot}/Obstacles/Ladder.prefab");

            GameObject spinner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spinner.name = "RotatingObstacle";
            spinner.transform.localScale = new Vector3(6f, 1f, 2f);
            spinner.AddComponent<RotatingObstacle>();
            spinner.AddComponent<ObstacleContactKill>();
            SavePrefab(spinner, $"{PrefabRoot}/Obstacles/RotatingObstacle.prefab");

            GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = "FalseBridge";
            bridge.transform.localScale = new Vector3(4f, 0.4f, 2f);
            bridge.GetComponent<Renderer>().sharedMaterial = bridgeMat;
            Collider bridgeSolid = bridge.GetComponent<Collider>();
            bridgeSolid.isTrigger = false;

            GameObject bridgeTrigger = new GameObject("FallTrigger");
            bridgeTrigger.transform.SetParent(bridge.transform, false);
            bridgeTrigger.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            BoxCollider triggerCol = bridgeTrigger.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(1.05f, 0.5f, 1.05f);

            FalseBridge fb = bridgeTrigger.AddComponent<FalseBridge>();
            SerializedObject fbSo = new SerializedObject(fb);
            fbSo.FindProperty("solidCollider").objectReferenceValue = bridgeSolid;
            fbSo.FindProperty("triggerZone").objectReferenceValue = triggerCol;
            fbSo.FindProperty("targetRenderer").objectReferenceValue = bridge.GetComponent<Renderer>();
            fbSo.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(bridge, $"{PrefabRoot}/Obstacles/FalseBridge.prefab");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "DisappearingFloor";
            floor.transform.localScale = new Vector3(4f, 0.5f, 4f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            Collider floorSolid = floor.GetComponent<Collider>();
            floorSolid.isTrigger = false;

            GameObject stepTrigger = new GameObject("StepTrigger");
            stepTrigger.transform.SetParent(floor.transform, false);
            stepTrigger.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            BoxCollider stepCol = stepTrigger.AddComponent<BoxCollider>();
            stepCol.isTrigger = true;
            stepCol.size = new Vector3(1.02f, 0.2f, 1.02f);
            DisappearingFloorStepDetector detector = stepTrigger.AddComponent<DisappearingFloorStepDetector>();

            DisappearingFloor df = floor.AddComponent<DisappearingFloor>();
            SerializedObject dfSo = new SerializedObject(df);
            dfSo.FindProperty("floorCollider").objectReferenceValue = floorSolid;
            dfSo.FindProperty("floorRenderer").objectReferenceValue = floor.GetComponent<Renderer>();
            dfSo.FindProperty("stepTrigger").objectReferenceValue = stepCol;
            dfSo.ApplyModifiedPropertiesWithoutUndo();

            detector.Bind(df);

            SavePrefab(floor, $"{PrefabRoot}/Obstacles/DisappearingFloor.prefab");

            GameObject checkpoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            checkpoint.name = "Checkpoint";
            checkpoint.tag = "Checkpoint";
            checkpoint.transform.localScale = new Vector3(2f, 0.2f, 2f);
            checkpoint.GetComponent<Renderer>().sharedMaterial = checkpointMat;
            checkpoint.GetComponent<Collider>().isTrigger = true;
            Checkpoint cp = checkpoint.AddComponent<Checkpoint>();
            SerializedObject cpSo = new SerializedObject(cp);
            cpSo.FindProperty("padRenderer").objectReferenceValue = checkpoint.GetComponent<Renderer>();
            cpSo.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(checkpoint, $"{PrefabRoot}/Obstacles/Checkpoint.prefab");

            GameObject finish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            finish.name = "FinishGoal";
            finish.transform.localScale = new Vector3(4f, 6f, 4f);
            finish.GetComponent<Renderer>().sharedMaterial = finishMat;
            finish.GetComponent<Collider>().isTrigger = true;
            finish.AddComponent<FinishGoal>();
            SavePrefab(finish, $"{PrefabRoot}/Obstacles/FinishGoal.prefab");
        }

        static void SavePrefab(GameObject source, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
        }

        static void WireSampleScene(GameObject playerPrefab)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            Camera mainCam = Camera.main;
            if (mainCam != null)
                mainCam.gameObject.SetActive(false);

            GameObject systems = GameObject.Find("GameSystems");
            if (systems == null)
                systems = new GameObject("GameSystems");

            CheckpointManager checkpointManager = systems.GetComponent<CheckpointManager>();
            if (checkpointManager == null)
                checkpointManager = systems.AddComponent<CheckpointManager>();

            ObbySessionManager session = systems.GetComponent<ObbySessionManager>();
            if (session == null)
                session = systems.AddComponent<ObbySessionManager>();

            GameObject spawn = GameObject.Find("SpawnPoint");
            if (spawn == null)
            {
                spawn = new GameObject("SpawnPoint");
                spawn.transform.position = new Vector3(0f, 2f, 0f);
            }

            SerializedObject cmSo = new SerializedObject(checkpointManager);
            cmSo.FindProperty("defaultSpawn").objectReferenceValue = spawn.transform;
            cmSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject smSo = new SerializedObject(session);
            smSo.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            smSo.FindProperty("initialSpawn").objectReferenceValue = spawn.transform;
            smSo.ApplyModifiedPropertiesWithoutUndo();

            WireGameOutcomeSystems(systems);

            if (GameObject.Find("VoidKillVolume") == null)
            {
                GameObject killPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Obstacles/KillVolume.prefab");
                if (killPrefab != null)
                {
                    GameObject kill = (GameObject)PrefabUtility.InstantiatePrefab(killPrefab);
                    kill.name = "VoidKillVolume";
                    kill.transform.position = new Vector3(0f, -25f, 0f);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        static void WireGameOutcomeSystems(GameObject systems)
        {
            EnsureSceneEventSystem();

            GameOutcomeManager outcome = systems.GetComponent<GameOutcomeManager>();
            if (outcome == null)
                outcome = systems.AddComponent<GameOutcomeManager>();

            WinLoseScreenUI resultUi = systems.GetComponentInChildren<WinLoseScreenUI>(true);
            if (resultUi == null)
                resultUi = BuildWinLoseScreenUi(systems.transform);

            GameplayHudUI gameplayHud = systems.GetComponentInChildren<GameplayHudUI>(true);
            if (gameplayHud == null)
                gameplayHud = BuildGameplayHud(systems.transform);

            SerializedObject outcomeSo = new SerializedObject(outcome);
            outcomeSo.FindProperty("maxDeaths").intValue = 10;
            outcomeSo.FindProperty("resultScreen").objectReferenceValue = resultUi;
            outcomeSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject uiSo = new SerializedObject(resultUi);
            uiSo.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            uiSo.FindProperty("gameSceneName").stringValue = MainMenuSceneBuilder.GameSceneName;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            WireGameAudio(systems);
        }

        static void WireGameAudio(GameObject systems)
        {
            GameAudioController audio = systems.GetComponent<GameAudioController>();
            if (audio == null)
                audio = systems.AddComponent<GameAudioController>();

            SerializedObject audioSo = new SerializedObject(audio);
            audioSo.FindProperty("jumpClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Jump.mp3");
            audioSo.FindProperty("deathClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Death.mp3");
            audioSo.FindProperty("winClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Win.mp3");
            audioSo.FindProperty("loseClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Lose.mp3");
            audioSo.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("HEHA/Wire Game Win Lose Systems")]
        public static void WireGameWinLoseSystemsMenu()
        {
            GameObject systems = GameObject.Find("GameSystems");
            if (systems == null)
            {
                Debug.LogWarning("HEHA: GameSystems object not found in the open scene.");
                return;
            }

            WireGameOutcomeSystems(systems);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("HEHA: Win/lose systems wired on GameSystems.");
        }

        static void EnsureSceneEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        static GameplayHudUI BuildGameplayHud(Transform parent)
        {
            GameObject root = new GameObject("GameplayHUD");
            root.transform.SetParent(parent, false);
            GameplayHudUI hud = root.AddComponent<GameplayHudUI>();

            RectTransform canvasRect = HehaUiBuilder.CreateOverlayCanvas(root.transform, "HudCanvas", out Canvas hudCanvas);
            hudCanvas.sortingOrder = 10;
            canvasRect.localScale = Vector3.one;

            RectTransform panel = HehaUiBuilder.CreatePanel(canvasRect, "DeathCounterPanel");
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(24f, -24f);
            panel.sizeDelta = new Vector2(280f, 56f);

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);
            panelImage.raycastTarget = false;

            Text deathText = HehaUiBuilder.CreateText(panel, "DeathCounterText", "Deaths: 0 / 10", 30,
                Vector2.zero, new Vector2(260f, 48f), Color.white, FontStyle.Bold);

            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("deathCounterText").objectReferenceValue = deathText;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            return hud;
        }

        static WinLoseScreenUI BuildWinLoseScreenUi(Transform parent)
        {
            GameObject root = new GameObject("ResultUI");
            root.transform.SetParent(parent, false);
            WinLoseScreenUI screenUi = root.AddComponent<WinLoseScreenUI>();

            HehaUiBuilder.CreateOverlayCanvas(root.transform, "ResultCanvas", out _);

            RectTransform canvasRect = root.transform.GetChild(0).GetComponent<RectTransform>();
            RectTransform backdrop = HehaUiBuilder.CreatePanel(canvasRect, "Backdrop");
            HehaUiBuilder.StretchFull(backdrop);
            Image backdropImage = backdrop.gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.72f);
            backdropImage.raycastTarget = true;

            GameObject winPanel = BuildResultPanel(backdrop, "WinPanel", "You Win!",
                new Color(0.15f, 0.55f, 0.3f, 0.95f), out Text winMessage, out Button winMainMenu,
                out Button winRestart);
            GameObject losePanel = BuildResultPanel(backdrop, "LosePanel", "You Lose!",
                new Color(0.55f, 0.15f, 0.15f, 0.95f), out Text loseMessage, out Button loseMainMenu,
                out Button loseRestart);
            winPanel.SetActive(false);
            losePanel.SetActive(false);

            SerializedObject uiSo = new SerializedObject(screenUi);
            uiSo.FindProperty("winPanel").objectReferenceValue = winPanel;
            uiSo.FindProperty("losePanel").objectReferenceValue = losePanel;
            uiSo.FindProperty("winMessageText").objectReferenceValue = winMessage;
            uiSo.FindProperty("loseMessageText").objectReferenceValue = loseMessage;
            uiSo.FindProperty("winMainMenuButton").objectReferenceValue = winMainMenu;
            uiSo.FindProperty("winRestartButton").objectReferenceValue = winRestart;
            uiSo.FindProperty("loseMainMenuButton").objectReferenceValue = loseMainMenu;
            uiSo.FindProperty("loseRestartButton").objectReferenceValue = loseRestart;
            uiSo.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            uiSo.FindProperty("gameSceneName").stringValue = MainMenuSceneBuilder.GameSceneName;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            return screenUi;
        }

        static GameObject BuildResultPanel(RectTransform parent, string name, string title, Color panelColor,
            out Text messageText, out Button mainMenuButton, out Button restartButton)
        {
            RectTransform panel = HehaUiBuilder.CreatePanel(parent, name);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(720f, 420f);
            panel.anchoredPosition = Vector2.zero;

            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = panelColor;
            panelImage.raycastTarget = true;

            HehaUiBuilder.CreateText(panel, "Title", title, 56, new Vector2(0f, 120f), new Vector2(640f, 90f),
                Color.white, FontStyle.Bold);
            messageText = HehaUiBuilder.CreateText(panel, "Message", string.Empty, 28, new Vector2(0f, 30f),
                new Vector2(620f, 80f), new Color(0.92f, 0.95f, 1f));
            mainMenuButton = HehaUiBuilder.CreateButton(panel, "MainMenuButton", "Main Menu",
                new Vector2(-110f, -120f), new Vector2(260f, 72f), new Color(0.25f, 0.3f, 0.4f));
            restartButton = HehaUiBuilder.CreateButton(panel, "RestartButton", "Restart", new Vector2(110f, -120f),
                new Vector2(260f, 72f), new Color(0.2f, 0.45f, 0.95f));

            return panel.gameObject;
        }
    }
}
#endif

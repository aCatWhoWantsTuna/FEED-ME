using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Custom Inspector for CoverUIManager providing one-click UI hierarchy generation and panel preview buttons.
/// </summary>
[CustomEditor(typeof(CoverUIManager))]
public class CoverUIManagerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CoverUIManager manager = (CoverUIManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Main Menu UI Builder & Preview", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.6f, 0.95f, 0.6f);
        if (GUILayout.Button("Build / Rebuild Main Menu UI in Scene", GUILayout.Height(32)))
        {
            LevelEditorMenuTools.BuildMainMenuUIOnManager(manager);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Switch Active Panel in Scene View:", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. Main Title", GUILayout.Height(26)))
        {
            manager.ShowMainMenuPanel();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
        if (GUILayout.Button("2. Level Select", GUILayout.Height(26)))
        {
            manager.ShowLevelSelectPanel();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
        if (GUILayout.Button("3. Dress-Up", GUILayout.Height(26)))
        {
            manager.ShowDressUpPanel();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(1f, 0.8f, 0.75f);
        if (GUILayout.Button("Reset Saved Player Currency & Level Progress", GUILayout.Height(24)))
        {
            PlayerProgressManager.ResetAllProgress();
        }
        GUI.backgroundColor = Color.white;
    }
}

/// <summary>
/// Custom Inspector for LevelEditorManager providing one-click level export, load, and monster spawning tools.
/// </summary>
[CustomEditor(typeof(LevelEditorManager))]
public class LevelEditorManagerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelEditorManager manager = (LevelEditorManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Editor Actions", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.6f, 0.95f, 0.6f);
        if (GUILayout.Button("Export Current Scene to LevelData", GUILayout.Height(32)))
        {
            manager.ExportCurrentSceneToAsset();
        }

        GUI.backgroundColor = new Color(0.65f, 0.85f, 1f);
        EditorGUI.BeginDisabledGroup(manager.targetLevelData == null);
        if (GUILayout.Button("Load Target LevelData into Scene", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog(
                "Load LevelData into Scene",
                $"Replace current scene monsters and candy waves with '{manager.targetLevelData.levelName}'?",
                "Load",
                "Cancel"))
            {
                manager.ApplyLevelDataToScene(manager.targetLevelData);
            }
        }
        EditorGUI.EndDisabledGroup();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Monster Spawning & Scene Sync", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1f, 0.75f, 0.85f);
        if (GUILayout.Button("+ Spawn Pink Monster", GUILayout.Height(26)))
        {
            manager.SpawnNewMonsterInEditor(MonsterType.Pink);
        }

        GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
        if (GUILayout.Button("+ Spawn Blue Monster", GUILayout.Height(26)))
        {
            manager.SpawnNewMonsterInEditor(MonsterType.Blue);
        }
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Sync Scene Monsters to GameFlowManager", GUILayout.Height(24)))
        {
            manager.SyncSceneMonstersToGameFlowManager();
            EditorUtility.SetDirty(manager.gameFlowManager);
        }
    }
}

/// <summary>
/// Custom Inspector for PauseMenuManager allowing one-click generation of PauseMenuPanel in Edit Mode or Play Mode.
/// </summary>
[CustomEditor(typeof(PauseMenuManager))]
public class PauseMenuManagerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PauseMenuManager manager = (PauseMenuManager)target;

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(0.6f, 0.95f, 0.6f);
        if (GUILayout.Button("Generate PauseMenuPanel in Canvas", GUILayout.Height(30)))
        {
            Undo.RecordObject(manager, "Generate PauseMenuPanel");
            if (manager.windowBoardSprite == null)
            {
                manager.windowBoardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/pic_board_d.png");
            }
            if (manager.buttonFrameSprite == null)
            {
                manager.buttonFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/pic_preview_frame.png");
            }
            GameObject created = manager.GeneratePauseMenuPanel();
            if (created != null)
            {
                Undo.RegisterCreatedObjectUndo(created, "Create PauseMenuPanel");
                Selection.activeGameObject = created;
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }
        GUI.backgroundColor = Color.white;
    }
}

/// <summary>
/// Custom Inspector for Monster providing quick preset buttons for speech bubble placement.
/// </summary>
[CustomEditor(typeof(Monster))]
public class MonsterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Monster monster = (Monster)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Speech Bubble Placement Presets", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Bubble -> Left of Monster"))
        {
            Undo.RecordObject(monster.speechBubble.transform, "Move Speech Bubble Left");
            monster.SetSpeechBubblePresetSide(SpeechBubbleDirection.LeftOfMonster, LevelDatabase.Instance);
            monster.UpdateBubbleText();
            EditorUtility.SetDirty(monster);
        }

        if (GUILayout.Button("Bubble -> Right of Monster"))
        {
            Undo.RecordObject(monster.speechBubble.transform, "Move Speech Bubble Right");
            monster.SetSpeechBubblePresetSide(SpeechBubbleDirection.RightOfMonster, LevelDatabase.Instance);
            monster.UpdateBubbleText();
            EditorUtility.SetDirty(monster);
        }
        EditorGUILayout.EndHorizontal();
    }
}

/// <summary>
/// Editor menu utilities for setting up the Main Menu in Level_0 and batch-exporting existing scenes.
/// </summary>
public static class LevelEditorMenuTools
{
    [MenuItem("Tools/Level Editor/Setup Level_0 as Main Menu (Build UI)")]
    public static void SetupMainMenu()
    {
        string scenePath = "Assets/Scenes/Level_0.unity";
        if (SceneManager.GetActiveScene().path != scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            EditorSceneManager.OpenScene(scenePath);
        }

        CoverUIManager manager = Object.FindAnyObjectByType<CoverUIManager>();
        if (manager == null)
        {
            Debug.LogError("[LevelEditorMenuTools] CoverUIManager not found in Level_0!");
            return;
        }

        BuildMainMenuUIOnManager(manager);
    }

    public static void BuildMainMenuUIOnManager(CoverUIManager manager)
    {
        if (manager == null) return;

        Undo.RecordObject(manager, "Build Main Menu UI");

        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/pic_preview_frame.png");
        Sprite boardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/pic_board_d.png");
        Sprite startBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Cover/pic_cover_start.png");
        Sprite monsterCoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Cover/pic_cover_monster.png");
        Sprite arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/pic_arrow.png");
        Sprite resetSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/pic_reset.png");
        Sprite rewardIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/pic_candy_pink.png");

        if (manager.coverUIRoot != null)
        {
            manager.coverUIRoot.SetActive(true);
        }

        manager.DisableGameplayObjectsInMainMenuScene();
        manager.BuildOrBindMainMenuUI(
            frameSprite,
            boardSprite,
            startBtnSprite,
            monsterCoverSprite,
            arrowSprite,
            resetSprite,
            rewardIconSprite);

        EditorUtility.SetDirty(manager);
        if (manager.coverUIRoot != null)
        {
            EditorUtility.SetDirty(manager.coverUIRoot);
        }
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Debug.Log("[LevelEditorMenuTools] Main Menu UI (MainMenuPanel, LevelSelectPanel, DressUpPanel) built in Level_0!");
    }

    [MenuItem("Tools/Level Editor/Open LevelEditor Scene")]
    public static void OpenLevelEditorScene()
    {
        string scenePath = "Assets/Scenes/LevelEditor.unity";
        if (File.Exists(scenePath))
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
        else
        {
            Debug.LogWarning($"Scene not found at {scenePath}");
        }
    }

    [MenuItem("Tools/Level Editor/Batch Export Existing Scenes (Level_1 ~ Level_3)")]
    public static void BatchExportExistingLevelScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;
        string[] sceneNames = { "Level_1", "Level_2", "Level_3" };

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");
        }

        LevelDatabase db = LevelDatabase.Instance;
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/Resources/LevelDatabase.asset");
        }

        foreach (string levelName in sceneNames)
        {
            string scenePath = $"Assets/Scenes/{levelName}.unity";
            if (!File.Exists(scenePath)) continue;

            EditorSceneManager.OpenScene(scenePath);

            string assetPath = $"Assets/Resources/Levels/{levelName}.asset";
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (levelData == null)
            {
                levelData = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(levelData, assetPath);
            }

            levelData.levelName = levelName;

            GameObject tempObj = new GameObject("_TempExporter");
            LevelEditorManager exporter = tempObj.AddComponent<LevelEditorManager>();
            exporter.levelDatabase = db;
            exporter.PopulateLevelDataFromScene(levelData);
            Object.DestroyImmediate(tempObj);

            EditorUtility.SetDirty(levelData);
            if (!db.levels.Contains(levelData))
            {
                db.levels.Add(levelData);
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        Debug.Log("[LevelEditorMenuTools] Successfully exported Level_1, Level_2, and Level_3 to Assets/Resources/Levels/!");
    }

    [MenuItem("Tools/Level Editor/Batch Export Tutorial Scenes (1 ~ 7 -> Lv1 ~ Lv7)")]
    public static void BatchExportTutorialScenes1To7()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Levels"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");
        }

        LevelDatabase db = LevelDatabase.Instance;
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/Resources/LevelDatabase.asset");
        }

        List<LevelData> exportedLevels = new List<LevelData>();

        for (int i = 1; i <= 7; i++)
        {
            string scenePath = $"Assets/Scenes/{i}.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[LevelEditorMenuTools] Scene not found: {scenePath}");
                continue;
            }

            EditorSceneManager.OpenScene(scenePath);

            string levelName = $"Lv{i}";
            string assetPath = $"Assets/Resources/Levels/{levelName}.asset";
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (levelData == null)
            {
                levelData = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(levelData, assetPath);
            }

            levelData.levelName = levelName;

            GameObject tempObj = new GameObject("_TempExporter");
            LevelEditorManager exporter = tempObj.AddComponent<LevelEditorManager>();
            exporter.levelDatabase = db;
            exporter.PopulateLevelDataFromScene(levelData);
            Object.DestroyImmediate(tempObj);

            exportedLevels.Add(levelData);
        }

        // Configure prerequisite chain: Lv1 is unlocked by default, Lv2~Lv7 require the previous level
        for (int i = 0; i < exportedLevels.Count; i++)
        {
            LevelData current = exportedLevels[i];
            current.prerequisiteLevel = (i == 0) ? null : exportedLevels[i - 1];
            EditorUtility.SetDirty(current);

            if (!db.levels.Contains(current))
            {
                db.levels.Add(current);
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath);
        }

        Debug.Log("[LevelEditorMenuTools] Successfully exported scenes 1~7 to Assets/Resources/Levels/Lv1~Lv7 with sequential unlock prerequisites!");
    }
}

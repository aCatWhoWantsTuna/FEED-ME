using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
/// Editor menu utilities for batch-exporting existing scenes to LevelData assets.
/// </summary>
public static class LevelEditorMenuTools
{
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
}

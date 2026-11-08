using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Provides level editing, exporting (Scene -> LevelData ScriptableObject),
/// and loading (LevelData -> Scene) functionality for both Edit Mode and Play Mode.
/// </summary>
public class LevelEditorManager : MonoBehaviour
{
    [Header("-- Database & Target Level Asset --")]
    [Tooltip("Reference to the central LevelDatabase (auto-loaded from Resources if left empty).")]
    public LevelDatabase levelDatabase;

    [Tooltip("Target LevelData asset to overwrite when exporting, or to load when clicking Load.")]
    public LevelData targetLevelData;

    [Tooltip("Name used when exporting a brand-new LevelData asset (saved to Assets/Resources/Levels/).")]
    public string newLevelAssetName = "Level_New";

    [Header("-- Play Mode Behavior in Editor --")]
    [Tooltip("If enabled, pressing Play directly in the LevelEditor scene will load 'targetLevelData' instead of using the scene's current objects.")]
    public bool loadTargetLevelOnPlayInEditor = false;

    [Header("-- Scene Component References --")]
    public GameFlowManager gameFlowManager;
    public DrawManager drawManager;

    public LevelDatabase Database
    {
        get
        {
            if (levelDatabase == null)
            {
                levelDatabase = LevelDatabase.Instance;
            }
            return levelDatabase;
        }
    }

    private void ResolveManagers()
    {
        if (gameFlowManager == null)
        {
            gameFlowManager = GetComponent<GameFlowManager>();
            if (gameFlowManager == null) gameFlowManager = FindAnyObjectByType<GameFlowManager>();
        }

        if (drawManager == null)
        {
            drawManager = GetComponent<DrawManager>();
            if (drawManager == null) drawManager = FindAnyObjectByType<DrawManager>();
        }
    }

    /// <summary>
    /// Determines which LevelData should be used at the start of Play Mode, if any.
    /// Returns null if the current scene layout should be played directly as-is.
    /// </summary>
    public LevelData GetRuntimeLevelDataToLoad()
    {
        if (LevelController.HasActiveLevelRequest)
        {
            return LevelController.GetCurrentLevelData();
        }

        if (loadTargetLevelOnPlayInEditor && targetLevelData != null)
        {
            return targetLevelData;
        }

        return null;
    }

    /// <summary>
    /// Captures all level-specific data from the current scene and writes it into the given LevelData asset.
    /// </summary>
    public void PopulateLevelDataFromScene(LevelData data)
    {
        if (data == null) return;
        ResolveManagers();

        if (!string.IsNullOrEmpty(newLevelAssetName) && string.IsNullOrEmpty(data.levelName))
        {
            data.levelName = newLevelAssetName;
        }

        // 1. Export DrawManager pen count
        if (drawManager != null)
        {
            data.maxPens = drawManager.GetActivePenCapacity();
        }

        // 2. Export GameFlowManager pipe, spawn point, and candy waves
        if (gameFlowManager != null)
        {
            data.spawnInterval = gameFlowManager.spawnInterval;
            data.spawnRandomOffset = gameFlowManager.spawnRandomOffset;

            if (gameFlowManager.spawnPoint != null)
            {
                data.spawnPointPosition = gameFlowManager.spawnPoint.position;
            }

            if (gameFlowManager.pipeTransform != null)
            {
                data.pipePosition = gameFlowManager.pipeTransform.position;
            }

            data.candyWaves.Clear();
            if (gameFlowManager.candyWaves != null)
            {
                foreach (CandyWave wave in gameFlowManager.candyWaves)
                {
                    if (wave == null) continue;
                    data.candyWaves.Add(new CandyWave
                    {
                        candyPrefab = wave.candyPrefab,
                        amount = wave.amount,
                        candyIcon = wave.candyIcon
                    });
                }
            }
        }

        // 3. Export all Monsters in the scene (sorted by hierarchy order)
        Monster[] monstersInScene = FindObjectsByType<Monster>(FindObjectsInactive.Exclude);
        System.Array.Sort(monstersInScene, (a, b) =>
        {
            Transform rootA = a.transform.parent != null ? a.transform.parent : a.transform;
            Transform rootB = b.transform.parent != null ? b.transform.parent : b.transform;
            return rootA.GetSiblingIndex().CompareTo(rootB.GetSiblingIndex());
        });

        data.monsters.Clear();
        foreach (Monster monster in monstersInScene)
        {
            if (monster == null) continue;
            data.monsters.Add(monster.ToSpawnData(Database));
        }

        // 4. Export all extra obstacles in the scene
        LevelObstacle[] obstaclesInScene = FindObjectsByType<LevelObstacle>(FindObjectsInactive.Exclude);
        data.obstacles.Clear();
        foreach (LevelObstacle obs in obstaclesInScene)
        {
            if (obs == null) continue;
            data.obstacles.Add(new ObstacleSpawnData
            {
                obstaclePrefab = obs.sourcePrefab,
                obstacleId = obs.obstacleId,
                position = obs.transform.position,
                eulerAngles = obs.transform.eulerAngles,
                scale = obs.transform.localScale
            });
        }
    }

    /// <summary>
    /// Initializes the current scene (either in Edit Mode or Play Mode) using the provided LevelData.
    /// </summary>
    public void ApplyLevelDataToScene(LevelData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[LevelEditorManager] Cannot apply null LevelData.");
            return;
        }

        ResolveManagers();
        LevelDatabase db = Database;

        // 1. Configure DrawManager pens
        if (drawManager != null)
        {
            drawManager.SetMaxPens(data.maxPens);
        }

        // 2. Configure Pipe, SpawnPoint, and Candy Waves on GameFlowManager
        if (gameFlowManager != null)
        {
            gameFlowManager.spawnInterval = data.spawnInterval;
            gameFlowManager.spawnRandomOffset = data.spawnRandomOffset;

            if (gameFlowManager.spawnPoint != null)
            {
                gameFlowManager.spawnPoint.position = data.spawnPointPosition;
            }

            if (gameFlowManager.pipeTransform != null)
            {
                gameFlowManager.pipeTransform.position = data.pipePosition;
            }

            // Deep-copy candy waves so runtime gameplay never mutates the ScriptableObject
            CandyWave[] wavesCopy = new CandyWave[data.candyWaves.Count];
            for (int i = 0; i < data.candyWaves.Count; i++)
            {
                CandyWave src = data.candyWaves[i];
                GameObject prefab = src.candyPrefab;
                Sprite icon = src.candyIcon;

                // Resolve fallback prefab/icon from LevelDatabase if needed
                if (db != null)
                {
                    if (prefab != null && icon == null)
                    {
                        icon = prefab.CompareTag("BlueCandy") ? db.blueCandyIcon : db.pinkCandyIcon;
                    }
                }

                wavesCopy[i] = new CandyWave
                {
                    candyPrefab = prefab,
                    amount = src.amount,
                    candyIcon = icon
                };
            }
            gameFlowManager.candyWaves = wavesCopy;
        }

        // 3. Clear existing MonsterGroups in the scene
        ClearExistingMonsters();

        // 4. Spawn MonsterGroups from LevelData
        List<Monster> spawnedMonsters = new List<Monster>();
        if (data.monsters != null)
        {
            for (int i = 0; i < data.monsters.Count; i++)
            {
                MonsterSpawnData mData = data.monsters[i];
                Monster spawned = SpawnMonsterFromData(mData, i, db);
                if (spawned != null)
                {
                    spawnedMonsters.Add(spawned);
                }
            }
        }

        if (gameFlowManager != null)
        {
            gameFlowManager.allMonstersInLevel = spawnedMonsters.ToArray();
        }

        // 5. Clear and respawn extra obstacles
        ClearExistingObstacles();
        if (data.obstacles != null)
        {
            foreach (ObstacleSpawnData obsData in data.obstacles)
            {
                if (obsData == null || obsData.obstaclePrefab == null) continue;
                GameObject obsObj = InstantiateObject(obsData.obstaclePrefab);
                if (obsObj != null)
                {
                    obsObj.transform.position = obsData.position;
                    obsObj.transform.eulerAngles = obsData.eulerAngles;
                    obsObj.transform.localScale = obsData.scale;

                    LevelObstacle marker = obsObj.GetComponent<LevelObstacle>();
                    if (marker == null) marker = obsObj.AddComponent<LevelObstacle>();
                    marker.sourcePrefab = obsData.obstaclePrefab;
                    marker.obstacleId = obsData.obstacleId;
                }
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private void ClearExistingMonsters()
    {
        Monster[] existingMonsters = FindObjectsByType<Monster>(FindObjectsInactive.Include);
        HashSet<GameObject> rootsToDestroy = new HashSet<GameObject>();

        foreach (Monster m in existingMonsters)
        {
            if (m == null) continue;
            GameObject groupRoot = m.transform.parent != null ? m.transform.parent.gameObject : m.gameObject;
            rootsToDestroy.Add(groupRoot);
        }

        foreach (GameObject root in rootsToDestroy)
        {
            if (root == null) continue;
            DestroyObjectImmediateOrRuntime(root);
        }
    }

    private void ClearExistingObstacles()
    {
        LevelObstacle[] existingObstacles = FindObjectsByType<LevelObstacle>(FindObjectsInactive.Include);
        foreach (LevelObstacle obs in existingObstacles)
        {
            if (obs == null) continue;
            DestroyObjectImmediateOrRuntime(obs.gameObject);
        }
    }

    private Monster SpawnMonsterFromData(MonsterSpawnData mData, int index, LevelDatabase db)
    {
        if (db == null)
        {
            Debug.LogError("[LevelEditorManager] LevelDatabase is missing! Cannot spawn MonsterGroup prefab.");
            return null;
        }

        GameObject prefab = db.GetMonsterGroupPrefab(mData.monsterType);
        if (prefab == null)
        {
            Debug.LogError($"[LevelEditorManager] Missing MonsterGroup prefab for {mData.monsterType} in LevelDatabase.");
            return null;
        }

        GameObject groupObj = InstantiateObject(prefab);
        if (groupObj == null) return null;

        string baseName = mData.monsterType == MonsterType.Blue ? "MonsterGroup_Blue" : "MonsterGroup_Pink";
        groupObj.name = index == 0 ? baseName : $"{baseName}_{index}";

        Monster monster = groupObj.GetComponentInChildren<Monster>(true);
        if (monster != null)
        {
            monster.ApplySpawnData(mData, db);
        }

        return monster;
    }

    /// <summary>
    /// Spawns a new MonsterGroup in the scene (useful when designing levels in the LevelEditor scene).
    /// </summary>
    public Monster SpawnNewMonsterInEditor(MonsterType type)
    {
        LevelDatabase db = Database;
        if (db == null)
        {
            Debug.LogError("[LevelEditorManager] LevelDatabase not found.");
            return null;
        }

        GameObject prefab = db.GetMonsterGroupPrefab(type);
        if (prefab == null)
        {
            Debug.LogError($"[LevelEditorManager] Prefab for {type} is not assigned on LevelDatabase.");
            return null;
        }

        GameObject groupObj = InstantiateObject(prefab);
        Monster monster = groupObj.GetComponentInChildren<Monster>(true);
        if (monster != null)
        {
            MonsterSpawnData defaultData = monster.ToSpawnData(db);
            defaultData.monsterWorldPosition = Vector3.zero;
            monster.ApplySpawnData(defaultData, db);
        }

        SyncSceneMonstersToGameFlowManager();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Selection.activeGameObject = groupObj;
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
        return monster;
    }

    /// <summary>
    /// Updates GameFlowManager.allMonstersInLevel to match all active Monster components in the scene.
    /// </summary>
    public void SyncSceneMonstersToGameFlowManager()
    {
        ResolveManagers();
        if (gameFlowManager != null)
        {
            gameFlowManager.allMonstersInLevel = FindObjectsByType<Monster>(FindObjectsInactive.Exclude);
        }
    }

    private GameObject InstantiateObject(GameObject prefab)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, gameObject.scene);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Level Object");
            return instance;
        }
#endif
        return Instantiate(prefab);
    }

    private void DestroyObjectImmediateOrRuntime(GameObject obj)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.DestroyObjectImmediate(obj);
            return;
        }
#endif
        Destroy(obj);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor helper: Exports the current scene into targetLevelData (or creates a new LevelData asset in Assets/Resources/Levels/).
    /// </summary>
    public LevelData ExportCurrentSceneToAsset()
    {
        LevelData asset = targetLevelData;
        if (asset == null)
        {
            string folderPath = "Assets/Resources/Levels";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");
            }

            string safeName = string.IsNullOrWhiteSpace(newLevelAssetName) ? "Level_New" : newLevelAssetName.Trim();
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{safeName}.asset");

            asset = ScriptableObject.CreateInstance<LevelData>();
            asset.levelName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            targetLevelData = asset;
        }

        Undo.RecordObject(asset, "Export Level Data");
        if ( string.IsNullOrEmpty(asset.levelName) )
        {
            asset.levelName = asset.name;
        }

        PopulateLevelDataFromScene(asset);
        EditorUtility.SetDirty(asset);

        // Register in LevelDatabase if not already present
        LevelDatabase db = Database;
        if (db != null && !db.levels.Contains(asset))
        {
            Undo.RecordObject(db, "Register Level in Database");
            db.levels.Add(asset);
            EditorUtility.SetDirty(db);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[LevelEditorManager] Exported level '{asset.levelName}' ({asset.monsters.Count} monsters, {asset.candyWaves.Count} candy waves) to {AssetDatabase.GetAssetPath(asset)}");
        return asset;
    }
#endif
}

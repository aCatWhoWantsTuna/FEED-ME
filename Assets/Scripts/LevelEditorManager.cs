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

        // 4. Export all extra obstacles and tutorial visual props in the scene
        data.obstacles.Clear();
        HashSet<GameObject> exportedObjects = new HashSet<GameObject>();

        LevelObstacle[] obstaclesInScene = FindObjectsByType<LevelObstacle>(FindObjectsInactive.Exclude);
        foreach (LevelObstacle obs in obstaclesInScene)
        {
            if (obs == null) continue;
            exportedObjects.Add(obs.gameObject);
            data.obstacles.Add(CreateObstacleSpawnDataFromObject(obs.gameObject, obs.sourcePrefab, obs.obstacleId));
        }

        // Also capture legacy root "Obstacle" and "Tutorial" child SpriteRenderers from prototype scenes
        string[] legacyContainerNames = { "Obstacle", "Tutorial" };
        foreach (string containerName in legacyContainerNames)
        {
            GameObject container = GameObject.Find(containerName);
            if (container == null || !container.activeInHierarchy) continue;

            SpriteRenderer[] childSprites = container.GetComponentsInChildren<SpriteRenderer>(false);
            foreach (SpriteRenderer sr in childSprites)
            {
                if (sr == null || exportedObjects.Contains(sr.gameObject)) continue;
                exportedObjects.Add(sr.gameObject);
                data.obstacles.Add(CreateObstacleSpawnDataFromObject(sr.gameObject, null, sr.gameObject.name));
            }
        }
    }

    private ObstacleSpawnData CreateObstacleSpawnDataFromObject(GameObject obj, GameObject sourcePrefab, string obstacleId)
    {
        ObstacleSpawnData spawnData = new ObstacleSpawnData
        {
            objectName = obj.name,
            obstaclePrefab = sourcePrefab,
            obstacleId = obstacleId,
            position = obj.transform.position,
            eulerAngles = obj.transform.eulerAngles,
            scale = obj.transform.lossyScale
        };

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            spawnData.sprite = sr.sprite;
            spawnData.sortingOrder = sr.sortingOrder;
        }

        BoxCollider2D box = obj.GetComponent<BoxCollider2D>();
        if (box != null && box.enabled)
        {
            spawnData.hasBoxCollider = true;
            spawnData.colliderOffset = box.offset;
            spawnData.colliderSize = box.size;
        }

        return spawnData;
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

        // 5. Clear and respawn extra obstacles and tutorial visual props
        ClearExistingObstacles();
        if (data.obstacles != null)
        {
            foreach (ObstacleSpawnData obsData in data.obstacles)
            {
                if (obsData == null) continue;

                GameObject obsObj = null;
                if (obsData.obstaclePrefab != null)
                {
                    obsObj = InstantiateObject(obsData.obstaclePrefab);
                }
                else if (obsData.sprite != null || obsData.hasBoxCollider)
                {
                    string objName = string.IsNullOrEmpty(obsData.objectName) ? "LevelProp" : obsData.objectName;
                    obsObj = new GameObject(objName);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        Undo.RegisterCreatedObjectUndo(obsObj, "Spawn Level Prop");
                    }
#endif
                    if (obsData.sprite != null)
                    {
                        SpriteRenderer sr = obsObj.AddComponent<SpriteRenderer>();
                        sr.sprite = obsData.sprite;
                        sr.sortingOrder = obsData.sortingOrder;
                    }

                    if (obsData.hasBoxCollider)
                    {
                        BoxCollider2D box = obsObj.AddComponent<BoxCollider2D>();
                        box.offset = obsData.colliderOffset;
                        box.size = obsData.colliderSize;
                    }
                }

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

        string[] legacyContainers = { "Obstacle", "Tutorial" };
        foreach (string containerName in legacyContainers)
        {
            GameObject legacyObj = GameObject.Find(containerName);
            if (legacyObj != null)
            {
                DestroyObjectImmediateOrRuntime(legacyObj);
            }
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

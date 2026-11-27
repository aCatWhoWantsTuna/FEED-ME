using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CandyWave
{
    public GameObject candyPrefab;
    public int amount;
    public Sprite candyIcon;
}

public class GameFlowManager : MonoBehaviour
{
    [Header("-- Make Candy Settings --")]
    public CandyWave[] candyWaves;
    public Transform spawnPoint;
    public float spawnInterval = 0.5f;
    public float spawnRandomOffset = 0.05f;

    [Header("-- Preview UI Setting --")]
    public Transform previewPanel;
    public GameObject previewItemPrefab;
    public GameObject arrowPrefab;

    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> spawnedArrows = new List<GameObject>();

    [Header("-- UI Button Setting --")]
    public Image playPauseButtonImage;
    public Sprite playIcon;
    public Sprite pauseIcon;

    [Header("-- Pipe Animation --")]
    public Transform pipeTransform;
    public float pipeSquashScale = 0.9f;     // Vertical squash scale factor
    public float pipeSquashScale_x = 1.1f;   // Horizontal stretch scale factor
    public float pipeSquashTime = 0.05f;     // Duration to squash
    public float pipeRecoverTime = 0.1f;     // Duration to recover

    [Header("-- Win Condition --")]
    public Monster[] allMonstersInLevel;
    public GameObject victoryPanel;

    private Coroutine spawnCoroutine;
    private bool isPlaying = false;
    private int spawnedCount = 0;

    private List<GameObject> spawnQueue = new List<GameObject>();
    private int[] remainingWaveAmounts;
    private int totalCandies = 0;
    private int currentWaveIndex = 0;
    private bool isGameEnded = false;

    void Start()
    {
        InitializeLevelIfNeeded();
        BuildSpawnQueue();
        GeneratePreviewUI();

        // Prevent UI button selection from triggering a duplicate Submit on Spacebar press
        if (playPauseButtonImage != null)
        {
            Button playPauseBtn = playPauseButtonImage.GetComponent<Button>();
            if (playPauseBtn != null)
            {
                Navigation nav = playPauseBtn.navigation;
                nav.mode = Navigation.Mode.None;
                playPauseBtn.navigation = nav;
            }
        }
    }

    /// <summary>
    /// Checks if a LevelData asset should be loaded into the scene on startup,
    /// and ensures allMonstersInLevel is populated.
    /// </summary>
    private void InitializeLevelIfNeeded()
    {
        // Ensure PauseMenuManager exists in gameplay scenes
        if (GetComponent<PauseMenuManager>() == null && FindAnyObjectByType<PauseMenuManager>() == null)
        {
            gameObject.AddComponent<PauseMenuManager>();
        }

        LevelEditorManager editorManager = GetComponent<LevelEditorManager>();
        if (editorManager == null)
        {
            editorManager = FindAnyObjectByType<LevelEditorManager>();
        }

        LevelData levelToLoad = null;
        if (editorManager != null)
        {
            levelToLoad = editorManager.GetRuntimeLevelDataToLoad();
        }
        else if (LevelController.HasActiveLevelRequest)
        {
            levelToLoad = LevelController.GetCurrentLevelData();
        }

        if (levelToLoad != null)
        {
            if (editorManager == null)
            {
                editorManager = gameObject.AddComponent<LevelEditorManager>();
            }
            editorManager.ApplyLevelDataToScene(levelToLoad);
        }

        // Auto-discover monsters in the scene if none are assigned or if references are missing
        bool needsMonsterRefresh = allMonstersInLevel == null || allMonstersInLevel.Length == 0;
        if (!needsMonsterRefresh)
        {
            foreach (Monster m in allMonstersInLevel)
            {
                if (m == null)
                {
                    needsMonsterRefresh = true;
                    break;
                }
            }
        }

        if (needsMonsterRefresh)
        {
            allMonstersInLevel = FindObjectsByType<Monster>(FindObjectsInactive.Exclude);
        }
    }

    /// <summary>
    /// Rebuilds the candy spawn queue and wave tracking arrays from candyWaves.
    /// </summary>
    public void BuildSpawnQueue()
    {
        spawnQueue.Clear();
        spawnedCount = 0;
        currentWaveIndex = 0;

        if (candyWaves == null)
        {
            remainingWaveAmounts = new int[0];
            totalCandies = 0;
            return;
        }

        remainingWaveAmounts = new int[candyWaves.Length];
        for (int w = 0; w < candyWaves.Length; w++)
        {
            CandyWave wave = candyWaves[w];
            if (wave == null || wave.candyPrefab == null) continue;

            remainingWaveAmounts[w] = wave.amount;
            for (int i = 0; i < wave.amount; i++)
            {
                spawnQueue.Add(wave.candyPrefab);
            }
        }

        totalCandies = spawnQueue.Count;
    }

    void Update()
    {
        if (isGameEnded) return;

        // Toggle candy spawning play/pause with Spacebar during active gameplay
        if (Time.timeScale > 0f &&
            (victoryPanel == null || !victoryPanel.activeInHierarchy) &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TogglePlayPause();
        }

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (allMonstersInLevel == null || allMonstersInLevel.Length == 0) return;
        if (isGameEnded) return;

        bool allSatisfied = true;

        foreach (Monster m in allMonstersInLevel)
        {
            if (m == null || !m.isSatisfied)
            {
                allSatisfied = false;
                break;
            }
        }

        if (allSatisfied)
        {
            isGameEnded = true;
            StartCoroutine(VictoryDelayRoutine());
        }
    }

    // Wait briefly after winning so the eating animation can finish playing
    private IEnumerator VictoryDelayRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        Debug.Log("All candies eaten! Victory!");

        // Award level candy reward to player currency
        LevelData completedLevel = LevelController.GetCurrentLevelData();
        if (completedLevel == null && LevelDatabase.Instance != null)
        {
            int idx = LevelDatabase.Instance.GetLevelIndexByName(SceneManager.GetActiveScene().name);
            completedLevel = LevelDatabase.Instance.GetLevelByIndex(idx);
        }
        if (completedLevel != null)
        {
            PlayerProgressManager.RecordLevelCompleted(completedLevel);
        }

        if (victoryPanel != null && !victoryPanel.activeSelf)
        {
            victoryPanel.SetActive(true);
            Time.timeScale = 0f;

            if (AudioManager.instance != null)
            {
                // Fade out BGM on the final level
                if (LevelController.IsFinalLevel())
                {
                    AudioManager.instance.FadeOutBGM(1.5f);
                }
            }
        }
    }

    // Build the candy preview queue UI with arrows between waves
    public void GeneratePreviewUI()
    {
        if (previewPanel == null || previewItemPrefab == null || candyWaves == null) return;

        foreach (Transform child in previewPanel)
        {
            Destroy(child.gameObject);
        }
        spawnedItems.Clear();
        spawnedArrows.Clear();

        for (int i = 0; i < candyWaves.Length; i++)
        {
            CandyWave wave = candyWaves[i];
            if (wave == null || wave.candyPrefab == null) continue;

            GameObject item = Instantiate(previewItemPrefab, previewPanel);
            item.transform.SetAsFirstSibling();
            spawnedItems.Add(item);

            Transform iconTransform = item.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && wave.candyIcon != null)
                {
                    iconImage.sprite = wave.candyIcon;
                }
            }

            Transform textTransform = item.transform.Find("AmountText");
            if (textTransform != null)
            {
                TextMeshProUGUI tmp = textTransform.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "x " + wave.amount;
                }
            }

            if (i < candyWaves.Length - 1 && arrowPrefab != null)
            {
                GameObject arrow = Instantiate(arrowPrefab, previewPanel);
                arrow.transform.SetAsFirstSibling();
                spawnedArrows.Add(arrow);
            }
        }

        RefreshQueueVisuals(0);
    }

    public void TogglePlayPause()
    {
        if (spawnedCount >= totalCandies) return;

        if (isPlaying)
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            if (playPauseButtonImage != null) playPauseButtonImage.sprite = playIcon;
            isPlaying = false;
        }
        else
        {
            spawnCoroutine = StartCoroutine(SpawnCandiesRoutine());
            if (playPauseButtonImage != null) playPauseButtonImage.sprite = pauseIcon;
            isPlaying = true;
        }
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator SpawnCandiesRoutine()
    {
        while (spawnedCount < totalCandies)
        {
            float randomX = Random.Range(-spawnRandomOffset, spawnRandomOffset);
            Vector3 finalSpawnPos = spawnPoint.position + new Vector3(randomX, 0f, 0f);

            Instantiate(spawnQueue[spawnedCount], finalSpawnPos, Quaternion.identity);

            // Trigger pipe squash animation
            if (pipeTransform != null)
            {
                StartCoroutine(AnimatePipeSquash());
            }

            if (remainingWaveAmounts != null && currentWaveIndex < remainingWaveAmounts.Length)
            {
                remainingWaveAmounts[currentWaveIndex]--;
                UpdatePreviewUI(currentWaveIndex, remainingWaveAmounts[currentWaveIndex]);

                if (remainingWaveAmounts[currentWaveIndex] <= 0)
                {
                    currentWaveIndex++;
                    RefreshQueueVisuals(currentWaveIndex);
                }
            }

            spawnedCount++;
            yield return new WaitForSeconds(spawnInterval);
        }

        if (playPauseButtonImage != null) playPauseButtonImage.sprite = playIcon;
        isPlaying = false;
        spawnCoroutine = null;
    }

    private IEnumerator AnimatePipeSquash()
    {
        if (pipeTransform == null) yield break;

        Vector3 originalScale = pipeTransform.localScale;
        Vector3 squashedScale = new Vector3(
            originalScale.x * pipeSquashScale_x,
            originalScale.y * pipeSquashScale,
            originalScale.z);

        float elapsedTime = 0f;
        while (elapsedTime < pipeSquashTime)
        {
            pipeTransform.localScale = Vector3.Lerp(originalScale, squashedScale, elapsedTime / pipeSquashTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        pipeTransform.localScale = squashedScale; // Ensure exact target squash scale

        // Recover back to original scale
        elapsedTime = 0f;
        while (elapsedTime < pipeRecoverTime)
        {
            pipeTransform.localScale = Vector3.Lerp(squashedScale, originalScale, elapsedTime / pipeRecoverTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        pipeTransform.localScale = originalScale; // Ensure exact original scale restoration
    }

    // Updates the preview UI item for the specified wave index and remaining candy count
    public void UpdatePreviewUI(int waveIndex, int remainingAmount)
    {
        if (waveIndex >= spawnedItems.Count) return;

        GameObject currentUI = spawnedItems[waveIndex];

        if (remainingAmount > 0)
        {
            // Candies remain in this wave: update the displayed count
            Transform textTransform = currentUI.transform.Find("AmountText");
            if (textTransform != null)
            {
                textTransform.GetComponent<TextMeshProUGUI>().text = "x " + remainingAmount;
            }
        }
        else
        {
            Destroy(currentUI);

            if (waveIndex < spawnedArrows.Count)
            {
                Destroy(spawnedArrows[waveIndex]);
            }
        }
    }

    // Updates the opacity of preview queue items so the active wave is fully opaque
    private void RefreshQueueVisuals(int activeIndex)
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
            {
                CanvasGroup cg = spawnedItems[i].GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    // Active wave is fully opaque; upcoming waves are semi-transparent
                    cg.alpha = (i == activeIndex) ? 1f : 0.4f;
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using TMPro; // TextMeshPro 

[System.Serializable]
public class CandyWave // insert in GameManager
{
    public GameObject candyPrefab; 
    public int amount;     
    public Sprite candyIcon;       
         
}

public class GameFlowManager : MonoBehaviour
{
    [Header("-- make candy settings--")]
    public CandyWave[] candyWaves; 
    public Transform spawnPoint;   
    public float spawnInterval = 0.5f; 
    public float spawnRandomOffset = 0.05f;

    [Header("-- preview UI setting --")]
    public Transform previewPanel;       
    public GameObject previewItemPrefab;
    public GameObject arrowPrefab;

    [Header("-- dynamic UI tracking list --")]
    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> spawnedArrows = new List<GameObject>();

    [Header("-- UI button setting --")]
    public Image playPauseButtonImage; 
    public Sprite playIcon;            
    public Sprite pauseIcon; 

    [Header("-- pipe anim --")]
    public Transform pipeTransform; 
    public float pipeSquashScale = 0.9f; // 往上縮到多短 
    public float pipeSquashScale_x = 1.1f; // 往上縮到多短 
    public float pipeSquashTime = 0.05f;  // 縮上去要多久
    public float pipeRecoverTime = 0.1f; // 彈回來要多久  

    // ----------

    private Coroutine spawnCoroutine; 
    private bool isPlaying = false;    
    private int spawnedCount = 0;      
    
    private List<GameObject> spawnQueue = new List<GameObject>(); 
    private int totalCandies = 0;
    private int currentWaveIndex = 0;


    private bool isGameEnded = false;

    void Start()
    {
        foreach (CandyWave wave in candyWaves)
        {
            for (int i = 0; i < wave.amount; i++)
            {
                spawnQueue.Add(wave.candyPrefab);
            }
        }
        totalCandies = spawnQueue.Count; 

        GeneratePreviewUI();
    }

    void Update()
    {
        if (isGameEnded) return; 

        Debug.Log("still running");
        CheckWinCondition();
    }


    [Header("--Win Or Not--")]
    public Monster[] allMonstersInLevel; 

    public GameObject victoryPanel;


    private void CheckWinCondition(){
        if (allMonstersInLevel == null || allMonstersInLevel.Length == 0) return;
        
        if (isGameEnded) return; 

        bool allSatisfied = true;
        
        foreach (Monster m in allMonstersInLevel){
            if (m.isSatisfied == false){
                allSatisfied = false; 
                break; 
            }
        }

        if (allSatisfied){
            isGameEnded = true; 
            StartCoroutine(VictoryDelayRoutine()); 
        }
    }

    // wait after winning. so the anim won't be cut
    private System.Collections.IEnumerator VictoryDelayRoutine(){
        yield return new WaitForSeconds(0.5f);

        Debug.Log("All Eaten! Victory!");
        
        if (victoryPanel != null && !victoryPanel.activeSelf){
            victoryPanel.SetActive(true);
            Time.timeScale = 0f; 
            
            if (AudioManager.instance != null){
                // Level_2 (last lavel): no music
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level_3"){
                    AudioManager.instance.FadeOutBGM(1.5f);
                }
            }
        }
    }

    // --- preview panel with arrows ---
    private void GeneratePreviewUI()
    {
        if (previewPanel == null || previewItemPrefab == null) return;

    
        // no panels for testing
        foreach (Transform child in previewPanel)
        {
            Destroy(child.gameObject);
        }
        spawnedItems.Clear();
        spawnedArrows.Clear();


        for (int i = 0; i < candyWaves.Length; i++){
            CandyWave wave = candyWaves[i];
            if (wave.candyPrefab == null) continue;

            GameObject item = Instantiate(previewItemPrefab, previewPanel);
            item.transform.SetAsFirstSibling();
            spawnedItems.Add(item);

            Transform iconTransform = item.transform.Find("Icon");
            if (iconTransform != null){
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && wave.candyIcon != null){
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
            playPauseButtonImage.sprite = playIcon; 
            isPlaying = false;
        }
        else
        {
            spawnCoroutine = StartCoroutine(SpawnCandiesRoutine());
            playPauseButtonImage.sprite = pauseIcon; 
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
            float randomX = Random.Range(-0.05f, 0.05f);
            Vector3 finalSpawnPos = spawnPoint.position + new Vector3(randomX, 0f, 0f);

            Instantiate(spawnQueue[spawnedCount], finalSpawnPos, Quaternion.identity);
            
            // squash pipe
            if (pipeTransform != null)
            {
                StartCoroutine(AnimatePipeSquash());
            }

            candyWaves[currentWaveIndex].amount--; 
            UpdatePreviewUI(currentWaveIndex, candyWaves[currentWaveIndex].amount); // renew UI preview panel
            
            if (candyWaves[currentWaveIndex].amount <= 0)
            {
                currentWaveIndex++;
                RefreshQueueVisuals(currentWaveIndex);
            }


            spawnedCount++; 
            yield return new WaitForSeconds(spawnInterval);
        }
        
        playPauseButtonImage.sprite = playIcon; 
        isPlaying = false;
        spawnCoroutine = null; 
    }

    // --- pipe anim ---
    private IEnumerator AnimatePipeSquash()
    {
        if (pipeTransform == null) yield break; 

        Vector3 originalScale = pipeTransform.localScale;
        
        Vector3 squashedScale = new Vector3(originalScale.x * pipeSquashScale_x, originalScale.y * pipeSquashScale, originalScale.z);

        float elapsedTime = 0f;
        while (elapsedTime < pipeSquashTime)
        {
            pipeTransform.localScale = Vector3.Lerp(originalScale, squashedScale, (elapsedTime / pipeSquashTime));
            elapsedTime += Time.deltaTime;
            yield return null; 
        }
        pipeTransform.localScale = squashedScale; // 確保數值精準到位

        // Recover
        elapsedTime = 0f;
        while (elapsedTime < pipeRecoverTime)
        {
            pipeTransform.localScale = Vector3.Lerp(squashedScale, originalScale, (elapsedTime / pipeRecoverTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        pipeTransform.localScale = originalScale; // 確保完全恢復原狀
    }

    // waveIndex: current wave 
    // remainingAmount: how many candies left
    public void UpdatePreviewUI(int waveIndex, int remainingAmount)
    {
        
        if (waveIndex >= spawnedItems.Count) return; 

        GameObject currentUI = spawnedItems[waveIndex];
        
        if (remainingAmount > 0)
        {
            // there's candy in the box: only change the value
            Transform textTransform = currentUI.transform.Find("AmountText");
            if (textTransform != null)
            {
                textTransform.GetComponent<TMPro.TextMeshProUGUI>().text = "x " + remainingAmount;
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
    // renew the reansformation
    private void RefreshQueueVisuals(int activeIndex)
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null) 
            {
                CanvasGroup cg = spawnedItems[i].GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    // now: very clear；not now: transparent
                    cg.alpha = (i == activeIndex) ? 1f : 0.4f; 
                }
            }
        }
    }
}
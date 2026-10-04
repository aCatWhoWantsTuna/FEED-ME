using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CoverUIManager : MonoBehaviour
{
    [Header("-- cover UI setting--")]
    public GameObject coverUIRoot; 
    public RectTransform titleRect; 
    
    [Header("-- floating anim --")]
    public float floatSpeed = 2f; 
    public float floatHeight = 15f; 

    private Vector2 titleStartPos;
    private bool isGameStarted = false;

    private static bool hasSeenCover = false;

    void Start()
    {   
        
        if (hasSeenCover) // for tutorial level, preventing the cover from being displayed repeatedly.
        {
            if (coverUIRoot != null) coverUIRoot.SetActive(false);
            isGameStarted = true;
            return; 
        }
        // pause!
        Time.timeScale = 0f; 
        
        if (coverUIRoot != null) coverUIRoot.SetActive(true);

        if (titleRect != null)
        {
            titleStartPos = titleRect.anchoredPosition;
        }
    }

    void Update()
    {
        if (!isGameStarted && titleRect != null)
        {
            float newY = titleStartPos.y + Mathf.Sin(Time.unscaledTime * floatSpeed) * floatHeight;
            titleRect.anchoredPosition = new Vector2(titleStartPos.x, newY);
        }
    }

    public void OnStartButtonClicked()
    {   
        hasSeenCover = true;
        isGameStarted = true;
        Time.timeScale = 1f; // time started
        
        if (coverUIRoot != null) 
        {
            coverUIRoot.SetActive(false); 
        }
    }

}
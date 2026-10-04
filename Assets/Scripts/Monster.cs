using UnityEngine;
using TMPro;

public class Monster : MonoBehaviour
{
    [Header("-- target setting --")]
    public string targetColorTag = "RedCandy"; 

    public int targetAmount = 3;
    [HideInInspector]
    public bool isSatisfied = false;
    
    [Header("-- pic: main --")]
    public GameObject speechBubble;   
    public TextMeshPro numberText;    
    public Sprite normalSprite;       
    public Sprite eat1Sprite;         
    public Sprite eat2Sprite;         
    public Sprite restSprite;  
    public GameObject crushEffectPrefab;

    [Header("-- open mouth --")]
    public float detectRadius = 3f; 

    private int currentAmount = 0; 
    private SpriteRenderer mySpriteRenderer; 
    private int animState = 0; 
    private float animTimer = 0f;
    private Vector3 originalScale; 

    [Header("--pic: pedestal--")]
    public GameObject pedestal; 
    [Header("-- pic: no candy alert --")]
    public GameObject noCandyAlert; 



    void Start()
    {
        mySpriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale; 
        
        if (noCandyAlert != null) noCandyAlert.SetActive(false);

        ChangeAnimState(0); 
        UpdateBubbleText(); 
    }
    void Update()
    {
        if (currentAmount >= targetAmount) return;

        // --- anim: breathing ---
        if (animState == 0) // only in normal status
        {
            float breath = (Mathf.Sin(Time.time * 4f) + 1f) / 2f; 
            float currentY = originalScale.y + (breath * 0.01f); 
            transform.localScale = new Vector3(originalScale.x, currentY, originalScale.z);
        }

        // find candy
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        bool foundCandy = false;
        foreach (Collider2D col in colliders)
        {
            // if (col.CompareTag(targetColorTag))
            if (col.gameObject.tag.Contains("Candy")) // Eat: any candy.
            {
                foundCandy = true;
                break;
            }

            
        }

        // switch the status
        if (foundCandy)
        {
            if (animState == 0) ChangeAnimState(1); 
            else if (animState == 1)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= 0.1f) ChangeAnimState(2); 
            }
        }
        else
        {
            if (animState == 2) ChangeAnimState(1); 
            else if (animState == 1)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= 0.1f) ChangeAnimState(0); 
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentAmount >= targetAmount) return;

        if (other.gameObject.tag.Contains("Candy"))
        {
            // anim: pop effect
            if (crushEffectPrefab != null)
            {
                Vector3 effectPos = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
                Instantiate(crushEffectPrefab, effectPos, Quaternion.identity);
            }
            string eatenCandyTag = other.tag;
            Destroy(other.gameObject); 

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX("candy_crush"); 
            }
            // currentAmount++; 


            if (eatenCandyTag == targetColorTag)
            {
                currentAmount++; 

                if (currentAmount >= targetAmount)
                {
                    speechBubble.SetActive(false); 
                    mySpriteRenderer.sprite = restSprite; 
                    transform.localScale = originalScale; 
                    isSatisfied = true;
                }
                else
                {
                    UpdateBubbleText(); 
                }
            }
            else
            {
                Monster[] allMonsters = FindObjectsByType<Monster>(FindObjectsInactive.Exclude);
                foreach (Monster m in allMonsters){
                    if (m != this && m.targetColorTag == eatenCandyTag){
                        m.ShowMissedAlert();
                    }
                }

            }

           
        }
    }

    // anim: open mouth
    private void ChangeAnimState(int newState)
    {
        animState = newState;
        animTimer = 0f; 

        if (newState == 0)
        {
            mySpriteRenderer.sprite = normalSprite;
        }
        else if (newState == 1)
        {
            mySpriteRenderer.sprite = eat1Sprite;
            transform.localScale = new Vector3(originalScale.x, originalScale.y * 1.2f, originalScale.z);
        }
        else if (newState == 2)
        {
            mySpriteRenderer.sprite = eat2Sprite;
            transform.localScale = new Vector3(originalScale.x, originalScale.y * 1.3f, originalScale.z);
        }
    }

    private void UpdateBubbleText()
    {
        int remain = targetAmount - currentAmount;
        numberText.text = remain.ToString();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

    public void ShowMissedAlert()
    {   
        if (isSatisfied) return;
        if (noCandyAlert != null && !noCandyAlert.activeSelf) 
        {
            noCandyAlert.SetActive(true); 
            StartCoroutine(PopUpAlert()); 

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX("alert");
            }
        }
    }


    private System.Collections.IEnumerator PopUpAlert()
    {
        Transform alertTransform = noCandyAlert.transform;
        
        Vector3 originalScale = alertTransform.localScale; 
        
        Vector3 finalScale = originalScale; 
        
        // initial: super small
        alertTransform.localScale = Vector3.zero;

        // first popup
        float time = 0f;
        float duration1 = 0.15f; 
        Vector3 overShootScale = originalScale * 1.3f; 
        
        while (time < duration1)
        {
            alertTransform.localScale = Vector3.Lerp(Vector3.zero, overShootScale, time / duration1);
            time += Time.deltaTime;
            yield return null;
        }

        // second shrink
        time = 0f;
        float duration2 = 0.1f; 
        
        while (time < duration2)
        {
            alertTransform.localScale = Vector3.Lerp(overShootScale, finalScale, time / duration2);
            time += Time.deltaTime;
            yield return null;
        }

        alertTransform.localScale = finalScale;
    }
}
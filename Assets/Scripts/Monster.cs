using System.Collections;
using UnityEngine;
using TMPro;

public class Monster : MonoBehaviour
{
    [Header("-- Target Setting --")]
    public string targetColorTag = "RedCandy";
    public int targetAmount = 3;

    [HideInInspector]
    public bool isSatisfied = false;

    [Header("-- Visual References --")]
    public GameObject speechBubble;
    public TextMeshPro numberText;
    public Sprite normalSprite;
    public Sprite eat1Sprite;
    public Sprite eat2Sprite;
    public Sprite restSprite;
    public GameObject crushEffectPrefab;

    [Header("-- Mouth Detection --")]
    public float detectRadius = 3f;

    [Header("-- Pedestal & Alert --")]
    public GameObject pedestal;
    public GameObject noCandyAlert;

    private int currentAmount = 0;
    private SpriteRenderer mySpriteRenderer;
    private int animState = 0;
    private float animTimer = 0f;
    private Vector3 originalScale;

    void Start()
    {
        mySpriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (noCandyAlert != null)
        {
            noCandyAlert.SetActive(false);
        }

        ChangeAnimState(0);
        UpdateBubbleText();
    }

    void Update()
    {
        if (currentAmount >= targetAmount) return;

        // Breathing animation while in normal idle state
        if (animState == 0)
        {
            float breath = (Mathf.Sin(Time.time * 4f) + 1f) / 2f;
            float currentY = originalScale.y + (breath * 0.01f);
            transform.localScale = new Vector3(originalScale.x, currentY, originalScale.z);
        }

        // Detect nearby candies to open mouth
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        bool foundCandy = false;
        foreach (Collider2D col in colliders)
        {
            if (col.gameObject.tag.Contains("Candy"))
            {
                foundCandy = true;
                break;
            }
        }

        // Transition mouth animation states
        if (foundCandy)
        {
            if (animState == 0)
            {
                ChangeAnimState(1);
            }
            else if (animState == 1)
            {
                animTimer += Time.deltaTime;
                if (animTimer >= 0.1f) ChangeAnimState(2);
            }
        }
        else
        {
            if (animState == 2)
            {
                ChangeAnimState(1);
            }
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
            // Spawn pop effect
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

            if (eatenCandyTag == targetColorTag)
            {
                currentAmount++;

                if (currentAmount >= targetAmount)
                {
                    if (speechBubble != null) speechBubble.SetActive(false);
                    if (mySpriteRenderer != null) mySpriteRenderer.sprite = restSprite;
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
                // Notify monsters that needed the eaten candy color
                Monster[] allMonsters = FindObjectsByType<Monster>(FindObjectsInactive.Exclude);
                foreach (Monster m in allMonsters)
                {
                    if (m != this && m.targetColorTag == eatenCandyTag)
                    {
                        m.ShowMissedAlert();
                    }
                }
            }
        }
    }

    private void ChangeAnimState(int newState)
    {
        animState = newState;
        animTimer = 0f;

        if (mySpriteRenderer == null)
        {
            mySpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (newState == 0)
        {
            if (mySpriteRenderer != null) mySpriteRenderer.sprite = normalSprite;
        }
        else if (newState == 1)
        {
            if (mySpriteRenderer != null) mySpriteRenderer.sprite = eat1Sprite;
            transform.localScale = new Vector3(originalScale.x, originalScale.y * 1.2f, originalScale.z);
        }
        else if (newState == 2)
        {
            if (mySpriteRenderer != null) mySpriteRenderer.sprite = eat2Sprite;
            transform.localScale = new Vector3(originalScale.x, originalScale.y * 1.3f, originalScale.z);
        }
    }

    public void UpdateBubbleText()
    {
        if (numberText != null)
        {
            int remain = Mathf.Max(0, targetAmount - currentAmount);
            numberText.text = remain.ToString();
        }
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

    private IEnumerator PopUpAlert()
    {
        Transform alertTransform = noCandyAlert.transform;
        Vector3 alertOriginalScale = alertTransform.localScale;
        Vector3 finalScale = alertOriginalScale;

        // Start from zero scale
        alertTransform.localScale = Vector3.zero;

        // First pop-up overshoot phase
        float time = 0f;
        float duration1 = 0.15f;
        Vector3 overShootScale = alertOriginalScale * 1.3f;

        while (time < duration1)
        {
            alertTransform.localScale = Vector3.Lerp(Vector3.zero, overShootScale, time / duration1);
            time += Time.deltaTime;
            yield return null;
        }

        // Second settle phase
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

    // =====================================================================
    // Level Editor & Data Serialization Helpers
    // =====================================================================

    /// <summary>
    /// Returns the CandyIcon child transform inside the SpeechBubble, if present.
    /// </summary>
    public Transform GetCandyIconTransform()
    {
        if (speechBubble == null) return null;
        foreach (Transform child in speechBubble.transform)
        {
            if (numberText != null && child == numberText.transform) continue;
            return child;
        }
        return null;
    }

    /// <summary>
    /// Exports this monster's configuration and transform data into a MonsterSpawnData entry.
    /// </summary>
    public MonsterSpawnData ToSpawnData(LevelDatabase database)
    {
        Transform groupRoot = transform.parent != null ? transform.parent : transform;
        MonsterType type = (targetColorTag == "BlueCandy") ? MonsterType.Blue : MonsterType.Pink;

        SpeechBubbleDirection dir = SpeechBubbleDirection.RightOfMonster;
        Vector3 bubbleOffset = new Vector3(2.08f, 0.74f, 0f);
        Vector2 numAnchoredPos = new Vector2(-1.2f, -1.67f);
        Vector3 iconLocalPos = new Vector3(0.7f, -0.8f, 0f);

        if (speechBubble != null)
        {
            bubbleOffset = speechBubble.transform.position - transform.position;
            SpriteRenderer bubbleSr = speechBubble.GetComponent<SpriteRenderer>();
            if (bubbleSr != null && bubbleSr.sprite != null)
            {
                if (database != null && bubbleSr.sprite == database.bubbleLeftOfMonsterSprite)
                {
                    dir = SpeechBubbleDirection.LeftOfMonster;
                }
                else if (bubbleSr.sprite.name.Contains("right"))
                {
                    // pic_eat_text_right has its tail on the right, meaning it sits to the LEFT of the monster
                    dir = SpeechBubbleDirection.LeftOfMonster;
                }
                else
                {
                    dir = SpeechBubbleDirection.RightOfMonster;
                }
            }
            else if (bubbleOffset.x < 0f)
            {
                dir = SpeechBubbleDirection.LeftOfMonster;
            }
        }

        if (numberText != null)
        {
            RectTransform rt = numberText.GetComponent<RectTransform>();
            if (rt != null)
            {
                numAnchoredPos = rt.anchoredPosition;
            }
        }

        Transform iconTransform = GetCandyIconTransform();
        if (iconTransform != null)
        {
            iconLocalPos = iconTransform.localPosition;
        }

        return new MonsterSpawnData
        {
            monsterType = type,
            targetColorTag = targetColorTag,
            targetAmount = targetAmount,
            groupPosition = groupRoot.position,
            monsterWorldPosition = transform.position,
            speechBubbleDirection = dir,
            speechBubbleOffsetFromMonster = bubbleOffset,
            numberTextAnchoredPosition = numAnchoredPos,
            candyIconLocalPosition = iconLocalPos
        };
    }

    /// <summary>
    /// Applies MonsterSpawnData to this monster and its parent MonsterGroup.
    /// </summary>
    public void ApplySpawnData(MonsterSpawnData data, LevelDatabase database)
    {
        if (data == null) return;

        targetColorTag = string.IsNullOrEmpty(data.targetColorTag)
            ? (data.monsterType == MonsterType.Blue ? "BlueCandy" : "RedCandy")
            : data.targetColorTag;
        targetAmount = data.targetAmount;
        currentAmount = 0;
        isSatisfied = false;

        Transform groupRoot = transform.parent != null ? transform.parent : transform;
        if (groupRoot != transform)
        {
            // Position the parent group so the Monster body lands at the exact saved world position
            Vector3 worldOffset = transform.position - groupRoot.position;
            groupRoot.position = data.monsterWorldPosition - worldOffset;
        }
        else
        {
            transform.position = data.monsterWorldPosition;
        }

        if (speechBubble != null)
        {
            speechBubble.SetActive(true);
            speechBubble.transform.position = transform.position + data.speechBubbleOffsetFromMonster;

            if (database != null)
            {
                Sprite targetBubbleSprite = database.GetSpeechBubbleSprite(data.speechBubbleDirection);
                SpriteRenderer bubbleSr = speechBubble.GetComponent<SpriteRenderer>();
                if (bubbleSr != null && targetBubbleSprite != null)
                {
                    bubbleSr.sprite = targetBubbleSprite;
                }
            }
        }

        if (numberText != null)
        {
            RectTransform rt = numberText.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = data.numberTextAnchoredPosition;
            }
        }

        Transform iconTransform = GetCandyIconTransform();
        if (iconTransform != null)
        {
            iconTransform.localPosition = data.candyIconLocalPosition;
        }

        UpdateBubbleText();
    }

    /// <summary>
    /// Helper for the Level Editor to quickly flip the speech bubble between the left and right sides of the monster.
    /// </summary>
    public void SetSpeechBubblePresetSide(SpeechBubbleDirection side, LevelDatabase database)
    {
        if (speechBubble == null) return;

        bool isBlue = targetColorTag == "BlueCandy";
        if (side == SpeechBubbleDirection.LeftOfMonster)
        {
            speechBubble.transform.position = transform.position + (isBlue
                ? new Vector3(-2.06f, 1.18f, 0f)
                : new Vector3(-2.03244f, 1.21634f, 0f));

            if (numberText != null)
            {
                RectTransform rt = numberText.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = isBlue ? new Vector2(-1.7f, -1.8f) : new Vector2(-1.2f, -2.39f);
            }

            Transform icon = GetCandyIconTransform();
            if (icon != null)
            {
                icon.localPosition = isBlue ? new Vector3(0.13f, -1.1f, 0f) : new Vector3(0.7f, -1.61f, 0f);
            }
        }
        else
        {
            speechBubble.transform.position = transform.position + new Vector3(2.08f, 0.74f, 0f);

            if (numberText != null)
            {
                RectTransform rt = numberText.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(-1.2f, -1.67f);
            }

            Transform icon = GetCandyIconTransform();
            if (icon != null)
            {
                icon.localPosition = new Vector3(0.7f, -0.8f, 0f);
            }
        }

        if (database != null)
        {
            Sprite sprite = database.GetSpeechBubbleSprite(side);
            SpriteRenderer sr = speechBubble.GetComponent<SpriteRenderer>();
            if (sr != null && sprite != null)
            {
                sr.sprite = sprite;
            }
        }
    }
}

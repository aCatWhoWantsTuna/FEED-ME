using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryUIAnimator : MonoBehaviour
{
    [Header("--UI component setting--")]
    public RectTransform background;
    public RectTransform titleText;
    public RectTransform redMonster;
    public RectTransform blueMonster;
    public GameObject nextButton;

    [Header("-- effect setting --")]
    public GameObject crushEffectPrefab; 

    private Vector2 bgFinalPos;
    private Vector2 titleFinalPos;
    private Vector2 redFinalPos;
    private Vector2 blueFinalPos;
    private bool hasInitialized = false;

    private void OnEnable()
    {
        if (!hasInitialized)
        {
            bgFinalPos = background.anchoredPosition;
            titleFinalPos = titleText.anchoredPosition;
            redFinalPos = redMonster.anchoredPosition;
            blueFinalPos = blueMonster.anchoredPosition;
            hasInitialized = true;
        }
        
        StartCoroutine(PlayVictorySequence());
    }

    private IEnumerator PlayVictorySequence()
    {
        if (nextButton != null) nextButton.SetActive(false);

        background.anchoredPosition = new Vector2(bgFinalPos.x, bgFinalPos.y + 1200f); 
        titleText.anchoredPosition = new Vector2(titleFinalPos.x - 2000f, titleFinalPos.y); 
        redMonster.anchoredPosition = redFinalPos; 
        redMonster.localScale = Vector3.zero; 
        blueMonster.anchoredPosition = blueFinalPos; 
        blueMonster.localScale = Vector3.zero; 
        if (AudioManager.instance != null){
            AudioManager.instance.PlaySFX("victory");
        }

        yield return StartCoroutine(Anim_BackgroundBounce());

        yield return StartCoroutine(Anim_TitleSlideIn());

        if (crushEffectPrefab != null)
        {
            
            GameObject effect = Instantiate(crushEffectPrefab, titleText.transform);
            
            RectTransform effectRect = effect.GetComponent<RectTransform>();
            if(effectRect != null)
            {
                effectRect.anchoredPosition = Vector2.zero;
                effectRect.localScale = Vector3.one; 
                effectRect.localPosition = new Vector3(effectRect.localPosition.x, effectRect.localPosition.y, 0f);
            }
            
            yield return new WaitForSecondsRealtime(0.1f);
        }

        yield return StartCoroutine(Anim_MonstersPop());
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Level_3")
        {
            DrawManager drawManager = FindAnyObjectByType<DrawManager>();
            if (drawManager != null)
            {
                drawManager.enabled = false;
            }

            Time.timeScale = 0f;

        }else{
            if (nextButton != null)
            {
                yield return StartCoroutine(Anim_NextButtonAppear());
            }
        }
        
    }

    private IEnumerator Anim_BackgroundBounce()
    {
        Vector2 startPos = new Vector2(bgFinalPos.x, bgFinalPos.y + 1200f);
        yield return MoveRect(background, startPos, bgFinalPos, 0.3f, false);
        yield return MoveRect(background, bgFinalPos, bgFinalPos + new Vector2(0, 80f), 0.15f, true);
        yield return MoveRect(background, bgFinalPos + new Vector2(0, 80f), bgFinalPos, 0.15f, false);
        yield return MoveRect(background, bgFinalPos, bgFinalPos + new Vector2(0, 30f), 0.1f, true);
        yield return MoveRect(background, bgFinalPos + new Vector2(0, 30f), bgFinalPos, 0.1f, false);
    }

    private IEnumerator Anim_TitleSlideIn()
    {
        Vector2 startPos = new Vector2(titleFinalPos.x - 1200f, titleFinalPos.y);
        float time = 0f;
        float duration = 0.7f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            float easeOut = 1f - Mathf.Pow(1f - t, 3f); 
            titleText.anchoredPosition = Vector2.Lerp(startPos, titleFinalPos, easeOut);
            yield return null;
        }
        titleText.anchoredPosition = titleFinalPos;
    }

    private IEnumerator Anim_MonstersPop()
    {
        float time = 0f;
        float expandDuration = 0.3f;
        while (time < expandDuration)
        {
            time += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, time / expandDuration);
            redMonster.localScale = new Vector3(scale, scale, 1f);
            blueMonster.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        time = 0f;
        float shrinkDuration = 0.2f;
        while (time < shrinkDuration)
        {
            time += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(1.2f, 1f, time / shrinkDuration);
            redMonster.localScale = new Vector3(scale, scale, 1f);
            blueMonster.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        redMonster.localScale = Vector3.one;
        blueMonster.localScale = Vector3.one;
    }

    private IEnumerator Anim_NextButtonAppear()
    {
        if (nextButton == null) yield break;
        nextButton.transform.localScale = Vector3.zero;
        nextButton.SetActive(true);
        float time = 0f;
        float duration = 0.3f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(0f, 1f, time / duration);
            nextButton.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        nextButton.transform.localScale = Vector3.one;
    }

    private IEnumerator MoveRect(RectTransform target, Vector2 from, Vector2 to, float duration, bool isEaseOut)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            float ease = isEaseOut ? (1f - Mathf.Pow(1f - t, 2f)) : (t * t); 
            target.anchoredPosition = Vector2.Lerp(from, to, ease);
            yield return null;
        }
        target.anchoredPosition = to;
    }


    public IEnumerator PlayExitSequence(System.Action onFinished)
    {
        float time = 0f;
        float duration = 0.9f;
        
        Vector2 startPos = background.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, 1500f); // flies up

        // SetParent (..., true) means they are not parent/child anymore, but fix the relative pos.
        if (redMonster != null) redMonster.SetParent(background.parent, true);
        if (blueMonster != null) blueMonster.SetParent(background.parent, true);

        Vector3 redStartScale = redMonster != null ? redMonster.localScale : Vector3.one;
        Vector3 blueStartScale = blueMonster != null ? blueMonster.localScale : Vector3.one;
        
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            
            float easeIn = t * t; 

            // Only Background Moves up.
            background.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeIn);
            
            if (redMonster != null)
            {
                redMonster.localScale = Vector3.Lerp(redStartScale, Vector3.zero, easeIn);
            }
            
            if (blueMonster != null)
            {
                blueMonster.localScale = Vector3.Lerp(blueStartScale, Vector3.zero, easeIn);
            }

            yield return null;
        }

        onFinished?.Invoke();
    }
}
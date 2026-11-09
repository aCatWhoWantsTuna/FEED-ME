using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryUIAnimator : MonoBehaviour
{
    [Header("-- UI Component Setting --")]
    public RectTransform background;
    public RectTransform titleText;
    public RectTransform redMonster;
    public RectTransform blueMonster;
    public GameObject nextButton;
    public GameObject levelSelectButton;

    [Header("-- Effect Setting --")]
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
        if (levelSelectButton != null) levelSelectButton.SetActive(false);

        DrawManager drawManager = FindAnyObjectByType<DrawManager>();
        if (drawManager != null)
        {
            if (drawManager.customPenCursor != null)
            {
                drawManager.customPenCursor.SetActive(false);
            }
            drawManager.enabled = false;
        }
        Cursor.visible = true;

        background.anchoredPosition = new Vector2(bgFinalPos.x, bgFinalPos.y + 1200f);
        titleText.anchoredPosition = new Vector2(titleFinalPos.x - 2000f, titleFinalPos.y);
        redMonster.anchoredPosition = redFinalPos;
        redMonster.localScale = Vector3.zero;
        blueMonster.anchoredPosition = blueFinalPos;
        blueMonster.localScale = Vector3.zero;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("victory");
        }

        yield return StartCoroutine(Anim_BackgroundBounce());

        yield return StartCoroutine(Anim_TitleSlideIn());

        if (crushEffectPrefab != null)
        {
            GameObject effect = Instantiate(crushEffectPrefab, titleText.transform);

            RectTransform effectRect = effect.GetComponent<RectTransform>();
            if (effectRect != null)
            {
                effectRect.anchoredPosition = Vector2.zero;
                effectRect.localScale = Vector3.one;
                effectRect.localPosition = new Vector3(effectRect.localPosition.x, effectRect.localPosition.y, 0f);
            }

            yield return new WaitForSecondsRealtime(0.1f);
        }

        yield return StartCoroutine(Anim_MonstersPop());

        bool showNext = !LevelController.IsFinalLevel() && nextButton != null;
        bool showLevelSelect = levelSelectButton != null;

        if (showNext || showLevelSelect)
        {
            yield return StartCoroutine(Anim_ButtonsAppear(showNext, showLevelSelect));
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

    private IEnumerator Anim_ButtonsAppear(bool showNext, bool showLevelSelect)
    {
        if (showNext && nextButton != null)
        {
            nextButton.transform.localScale = Vector3.zero;
            nextButton.SetActive(true);
        }

        if (showLevelSelect && levelSelectButton != null)
        {
            levelSelectButton.transform.localScale = Vector3.zero;
            levelSelectButton.SetActive(true);
        }

        float time = 0f;
        float duration = 0.3f;
        float finalScale = 0.8f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float scale = Mathf.Lerp(0f, finalScale, time / duration);
            Vector3 targetScale = new Vector3(scale, scale, 1f);

            if (showNext && nextButton != null)
            {
                nextButton.transform.localScale = targetScale;
            }
            if (showLevelSelect && levelSelectButton != null)
            {
                levelSelectButton.transform.localScale = targetScale;
            }
            yield return null;
        }

        Vector3 ScaleVector = new Vector3(finalScale, finalScale, 1f);
        if (showNext && nextButton != null) nextButton.transform.localScale = ScaleVector;
        if (showLevelSelect && levelSelectButton != null) levelSelectButton.transform.localScale = ScaleVector;
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
        Vector2 targetPos = startPos + new Vector2(0, 1500f); // Fly upward

        // Reparent monsters while preserving their world/screen positions so only the background flies up
        if (redMonster != null) redMonster.SetParent(background.parent, true);
        if (blueMonster != null) blueMonster.SetParent(background.parent, true);

        Vector3 redStartScale = redMonster != null ? redMonster.localScale : Vector3.one;
        Vector3 blueStartScale = blueMonster != null ? blueMonster.localScale : Vector3.one;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;

            float easeIn = t * t;

            // Only the background moves upward while monsters shrink to zero
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

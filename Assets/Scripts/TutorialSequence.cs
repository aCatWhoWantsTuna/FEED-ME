using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TutorialSequence : MonoBehaviour
{
    [Header("-- Cover Lock --")]
    public GameObject coverPanel;

    [Header("-- Step 1~6 Visuals --")]
    public GameObject[] tutorialSteps;

    [Header("-- Press Space Prompt --")]
    public GameObject pressSpacePrompt;

    [Header("-- Press Space Positions --")]
    public Vector2[] spacePromptPositions;

    [Header("-- Start Button --")]
    public GameObject startButton;

    [Header("-- Animation Settings --")]
    public float popDuration = 0.2f;
    public float popOvershoot = 1.15f;

    private int currentStep = 0;
    private bool isTutorialFinished = false;
    private Coroutine currentAnimCoroutine;

    private static int savedStep = 0;
    private static bool savedFinished = false;

    void Start()
    {
        currentStep = savedStep;
        isTutorialFinished = savedFinished;

        // Preserve tutorial completion state across scene resets
        if (isTutorialFinished)
        {
            if (pressSpacePrompt != null) pressSpacePrompt.SetActive(false);

            if (startButton != null)
            {
                startButton.SetActive(true);
                startButton.transform.localScale = Vector3.one;
            }

            for (int i = 0; i < tutorialSteps.Length; i++)
            {
                tutorialSteps[i].SetActive(false);
            }

            return;
        }

        if (pressSpacePrompt != null) pressSpacePrompt.SetActive(true);
        if (startButton != null) startButton.SetActive(false);

        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            if (i == currentStep)
            {
                tutorialSteps[i].SetActive(true);
                PlayPopAnimation(tutorialSteps[i]);
                UpdateSpacePromptPosition();
            }
            else
            {
                tutorialSteps[i].SetActive(false);
            }
        }
    }

    void Update()
    {
        if (coverPanel != null && coverPanel.activeInHierarchy)
        {
            return;
        }

        if (!isTutorialFinished && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            NextStep();
        }
    }

    public void NextStep()
    {
        if (currentStep < tutorialSteps.Length)
        {
            tutorialSteps[currentStep].SetActive(false);
        }

        currentStep++;
        savedStep = currentStep;

        if (currentStep < tutorialSteps.Length)
        {
            tutorialSteps[currentStep].SetActive(true);
            PlayPopAnimation(tutorialSteps[currentStep]);
            UpdateSpacePromptPosition();

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX("notification");
            }
        }
        else
        {
            isTutorialFinished = true;
            savedFinished = true; // Mark all tutorial steps as completed

            if (pressSpacePrompt != null) pressSpacePrompt.SetActive(false);

            if (startButton != null)
            {
                startButton.SetActive(true);
                PlayPopAnimation(startButton);
            }
        }
    }

    private void UpdateSpacePromptPosition()
    {
        if (pressSpacePrompt != null && spacePromptPositions != null && currentStep < spacePromptPositions.Length)
        {
            Vector2 targetPos = spacePromptPositions[currentStep];
            pressSpacePrompt.transform.localPosition = new Vector3(targetPos.x, targetPos.y, pressSpacePrompt.transform.localPosition.z);
        }
    }

    public void OnStartButtonClicked()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("click");
        }

        // Load Level_1 (index 0) via LevelController so it initializes from LevelData
        LevelController.LoadLevelByIndex(0);
        gameObject.SetActive(false);
    }

    private void PlayPopAnimation(GameObject target)
    {
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
        }
        currentAnimCoroutine = StartCoroutine(PopRoutine(target.transform));
    }

    private IEnumerator PopRoutine(Transform targetTransform)
    {
        targetTransform.localScale = Vector3.zero;
        float time = 0f;
        float halfDuration = popDuration / 2f;
        Vector3 overshootScale = Vector3.one * popOvershoot;

        while (time < halfDuration)
        {
            time += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(Vector3.zero, overshootScale, time / halfDuration);
            yield return null;
        }

        time = 0f;
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(overshootScale, Vector3.one, time / halfDuration);
            yield return null;
        }

        targetTransform.localScale = Vector3.one;
    }
}

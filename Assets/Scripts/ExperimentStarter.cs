using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ExperimentStarter : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject startScreen;
    public TextMeshProUGUI startText;
    public TextMeshProUGUI instructionText;

    [Header("Start Screen Settings")]
    public string startMessage = "Click anywhere to start the experiment";
    public string instructionMessage = "Tap the circles when you see a good apple fall.\nDon't tap if the apple turns bad!";

    [Header("Fade Settings")]
    public float fadeOutDuration = 0.5f;

    private Main mainController;
    private Canvas startCanvas;
    private bool experimentStarted = false;

    void Awake()
    {
        mainController = FindObjectOfType<Main>();
        if (mainController == null)
        {
            Debug.LogError("ExperimentStarter: Main controller not found!");
            return;
        }

        mainController.enabled = false;

        if (startScreen == null)
        {
            CreateStartScreen();
        }

        SetupStartScreen();
    }

    void CreateStartScreen()
    {
        GameObject canvasObj = new GameObject("StartCanvas");
        startCanvas = canvasObj.AddComponent<Canvas>();
        startCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        startCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        startScreen = new GameObject("StartScreen");
        startScreen.transform.SetParent(canvasObj.transform, false);

        Image backgroundImage = startScreen.AddComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.8f);

        RectTransform bgRect = startScreen.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject instructionObj = new GameObject("InstructionText");
        instructionObj.transform.SetParent(startScreen.transform, false);

        instructionText = instructionObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = instructionMessage;
        instructionText.fontSize = 96;
        instructionText.color = Color.white;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.fontStyle = FontStyles.Normal;

        RectTransform instructionRect = instructionObj.GetComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0.1f, 0.6f);
        instructionRect.anchorMax = new Vector2(0.9f, 0.9f);
        instructionRect.offsetMin = Vector2.zero;
        instructionRect.offsetMax = Vector2.zero;

        GameObject startObj = new GameObject("StartText");
        startObj.transform.SetParent(startScreen.transform, false);

        startText = startObj.AddComponent<TextMeshProUGUI>();
        startText.text = startMessage;
        startText.fontSize = 96;
        startText.color = Color.yellow;
        startText.alignment = TextAlignmentOptions.Center;
        startText.fontStyle = FontStyles.Bold;

        RectTransform startRect = startObj.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.1f, 0.3f);
        startRect.anchorMax = new Vector2(0.9f, 0.5f);
        startRect.offsetMin = Vector2.zero;
        startRect.offsetMax = Vector2.zero;

        StartCoroutine(BlinkStartText());
    }

    void SetupStartScreen()
    {
        if (startText != null)
        {
            startText.text = startMessage;
            StartCoroutine(BlinkStartText());
        }

        if (instructionText != null)
        {
            instructionText.text = instructionMessage;
        }

        if (startScreen != null)
        {
            startScreen.SetActive(true);
        }
    }

    void Update()
    {
        if (!experimentStarted)
        {
            if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            {
                StartExperiment();
            }
        }
    }

    void StartExperiment()
    {
        if (experimentStarted) return;

        experimentStarted = true;
        Debug.Log("Experiment starting - user clicked to begin");

        EventManager.TriggerEvent(EventManager.EventType.GameStateChanged, gameObject, "experiment_starting");

        StartCoroutine(FadeOutAndStart());
    }

    IEnumerator FadeOutAndStart()
    {
        if (startScreen != null)
        {
            Image backgroundImage = startScreen.GetComponent<Image>();
            if (backgroundImage != null)
            {
                float startTime = Time.time;
                Color startColor = backgroundImage.color;

                Color startTextColor = startText != null ? startText.color : Color.white;
                Color instructionTextColor = instructionText != null ? instructionText.color : Color.white;

                while (Time.time < startTime + fadeOutDuration)
                {
                    float t = (Time.time - startTime) / fadeOutDuration;
                    float alpha = Mathf.Lerp(1f, 0f, t);

                    Color newBgColor = startColor;
                    newBgColor.a = alpha * 0.8f;
                    backgroundImage.color = newBgColor;

                    if (startText != null)
                    {
                        Color newStartColor = startTextColor;
                        newStartColor.a = alpha;
                        startText.color = newStartColor;
                    }

                    if (instructionText != null)
                    {
                        Color newInstructionColor = instructionTextColor;
                        newInstructionColor.a = alpha;
                        instructionText.color = newInstructionColor;
                    }

                    yield return null;
                }
            }

            startScreen.SetActive(false);
        }

        if (mainController != null)
        {
            mainController.enabled = true;
            Debug.Log("Main controller enabled - experiment will begin");
        }
    }

    IEnumerator BlinkStartText()
    {
        if (startText == null) yield break;

        while (!experimentStarted && startText != null)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
            {
                if (experimentStarted || startText == null) yield break;
                Color color = startText.color;
                color.a = Mathf.Lerp(1f, 0.3f, t);
                startText.color = color;
                yield return null;
            }

            for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
            {
                if (experimentStarted || startText == null) yield break;
                Color color = startText.color;
                color.a = Mathf.Lerp(0.3f, 1f, t);
                startText.color = color;
                yield return null;
            }
        }
    }

    public void ShowStartScreen()
    {
        experimentStarted = false;

        if (mainController != null)
        {
            mainController.enabled = false;
        }

        if (startScreen != null)
        {
            startScreen.SetActive(true);

            Image backgroundImage = startScreen.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(0, 0, 0, 0.8f);
            }

            if (startText != null)
            {
                startText.color = Color.yellow;
                StartCoroutine(BlinkStartText());
            }

            if (instructionText != null)
            {
                instructionText.color = Color.white;
            }
        }
    }

    public bool HasExperimentStarted()
    {
        return experimentStarted;
    }
}

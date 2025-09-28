using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class ExperimentStarter : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject startScreen;
    public TextMeshProUGUI startText;
    public TextMeshProUGUI instructionText;

    [Header("Background Reference")]
    public GameObject backgroundGameObject;

    [Header("Start Screen Settings")]
    public string startMessage = "Berühre den Bildschirm, um zu starten.";
    public string instructionMessage = "Fange die guten Äpfel wenn sie die weißen Kreise berühren, indem du auf den entsprechenden weißen Kreis drückst.\n\nBerühre den Kreis nicht, wenn der Apfel während des Fallens verrottet!";

    [Header("Border Settings")]
    public bool showBorder = true;
    public Color borderColor = Color.red;
    public float borderThickness = 2f;

    [Header("Fade Settings")] 
    public float fadeOutDuration = 0.5f;

    [Header("Responsive Settings")]
    public bool useBackgroundBounds = true;
    public float fontSizeMultiplier = 0.01f;

    private Main mainController;
    private Canvas startCanvas;
    private bool experimentStarted = false;
    private Vector2 backgroundSize;
    private Outline contentPanelOutline;

    void Awake()
    {
        mainController = FindFirstObjectByType<Main>();
        if (mainController == null)
        {
            Debug.LogError("ExperimentStarter: Main controller not found!");
            return;
        }

        mainController.enabled = false;

        GetBackgroundDimensions();

        if (startScreen == null)
        {
            CreateStartScreen();
        }

        SetupStartScreen();
    }

    void GetBackgroundDimensions()
    {
        if (backgroundGameObject != null)
        {
            SpriteRenderer bgRenderer = backgroundGameObject.GetComponent<SpriteRenderer>();
            if (bgRenderer != null && bgRenderer.sprite != null)
            {
                backgroundSize = bgRenderer.bounds.size;
                Debug.Log($"Background bounds size: {backgroundSize}");
            }
            else
            {
                Debug.LogWarning("Background GameObject doesn't have a SpriteRenderer or sprite!");
                FallbackToScreenSize();
               
            }
        }
        else
        {
            Debug.LogWarning("No background GameObject assigned, using screen size");
            FallbackToScreenSize();
        }
    }

    void FallbackToScreenSize()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float screenHeight = mainCamera.orthographicSize * 2f;
            float screenWidth = screenHeight * mainCamera.aspect;
            backgroundSize = new Vector2(screenWidth, screenHeight);
            Debug.Log($"Using camera bounds as background size: {backgroundSize}");
        }
        else
        {
            backgroundSize = new Vector2(16f, 9f);
            Debug.LogWarning("Using fallback background size");
        }
    }

    void CreateStartScreen()
    {
        GameObject canvasObj = new GameObject("StartCanvas");
        startCanvas = canvasObj.AddComponent<Canvas>();
        startCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        startCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        if (useBackgroundBounds)
        {
            float pixelsPerUnit = 100f;
            scaler.referenceResolution = new Vector2(
                backgroundSize.x * pixelsPerUnit,
                backgroundSize.y * pixelsPerUnit);
        }
        else
        {
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);
        }

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
        startText.fontSize = CalculateResponsiveFontSize() * 1.2f;
        startText.color = Color.yellow;
        startText.alignment = TextAlignmentOptions.Center;
        startText.fontStyle = FontStyles.Bold;

        RectTransform startRect = startObj.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.1f, 0.3f);
        startRect.anchorMax = new Vector2(0.9f, 0.5f);
        startRect.offsetMin = Vector2.zero;
        startRect.offsetMax = Vector2.zero;

        CreateContentPanelWithOutlineBorder(startScreen);

        StartCoroutine(BlinkStartText());
    }

    void CreateContentPanelWithOutlineBorder(GameObject parent)
    {
        GameObject contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(parent.transform, false);

        Image contentBg = contentPanel.AddComponent<Image>();
        contentBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        if (contentBg.sprite == null)
        {
            // Create a simple white sprite if none exists
            Texture2D whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            contentBg.sprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        RectTransform contentRect = contentPanel.GetComponent<RectTransform>();
        SetContentPanelPosition(contentRect);

        if (showBorder)
        {
            contentPanelOutline = contentPanel.AddComponent<Outline>();
            contentPanelOutline.effectColor = borderColor;
            contentPanelOutline.effectDistance = new Vector2(borderThickness, borderThickness);

            Debug.Log($"FORCED Border created - Color: {contentPanelOutline.effectColor}, Distance: {contentPanelOutline.effectDistance}");
            Debug.Log($"Content panel has Image: {contentBg != null}");
            Debug.Log($"Outline component added: {contentPanelOutline != null}");
            Debug.Log($"Image color: {contentBg.color}");
        }

        GameObject instructionObj = new GameObject("InstructionText");
        instructionObj.transform.SetParent(contentPanel.transform, false);

        instructionText = instructionObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = instructionMessage;
        instructionText.fontSize = CalculateResponsiveFontSize();
        instructionText.color = Color.white;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.fontStyle = FontStyles.Normal;

        RectTransform instructionRect = instructionObj.GetComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0.05f, 0.5f);
        instructionRect.anchorMax = new Vector2(0.95f, 0.9f);
        instructionRect.offsetMin = Vector2.zero;
        instructionRect.offsetMax = Vector2.zero;

 
        GameObject startObj = new GameObject("StartText");
        startObj.transform.SetParent(contentPanel.transform, false);

        startText = startObj.AddComponent<TextMeshProUGUI>();
        startText.text = startMessage;
        startText.fontSize = CalculateResponsiveFontSize() * 1.2f; // Slightly larger
        startText.color = Color.yellow;
        startText.alignment = TextAlignmentOptions.Center;
        startText.fontStyle = FontStyles.Bold;

        RectTransform startRect = startObj.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.05f, 0.1f);
        startRect.anchorMax = new Vector2(0.95f, 0.4f);
        startRect.offsetMin = Vector2.zero;
        startRect.offsetMax = Vector2.zero;


    }

    private void SetContentPanelPosition(RectTransform contentRect)
    {
        if (backgroundGameObject != null && useBackgroundBounds)
        {
            SpriteRenderer bgRenderer = backgroundGameObject.GetComponent<SpriteRenderer>();
            if (bgRenderer != null)
            {
                Bounds bgBounds = bgRenderer.bounds;

                Camera mainCamera = Camera.main;
                if(mainCamera != null)
                {
                    Vector3 bottomLeft = mainCamera.WorldToScreenPoint(bgBounds.min);
                    Vector3 topRight = mainCamera.WorldToScreenPoint(bgBounds.max);

                    float normalizedLeft = bottomLeft.x / Screen.width;
                    float normalizedBottom = bottomLeft.y / Screen.height;
                    float normalizedRight = topRight.x / Screen.width;
                    float normalizedTop = topRight.y / Screen.height;

                    float marginConstant = 0.0f;

                    float marginX = (normalizedRight - normalizedLeft) * marginConstant;
                    float marginY = (normalizedTop - normalizedBottom) * marginConstant;

                    contentRect.anchorMin = new Vector2(
                        normalizedLeft + marginX,
                        normalizedBottom + marginY);
                    contentRect.anchorMax = new Vector2(
                        normalizedRight - marginX,
                        normalizedTop - marginY);

                    contentRect.offsetMin = Vector2.zero;
                    contentRect.offsetMax = Vector2.zero;

                    Debug.Log($"Background screen bounds: ({normalizedLeft:F2}, {normalizedBottom:F2}) to ({normalizedRight:F2}, {normalizedTop:F2})");
                    Debug.Log($"Content panel bounds: ({contentRect.anchorMin.x:F2}, {contentRect.anchorMin.y:F2}) to ({contentRect.anchorMax.x:F2}, {contentRect.anchorMax.y:F2})");
                }
            }
        }
    }

    float CalculateResponsiveFontSize()
    {
        float baseFontSize;

        if (useBackgroundBounds && backgroundGameObject != null)
        {
            SpriteRenderer bgRenderer = backgroundGameObject.GetComponent<SpriteRenderer>();

            if (bgRenderer != null && bgRenderer.sprite != null)
            {
                float pixelsPerUnit = bgRenderer.sprite.pixelsPerUnit;
                baseFontSize = backgroundSize.y * pixelsPerUnit * fontSizeMultiplier;
                Debug.Log($"Background PPU: {pixelsPerUnit}, WorldHeight: {backgroundSize.y}");
            }
            else
            {
                baseFontSize = Screen.height * fontSizeMultiplier;
            }

        }
        else
        {
            baseFontSize = Screen.height * fontSizeMultiplier;
        }


        baseFontSize = Mathf.Clamp(baseFontSize, 5f, 200f);

        Debug.Log($"Calculated responsive font size: {baseFontSize}");
        return baseFontSize;
    }

    void SetupStartScreen()
    {
        if (startText != null)
        {
            startText.text = startMessage;
            startText.fontSize = CalculateResponsiveFontSize() * 1.2f;
            StartCoroutine(BlinkStartText());
        }

        if (instructionText != null)
        {
            instructionText.text = instructionMessage;
            instructionText.fontSize = CalculateResponsiveFontSize();
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
            Image contentPanelImage = startScreen.GetComponentInChildren<Image>();

            if (backgroundImage != null)
            {
                float startTime = Time.time;
                Color startBgColor = backgroundImage.color;
                Color startContentColor = contentPanelImage != null ? contentPanelImage.color : Color.black;
                Color startTextColor = startText != null ? startText.color : Color.white;
                Color instructionTextColor = instructionText != null ? instructionText.color : Color.white;
                Color startOutlineColor = contentPanelOutline != null ? contentPanelOutline.effectColor : Color.white;

                while (Time.time < startTime + fadeOutDuration)
                {
                    float t = (Time.time - startTime) / fadeOutDuration;
                    float alpha = Mathf.Lerp(1f, 0f, t);


                    Color newBgColor = startBgColor;
                    newBgColor.a = alpha * 0.8f;
                    backgroundImage.color = newBgColor;

   
                    if (contentPanelImage != null)
                    {
                        Color newContentColor = startContentColor;
                        newContentColor.a = alpha * 0.95f;
                        contentPanelImage.color = newContentColor;
                    }

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


                    if (contentPanelOutline != null)
                    {
                        Color newOutlineColor = startOutlineColor;
                        newOutlineColor.a = alpha;
                        contentPanelOutline.effectColor = newOutlineColor;
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

            // Reset content panel
            Image contentPanelImage = startScreen.GetComponentInChildren<Image>();
            if (contentPanelImage != null)
            {
                contentPanelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            }

            // Reset outline
            if (contentPanelOutline != null)
            {
                contentPanelOutline.effectColor = borderColor;
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

    // Methods to control border at runtime
    public void SetBorderColor(Color newColor)
    {
        borderColor = newColor;
        if (contentPanelOutline != null)
        {
            contentPanelOutline.effectColor = newColor;
        }
    }

    public void SetBorderThickness(float thickness)
    {
        borderThickness = thickness;
        if (contentPanelOutline != null)
        {
            contentPanelOutline.effectDistance = new Vector2(thickness, thickness);
        }
    }

    public void SetBorderVisible(bool visible)
    {
        showBorder = visible;
        if (contentPanelOutline != null)
        {
            contentPanelOutline.enabled = visible;
        }
    }

    // Debug method to check background info
    [ContextMenu("Debug Background Info")]
    void DebugBackgroundInfo()
    {
        GetBackgroundDimensions();
        Debug.Log($"Background size: {backgroundSize}");
        Debug.Log($"Screen resolution: {Screen.width}x{Screen.height}");
        Debug.Log($"Calculated font size: {CalculateResponsiveFontSize()}");
    }
}

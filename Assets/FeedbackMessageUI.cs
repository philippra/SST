using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FeedbackMessageUI : MonoBehaviour
{
    [Header("Message Settings")]
    public float displayDuration = 2f;
    public string badAppleMessage = "Schlechten Apfel gefangen!";

    [Header("Styling")]
    public Color messageColor = Color.white;
    public Color backgroundColor = new Color(0, 0, 0, 0.8f);
    public int fontSize = 32;

    private Canvas feedbackCanvas;
    private GameObject messagePanel;
    private Text feedbackText;

    private static FeedbackMessageUI instance;

    public static FeedbackMessageUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<FeedbackMessageUI>();
            }
            return instance;
        }
    }

    void Awake()
    {
        // Ensure singleton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        CreateUI();
        Debug.Log("FeedbackMessageUI initialized");
    }

    void CreateUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("FeedbackCanvas");
        canvasObj.transform.SetParent(transform);

        feedbackCanvas = canvasObj.AddComponent<Canvas>();
        feedbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        feedbackCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create background panel
        messagePanel = new GameObject("MessagePanel");
        messagePanel.transform.SetParent(canvasObj.transform, false);

        Image panelImage = messagePanel.AddComponent<Image>();
        panelImage.color = backgroundColor;

        RectTransform panelRect = messagePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700, 200);
        panelRect.anchoredPosition = Vector2.zero;

        // Create text
        GameObject textObj = new GameObject("FeedbackText");
        textObj.transform.SetParent(messagePanel.transform, false);

        feedbackText = textObj.AddComponent<Text>();
        feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        feedbackText.fontSize = fontSize;
        feedbackText.color = messageColor;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.fontStyle = FontStyle.Bold;

        RectTransform textRect = feedbackText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 20);
        textRect.offsetMax = new Vector2(-20, -20);

        // CRITICAL: Hide the panel immediately after creation
        messagePanel.SetActive(false);

        Debug.Log("UI created and hidden");
    }

    public void ShowBadAppleMessage()
    {
        Debug.Log("ShowBadAppleMessage called");

        if (messagePanel == null)
        {
            Debug.LogError("Message panel is null! UI not properly initialized.");
            return;
        }

        if (feedbackText == null)
        {
            Debug.LogError("Feedback text is null! UI not properly initialized.");
            return;
        }

        // Set the text and show the panel
        feedbackText.text = badAppleMessage;
        messagePanel.SetActive(true);

        Debug.Log($"Showing message: '{badAppleMessage}' - Panel active: {messagePanel.activeInHierarchy}");

        // Trigger event that feedback message is showing
        EventManager.TriggerEvent(EventManager.EventType.FeedbackMessageStateChanged, gameObject, true);

        // Start coroutine to hide after duration
        StartCoroutine(HideMessageAfterDelay());
    }

    public void HideMessage()
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
            Debug.Log("Message hidden");
        }
    }

    private IEnumerator HideMessageAfterDelay()
    {
        Debug.Log($"Starting delay for {displayDuration} seconds");
        yield return new WaitForSeconds(displayDuration);

        HideMessage();

        // Trigger event that feedback message is no longer showing
        EventManager.TriggerEvent(EventManager.EventType.FeedbackMessageStateChanged, gameObject, false);
        Debug.Log("Message hide event triggered");
    }

    // Optional: Method to show custom messages
    public void ShowCustomMessage(string message, float duration = -1)
    {
        if (messagePanel != null && feedbackText != null)
        {
            feedbackText.text = message;
            messagePanel.SetActive(true);

            float actualDuration = duration > 0 ? duration : displayDuration;
            StartCoroutine(HideCustomMessageAfterDelay(actualDuration));
        }
    }

    private IEnumerator HideCustomMessageAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        HideMessage();
    }

    // Debug method to test the UI
    [ContextMenu("Test Show Message")]
    public void TestShowMessage()
    {
        ShowBadAppleMessage();
    }
}

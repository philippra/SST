using UnityEngine;
using System.Collections;

public class Mover : MonoBehaviour
{
    private float moveSpeed = 3.07f; // For 1300 milliseconds (1.3 seconds) falling duration

    [Header("Timing Settings")]
    public float bottomWaitTime = 1f;
    [Header("Response Settings")]
    public float validResponseStartTime = 0.5f;  // 500ms after falling
    public float validResponseEndTime = 0.8f;    // 800ms after falling
    public float resetDelay = 1f;
    [Header("Sprite Settings")]
    public Sprite normalAppleSprite;
    public Sprite badAppleSprite;
    [Header("Auto Start")]
    public bool startOnAwake = false;

    private Camera mainCamera;
    private Vector2 topPosition;
    private Vector2 bottomPosition;
    private float fallingStartTime = 0f;
    private bool isInValidResponseWindow = false;
    private bool responseRegistered = false;
    private bool isStopTrial = false;
    private bool stopSignalShown = false;
    private bool correctResponse = false;
    private bool trialTypeSetExternally = false;

    // Reference to Main for getting global SSD
    private Main mainController;

    private bool _isMoving = false;

    public bool isMoving
    {
        get => _isMoving;
        private set
        {
            if (_isMoving != value)
            {
                _isMoving = value;
                EventManager.TriggerEvent(EventManager.EventType.FruitMovementChanged, gameObject, value);
            }
        }
    }

    private bool _isBusy = false;
    public bool isBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                EventManager.TriggerEvent(EventManager.EventType.FruitBusyStateChanged, gameObject, value);
            }
        }
    }

    private Coroutine movementCoroutine;
    private Coroutine responseWindowCoroutine;
    private Coroutine stopSignalCoroutine;

    [HideInInspector]
    public bool isAvailableForMovement = true;

    private FruitSlicer fruitSlicer;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        mainCamera = Camera.main;

        mainController = FindObjectOfType<Main>();
        if (mainController == null)
        {
            Debug.LogError("Main controller not found! SSD will not work properly.");
        }

        fruitSlicer = GetComponent<FruitSlicer>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (normalAppleSprite == null && spriteRenderer != null)
        {
            normalAppleSprite = spriteRenderer.sprite;
        }

        SetupPositions();

        if (startOnAwake)
        {
            StartMovement();
        }
    }

    void SetupPositions()
    {
        topPosition = new Vector2(transform.position.x, 2f);
        bottomPosition = new Vector2(transform.position.x, -2f);
        transform.position = topPosition;
    }

    public void SetTrialType(bool shouldBeStopTrial)
    {
        isStopTrial = shouldBeStopTrial;
        trialTypeSetExternally = true;
        Debug.Log($"{gameObject.name} trial type set to: {(isStopTrial ? "STOP" : "GO")}");
    }

    IEnumerator MovementCycle()
    {
        isAvailableForMovement = false;
        isBusy = true;

        stopSignalShown = false;
        correctResponse = false;

        Debug.Log($"{gameObject.name} starting to move down - Stop trial: {isStopTrial} - Current SSD: {mainController.GetCurrentStopSignalDelay()}");

        fallingStartTime = Time.time;
        responseRegistered = false;

        if (responseWindowCoroutine != null)
        {
            StopCoroutine(responseWindowCoroutine);
        }
        responseWindowCoroutine = StartCoroutine(TrackResponseWindow());

        if (isStopTrial)
        {
            if (stopSignalCoroutine != null)
            {
                StopCoroutine(stopSignalCoroutine);
            }
            stopSignalCoroutine = StartCoroutine(TrackStopSignalDelay());
        }

        yield return StartCoroutine(MoveToPosition(bottomPosition));


        if (!responseRegistered)
        {
            if (isStopTrial)
            {
                // Correct response for stop trial is NO response
                correctResponse = true;
                Debug.Log($"{gameObject.name} CORRECT - successfully stopped on stop trial");
            }
            else
            {
                // Incorrect response for go trial is NO response (miss)
                correctResponse = false;
                Debug.Log($"{gameObject.name} MISSED - failed to respond on go trial");
            }

            EventManager.TriggerEvent(EventManager.EventType.ResponseRegistered, gameObject, null);

            Debug.Log($"{gameObject.name} waiting at bottom for {bottomWaitTime} seconds");
            yield return new WaitForSeconds(bottomWaitTime);
        }

        if (!responseRegistered)
        {

            Debug.Log($"{gameObject.name} resetting to top");
            ResetToTop();
        }

        //Debug.Log($"{gameObject.name} movement cycle complete, clearing coroutine");
        movementCoroutine = null;

        trialTypeSetExternally = false;

        isAvailableForMovement = true;
        isBusy = false;
    }

    IEnumerator TrackStopSignalDelay()
    {
        int currentSSD = mainController != null ? mainController.GetCurrentStopSignalDelay() : 250;
        float delayInSeconds = currentSSD / 1000f;

        Debug.Log($"{gameObject.name} using global SSD of {currentSSD}ms");
        yield return new WaitForSeconds(delayInSeconds);

        if (!responseRegistered && spriteRenderer != null && badAppleSprite != null)
        {
            spriteRenderer.sprite = badAppleSprite;
            stopSignalShown = true;
            Debug.Log($"{gameObject.name} stop signal shown - apple turned bad after {currentSSD}ms");
        }
    }

    IEnumerator TrackResponseWindow()
    {
        float timeToWait = validResponseStartTime;
        yield return new WaitForSeconds(timeToWait);

        isInValidResponseWindow = true;
        EventManager.TriggerEvent(EventManager.EventType.ValidResponseWindowChanged, gameObject, true);
        Debug.Log($"{gameObject.name} entered valid response window at {Time.time - fallingStartTime:F2}s after falling");

        timeToWait = validResponseEndTime - validResponseStartTime;
        yield return new WaitForSeconds(timeToWait);

        isInValidResponseWindow = false;
        EventManager.TriggerEvent(EventManager.EventType.ValidResponseWindowChanged, gameObject, false);
        Debug.Log($"{gameObject.name} exited valid response window at {Time.time - fallingStartTime:F2}s after falling");
    }

    IEnumerator MoveToPosition(Vector2 targetPosition)
    {
        Debug.Log("MoveToPosition started.");
        isMoving = true;

        while (Vector2.Distance(transform.position, targetPosition) > 0.01f && !responseRegistered)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (this.name == "leftFruit")
                transform.Rotate(0f, 0f, 15f * Time.deltaTime);
            else
                transform.Rotate(0f, 0f, -15f * Time.deltaTime);
            yield return null;
        }

        if (!responseRegistered)
        {
            transform.position = targetPosition;
        }

        isMoving = false;
        //Debug.Log("MoveToPosition completed.");
    }

    public void StartMovement()
    {
        if (isAvailableForMovement && movementCoroutine == null)
        {
            Debug.Log($"{gameObject.name} starting movement cycle");
            isAvailableForMovement = false;
            movementCoroutine = StartCoroutine(MovementCycle());
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} is not available for movement! " +
                             $"Available={isAvailableForMovement}, Coroutine={movementCoroutine != null}");
        }
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            Debug.Log($"{gameObject.name} stopping existing movement coroutine");
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        if (responseWindowCoroutine != null)
        {
            StopCoroutine(responseWindowCoroutine);
            responseWindowCoroutine = null;
        }

        if (stopSignalCoroutine != null)
        {
            StopCoroutine(stopSignalCoroutine);
            stopSignalCoroutine = null;
        }

        isMoving = false;
        isInValidResponseWindow = false;
        isBusy = false;
        isAvailableForMovement = true;
        trialTypeSetExternally = false;
        Debug.Log($"{gameObject.name} movement stopped");
    }

    public void RegisterResponse()
    {
        if (isInValidResponseWindow && !responseRegistered)
        {
            Debug.Log($"{gameObject.name} response registered at {Time.time - fallingStartTime:F2}s after falling");

            if (isStopTrial && stopSignalShown)
            {
                // Responded after seeing stop signal - incorrect
                correctResponse = false;
                Debug.Log($"{gameObject.name} INCORRECT RESPONSE - should have stopped after seeing bad apple!");
                
                if (FeedbackMessageUI.Instance != null)
                {
                    FeedbackMessageUI.Instance.ShowBadAppleMessage();
                }

            }
            else if (isStopTrial && !stopSignalShown)
            {
                // Responded before stop signal on stop trial - incorrect
                correctResponse = false;
                Debug.Log($"{gameObject.name} CORRECT RESPONSE - responded before stop signal appeared");
            }
            else
            {
                // Go trial response - correct
                correctResponse = true;
                Debug.Log($"{gameObject.name} CORRECT RESPONSE on go trial");
            }

            responseRegistered = true;

            EventManager.TriggerEvent(EventManager.EventType.ResponseRegistered, gameObject, null);

            FruitSlicer slicer = GetComponent<FruitSlicer>();
            if (slicer != null && !isStopTrial)
            {
                Debug.Log($"Slicing fruit at current position: {transform.position}");
                slicer.SliceFruit();
            }
            else
            {
                Debug.Log("No position-aware slicer found, just hiding sprite");
                GetComponent<SpriteRenderer>().enabled = false;
            }

            StartCoroutine(ResetAfterResponse());
        }
        else if (isMoving && !isInValidResponseWindow)
        {
            Debug.Log($"{gameObject.name} response MISSED - outside window at {Time.time - fallingStartTime:F2}s after falling");
        }
    }

    IEnumerator ResetAfterResponse()
    {
        yield return new WaitForSeconds(resetDelay);
        Debug.Log("Resetting after response");
        ResetToTop();
    }

    public void ResetToTop()
    {
        Debug.Log($"{gameObject.name} resetting to top, coroutine status: {(movementCoroutine != null ? "active" : "null")}");
        StopMovement();
        transform.position = topPosition;
        transform.eulerAngles = new Vector3(0f, 0f, 0f);

        if (spriteRenderer != null && normalAppleSprite != null)
        {
            spriteRenderer.sprite = normalAppleSprite;
        }

        // Reset the position-aware slicer if available
        FruitSlicer slicer = GetComponent<FruitSlicer>();
        if (slicer != null)
        {
            slicer.ResetFruit();
        }
        else
        {
            GetComponent<SpriteRenderer>().enabled = true;
        }

        isAvailableForMovement = true;
    }

    public bool IsInValidResponseWindow()
    {
        return isInValidResponseWindow;
    }

    public bool WasStopTrial()
    {
        return isStopTrial;
    }

    public bool WasCorrectResponse()
    {
        return correctResponse;
    }
}
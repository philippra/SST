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
    [Header("SSD settings")]
    public int stopSignalDelay = 250;
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

        // Get the FruitSlicer component
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
        // Update to use the exact positions specified (2 to -2)
        topPosition = new Vector2(transform.position.x, 2f);
        bottomPosition = new Vector2(transform.position.x, -2f);
        transform.position = topPosition;
    }

    IEnumerator MovementCycle()
    {
        isAvailableForMovement = false;
        isBusy = true;

        isStopTrial = (Random.Range(0.0f, 1.0f) <= 0.35f);
        stopSignalShown = false;

        // Move down to bottom
        Debug.Log($"{gameObject.name} starting to move down - Stop trial: {isStopTrial}");

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

        // Only do the bottom wait if no response was registered
        if (!responseRegistered)
        {
            if (isStopTrial && stopSignalDelay < 500)
            {
                stopSignalDelay += 50;
                Debug.Log($"Stop signal delay is now {stopSignalDelay}ms");
            }
            // Wait at bottom
            Debug.Log($"{gameObject.name} waiting at bottom for {bottomWaitTime} seconds");
            yield return new WaitForSeconds(bottomWaitTime);
        }


        if (!responseRegistered)
        {
            // Reset to top position after no response
            Debug.Log($"{gameObject.name} resetting to top");
            ResetToTop();
        }

        Debug.Log($"{gameObject.name} movement cycle complete, clearing coroutine");
        movementCoroutine = null;

        // Now it's available again
        isAvailableForMovement = true;
        isBusy = false;
    }

    IEnumerator TrackStopSignalDelay()
    {
        float delayInSeconds = stopSignalDelay / 1000f;
        yield return new WaitForSeconds(delayInSeconds);

        if (!responseRegistered && spriteRenderer != null && badAppleSprite != null)
        {
            spriteRenderer.sprite = badAppleSprite;
            stopSignalShown = true;
            Debug.Log($"{gameObject.name} stop signal shown - apple turned bad after {stopSignalDelay}ms");
        }

    }

    IEnumerator TrackResponseWindow()
    {
        // Wait until response window starts
        float timeToWait = validResponseStartTime;
        yield return new WaitForSeconds(timeToWait);

        // Enter valid response window
        isInValidResponseWindow = true;
        EventManager.TriggerEvent(EventManager.EventType.ValidResponseWindowChanged, gameObject, true);
        Debug.Log($"{gameObject.name} entered valid response window at {Time.time - fallingStartTime:F2}s after falling");

        // Wait until response window ends
        timeToWait = validResponseEndTime - validResponseStartTime;
        yield return new WaitForSeconds(timeToWait);

        // Exit valid response window
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
        Debug.Log("MoveToPosition completed.");
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
        Debug.Log($"{gameObject.name} movement stopped");
    }

    public void RegisterResponse()
    {
        if (isInValidResponseWindow && !responseRegistered)
        {
            Debug.Log($"{gameObject.name} response registered at {Time.time - fallingStartTime:F2}s after falling");
            
            if (isStopTrial && stopSignalShown)
            {
                Debug.Log($"{gameObject.name} INCORRECT RESPONSE - should have stopped after seeing bad apple!");
                stopSignalDelay = stopSignalDelay > 0 ? stopSignalDelay-50 : 0;
                Debug.Log($"Stop signal delay is now {stopSignalDelay}ms");
            }
            else if (isStopTrial && !stopSignalShown)
            {
                Debug.Log($"{gameObject.name} PREMATURE INCORRECT RESPONSE - did not wait until apple turned brown.");
                stopSignalDelay = stopSignalDelay > 0 ? stopSignalDelay - 50 : 0;
                Debug.Log($"Stop signal delay is now {stopSignalDelay}ms");
            }
            else
            {
                Debug.Log($"{gameObject.name} correct response on go trial");
            }


            responseRegistered = true;

            // Use the position-aware slicer
            FruitSlicer slicer = GetComponent<FruitSlicer>();
            if (slicer != null)
            {
                // This will use the current position of the apple
                Debug.Log($"Slicing fruit at current position: {transform.position}");
                slicer.SliceFruit();
            }
            else
            {
                // Fallback to the old behavior
                Debug.Log("No position-aware slicer found, just hiding sprite");
                GetComponent<SpriteRenderer>().enabled = false;
            }

            // Start the reset timer
            StartCoroutine(ResetAfterResponse());
        }
        else if (isMoving && !isInValidResponseWindow)
        {
            Debug.Log($"{gameObject.name} response MISSED - outside window at {Time.time - fallingStartTime:F2}s after falling");
        }
    }

    IEnumerator ResetAfterResponse()
    {
        // Wait for the reset delay
        yield return new WaitForSeconds(resetDelay);
        Debug.Log("Resetting after response");
        // Reset position and make visible again
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

    // Public method to check if this fruit is currently in its valid response window
    public bool IsInValidResponseWindow()
    {
        return isInValidResponseWindow;
    }
}

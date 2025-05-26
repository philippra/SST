using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Main : MonoBehaviour
{
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("Fruit List")]
    public List<GameObject> fruits = new List<GameObject>();

    // Track which fruits are busy
    private HashSet<GameObject> busyFruits = new HashSet<GameObject>();
    // Track which fruits are in their valid response window
    private HashSet<GameObject> responseFruits = new HashSet<GameObject>();
    // tracks which fruits currently have running slice animations
    private HashSet<GameObject> animatingFruits = new HashSet<GameObject>();

    private bool feedbackMessageShowing = false;

    void Start()
    {
        // Subscribe to movement state changes
        EventManager.Subscribe(EventManager.EventType.FruitMovementChanged, OnFruitMovementChanged);
        EventManager.Subscribe(EventManager.EventType.FruitBusyStateChanged, OnFruitBusyChanged);
        EventManager.Subscribe(EventManager.EventType.ValidResponseWindowChanged, OnValidResponseWindowChanged);
        EventManager.Subscribe(EventManager.EventType.SliceAnimationStateChanged, OnAnimationStateChanged);
        EventManager.Subscribe(EventManager.EventType.FeedbackMessageStateChanged, OnFeedbackMessageStateChanged);

        // Make sure all fruits have the correct tag
        foreach (var fruit in fruits)
        {
            if (!fruit.CompareTag("Fruit"))
            {
                Debug.LogWarning($"{fruit.name} does not have the 'Fruit' tag. This may cause issues with response detection.");
            }
            AppleSlicePrefabSetup sliceSetup = fruit.GetComponent<AppleSlicePrefabSetup>();
            if (sliceSetup != null)
            {
                sliceSetup.SetupSlicePrefabs();
            }
        }

        // Start the first fruit movement after a brief delay
        StartCoroutine(InitialMovementDelay());
    }

    void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        EventManager.Unsubscribe(EventManager.EventType.FruitMovementChanged, OnFruitMovementChanged);
        EventManager.Unsubscribe(EventManager.EventType.FruitBusyStateChanged, OnFruitBusyChanged);
        EventManager.Unsubscribe(EventManager.EventType.ValidResponseWindowChanged, OnValidResponseWindowChanged);
        EventManager.Unsubscribe(EventManager.EventType.SliceAnimationStateChanged, OnAnimationStateChanged);
    }

    IEnumerator InitialMovementDelay()
    {
        yield return new WaitForSeconds(0.1f);
        StartFruitMovement();
    }

    void OnFruitMovementChanged(GameObject fruit, object movingObj)
    {
        bool moving = (bool)movingObj;
        if (moving)
        {
            Debug.Log($"{fruit.name} is now moving.");
        }
        else
        {
            Debug.Log($"{fruit.name} stopped moving.");
        }
    }

    void OnFruitBusyChanged(GameObject fruit, object busyObj)
    {
        bool busy = (bool)busyObj;
        // Update our tracking
        if (busy)
        {
            busyFruits.Add(fruit);
            //Debug.Log($"{fruit.name} is now busy (in movement cycle). Total busy: {busyFruits.Count}");
        }
        else
        {
            if (busyFruits.Contains(fruit))
            {
                busyFruits.Remove(fruit);
                //Debug.Log($"{fruit.name} is no longer busy (cycle complete). Total busy: {busyFruits.Count}");

                // Wait a frame before checking if all fruits are done
                StartCoroutine(CheckAllFruitsComplete());
            }
        }
        CheckFruitStates();
    }

    void OnValidResponseWindowChanged(GameObject fruit, object inWindowObj)
    {
        bool inWindow = (bool)inWindowObj;
        // Track fruits in their response window
        if (inWindow)
        {
            responseFruits.Add(fruit);
            //Debug.Log($"{fruit.name} entered valid response window. Total in window: {responseFruits.Count}");
        }
        else
        {
            if (responseFruits.Contains(fruit))
            {
                responseFruits.Remove(fruit);
                //Debug.Log($"{fruit.name} exited valid response window. Total in window: {responseFruits.Count}");
            }
        }
    }

    void OnAnimationStateChanged(GameObject fruit, object animationObj)
    {
        bool animationRunning = (bool)animationObj;
        if (animationRunning)
        {
            animatingFruits.Add(fruit);
            Debug.Log($"{fruit.name} has an animation running.");
        }
        else
        {
            if (animatingFruits.Contains(fruit))
            {
                animatingFruits.Remove(fruit);
                Debug.Log($"{fruit.name} has no animation running.");

                StartCoroutine(CheckAllFruitsComplete());
            }

        }
    }

    void OnFeedbackMessageStateChanged(GameObject sender, object showingObj)
    {
        bool showing = (bool)showingObj;
        feedbackMessageShowing = showing;

        if (showing)
        {
            Debug.Log("Feedback message is now showing - preventing new trials");
        }
        else
        {
            Debug.Log("Feedback message hidden - can start new trials");
            StartCoroutine(CheckAllFruitsComplete());
        }
    }

    IEnumerator CheckAllFruitsComplete()
    {
        // Wait a frame to ensure all state changes have propagated
        yield return null;

        // NEW: Include feedback message state in the check
        if (busyFruits.Count == 0 && animatingFruits.Count == 0 && !feedbackMessageShowing)
        {
            Debug.Log("All fruit movement cycles complete and no feedback showing. Starting new fruit movement.");
            StartFruitMovement();
        }
        else
        {
            Debug.Log($"Waiting for completion - Busy fruits: {busyFruits.Count}, Animating fruits: {animatingFruits.Count}, Feedback showing: {feedbackMessageShowing}");
        }
    }

    void CheckFruitStates()
    {
        Debug.Log("--- Fruit State Check ---");
        foreach (var fruit in fruits)
        {
            Mover mover = fruit.GetComponent<Mover>();
            bool isInBusySet = busyFruits.Contains(fruit);
            bool isInResponseWindow = responseFruits.Contains(fruit);
            Debug.Log($"{fruit.name}: busy={isInBusySet}, inResponseWindow={isInResponseWindow}, availableForMovement={mover.isAvailableForMovement}");
        }
        Debug.Log($"Feedback message showing: {feedbackMessageShowing}");
        Debug.Log("-------------------------");
    }

    public GameObject SelectFruit()
    {
        // Select a truly random fruit each time
        int randomIndex = Random.Range(0, fruits.Count);
        GameObject selectedFruit = fruits[randomIndex];

        Debug.Log($"Selected random fruit: {selectedFruit.name}");
        return selectedFruit;
    }

    public void StartFruitMovement()
    {
        Debug.Log("StartFruitMovement called");

        // First, select the fruit
        GameObject selectedFruit = SelectFruit();

        if (selectedFruit != null)
        {
            // Start a coroutine to delay the actual movement
            StartCoroutine(DelayedMovementStart(selectedFruit));
        }
    }

    IEnumerator DelayedMovementStart(GameObject fruit)
    {
        // Wait a short time to ensure any previous coroutines are fully cleaned up
        // Debug.Log($"Waiting briefly before starting movement of {fruit.name}");
        // Wait at top for random time
        float randomWait = Random.Range(minWaitTime, maxWaitTime);
        Debug.Log($"{gameObject.name} waiting at top for {randomWait} seconds");
        yield return new WaitForSeconds(randomWait);

        // Now start the movement
        Mover mover = fruit.GetComponent<Mover>();
        if (mover != null)
        {
            Debug.Log($"Now starting delayed movement of {fruit.name}");
            mover.StartMovement();
        }
    }
}

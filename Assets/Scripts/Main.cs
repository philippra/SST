using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;

public class Main : MonoBehaviour
{
    [Header("Trial Sequence Settings")]
    public string trialSequenceFileName = "trial_sequence.json";
    public bool generateSequenceIfMissing = true;

    [Header("Trial Timing Settings")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    [Header("Fruit References")]
    public GameObject leftFruit;
    public GameObject rightFruit;

    [Header("Stop Signal Settings")]
    private int globalStopSignalDelay = 250;
    public int ssdStep = 50;


    private ExperimentSequence currentSequence; // Trial sequence data
    private int currentTrialIndex = 0;
    private bool sequenceLoaded = false;

    // Track which fruits are busy
    private HashSet<GameObject> busyFruits = new HashSet<GameObject>();
    // Track which fruits are in their valid response window
    private HashSet<GameObject> responseFruits = new HashSet<GameObject>();
    // tracks which fruits currently have running slice animations
    private HashSet<GameObject> animatingFruits = new HashSet<GameObject>();

    private ExperimentStarter experimentStarter;

    private bool trialInitiated = false;
    private bool feedbackMessageShowing = false;
    private bool animationRunning = false;


    void Start()
    {
        experimentStarter = FindObjectOfType<ExperimentStarter>();

        EventManager.Subscribe(EventManager.EventType.TrialInitiated, OnTrialInitiated);
        EventManager.Subscribe(EventManager.EventType.FruitMovementChanged, OnFruitMovementChanged);
        EventManager.Subscribe(EventManager.EventType.FruitBusyStateChanged, OnFruitBusyChanged);
        EventManager.Subscribe(EventManager.EventType.ValidResponseWindowChanged, OnValidResponseWindowChanged);
        EventManager.Subscribe(EventManager.EventType.SliceAnimationStateChanged, OnAnimationStateChanged);
        EventManager.Subscribe(EventManager.EventType.ResponseRegistered, OnResponseRegistered);
        EventManager.Subscribe(EventManager.EventType.FeedbackMessageStateChanged, OnFeedbackMessageStateChanged);

        SetupFruits();

        LoadTrialSequence();


        if (sequenceLoaded)
        {
            Debug.Log("Trial sequence loaded successfully. Waiting for user to start experiment...");
            StartCoroutine(InitialTrialDelay());
        }
        else
        {
            Debug.LogError("EXPERIMENT STOPPED: No valid trial sequence found!");
            enabled = false;
        }
    }

    void OnDestroy()
    {
        EventManager.Unsubscribe(EventManager.EventType.TrialInitiated, OnTrialInitiated);
        EventManager.Unsubscribe(EventManager.EventType.FruitMovementChanged, OnFruitMovementChanged);
        EventManager.Unsubscribe(EventManager.EventType.FruitBusyStateChanged, OnFruitBusyChanged);
        EventManager.Unsubscribe(EventManager.EventType.ValidResponseWindowChanged, OnValidResponseWindowChanged);
        EventManager.Unsubscribe(EventManager.EventType.SliceAnimationStateChanged, OnAnimationStateChanged);
        EventManager.Unsubscribe(EventManager.EventType.ResponseRegistered, OnResponseRegistered);
        EventManager.Unsubscribe(EventManager.EventType.FeedbackMessageStateChanged, OnFeedbackMessageStateChanged);
    }

    void SetupFruits()
    {
        if (leftFruit == null || rightFruit == null)
        {
            Debug.LogError("Left and Right fruit references must be assigned!");
            return;
        }

        GameObject[] fruits = { leftFruit, rightFruit };
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
    }

    void LoadTrialSequence()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, trialSequenceFileName);

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                currentSequence = JsonUtility.FromJson<ExperimentSequence>(json);
                sequenceLoaded = true;
                currentTrialIndex = 0;

                Debug.Log($"✓ Loaded trial sequence: {currentSequence.trials.Count} trials");
                Debug.Log($"Experiment: {currentSequence.experimentName}, Created: {currentSequence.createdDate}");

                if (currentSequence.trials == null || currentSequence.trials.Count == 0)
                {
                    throw new System.Exception("Trial sequence is empty!");
                }

                int goCount = 0, leftStopCount = 0, rightStopCount = 0;
                foreach (var trial in currentSequence.trials)
                {
                    if (trial.trialType == "go") goCount++;
                    else if (trial.trialType == "stop_left") leftStopCount++;
                    else if (trial.trialType == "stop_right") rightStopCount++;
                }
                Debug.Log($"Trial breakdown - Go: {goCount}, Stop Left: {leftStopCount}, Stop Right: {rightStopCount}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Error loading trial sequence from {filePath}: {e.Message}");
                sequenceLoaded = false;

                if (generateSequenceIfMissing)
                {
                    Debug.LogWarning("Attempting to generate new sequence...");
                    TryGenerateSequence();
                }
            }
        }
        else
        {
            Debug.LogError($"✗ Trial sequence file not found: {filePath}");
            Debug.LogError("Make sure the file exists in the StreamingAssets folder!");

            if (generateSequenceIfMissing)
            {
                Debug.LogWarning("Attempting to generate new sequence...");
                TryGenerateSequence();
            }
            else
            {
                sequenceLoaded = false;
                Debug.LogError("Set 'generateSequenceIfMissing' to true if you want automatic generation.");
            }
        }
    }

    void TryGenerateSequence()
    {
        try
        {
            Debug.Log("Generating new trial sequence...");
            GenerateAndSaveSequence();

            string filePath = Path.Combine(Application.streamingAssetsPath, trialSequenceFileName);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                currentSequence = JsonUtility.FromJson<ExperimentSequence>(json);
                sequenceLoaded = true;
                currentTrialIndex = 0;
                Debug.Log($"✓ Successfully generated and loaded new trial sequence: {currentSequence.trials.Count} trials");
            }
            else
            {
                throw new System.Exception("Generated file was not found after creation!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Failed to generate trial sequence: {e.Message}");
            sequenceLoaded = false;
        }
    }

    void GenerateAndSaveSequence()
    {
        TrialSequenceGenerator generator = GetComponent<TrialSequenceGenerator>();
        if (generator == null)
        {
            generator = gameObject.AddComponent<TrialSequenceGenerator>();
        }

        ExperimentSequence sequence = generator.GenerateTrialSequence();
        generator.SaveSequenceToFile(sequence);
    }

    public int GetCurrentStopSignalDelay()
    {
        return globalStopSignalDelay;
    }

    public void SetCurrentStopSignalDelay(int deltaDelay)
    {
        globalStopSignalDelay += deltaDelay;
    }

    void OnResponseRegistered(GameObject fruit, object responseData)
    {
        Mover mover = fruit.GetComponent<Mover>();
        if (mover != null)
        {
            bool wasStopTrial = mover.WasStopTrial();
            bool wasCorrectResponse = mover.WasCorrectResponse();

            if (wasStopTrial)
            {
                if (wasCorrectResponse)
                {
                    // Correct stop - increase SSD to make it harder
                    globalStopSignalDelay += ssdStep;
                    Debug.Log($"Correct stop response - SSD increased to {globalStopSignalDelay}ms");
                }
                else
                {
                    // Failed to stop - decrease SSD to make it easier
                    globalStopSignalDelay = Mathf.Max(0, globalStopSignalDelay - ssdStep);
                    Debug.Log($"Failed to stop - SSD decreased to {globalStopSignalDelay}ms");
                }
            }
        }
    }

    IEnumerator InitialTrialDelay()
    {
        yield return new WaitForSeconds(0.1f);
        Debug.Log("=== EXPERIMENT STARTING ===");
        Debug.Log("First trial will begin shortly...");
        StartNextTrial();
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
        if (busy)
        {
            busyFruits.Add(fruit);
            trialInitiated = false;
            //Debug.Log($"{fruit.name} is now busy (in movement cycle). Total busy: {busyFruits.Count}");
        }
        else
        {
            if (busyFruits.Contains(fruit))
            {
                busyFruits.Remove(fruit);

                Debug.Log($"{fruit.name} is no longer busy (cycle complete). Total busy: {busyFruits.Count}");
                StartCoroutine(CheckTrialComplete());

                //Debug.Log($"{fruit.name} is no longer busy (cycle complete). Total busy: {busyFruits.Count}");

                // Wait a frame before checking if all fruits are done
                StartCoroutine(CheckAllFruitsComplete());

            }
        }
    }

    void OnValidResponseWindowChanged(GameObject fruit, object inWindowObj)
    {
        bool inWindow = (bool)inWindowObj;
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
        animationRunning = (bool)animationObj;
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
                StartCoroutine(CheckTrialComplete());
            }
        }
    }

    void OnTrialInitiated(GameObject sender, object data)
    {
        int trialIndex = (int)data;
        Debug.Log($"Trial {trialIndex} initiation started.");
    }

    IEnumerator CheckTrialComplete()
    {
        yield return null; // Wait a frame



        if (busyFruits.Count == 0 && animatingFruits.Count == 0 && !feedbackMessageShowing && !animationRunning)

        {

            Debug.Log($"Trial {currentTrialIndex} complete.");

            yield return new WaitForSeconds(0.5f); // Brief pause between trials

            StartNextTrial();

        }

        else

        {

            Debug.Log($"Waiting for trial completion - Busy fruits: {busyFruits.Count}, Animating fruits: {animatingFruits.Count}");

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
        yield return null; // Wait a frame

        if (busyFruits.Count == 0 && animatingFruits.Count == 0 && !feedbackMessageShowing)
        {
            Debug.Log("No fruit is moving or being animated.");
            yield return new WaitForSeconds(0.5f); // Brief pause between trials
            StartNextTrial();
        }
        
        else
        {
            Debug.Log($"Waiting for completion - Busy fruits: {busyFruits.Count}, Animating fruits: {animatingFruits.Count}, Feedback showing: {feedbackMessageShowing}");
        }
    }

    public void StartNextTrial()
    {
        if (!sequenceLoaded)
        {
            Debug.LogError("Cannot start trial: No valid trial sequence loaded!");
            return;
        }

        if (trialInitiated)
        {
            Debug.LogWarning("Trial start already in progress, ignoring duplciate call.");
            return;
        }

        if (currentTrialIndex < currentSequence.trials.Count && busyFruits.Count == 0 && !feedbackMessageShowing && !animationRunning)
        {
            trialInitiated = true;
            EventManager.TriggerEvent(EventManager.EventType.TrialInitiated, gameObject, currentTrialIndex);

            TrialData trial = currentSequence.trials[currentTrialIndex];
            Debug.Log($"Starting Trial {trial.trialNumber} ({currentTrialIndex + 1}/{currentSequence.trials.Count}): {trial.trialType} on {trial.targetSide}");

            StartCoroutine(ExecuteTrial(trial));
            currentTrialIndex++;
        }
        else if (currentTrialIndex >= currentSequence.trials.Count)
        {
            Debug.Log("All trials completed!");
            OnExperimentComplete();
        }


        Debug.Log($"Feedback message showing: {feedbackMessageShowing}");
        Debug.Log("-------------------------");

    }

    IEnumerator ExecuteTrial(TrialData trial)
    {
        // Generate random wait time
        float randomWaitTime = Random.Range(minWaitTime, maxWaitTime);
        Debug.Log($"Trial {trial.trialNumber}: Waiting {randomWaitTime:F2}s before starting {trial.trialType} on {trial.targetSide}");
        yield return new WaitForSeconds(randomWaitTime);

        // Select the fruit based on trial data
        GameObject selectedFruit = (trial.targetSide == "left") ? leftFruit : rightFruit;

        if (selectedFruit != null && busyFruits.Count == 0)
        {
            Mover mover = selectedFruit.GetComponent<Mover>();
            if (mover != null)
            {
                // Set whether this should be a stop trial
                bool isStopTrial = (trial.trialType == "stop_left" || trial.trialType == "stop_right");
                mover.SetTrialType(isStopTrial);

                Debug.Log($"Starting {trial.trialType} trial on {selectedFruit.name}");
                mover.StartMovement();
            }
        }
    }

    void OnExperimentComplete()

    {

        Debug.Log("=== EXPERIMENT COMPLETED ===");

        Debug.Log($"Total trials completed: {currentTrialIndex}");

        Debug.Log($"Final Stop Signal Delay: {globalStopSignalDelay}ms");

        EventManager.TriggerEvent(EventManager.EventType.GameStateChanged, gameObject, "experiment_complete");

    }


    // Public methods for debugging/testing
    [ContextMenu("Skip to Next Trial")]
    public void SkipToNextTrial()
    {
        // Stop all current activities and move to next trial
        StopAllFruits();
        StartCoroutine(CheckTrialComplete());
    }

    [ContextMenu("Restart Experiment")]
    public void RestartExperiment()
    {
        StopAllFruits();
        currentTrialIndex = 0;
        globalStopSignalDelay = 250;

        // Show the start screen again if we have an experiment starter
        if (experimentStarter != null)
        {
            experimentStarter.ShowStartScreen();
        }
        else
        {
            StartCoroutine(InitialTrialDelay());
        }
    }

    void StopAllFruits()
    {
        if (leftFruit != null)
        {
            Mover leftMover = leftFruit.GetComponent<Mover>();
            if (leftMover != null) leftMover.StopMovement();
        }

        if (rightFruit != null)
        {
            Mover rightMover = rightFruit.GetComponent<Mover>();
            if (rightMover != null) rightMover.StopMovement();
        }

        busyFruits.Clear();
        animatingFruits.Clear();
        responseFruits.Clear();
    }

    // Getter methods for external access
    public TrialData GetCurrentTrial()
    {
        if (sequenceLoaded && currentTrialIndex > 0 && currentTrialIndex <= currentSequence.trials.Count)
        {
            return currentSequence.trials[currentTrialIndex - 1];
        }
        return null;
    }

    public int GetCurrentTrialNumber()
    {
        return currentTrialIndex;
    }

    public int GetTotalTrials()
    {
        return sequenceLoaded ? currentSequence.trials.Count : 0;
    }
}

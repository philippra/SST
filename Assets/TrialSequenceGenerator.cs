using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TrialData
{
    public int trialNumber;
    public string trialType; // "go", "stop_left", "stop_right"
    public string targetSide; // "left", "right"

    public TrialData(int number, string type, string side)
    {
        trialNumber = number;
        trialType = type;
        targetSide = side;
    }
}

[System.Serializable]
public class ExperimentSequence
{
    public List<TrialData> trials;
    public int totalTrials;
    public float stopTrialPercentage;
    public string experimentName;
    public string createdDate;

    public ExperimentSequence()
    {
        trials = new List<TrialData>();
    }
}

public class TrialSequenceGenerator : MonoBehaviour
{
    [Header("Sequence Generation Settings")]
    public int totalTrials = 32;
    [Range(0f, 1f)]
    public float stopTrialPercentage = 0.375f; // 37.5%
    public string experimentName = "StopSignalTask";

    [Header("File Settings")]
    public string fileName = "trial_sequence.json";

    public ExperimentSequence GenerateTrialSequence()
    {
        ExperimentSequence sequence = new ExperimentSequence();
        sequence.totalTrials = totalTrials;
        sequence.stopTrialPercentage = stopTrialPercentage;
        sequence.experimentName = experimentName;
        sequence.createdDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        int stopTrials = Mathf.RoundToInt(totalTrials * stopTrialPercentage);
        int goTrials = totalTrials - stopTrials;

        int leftStopTrials = stopTrials / 2;
        int rightStopTrials = stopTrials - leftStopTrials;

        Debug.Log($"Generating sequence: {goTrials} go trials, {leftStopTrials} left stop trials, {rightStopTrials} right stop trials");

        List<TrialData> trialList = new List<TrialData>();

        for (int i = 0; i < goTrials; i++)
        {
            string side = (i % 2 == 0) ? "left" : "right";
            trialList.Add(new TrialData(i + 1, "go", side));
        }

        for (int i = 0; i < leftStopTrials; i++)
        {
            trialList.Add(new TrialData(goTrials + i + 1, "stop_left", "left"));
        }

        for (int i = 0; i < rightStopTrials; i++)
        {
            trialList.Add(new TrialData(goTrials + leftStopTrials + i + 1, "stop_right", "right"));
        }

        for (int i = 0; i < trialList.Count; i++)
        {
            TrialData temp = trialList[i];
            int randomIndex = Random.Range(i, trialList.Count);
            trialList[i] = trialList[randomIndex];
            trialList[randomIndex] = temp;

            trialList[i].trialNumber = i + 1;
        }

        sequence.trials = trialList;
        return sequence;
    }

    public void SaveSequenceToFile(ExperimentSequence sequence)
    {
        string json = JsonUtility.ToJson(sequence, true);
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);

        if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
        {
            System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);
        }

        System.IO.File.WriteAllText(filePath, json);
        Debug.Log($"Trial sequence saved to: {filePath}");
    }

    [ContextMenu("Analyze Existing Trial Sequence")]
    public void AnalyzeExistingSequence()
    {
        string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"No trial sequence file found at: {filePath}");
            return;
        }

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            ExperimentSequence sequence = JsonUtility.FromJson<ExperimentSequence>(json);

            Debug.Log("=== ANALYZING EXISTING SEQUENCE ===");
            Debug.Log($"File: {fileName}");
            Debug.Log($"Experiment: {sequence.experimentName}");
            Debug.Log($"Created: {sequence.createdDate}");
            Debug.Log($"Total trials: {sequence.trials.Count}");

            int goCount = 0, leftStopCount = 0, rightStopCount = 0;
            int leftGoCount = 0, rightGoCount = 0;

            foreach (var trial in sequence.trials)
            {
                if (trial.trialType == "go")
                {
                    goCount++;
                    if (trial.targetSide == "left") leftGoCount++;
                    else rightGoCount++;
                }
                else if (trial.trialType == "stop_left") leftStopCount++;
                else if (trial.trialType == "stop_right") rightStopCount++;
            }

            Debug.Log($"\n=== BREAKDOWN ===");
            Debug.Log($"Go trials: {goCount} (Left: {leftGoCount}, Right: {rightGoCount})");
            Debug.Log($"Stop trials: {leftStopCount + rightStopCount} (Left: {leftStopCount}, Right: {rightStopCount})");
            Debug.Log($"TOTAL Left side: {leftGoCount + leftStopCount}");
            Debug.Log($"TOTAL Right side: {rightGoCount + rightStopCount}");

            Debug.Log($"\n=== ALL LEFT STOP TRIALS ===");
            for (int i = 0; i < sequence.trials.Count; i++)
            {
                var trial = sequence.trials[i];
                if (trial.trialType == "stop_left")
                {
                    Debug.Log($"Trial {trial.trialNumber} (index {i}): {trial.trialType} on {trial.targetSide}");
                }
            }

            Debug.Log($"\n=== ALL RIGHT STOP TRIALS ===");
            for (int i = 0; i < sequence.trials.Count; i++)
            {
                var trial = sequence.trials[i];
                if (trial.trialType == "stop_right")
                {
                    Debug.Log($"Trial {trial.trialNumber} (index {i}): {trial.trialType} on {trial.targetSide}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error analyzing sequence: {e.Message}");
        }
    }

    [ContextMenu("Generate and Save Trial Sequence")]
    public void GenerateAndSaveSequence()
    {
        ExperimentSequence sequence = GenerateTrialSequence();
        SaveSequenceToFile(sequence);

        Debug.Log("=== DETAILED TRIAL ANALYSIS ===");
        Debug.Log($"Generated {sequence.trials.Count} trials:");

        int goCount = 0, leftStopCount = 0, rightStopCount = 0;
        int leftGoCount = 0, rightGoCount = 0;

        foreach (var trial in sequence.trials)
        {
            if (trial.trialType == "go")
            {
                goCount++;
                if (trial.targetSide == "left") leftGoCount++;
                else rightGoCount++;
            }
            else if (trial.trialType == "stop_left") leftStopCount++;
            else if (trial.trialType == "stop_right") rightStopCount++;
        }

        Debug.Log($"Go trials: {goCount} (Left: {leftGoCount}, Right: {rightGoCount})");
        Debug.Log($"Stop trials: {leftStopCount + rightStopCount} (Left: {leftStopCount}, Right: {rightStopCount})");
        Debug.Log($"TOTAL Left: {leftGoCount + leftStopCount}, TOTAL Right: {rightGoCount + rightStopCount}");


        Debug.Log("\n=== FIRST 10 TRIALS ===");
        for (int i = 0; i < Mathf.Min(10, sequence.trials.Count); i++)
        {
            var trial = sequence.trials[i];
            Debug.Log($"Trial {trial.trialNumber}: {trial.trialType} on {trial.targetSide}");
        }

        int expectedStopTrials = Mathf.RoundToInt(totalTrials * stopTrialPercentage);
        int expectedGoTrials = totalTrials - expectedStopTrials;
        int expectedLeftStops = expectedStopTrials / 2;
        int expectedRightStops = expectedStopTrials - expectedLeftStops;

        Debug.Log("\n=== VERIFICATION ===");
        Debug.Log($"Expected: {expectedGoTrials} go, {expectedLeftStops} left stop, {expectedRightStops} right stop");
        Debug.Log($"Actual:   {goCount} go, {leftStopCount} left stop, {rightStopCount} right stop");

        bool correct = (goCount == expectedGoTrials &&
                       leftStopCount == expectedLeftStops &&
                       rightStopCount == expectedRightStops);
        Debug.Log($"GENERATION CORRECT: {(correct ? "YES" : "NO")}");
    }
}

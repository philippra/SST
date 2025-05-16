using UnityEngine;
using System.Collections;

public class Mover : MonoBehaviour
{
    private float moveSpeed = 4f;
    [Header("Timing Settings")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;
    public float bottomWaitTime = 1f;
    [Header("Auto Start")]
    public bool startOnAwake = false; // Add option to control auto-start

    private Camera mainCamera;
    private Vector2 topPosition;
    private Vector2 bottomPosition;
    public bool isMoving = false;
    private Coroutine movementCoroutine; // Track the movement coroutine

    [Header("Other fruit")]
    public GameObject otherFruit;

    private float startTimeFall = 0f;
    void Start()
    {
        mainCamera = Camera.main;
        SetupPositions();

        // Optional auto-start
        if (startOnAwake)
        {
            StartMovement();
        }
    }

    void SetupPositions()
    {
        Vector2 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        topPosition = new Vector2(transform.position.x, screenBounds.y - 2f);
        bottomPosition = new Vector2(transform.position.x, -screenBounds.y + 2f);
        transform.position = topPosition;
    }

    // Convert MovementCycle to a coroutine
    IEnumerator MovementCycle()
    {
        while (!otherFruit.GetComponent<Mover>().isMoving) // Loop indefinitely
        {
            // Wait at top for random time
            float randomWait = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(randomWait);
            Debug.Log("Random wait over.");

            // Move down to bottom
            yield return StartCoroutine(MoveToPosition(bottomPosition));
            Debug.Log("Falling phase over.");

            // Wait at bottom
            yield return new WaitForSeconds(bottomWaitTime);
            Debug.Log("Bottom wait over.");

            // Reset to top position
            ResetToTop();
            Debug.Log("Reset to top position.");
        }
    }

    IEnumerator MoveToPosition(Vector2 targetPosition)
    {
        Debug.Log("MoveToPosition started.");
        isMoving = true;
        startTimeFall = Time.time;
        while (Vector2.Distance(transform.position, targetPosition) > 0.01f)
        {
            //Debug.Log(Time.time - startTimeFall);
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (this.name == "leftFruit")
                transform.Rotate(0f, 0f, 15f * Time.deltaTime);
            else
                transform.Rotate(0f, 0f, -15f * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
        Debug.Log("MoveToPosition completed.");
    }

    public void StartMovement()
    {
        if (movementCoroutine == null && !otherFruit.GetComponent<Mover>().isMoving) // Only start if not already running
        {
            isMoving = true;
            Debug.Log("Starting Movement.");
            movementCoroutine = StartCoroutine(MovementCycle());
        }
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        isMoving = false;
        Debug.Log("Movement stopped.");
    }

    // Optional: Method to reset to top position
    public void ResetToTop()
    {
        StopMovement();
        transform.position = topPosition;
        transform.eulerAngles = new Vector3(0f, 0f, 0f);
    }
}

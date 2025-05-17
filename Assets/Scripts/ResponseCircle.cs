using UnityEngine;

public class ResponseCircle : MonoBehaviour
{
    [Header("Detection Settings")]
    public string fruitTag = "Fruit"; // Still useful for visual identification

    [Header("Response Feedback")]
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;
    public Color normalColor = Color.white;
    public float colorFlashDuration = 0.2f;

    [Header("Fruit Reference")]
    public GameObject targetFruit; // Direct reference to the fruit this circle is responsible for

    private SpriteRenderer spriteRenderer;
    private Mover fruitMover;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("ResponseCircle is missing a SpriteRenderer component!");
        }
        else
        {
            spriteRenderer.color = normalColor;
        }

        // Get the Mover component directly from the targetFruit
        if (targetFruit != null)
        {
            fruitMover = targetFruit.GetComponent<Mover>();
            if (fruitMover == null)
            {
                Debug.LogError($"Target fruit {targetFruit.name} is missing a Mover component!");
            }
        }
        else
        {
            Debug.LogError("No target fruit assigned to ResponseCircle!");
        }
    }

    void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            // Check if the click is within this circle
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D collider = Physics2D.OverlapPoint(mousePosition);

            if (collider != null && collider.gameObject == gameObject)
            {
                HandleClick();
            }
        }
    }

    void HandleClick()
    {
        if (fruitMover != null)
        {
            if (fruitMover.IsInValidResponseWindow())
            {
                // Valid response within the time window
                Debug.Log($"Valid response for {targetFruit.name}");
                fruitMover.RegisterResponse();
                FlashColor(validColor);
            }
            else
            {
                // Response outside the valid window
                Debug.Log($"Invalid response timing for {targetFruit.name}");
                FlashColor(invalidColor);
            }
        }
        else
        {
            // No fruit assigned or Mover component missing
            Debug.Log("No fruit to respond to");
            FlashColor(invalidColor);
        }
    }

    void FlashColor(Color flashColor)
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashColorCoroutine(flashColor));
        }
    }

    System.Collections.IEnumerator FlashColorCoroutine(Color flashColor)
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(colorFlashDuration);
        spriteRenderer.color = normalColor;
    }
}
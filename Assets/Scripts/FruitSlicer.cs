using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FruitSlicer : MonoBehaviour
{
    [Header("Slice Settings")]
    public Sprite[] sliceSprites;

    [Header("Physics Settings")]
    public float explosionForce = 2f;
    public float torqueForce = 2f;
    public float lifetime = 1f;

    private SpriteRenderer mainRenderer;
    private Vector3 currentPosition;

    private bool _animationRunning = false;
    private int activeSliceCount = 0;

    public bool animationRunning
    {
        get => _animationRunning;
        private set
        {
            if (_animationRunning != value)
            {
                _animationRunning = value;
                EventManager.TriggerEvent(EventManager.EventType.SliceAnimationStateChanged, gameObject, value);
            }
        }
    }

    void Start()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
    }

    public void SliceFruit()
    {
        currentPosition = transform.position;

        Debug.Log($"SliceFruit called at position: {currentPosition}");

        animationRunning = true;
        activeSliceCount = 0;

        // Hide the original apple
        if (mainRenderer != null)
        {
            mainRenderer.enabled = false;
        }

        // Create slices at the current position
        for (int i = 0; i < sliceSprites.Length; i++)
        {
            Debug.Log("Creating slice " + sliceSprites[i]);
            if (sliceSprites[i] != null)
            {
                CreateSlice(sliceSprites[i], currentPosition);
                activeSliceCount++;
            }
        }
    }

    private void CreateSlice(Sprite sliceSprite, Vector3 position)
    {
        GameObject slice = new GameObject("AppleSlice");

        slice.transform.position = position;

        SpriteRenderer renderer = slice.AddComponent<SpriteRenderer>();
        renderer.sprite = sliceSprite;

        // Match the sorting layer and order
        if (mainRenderer != null)
        {
            renderer.sortingLayerID = mainRenderer.sortingLayerID;
            renderer.sortingOrder = mainRenderer.sortingOrder;
        }

        Rigidbody2D rb = slice.AddComponent<Rigidbody2D>();

        // Apply random force and torque
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        rb.AddForce(randomDirection * explosionForce, ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(-torqueForce, torqueForce), ForceMode2D.Impulse);

        rb.gravityScale = 1f;

        //Debug.Log($"Created slice at position {position} with sprite {sliceSprite.name}");

        StartCoroutine(FadeAndDestroy(slice, renderer));
    }

    private IEnumerator FadeAndDestroy(GameObject slice, SpriteRenderer renderer)
    {
        yield return new WaitForSeconds(lifetime * 0.7f);

        float fadeTime = lifetime * 0.3f;
        float startTime = Time.time;
        Color startColor = renderer.color;

        while (Time.time < startTime + fadeTime)
        {
            float t = (Time.time - startTime) / fadeTime;
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            renderer.color = newColor;
            yield return null;
        }

        //Debug.Log("Destroying slice " + slice);

        Destroy(slice);

        activeSliceCount--;
        if(activeSliceCount <= 0)
        {
            Debug.Log("All slices destroyed, animation complete");
            animationRunning = false;
        }
    }

    public void ResetFruit()
    {
        // Show the original apple again
        if (mainRenderer != null)
        {
            mainRenderer.enabled = true;
        }

        animationRunning = false;
        activeSliceCount = 0;
    }
}

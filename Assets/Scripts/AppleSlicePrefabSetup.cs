using UnityEngine;

public class AppleSlicePrefabSetup : MonoBehaviour
{
    [Header("Prefab Setup")]
    public GameObject[] appleSlicePrefabs;  // Array to hold the 5 slice prefabs

    [Header("Sprite References")]
    public Sprite[] appleSliceSprites;      // Array of 5 apple slice sprites

    public void SetupSlicePrefabs()
    {
        // Make sure we have both arrays properly initialized
        if (appleSlicePrefabs == null || appleSlicePrefabs.Length != 5)
        {
            Debug.LogError("Apple slice prefabs array must have exactly 5 elements!");
            return;
        }

        if (appleSliceSprites == null || appleSliceSprites.Length != 5)
        {
            Debug.LogError("Apple slice sprites array must have exactly 5 elements!");
            return;
        }

        // Assign sprites to each prefab
        for (int i = 0; i < 5; i++)
        {
            if (appleSlicePrefabs[i] != null)
            {
                SpriteRenderer renderer = appleSlicePrefabs[i].GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = appleSliceSprites[i];
                }
                else
                {
                    Debug.LogWarning($"Slice prefab {i} is missing a SpriteRenderer component!");
                }
            }
            else
            {
                Debug.LogWarning($"Slice prefab {i} is null!");
            }
        }
    }
}
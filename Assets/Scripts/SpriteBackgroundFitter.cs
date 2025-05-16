using UnityEngine;

public class SpriteBackgroundFitter : MonoBehaviour
{
    [Header("BackgroundSettings")]
    public SpriteRenderer backgroundSprite;
    public Camera targetCamera;

    [Header("Fitting Options")]
    public bool fitOnStart = true;
    public bool maintainAspectRatio = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (backgroundSprite == null)
            backgroundSprite = GetComponent<SpriteRenderer>();

        if (fitOnStart)
            FitBackgroundToCamera();

    }

    public void FitBackgroundToCamera(){
        if (backgroundSprite == null || targetCamera == null)
            return;

        // get camera bounds in world coordinates
        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;

        // get sprite bounds
        Vector2 spriteSize = backgroundSprite.sprite.bounds.size;

        if (maintainAspectRatio)
        {
            float scaleX = cameraWidth / spriteSize.x;
            float scaleY = cameraHeight / spriteSize.y;
            float scale = Mathf.Max(scaleX, scaleY);

            transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            // stretch to exact camera dimensions
            Vector3 scale = new Vector3(
                cameraWidth / spriteSize.x,
                cameraHeight / spriteSize.y,
                1f);
            transform.localScale = scale;
        }

        // center the background
        transform.position = new Vector3(
            targetCamera.transform.position.x, 
            targetCamera.transform.position.y, 
            targetCamera.transform.position.z);
    }
}

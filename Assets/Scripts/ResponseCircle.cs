using UnityEngine;
public class ResponseCircle : MonoBehaviour
{
    [Header("Detection Settings")]
    public string appleTag = "Fruit"; // Set this to the tag of your apple objects

    // This method is called when another collider enters this trigger collider
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that entered has the apple tag
        if (other.gameObject.CompareTag(appleTag))
        {
            Debug.Log("Apple contacted the response circle!");

            // You can also access the specific apple object if needed
            Debug.Log("Apple name: " + other.gameObject.name);
        }
    }

    // Optional: Detect when apple exits the circle
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag(appleTag))
        {
            Debug.Log("Apple left the response circle!");
        }
    }

    // Optional: Detect while apple is staying in the circle
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.CompareTag(appleTag))
        {
            //Debug.Log("Apple is staying in the response circle!");
        }
    }
}
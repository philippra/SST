using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Main : MonoBehaviour
{
    [Header("Left Fruit")]
    public GameObject leftFruit;
    [Header("Right Fruit")]
    public GameObject rightFruit;

    [Header("Fruit List")]
    public List<GameObject> fruits = new List<GameObject>();

    void Update()
    {
        if (!leftFruit.GetComponent<Mover>().isMoving && !rightFruit.GetComponent<Mover>().isMoving)
        {
            StartFruitMovement();
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log("Something Hit: " + hit.collider.name);
            }
        }
    }

    public GameObject SelectFruit()
    {
        int randomIndex = Random.Range(0, 2);
        GameObject selectedFruit = fruits[randomIndex];

        return selectedFruit;
    }

    public void StartFruitMovement()
    {
        //Debug.Log("StartFruitMovement called.");
        GameObject selectedFruit = SelectFruit();

        if (selectedFruit != null)
        {
            //Debug.Log("Initiating movement for " + selectedFruit.name);
            selectedFruit.GetComponent<Mover>().StartMovement();
        }
    }
}

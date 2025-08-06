using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    // Rigidbody of the player.
    private Rigidbody rb;

    // Movement along X and Y axes.
    private float movementX;
    private float movementY;

    // Count of collected items (pickups).
    private int count;

    // Total number of items to collect (for potential win condition).
    private int totalCount;

    // Speed at which the player moves.
    public float Speed = 0;

    // UI Text element to display the count of collected items.
    public TextMeshProUGUI CountText;

    // UI Text element to display win message (if needed).
    public GameObject WinTextObject;

    // Start is called before the first frame update.
    void Start()
    {
        // Get and store the Rigidbody component attached to the player.
        rb = GetComponent<Rigidbody>();

        // Safe state initialization.
        count = 0;

        // Retrieve the count of all objects tagged as "PickUp" in the scene.
        totalCount = GameObject.FindGameObjectsWithTag("PickUp").Length;
        Debug.Log($"Total PickUp objects in the scene: {totalCount}");

        // Update the count text UI element at the start
        SetCountText();

        // Ensure the win text is hidden at the start
        WinTextObject.SetActive(false);
    }

    // This function is called to set movement from a gesture input.
    public void SetMovementFromGesture(Vector2 movement)
    {
        movementX = movement.x;
        movementY = movement.y;
    }

    // This function is called when a move input is detected.
    private void OnMove(InputValue movementValue)
    {
        // Convert the input value into a Vector2 for movement.
        Vector2 movementVector = movementValue.Get<Vector2>();

        // Store the X and Y components of the movement.
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    // FixedUpdate is called once per fixed frame-rate frame.
    private void FixedUpdate()
    {
        // Create a 3D movement vector using the X and Y inputs.
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);

        // Apply force to the Rigidbody to move the player.
        rb.AddForce(movement * Speed);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object the player collided with has the "PickUp" tag
        if (other.gameObject.CompareTag("PickUp"))
        {
            // Deactivate the collided object (making it disappear)
            other.gameObject.SetActive(false);

            // Increment the count of collected items
            count++;

            // Update the count text UI element
            SetCountText();

            // If all items are collected, display the win message
            if (count == totalCount)
            {
                WinTextObject.SetActive(true);
            }
        }
    }

    // Update the count text UI element.
    private void SetCountText()
    {
        CountText.text = "Count: " + count.ToString();
    }
}

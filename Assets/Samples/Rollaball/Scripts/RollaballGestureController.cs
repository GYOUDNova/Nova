using UnityEngine;

public class RollaballGestureController : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerController PlayerController;

    [Header("Gesture Settings")]
    public bool EnableGestureControl = true;

    // Single method that handles all directions
    public void OnGestureDetected(string direction)
    {
        if (!EnableGestureControl || PlayerController == null) return;

        Vector2 movement = direction.ToLower() switch
        {
            "up" => Vector2.up,
            "down" => Vector2.down,
            "left" => Vector2.left,
            "right" => Vector2.right,
            "stop" => Vector2.zero,
            _ => Vector2.zero
        };

        PlayerController.SetMovementFromGesture(movement);
    }

    // For testing with keyboard
    // TODO: Remove when integrated with actual gesture input
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) OnGestureDetected("up");
        if (Input.GetKeyDown(KeyCode.K)) OnGestureDetected("down");
        if (Input.GetKeyDown(KeyCode.J)) OnGestureDetected("left");
        if (Input.GetKeyDown(KeyCode.L)) OnGestureDetected("right");
        if (Input.GetKeyDown(KeyCode.Space)) OnGestureDetected("stop");
    }
}

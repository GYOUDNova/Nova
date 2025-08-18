using UnityEngine;

namespace NOVA.Scripts
{
    public class PlayerInput : MonoBehaviour
    {
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 5f;

        Rigidbody rb;
        Renderer renderer;

        private bool isBlue = true;
        private bool isRed = true;
        private bool isWhite = true;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            renderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleColor_GreenBlue();
            }
        }

        public void Jump()
        {
            rb.AddForce(Vector3.up * 200);
            Debug.Log("Keyboard Input: Jumping!");
        }

        public void ToggleColor_GreenBlue()
        {
            if (isBlue)
            {
                renderer.material.color = Color.green;
            }
            else
            {
                renderer.material.color = Color.blue;
            }

            isBlue = !isBlue;
        }

        public void ToggleColor_YellowRed()
        {
            if (isRed) { renderer.material.color = Color.yellow; }
            else { renderer.material.color = Color.red; }

            isRed = !isRed;
        }

        public void ToggleColor_WhiteBlack()
        {
            if (isWhite) { renderer.material.color = Color.black; }
            else { renderer.material.color = Color.white; }

            isWhite = !isWhite;
        }

        public void MoveLeft()
        {
            // move the player left
            rb.AddForce(Vector3.left * 5f, ForceMode.VelocityChange);
        }
        public void MoveRight()
        {
            // move the player right
            rb.AddForce(Vector3.right * 5f, ForceMode.VelocityChange);
        }
    }
}

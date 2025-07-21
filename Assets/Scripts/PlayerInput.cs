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
                ToggleColor();
            }
        }

        public void Jump()
        {
            rb.AddForce(Vector3.up * 200);
            Debug.Log("Keyboard Input: Jumping!");
        }

        public void ToggleColor()
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
    }
}

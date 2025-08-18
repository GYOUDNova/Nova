using UnityEngine;
using System.Collections;

namespace KartGame.KartSystems
{
    public class GestureInput : BaseInput
    {
        private string currentGesture;
        private bool left;
        private bool right;
        private float gestureTimeout = 1.0f; // Timeout in seconds
        private bool keepLooping;
        private Coroutine resetCoroutine; // Tracks the active reset coroutine

        private float turnValue; // Gradual turn value
        private float turnBuildSpeed = 0.5f; // Speed at which the turn value builds up

        private static object lockObj = new();

        public void SetGesture(string gesture)
        {
            currentGesture = gesture.ToLower();
        }

        public void SetDirectionGesture(string gesture)
        {
            if (gesture == "right")
            {
                right = true;
                left = false;
            }
            else if (gesture == "left")
            {
                right = false;
                left = true;
            }

            keepLooping = true;

            if (resetCoroutine == null) // Start watchdog only once
            {
                resetCoroutine = StartCoroutine(ResetDirectionGesturesAfterTimeout());
            }
        }

        private IEnumerator ResetDirectionGesturesAfterTimeout()
        {
            while (keepLooping)
            {
                keepLooping = false;
                yield return new WaitForSeconds(0.3f);
            }

            Debug.Log("Turn Reset");
            resetCoroutine = null;
            ResetDirectionGestures();
        }

        private void ResetDirectionGestures()
        {
            right = false;
            left = false;
            turnValue = 0f; // Reset the turn value
        }

        public override InputData GenerateInput()
        {
            bool accelerate;
            bool brake;

            if (currentGesture == null || currentGesture == "stop")
            {
                accelerate = currentGesture == "stop";
                brake = currentGesture == "stop";
            }
            else
            {
                accelerate = currentGesture == "up";
                brake = currentGesture == "down";
            }

            // Gradually build up the turn value if a direction is active
            if (right || left)
            {
                turnValue = Mathf.Clamp(turnValue + Time.deltaTime * turnBuildSpeed, 0.1f, 1f);
            }
            else
            {
                // Gradually reduce the turn value if no direction is active
                turnValue = Mathf.Clamp(turnValue - Time.deltaTime * turnBuildSpeed, 0f, 1f);
            }

            float turn = 0f;

            if (right) turn = turnValue;
            else if (left) turn = -turnValue;

            return new InputData
            {
                Accelerate = accelerate,
                Brake = brake,
                TurnInput = turn
            };
        }
    }
}

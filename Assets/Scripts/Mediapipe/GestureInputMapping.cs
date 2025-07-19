using System;
using UnityEngine;
using UnityEngine.UI;

namespace NOVA.Scripts
{
    public class GestureInputMapping : MonoBehaviour
    {
        private void OnEnable()
        {
            GestureEvent.OnGestureRecognized += HandleGesture;
        }

        private void OnDisable()
        {
            GestureEvent.OnGestureRecognized -= HandleGesture;
        }

        private void HandleGesture(string gesture)
        {

            if (gesture == GestureRecognizer.OPEN_PALM)
            {
                SimulateSpacebarPress();
            }
        }

        private void SimulateSpacebarPress()
        {
            Debug.Log("Gesture Input recognized as 'Space' - No Functionality");
        }
    }
}

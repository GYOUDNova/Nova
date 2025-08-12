using UnityEngine;

namespace KartGame.KartSystems
{
    public class GestureController : MonoBehaviour
    {
        public GestureInput gestureInputComponent;

        [Header("Gesture Settings")]
        public bool EnableGestureControl = true;

        public void OnGestureDetected(string direction)
        {
            if (!EnableGestureControl || gestureInputComponent == null) return;
            gestureInputComponent.SetGesture(direction);
        }

    }
}

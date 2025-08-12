using System;
using UnityEngine;

namespace NOVA.Scripts
{
    public class GestureEvent
    {
        public static event Action<string> OnGestureRecognized;

        public static void TriggerGesture(string gesture)
        {
            OnGestureRecognized?.Invoke(gesture);
        }
    }
}

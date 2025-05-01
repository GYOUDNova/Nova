using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NOVA.Scripts
{
    public class GestureInputController : MonoBehaviour
    {
        [SerializeField]
        public GestureDictionary GestureDictionary;

        public Dictionary<string, UnityEvent> GestureInputMapping;

        public void Start()
        {
            GestureInputMapping = GestureDictionary.ToDictionary();
        }

        // function to take string and activate the input action
        public void ActivateGestureInput(string gestureName)
        {
            if (GestureInputMapping.ContainsKey(gestureName))
            {
                GestureInputMapping[gestureName].Invoke();
                Debug.Log($"Activated gesture input: {gestureName}");
            }
            else
            {
                Debug.LogWarning($"Gesture input not found: {gestureName}");
            }
        }
    }

    [Serializable]
    public class GestureDictionary
    {
        [SerializeField]
        public List<GestureInput> GestureInputs;

        public Dictionary<string, UnityEvent> ToDictionary()
        {
            Dictionary<string, UnityEvent> gestureInputMapping = new Dictionary<string, UnityEvent>();

            foreach (var gestureInput in GestureInputs)
            {
                if (!gestureInputMapping.ContainsKey(gestureInput.GestureName))
                {
                    gestureInputMapping.Add(gestureInput.GestureName, gestureInput.GestureEvent);
                }
            }

            return gestureInputMapping;
        }
    }

    [Serializable]
    public class GestureInput
    {
        [SerializeField]
        public string GestureName;
        [SerializeField]
        public UnityEvent GestureEvent;
    }
}

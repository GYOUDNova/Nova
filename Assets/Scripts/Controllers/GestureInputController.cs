using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NOVA.Scripts
{
    public class GestureInputController : MonoBehaviour
    {
        [SerializeField]
        public GestureDictionary gestureDictionary;

        public Dictionary<string, UnityEvent> gestureInputMapping;

        public void Start()
        {
            gestureInputMapping = gestureDictionary.ToDictionary();
        }

        // function to take string and activate the input action
        public void ActivateGestureInput(string gestureName)
        {
            if (gestureInputMapping.ContainsKey(gestureName))
            {
                gestureInputMapping[gestureName].Invoke();
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
        public List<GestureInput> gestureInputs;

        public Dictionary<string, UnityEvent> ToDictionary()
        {
            Dictionary<string, UnityEvent> gestureInputMapping = new Dictionary<string, UnityEvent>();

            foreach (var gestureInput in gestureInputs)
            {
                if (!gestureInputMapping.ContainsKey(gestureInput.gestureName))
                {
                    gestureInputMapping.Add(gestureInput.gestureName, gestureInput.gestureEvent);
                }
            }

            return gestureInputMapping;
        }
    }

    [Serializable]
    public class GestureInput
    {
        [SerializeField]
        public string gestureName;
        [SerializeField]
        public UnityEvent gestureEvent;
    }
}

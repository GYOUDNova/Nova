using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NOVA.Scripts
{
    public class GestureInputController : MonoBehaviour
    {
        [SerializeField]
        public GestureDictionary gestureDictionary;

        [SerializeField]
        public GestureChainDictionary gestureChainDictionary;

        public Dictionary<string, UnityEvent> singleGestureInputMapping;

        public Dictionary<string, UnityEvent> gestureChainMapping;

        [NonSerialized]
        public List<string> currentGestureChain = new List<string>();

        int longestChainLength = 0;

        bool chainKeepWaiting = false;

        bool chainCoroutineRunning = false;

        public void Start()
        {
            singleGestureInputMapping = gestureDictionary.ToDictionary();

            gestureChainMapping = gestureChainDictionary.ToDictionary();

            longestChainLength = gestureChainDictionary.GetLongestChainLength();
        }

        public void ResetLongestChainLength()
        {
            longestChainLength = gestureChainDictionary.GetLongestChainLength();
        }

        // function to take string and activate the input action
        public void ActivateGestureInput(string gestureName)
        {
            if (singleGestureInputMapping.ContainsKey(gestureName))
            {
                singleGestureInputMapping[gestureName].Invoke();
                Debug.Log($"Activated gesture input: {gestureName}");
            }
            else
            {
                Debug.LogWarning($"Gesture input not found: {gestureName}");
            }
        }

        public void ActivateGestureChainInput()
        {
            // print the current gesture chain
            Debug.Log($"Current gesture chain: {string.Join("", currentGestureChain)}");

            // combine the current gesture chain into a string
            string currentGestureChainKey = string.Join("", currentGestureChain);

            // check if the current gesture chain is in the dictionary
            if (gestureChainMapping.ContainsKey(currentGestureChainKey))
            {
                gestureChainMapping[currentGestureChainKey].Invoke();
                Debug.Log($"Activated gesture chain input: {string.Join(", ", currentGestureChain)}");
            }
            // if the gesture chain is not in the dictionary run activateGestureInput with the last gesture in the chain
            else
            {
                string lastGesture = currentGestureChain[currentGestureChain.Count - 1];
                ActivateGestureInput(lastGesture);
                Debug.Log($"Activated single gesture");
            }
        }


        public IEnumerator ChainCoroutine()
        {
            chainKeepWaiting = true;
            while (chainKeepWaiting)
            {
                chainKeepWaiting = false;
                yield return new WaitForSeconds(1f);
                // check if the chain length is greater than the longest chain length then stop waiting
                if (currentGestureChain.Count > longestChainLength)
                {
                    break;
                }

            }
            ActivateGestureChainInput();
            currentGestureChain.Clear();
            chainCoroutineRunning = false;

        }

        public void AddGestureToChain(string gestureInput)
        {
            if (currentGestureChain == null)
            {
                currentGestureChain = new List<string>();
            }

            currentGestureChain.Add(gestureInput);
            // check if chain coroutine is running
            if (chainCoroutineRunning)
            {
                chainKeepWaiting = true;
            }
            else
            {
                chainCoroutineRunning = true;
                StartCoroutine(ChainCoroutine());
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
    public class GestureChainDictionary
    {
        [SerializeField]
        public List<GestureChainInput> gestureChainInputs;

        public Dictionary<string, UnityEvent> ToDictionary()
        {
            Dictionary<string, UnityEvent> gestureInputMapping = new Dictionary<string, UnityEvent>();

            foreach (var gestureInput in gestureChainInputs)
            {
                // create a string from the list of gesture chain names
                string gestureChainKey = string.Join("", gestureInput.gestureChainNames);

                if (!gestureInputMapping.ContainsKey(gestureChainKey))
                {
                    gestureInputMapping.Add(gestureChainKey, gestureInput.gestureEvent);
                }
            }

            return gestureInputMapping;
        }



        // function to return longest gesture chain length
        public int GetLongestChainLength()
        {
            int longestChainLength = 0;

            foreach (var gestureInput in gestureChainInputs)
            {
                if (gestureInput.gestureChainNames.Count > longestChainLength)
                {
                    longestChainLength = gestureInput.gestureChainNames.Count;
                }
            }

            return longestChainLength;
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

    [Serializable]
    public class GestureChainInput
    {
        [SerializeField]
        public List<string> gestureChainNames;
        [SerializeField]
        public UnityEvent gestureEvent;
    }
}

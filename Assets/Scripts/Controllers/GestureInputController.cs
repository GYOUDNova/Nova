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
        public GestureDictionary GestureDictionary;

        [SerializeField]
        public GestureChainDictionary GestureChainDictionary;

        public Dictionary<string, UnityEvent> SingleGestureInputMapping;

        public Dictionary<string, UnityEvent> GestureChainMapping;

        [NonSerialized]
        public List<string> CurrentGestureChain = new List<string>();

        int longestChainLength = 0;

        bool chainKeepWaiting = false;

        bool chainLockoutRunning = false;

        bool chainCoroutineRunning = false;

        void Awake()
        {
            UnityMainThreadDispatcher.Initialize();
        }

        public void Start()
        {
            SingleGestureInputMapping = GestureDictionary.ToDictionary();

            GestureChainMapping = GestureChainDictionary.ToDictionary();

            longestChainLength = GestureChainDictionary.GetLongestChainLength();
        }

        public void ResetLongestChainLength()
        {
            longestChainLength = GestureChainDictionary.GetLongestChainLength();
        }

        // function to take string and activate the input action
        public void ActivateGestureInput(string gestureName)
        {
            if (SingleGestureInputMapping.ContainsKey(gestureName))
            {
                SingleGestureInputMapping[gestureName].Invoke();
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
            Debug.Log($"Current gesture chain: {string.Join("", CurrentGestureChain)}");

            // combine the current gesture chain into a string
            string currentGestureChainKey = string.Join("", CurrentGestureChain);

            // check if the current gesture chain is in the dictionary
            if (GestureChainMapping.ContainsKey(currentGestureChainKey))
            {
                GestureChainMapping[currentGestureChainKey].Invoke();
                Debug.Log($"Activated gesture chain input: {string.Join(", ", CurrentGestureChain)}");
            }
            // if the gesture chain is not in the dictionary run activateGestureInput with the last gesture in the chain
            else
            {
                if (CurrentGestureChain.Count == 0)
                {
                    //Debug.LogWarning("No gestures in the chain to activate.");
                    return;
                }
                string lastGesture = CurrentGestureChain[CurrentGestureChain.Count - 1];
                ActivateGestureInput(lastGesture);
                Debug.Log($"Activated single gesture");
            }
        }

        public bool CheckGestureChainInput()
        {
            // combine the current gesture chain into a string
            string currentGestureChainKey = string.Join("", CurrentGestureChain);

            // check if the current gesture chain is in the dictionary
            foreach (string gesture in CurrentGestureChain)
            {
                // check if any gesture contains a substring of the last gesture in the chain
                if (gesture.Contains(CurrentGestureChain[CurrentGestureChain.Count - 1]))
                {
                    return true;
                }
            }
            return false;
        }


        public IEnumerator ChainCoroutine()
        {
            chainKeepWaiting = true;
            while (chainKeepWaiting)
            {
                chainKeepWaiting = false;
                // check if the chain length is greater than the longest chain length then stop waiting
                if (CurrentGestureChain.Count >= longestChainLength || !CheckGestureChainInput())
                {
                    break;
                }
                yield return new WaitForSeconds(3f);
            }
            ActivateGestureChainInput();
            CurrentGestureChain.Clear();
            chainCoroutineRunning = false;

        }

        // function that prevents adding a gesture to the chain if the chain is already running
        // and adds the gesture to the chain if it is not running
        public IEnumerator holdChain(string gestureInput, float delay)
        {
            chainLockoutRunning = true;

            // add the gesture to the chain
            CurrentGestureChain.Add(gestureInput);
            // log the gesture input
            Debug.Log($"Added gesture to chain: {gestureInput}");
            yield return new WaitForSeconds(delay);
            chainLockoutRunning = false; // reset the lockout running state
        }

        public void AddGestureToChain(string gestureInput)
        {
            if (CurrentGestureChain == null)
            {
                CurrentGestureChain = new List<string>();
            }
            if (CurrentGestureChain.Count < longestChainLength)
            {
                //Debug.Log(gestureInput);
                if (!chainLockoutRunning)
                {
                    StartCoroutine(holdChain(gestureInput, 2f));
                }
            }
            else if (longestChainLength < 1)
            {
                if (!chainLockoutRunning)
                {
                    StartCoroutine(holdChain(gestureInput, 1f));
                }
            }
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
    public class GestureChainDictionary
    {
        [SerializeField]
        public List<GestureChainInput> GestureChainInputs;

        public Dictionary<string, UnityEvent> ToDictionary()
        {
            Dictionary<string, UnityEvent> gestureInputMapping = new Dictionary<string, UnityEvent>();

            foreach (var gestureInput in GestureChainInputs)
            {
                // create a string from the list of gesture chain names
                string gestureChainKey = string.Join("", gestureInput.GestureChainNames);

                if (!gestureInputMapping.ContainsKey(gestureChainKey))
                {
                    gestureInputMapping.Add(gestureChainKey, gestureInput.GestureEvent);
                }
            }

            return gestureInputMapping;
        }



        // function to return longest gesture chain length
        public int GetLongestChainLength()
        {
            int longestChainLength = 0;

            foreach (var gestureInput in GestureChainInputs)
            {
                if (gestureInput.GestureChainNames.Count > longestChainLength)
                {
                    longestChainLength = gestureInput.GestureChainNames.Count;
                }
            }

            return longestChainLength;
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

    [Serializable]
    public class GestureChainInput
    {
        [SerializeField]
        public List<string> GestureChainNames;
        [SerializeField]
        public UnityEvent GestureEvent;
    }
}

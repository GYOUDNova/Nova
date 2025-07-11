//using UnityEngine;
//using UnityEngine.UI;

//public class GestureManager : MonoBehaviour
//{
//    [Header("Hand References")]
//    public Hand leftHand;
//    public Hand rightHand;

//    [Header("UI References")]
//    [SerializeField] public Text gestureDisplayText; // Assign a UI Text element in the Inspector

//    private void Update()
//    {
//        // Check gestures from both hands in one statement
//        string gestureInfo = GetCurrentGestureInfo();

//        // Update UI if we have a Text component assigned
//        if (gestureDisplayText != null)
//        {
//            gestureDisplayText.text = gestureInfo;
//        }
//    }

//    private string GetCurrentGestureInfo()
//    {
//        string leftGesture = leftHand.GetGesture();
//        string rightGesture = rightHand.GetGesture();

//        // If neither hand has a gesture, return empty
//        if (leftGesture == Hand.NO_GESTURE && rightGesture == Hand.NO_GESTURE)
//            return "No gestures detected";

//        // Format the output based on which hands have gestures
//        string output = "";

//        if (leftGesture != Hand.NO_GESTURE)
//            output += $"Left: {leftGesture}";

//        if (rightGesture != Hand.NO_GESTURE)
//        {
//            if (!string.IsNullOrEmpty(output))
//                output += "\n"; // Add newline if we already have left hand info
//            output += $"Right: {rightGesture}";
//        }

//        return output;
//    }
//}

using System.Collections.Generic;
using UnityEngine;
using Mediapipe;

public static class GestureRecognizer
{
    // Landmark indices based on MediaPipe Hand Landmark Model
    private const int WRIST = 0;
    private const int THUMB_CMC = 1;
    private const int THUMB_MCP = 2;
    private const int THUMB_IP = 3;
    private const int THUMB_TIP = 4;
    private const int INDEX_FINGER_MCP = 5;
    private const int INDEX_FINGER_PIP = 6;
    private const int INDEX_FINGER_DIP = 7;
    private const int INDEX_FINGER_TIP = 8;
    private const int MIDDLE_FINGER_MCP = 9;
    private const int MIDDLE_FINGER_PIP = 10;
    private const int MIDDLE_FINGER_DIP = 11;
    private const int MIDDLE_FINGER_TIP = 12;
    private const int RING_FINGER_MCP = 13;
    private const int RING_FINGER_PIP = 14;
    private const int RING_FINGER_DIP = 15;
    private const int RING_FINGER_TIP = 16;
    private const int PINKY_MCP = 17;
    private const int PINKY_PIP = 18;
    private const int PINKY_DIP = 19;
    private const int PINKY_TIP = 20;

    // Predefined gesture names
    public const string NO_GESTURE = "None";
    public const string CLOSED_FIST = "Closed Fist";
    public const string OPEN_PALM = "Open Palm";
    public const string THUMBS_UP = "Thumbs Up";
    public const string POINTING = "Pointing";

    // Thresholds for gesture recognition (tune these as needed)
    private static readonly float[] FingerClosedThresholds = new float[5] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
    private const float OpenPalmThreshold = 1.2f;
    private const float ThumbsUpThreshold = 0.8f;

    /// <summary>
    /// Detects the gesture based on the given hand landmarks.
    /// </summary>
    /// <param name="landmarks">NormalizedLandmarkList from MediaPipe</param>
    /// <returns>The name of the detected gesture.</returns>
    public static string DetectGesture(NormalizedLandmarkList landmarks)
    {
        if (landmarks == null || landmarks.Landmark == null || landmarks.Landmark.Count < 21)
        {
            return NO_GESTURE;
        }

        if (CheckClosedFistGesture(landmarks))
        {
            return CLOSED_FIST;
        }
        else if (CheckOpenPalmGesture(landmarks))
        {
            return OPEN_PALM;
        }
        else if (CheckThumbsUpGesture(landmarks))
        {
            return THUMBS_UP;
        }
        else if (CheckPointingGesture(landmarks))
        {
            return POINTING;
        }

        return NO_GESTURE;
    }

    private static bool CheckClosedFistGesture(NormalizedLandmarkList landmarks)
    {
        float thumbToPinkyDistance = GetDistance(landmarks, THUMB_MCP, PINKY_MCP);

        float thumbCurl = GetDistance(landmarks, THUMB_TIP, PINKY_DIP) / thumbToPinkyDistance;
        float indexCurl = GetDistance(landmarks, INDEX_FINGER_TIP, INDEX_FINGER_MCP) / thumbToPinkyDistance;
        float middleCurl = GetDistance(landmarks, MIDDLE_FINGER_TIP, MIDDLE_FINGER_MCP) / thumbToPinkyDistance;
        float ringCurl = GetDistance(landmarks, RING_FINGER_TIP, RING_FINGER_MCP) / thumbToPinkyDistance;
        float pinkyCurl = GetDistance(landmarks, PINKY_TIP, PINKY_MCP) / thumbToPinkyDistance;

        return thumbCurl < FingerClosedThresholds[0] &&
               indexCurl < FingerClosedThresholds[1] &&
               middleCurl < FingerClosedThresholds[2] &&
               ringCurl < FingerClosedThresholds[3] &&
               pinkyCurl < FingerClosedThresholds[4];
    }

    private static bool CheckOpenPalmGesture(NormalizedLandmarkList landmarks)
    {
        float thumbToPinkyDistance = GetDistance(landmarks, THUMB_MCP, PINKY_MCP);

        float thumbExtension = GetDistance(landmarks, THUMB_TIP, WRIST) / thumbToPinkyDistance;
        float indexExtension = GetDistance(landmarks, INDEX_FINGER_TIP, WRIST) / thumbToPinkyDistance;
        float middleExtension = GetDistance(landmarks, MIDDLE_FINGER_TIP, WRIST) / thumbToPinkyDistance;
        float ringExtension = GetDistance(landmarks, RING_FINGER_TIP, WRIST) / thumbToPinkyDistance;
        float pinkyExtension = GetDistance(landmarks, PINKY_TIP, WRIST) / thumbToPinkyDistance;

        return thumbExtension > OpenPalmThreshold &&
               indexExtension > OpenPalmThreshold &&
               middleExtension > OpenPalmThreshold &&
               ringExtension > OpenPalmThreshold &&
               pinkyExtension > OpenPalmThreshold;
    }

    private static bool CheckThumbsUpGesture(NormalizedLandmarkList landmarks)
    {
        float thumbToPinkyDistance = GetDistance(landmarks, THUMB_MCP, PINKY_MCP);

        float thumbExtension = GetDistance(landmarks, THUMB_TIP, INDEX_FINGER_MCP) / thumbToPinkyDistance;
        float indexCurl = GetDistance(landmarks, INDEX_FINGER_TIP, INDEX_FINGER_MCP) / thumbToPinkyDistance;
        float middleCurl = GetDistance(landmarks, MIDDLE_FINGER_TIP, MIDDLE_FINGER_MCP) / thumbToPinkyDistance;
        float ringCurl = GetDistance(landmarks, RING_FINGER_TIP, RING_FINGER_MCP) / thumbToPinkyDistance;
        float pinkyCurl = GetDistance(landmarks, PINKY_TIP, PINKY_MCP) / thumbToPinkyDistance;

        return thumbExtension > ThumbsUpThreshold &&
               indexCurl < FingerClosedThresholds[1] &&
               middleCurl < FingerClosedThresholds[2] &&
               ringCurl < FingerClosedThresholds[3] &&
               pinkyCurl < FingerClosedThresholds[4];
    }

    private static bool CheckPointingGesture(NormalizedLandmarkList landmarks)
    {
        float thumbToPinkyDistance = GetDistance(landmarks, THUMB_MCP, PINKY_MCP);

        float indexExtension = GetDistance(landmarks, INDEX_FINGER_TIP, WRIST) / thumbToPinkyDistance;
        float thumbCurl = GetDistance(landmarks, THUMB_TIP, INDEX_FINGER_MCP) / thumbToPinkyDistance;
        float middleCurl = GetDistance(landmarks, MIDDLE_FINGER_TIP, MIDDLE_FINGER_MCP) / thumbToPinkyDistance;
        float ringCurl = GetDistance(landmarks, RING_FINGER_TIP, RING_FINGER_MCP) / thumbToPinkyDistance;
        float pinkyCurl = GetDistance(landmarks, PINKY_TIP, PINKY_MCP) / thumbToPinkyDistance;

        return indexExtension > OpenPalmThreshold &&
               thumbCurl < FingerClosedThresholds[0] &&
               middleCurl < FingerClosedThresholds[2] &&
               ringCurl < FingerClosedThresholds[3] &&
               pinkyCurl < FingerClosedThresholds[4];
    }

    private static float GetDistance(NormalizedLandmarkList landmarks, int index1, int index2)
    {
        var landmark1 = landmarks.Landmark[index1];
        var landmark2 = landmarks.Landmark[index2];

        float dx = landmark1.X - landmark2.X;
        float dy = landmark1.Y - landmark2.Y;
        float dz = landmark1.Z - landmark2.Z;

        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}

using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using NOVA.Scripts;
using System;

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

    private static List<float> currentClosestDistance = new List<float>();
    private static List<string> gestureList = new List<string>();
    private static GestureSqliteHandler gestureSqliteHandler = GestureSqliteHandler.Instance();
    private static float tolerance = 0.1f; // Default tolerance for gesture matching
    private static int toleranceMultipler = 8; // Multiplier for tolerance based on configuration
    private static Dictionary<string, List<float>> knownGestureDistances = new Dictionary<string, List<float>>();


    //on awake
    static GestureRecognizer()
    {
        // Initialize the gesture list from the database or predefined gestures
        var gesturesData = gestureSqliteHandler.GetAllUIGestures();
        foreach (var gesture in gesturesData)
        {
            gestureList.Add(gesture.Data.Name);
        }
        tolerance = (gestureSqliteHandler.GetActiveConfiguration().LandmarkTolerance) * toleranceMultipler;

        foreach (var gesture in gestureList)
        {
            knownGestureDistances.Add(gesture, gestureSqliteHandler.GetDistancesByName(gesture));
        }

    }

    /// <summary>
    /// Detects the gesture based on the given hand landmarks.
    /// </summary>
    /// <param name="landmarks">NormalizedLandmarkList from MediaPipe</param>
    /// <returns>The name of the detected gesture.</returns>
    public static string DetectGesture(NormalizedLandmarkList landmarks)
    {
        string closestGesture = null;
        List<float> unknownGestureDistances = GetNormalizedLandmarkDistances(landmarks);

        foreach (var gesture in gestureList)
        {
            if (CheckGestureMatch(unknownGestureDistances, gesture, closestGesture, gestureSqliteHandler, tolerance))
            {
                closestGesture = gesture; // Return the name of the matched gesture
            }
        }
        currentClosestDistance.Clear();
        if (closestGesture != null)
        {
            return closestGesture; // Return the name of the matched gesture
        }
        else
        {
            return NO_GESTURE; // If no gesture matched, return "None"
        }

        //if (landmarks == null || landmarks.Landmark == null || landmarks.Landmark.Count < 21)
        //{
        //    return NO_GESTURE;
        //}

        //if (CheckClosedFistGesture(landmarks))
        //{
        //    return CLOSED_FIST;
        //}
        //else if (CheckOpenPalmGesture(landmarks))
        //{
        //    return OPEN_PALM;
        //}
        //else if (CheckThumbsUpGesture(landmarks))
        //{
        //    return THUMBS_UP;
        //}
        //else if (CheckPointingGesture(landmarks))
        //{
        //    return POINTING;
        //}

        return NO_GESTURE;
    }

    // takes in the landmarks we're checking and the landmarks from another gesture if it matches and is a closer match then the current closest return true
    private static bool CheckGestureMatch(List<float> unkownGestureDistances, string newGesture, string currentClosestGesture, GestureSqliteHandler gestureSqliteHandler, float tolerance)
    {
        
        //List<float> knownGestureDistances = GetLandmarkDistances(gestureSqliteHandler.GetGestureInfo(newGesture).Landmarks);

        float distance = 0f;
        List<float> distances = new List<float>();

        int closerCounter = 0;
        // get list of floats from known gesture based on name
        List<float> knownDistances = knownGestureDistances[newGesture];


        // compare each of the distances in the list of floats to see if the are within +-tolerance range
        for (int i = 0; i < knownDistances.Count; i++)
        {
            float unknownDistance = unkownGestureDistances[i];
            float knownDistance = knownDistances[i];

            // if the unknown distance - known distance is within +- range of tolerance continue
            if ((distance = (Mathf.Abs(unknownDistance - knownDistance))) > tolerance)
            {
                return false; // If any distance is out of tolerance, return false
            }
            // else store the distance
            else
            {
                distances.Add(distance);
            }
        }
        // if we get here the known gesture is a match, now we need to check if the current closest gesture is closer than the new gesture
        if (currentClosestGesture == null)
        {
            currentClosestDistance = new List<float>(knownDistances);
            return true; // If no current closest gesture, accept the new gesture
        }
        else
        {
            // comare all the disances of the new gesture to the current closest gesture
            for (int i = 0; i < currentClosestDistance.Count; i++)
            {
                float currentDistance = currentClosestDistance[i];
                float newDistance = distances[i];

                // if currentDistance is less than newDistance, then the current closest gesture is closer and increment closerCounter
                if (currentDistance < newDistance)
                {
                    closerCounter++;
                }
            }
            // if closer counter is greater than half the number of distances, then the current closest gesture is closer
            if (closerCounter > distances.Count / 2)
            {
                return false; // If the current closest gesture is closer, return false
            }
            else
            {
                return true; // Otherwise, accept the new gesture
            }
        }
    }

    // get a vector of distances
    public static List<float> GetNormalizedLandmarkDistances(NormalizedLandmarkList landmarks)
    {
        // calculate distance from pinky to thumb
        float palmToThumbKnuckleDistance = GetDistance(landmarks, WRIST, THUMB_MCP);


        List<float> distances = new List<float>();

        //float thumbCurl = GetDistance(landmarks, THUMB_TIP, PINKY_DIP) / palmToThumbKnuckleDistance;
        //distances.Add(thumbCurl);
        //float indexCurl = GetDistance(landmarks, INDEX_FINGER_TIP, INDEX_FINGER_MCP) / palmToThumbKnuckleDistance;
        //distances.Add(indexCurl);
        //float middleCurl = GetDistance(landmarks, MIDDLE_FINGER_TIP, MIDDLE_FINGER_MCP) / palmToThumbKnuckleDistance;
        //distances.Add(middleCurl);
        //float ringCurl = GetDistance(landmarks, RING_FINGER_TIP, RING_FINGER_MCP) / palmToThumbKnuckleDistance;
        //distances.Add(ringCurl);
        //float pinkyCurl = GetDistance(landmarks, PINKY_TIP, PINKY_MCP) / palmToThumbKnuckleDistance;
        //distances.Add(pinkyCurl);

        for (int i = 0; i < landmarks.Landmark.Count; i++)
        {
            for (int j = i + 1; j < landmarks.Landmark.Count; j++)
            {
                float distance = GetDistance(landmarks, i, j) / palmToThumbKnuckleDistance;
                distances.Add(distance);
            }
        }

        return distances;
    }
    // get a vector of distances
    public static List<float> GetLandmarkDistances(List<NOVA.Scripts.Landmark> landmarks)
    {

        // calculate distance from pinky to thumb
        float palmToThumbKnuckleDistance = GetDatabaseDistance(landmarks, WRIST, THUMB_MCP);


        List<float> distances = new List<float>();

        //float thumbCurl = GetDatabaseDistance(landmarks, THUMB_TIP, PINKY_DIP) / palmToThumbKnuckleDistance;
        //distances.Add(thumbCurl);
        //float indexCurl = GetDatabaseDistance(landmarks, INDEX_FINGER_TIP, INDEX_FINGER_MCP) / palmToThumbKnuckleDistance;
        //distances.Add(indexCurl);
        //float middleCurl = GetDatabaseDistance(landmarks, MIDDLE_FINGER_TIP, MIDDLE_FINGER_MCP) / palmToThumbKnuckleDistance;
        //distances.Add(middleCurl);
        //float ringCurl = GetDatabaseDistance(landmarks, RING_FINGER_TIP, RING_FINGER_MCP) / palmToThumbKnuckleDistance;
        //distances.Add(ringCurl);
        //float pinkyCurl = GetDatabaseDistance(landmarks, PINKY_TIP, PINKY_MCP) / palmToThumbKnuckleDistance;
        //distances.Add(pinkyCurl);

        for (int i = 0; i < landmarks.Count; i++)
        {
            for (int j = i + 1; j < landmarks.Count; j++)
            {
                float distance = GetDatabaseDistance(landmarks, i, j) / palmToThumbKnuckleDistance;
                distances.Add(distance);
            }
        }

        return distances;
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

    private static float GetDatabaseDistance(List<NOVA.Scripts.Landmark> landmarks, int index1, int index2)
    {
        var landmark1 = landmarks[index1];
        var landmark2 = landmarks[index2];

        float dx = landmark1.X - landmark2.X;
        float dy = landmark1.Y - landmark2.Y;
        float dz = landmark1.Z - landmark2.Z;

        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}

using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using NOVA.Scripts;
using Landmark = NOVA.Scripts.Landmark;

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

    private static List<float> currentClosestDistance = new List<float>();
    private static List<string> gestureList = new List<string>();
    private static GestureSqliteHandler gestureSqliteHandler = GestureSqliteHandler.Instance();
    private static float tolerance = 0.1f; // Default tolerance for gesture matching
    private static int toleranceMultipler = 8; // Multiplier for tolerance based on configuration
    private static Dictionary<string, List<float>> knownGestureDistances = new Dictionary<string, List<float>>();

    // on awake
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
        var unknownGestureDistances = GetNormalizedLandmarkDistances(landmarks);

        foreach (var gesture in gestureList)
        {
            if (CheckGestureMatch(unknownGestureDistances, gesture, closestGesture, gestureSqliteHandler, tolerance))
            {
                closestGesture = gesture;
            }
        }

        currentClosestDistance.Clear();
        return closestGesture ?? NO_GESTURE;
    }

    /// <summary>
    /// Centers, rotates to a palm-aligned plane, and scales landmarks so palm width is 1.
    /// </summary>
    private static List<Vector3> NormalizeLandmarks(NormalizedLandmarkList lm)
    {
        var pts = new List<Vector3>();
        foreach (var l in lm.Landmark)
            pts.Add(new Vector3(l.X, l.Y, l.Z));

        // center at wrist
        var wrist = pts[WRIST];
        for (int i = 0; i < pts.Count; i++)
            pts[i] -= wrist;

        // build palm axes: X = wrist->middle_MCP, temp = wrist->index_MCP
        var xAxis = pts[MIDDLE_FINGER_MCP].normalized;
        var temp = pts[INDEX_FINGER_MCP].normalized;
        var yAxis = (temp - Vector3.Project(temp, xAxis)).normalized;
        var zAxis = Vector3.Cross(xAxis, yAxis);

        // rotation matrix
        var m = new Matrix4x4();
        m.SetColumn(0, new Vector4(xAxis.x, xAxis.y, xAxis.z, 0));
        m.SetColumn(1, new Vector4(yAxis.x, yAxis.y, yAxis.z, 0));
        m.SetColumn(2, new Vector4(zAxis.x, zAxis.y, zAxis.z, 0));
        m.SetColumn(3, new Vector4(0, 0, 0, 1));

        // apply rotation
        for (int i = 0; i < pts.Count; i++)
            pts[i] = m.MultiplyPoint3x4(pts[i]);

        // scale by average palm width
        float d1 = Vector3.Distance(pts[WRIST], pts[MIDDLE_FINGER_MCP]);
        float d2 = Vector3.Distance(pts[INDEX_FINGER_MCP], pts[PINKY_MCP]);
        float scale = (d1 + d2) * 0.5f;
        for (int i = 0; i < pts.Count; i++)
            pts[i] /= scale;

        return pts;
    }

    /// <summary>
    /// Computes normalized pairwise distances on a centered, aligned, unit-scaled hand.
    /// </summary>
    public static List<float> GetNormalizedLandmarkDistances(NormalizedLandmarkList landmarks)
    {
        var pts = NormalizeLandmarks(landmarks);
        var distances = new List<float>();

        for (int i = 0; i < pts.Count; i++)
        {
            for (int j = i + 1; j < pts.Count; j++)
            {
                distances.Add(Vector3.Distance(pts[i], pts[j]));
            }
        }

        return distances;
    }

    /// <summary>
    /// Adapter method to compute distances from a list of Landmark objects.
    /// </summary>
    public static List<float> GetLandmarkDistances(List<Landmark> landmarks)
    {
        // Convert landmarks to normalized landmarks
        var normalizedLandmarks = new NormalizedLandmarkList();

        foreach (var lm in landmarks)
        {
            normalizedLandmarks.Landmark.Add(new NormalizedLandmark
            {
                X = lm.X,
                Y = lm.Y,
                Z = lm.Z
            });
        }

        return GetNormalizedLandmarkDistances(normalizedLandmarks);
    }

    private static bool CheckGestureMatch(List<float> unknownDistances, string newGesture, string currentClosestGesture, GestureSqliteHandler handler, float tol)
    {
        var distances = new List<float>();
        var known = knownGestureDistances[newGesture];

        for (int i = 0; i < known.Count; i++)
        {
            float distance = Mathf.Abs(unknownDistances[i] - known[i]);
            if (distance > tol) return false;
            distances.Add(distance);
        }

        if (currentClosestGesture == null)
        {
            currentClosestDistance = new List<float>(distances);
            return true;
        }

        // already have a closest gesture, check if this one is closer
        int closer = 0;
        for (int i = 0; i < currentClosestDistance.Count; i++)
            if (currentClosestDistance[i] < distances[i]) closer++;

        // this verifies 
        return closer <= distances.Count / 2;
    }

    // Unused (kept for reference)

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

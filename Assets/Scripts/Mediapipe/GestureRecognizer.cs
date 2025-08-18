using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using NOVA.Scripts;
using Landmark = NOVA.Scripts.Landmark;
using System.Linq;

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
    private static Dictionary<string, List<string>> knownGestureDirection = new Dictionary<string, List<string>>();

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
            knownGestureDirection.Add(gesture, gestureSqliteHandler.GetDirectionsByName(gesture));
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
        var unknownGestureDirection = GetDetectedGestureDirection(landmarks);

        foreach (var gesture in gestureList)
        {
            var storedDirections = knownGestureDirection[gesture];

            bool directionsMatch = storedDirections.SequenceEqual(unknownGestureDirection);

            //Debug.Log($"Comparing against Gesture {gesture} with directions [{string.Join(", ", storedDirections)}] against detected gesture with [{string.Join(", ", unknownGestureDirection)}]");

            if (directionsMatch)
            {
                if (CheckGestureMatch(unknownGestureDistances, gesture, closestGesture, gestureSqliteHandler, tolerance))
                {
                    closestGesture = gesture;
                }
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

    public static List<string> GetDetectedGestureDirection(NormalizedLandmarkList landmarks)
    {
        List<string> directions = new List<string>();

        if (landmarks.Landmark.Count >= 21) // Ensure we have all hand landmarks
        {
            // Wrist (0) to Index Finger Tip (8) - Inverted (short-term fix)
            string directionWristToIndex = GetGestureDirection(
                new Vector3(landmarks.Landmark[8].X, landmarks.Landmark[8].Y, landmarks.Landmark[8].Z),
                new Vector3(landmarks.Landmark[0].X, landmarks.Landmark[0].Y, landmarks.Landmark[0].Z)                
            );
            directions.Add(directionWristToIndex);

            // Index Finger Tip (8) to Middle Finger Tip (12) - Inverted (short-term fix)
            string directionIndexToMiddle = GetGestureDirection(
                new Vector3(landmarks.Landmark[12].X, landmarks.Landmark[12].Y, landmarks.Landmark[12].Z),
                new Vector3(landmarks.Landmark[8].X, landmarks.Landmark[8].Y, landmarks.Landmark[8].Z)
            );
            directions.Add(directionIndexToMiddle);
        }

        return directions;
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

    public static string GetGestureDirection(Vector3 start, Vector3 end)
    {
        // Calculate the direction vector
        Vector3 direction = end - start;

        // Determine the dominant direction (2D projection for simplicity)
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) // Horizontal movement
        {
            return direction.x > 0 ? "Right" : "Left";
        }
        else // Vertical movement
        {
            return direction.y > 0 ? "Up" : "Down";
        }
    }

    public static List<string> GetGestureDirections(NormalizedLandmarkList landmarks)
    {
        List<string> directions = new List<string>();

        // Example: Wrist (Landmark 0) to Index Finger Tip (Landmark 8)
        string directionWristToIndex = GetGestureDirection(
            new Vector3(landmarks.Landmark[0].X, landmarks.Landmark[0].Y, landmarks.Landmark[0].Z), // Wrist
            new Vector3(landmarks.Landmark[8].X, landmarks.Landmark[8].Y, landmarks.Landmark[8].Z)  // Index Tip
        );

        directions.Add(directionWristToIndex);

        // Example: Index Finger Tip (Landmark 8) to Middle Finger Tip (Landmark 12)
        string directionIndexToMiddle = GetGestureDirection(
            new Vector3(landmarks.Landmark[8].X, landmarks.Landmark[8].Y, landmarks.Landmark[8].Z), // Index Tip
            new Vector3(landmarks.Landmark[12].X, landmarks.Landmark[12].Y, landmarks.Landmark[12].Z) // Middle Tip
        );

        directions.Add(directionIndexToMiddle);

        return directions;
    }

    // Future Implementation using Angles if desired

    public static float CalculateAngle(Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        // Vectors from pointB to pointA and pointB to pointC
        Vector3 vectorBA = pointA - pointB;
        Vector3 vectorBC = pointC - pointB;

        // Normalize vectors
        vectorBA.Normalize();
        vectorBC.Normalize();

        // Compute the dot product
        float dotProduct = Vector3.Dot(vectorBA, vectorBC);

        // Clamp the dot product to avoid floating-point precision issues
        dotProduct = Mathf.Clamp(dotProduct, -1.0f, 1.0f);

        // Calculate the angle in degrees
        float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

        return angle;
    }

    public static List<float> GetLandmarkAngles(List<Landmark> landmarks)
    {
        List<float> angles = new List<float>();

        // Example: Calculate angles between thumb, index finger, and middle finger
        if (landmarks.Count >= 5)
        {
            // Thumb (Landmark 0), Index Finger (Landmark 1), Middle Finger (Landmark 2)
            float angleThumbIndexMiddle = CalculateAngle(
                new Vector3(landmarks[0].X, landmarks[0].Y, landmarks[0].Z),
                new Vector3(landmarks[1].X, landmarks[1].Y, landmarks[1].Z),
                new Vector3(landmarks[2].X, landmarks[2].Y, landmarks[2].Z)
            );

            // Add the calculated angle
            angles.Add(angleThumbIndexMiddle);
        }

        // Repeat for other sets of landmarks as necessary
        // Example for left/right direction: wrist, index, and pinky
        if (landmarks.Count >= 20)
        {
            float angleWristIndexPinky = CalculateAngle(
                new Vector3(landmarks[0].X, landmarks[0].Y, landmarks[0].Z),  // Wrist
                new Vector3(landmarks[5].X, landmarks[5].Y, landmarks[5].Z),  // Index
                new Vector3(landmarks[17].X, landmarks[17].Y, landmarks[17].Z) // Pinky
            );

            angles.Add(angleWristIndexPinky);
        }

        return angles;
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

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace NOVA.Scripts
{
    /// <summary>
    /// This class provides a method to reset the local state of the application by wiping the database and any assets.
    /// </summary>
    public static class ResetLocalState
    {
        [MenuItem("Window/Reset Local State")]
        public static void Reset()
        {
            // Remove any files in the GestureAssets directory
            string gestureAssetsDir = Path.Combine(Application.streamingAssetsPath, HelperConstants.GestureAssetsDirName);

            try
            {
                if (Directory.Exists(gestureAssetsDir))
                {
                    DirectoryInfo directoryInfo = new(gestureAssetsDir);

                    foreach (var file in directoryInfo.GetFiles())
                    {
                        file.Delete();
                    }
                }
                else
                {
                    Debug.LogWarning($"Gesture assets directory does not exist: {gestureAssetsDir}. This is likely because the database does not exist.");
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to delete files in {gestureAssetsDir}: {e.Message}");
            }

            // Remove any files in the Resources directory
            string resourcesDir = Path.Combine(Application.dataPath, HelperConstants.ResourcesDirectory);
            try
            {
                if (Directory.Exists(resourcesDir))
                {
                    DirectoryInfo directoryInfo = new(resourcesDir);
                    foreach (var file in directoryInfo.GetFiles())
                    {
                        file.Delete();
                    }
                }
                else
                {
                    Debug.LogWarning($"Resources directory does not exist: {resourcesDir}. This is likely because there aren't any gestures in the database or the database does not exist.");
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to delete files in {resourcesDir}: {e.Message}");
            }

            // Release the Singleton instance
            GestureSqliteHandler.ReleaseInstance();
            Debug.Log("Local state reset.");
        }
    }
}
#endif

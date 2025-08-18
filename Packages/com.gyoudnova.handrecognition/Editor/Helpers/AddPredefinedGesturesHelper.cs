
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NOVA.Scripts
{
    public class AddPredefinedGesturesHelper
    {
        public static IEnumerator AddPredefinedGestures()
        {
            Debug.Log("Adding predefined gesture to the database...");

            // Steps to follow:
            // 1. Read all images from the StreamingAssets/Gesture Assets/Images folder.
            // 2. First, verify that NO predefined gestures exist in the database.
            //  2.1. If no predefined gestures exist, we are good to proceed.
            //  2.2. If predefined gestures exist, we will not add any new ones.
            // 3. Use a template to apply things like name and category to each gesture (<gesture_name>.extension, category is Predefined)

            // We would have to expose private methods to do the following:
            // 4. Attempt to generate landmarks for each image.
            // 5. Create a new gesture for each image.


            // Get the current instance of the GestureSqliteHandler, as we will need it to check different states of the database.
            GestureSqliteHandler gestureSqliteHandler = GestureSqliteHandler.Instance();

            // #1
            string gestureAssetsDir = Path.Combine(Application.streamingAssetsPath, HelperConstants.GestureAssetsDirName);
            string imagesDir = Path.Combine(gestureAssetsDir, HelperConstants.ImagesDirName);

            if (!Directory.Exists(imagesDir))
            {
                Debug.LogError($"Images directory does not exist: {imagesDir}");
                yield break;
            }

            // Retrieve the active configuration to determine image extensions later
            var currentConfig = gestureSqliteHandler.GetActiveConfiguration();

            // #2

            // Get all images (with the active extension), and attempt to find a gesture with the same name.
            GestureImageExtension gestureImageExtension = currentConfig.ImageExtension;
            string extension = gestureImageExtension.GetExtension();
            Debug.Log($"Looking for images with extension: {extension}");
            List<string> imageFiles = Directory.GetFiles(imagesDir).ToList();

            foreach (var file in imageFiles)
            {
                Debug.Log($"Found file: {file}");
            }

            // remove any .meta files from the list, and any files that do not match the expected extension
            imageFiles = imageFiles
                .Where(file => file.EndsWith($".{extension}", System.StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".meta"))
                .ToList();

            if (imageFiles.Count == 0)
            {
                Debug.LogError("No images found in the predefined directory.");
                yield break;
            }

            // Check if any predefined gestures exist in the database
            if (gestureSqliteHandler.HasPredefinedGestures())
            {
                Debug.LogError("Predefined gestures already exist in the database. Skipping addition of new gestures.");
                yield break;
            }

            // #3

            // Initialize Mediapipe handler
            NovaMediapipeHandler mediapipeHandler = new NovaMediapipeHandler();
            yield return mediapipeHandler.InitializeIfNeeded(HelperConstants.CameraWidth, HelperConstants.CameraHeight);

            // Iterate through each image file and process it
            foreach (string imageFile in imageFiles)
            {
                // Extract the name of the gesture from the file name
                string gestureName = Path.GetFileNameWithoutExtension(imageFile);
                gestureName = StringHandler.GetNormalizedString(gestureName);

                if (gestureSqliteHandler.GestureExists(gestureName))
                {
                    Debug.LogWarning($"Gesture '{gestureName}' already exists in the database. Skipping this gesture.");
                    continue;
                }

                byte[] bytes = File.ReadAllBytes(imageFile);
                if (bytes == null || bytes.Length == 0)
                {
                    Debug.LogError($"Failed to read image file: {imageFile}");
                    continue;
                }

                // Load image as a Texture2D
                var predefinedTexture = new Texture2D(HelperConstants.CameraWidth, HelperConstants.CameraHeight);
                if (!predefinedTexture.LoadImage(bytes))
                {
                    Debug.LogError($"Failed to load image from file: {imageFile}");
                    continue;
                }

                // Check if the texture is too small
                if (predefinedTexture.width < HelperConstants.CameraWidth || predefinedTexture.height < HelperConstants.CameraHeight)
                {
                    Debug.LogError($"Image '{gestureName}' is too small. Minimum size is {HelperConstants.CameraWidth}x{HelperConstants.CameraHeight}px.");
                    continue;
                }

                // Resize the texture if it does not match the expected dimensions
                if (predefinedTexture.width != HelperConstants.CameraWidth || predefinedTexture.height != HelperConstants.CameraHeight)
                {
                    predefinedTexture = NovaMediapipeHandler.ResizeTexture(predefinedTexture,
                                                       HelperConstants.CameraWidth,
                                                       HelperConstants.CameraHeight);
                }

                // Move the image to the Resources folder for runtime access
                string resourcesDir = Path.Combine(Application.dataPath, HelperConstants.ResourcesDirectory);
                FileHandler.SaveTextureToResources(predefinedTexture, gestureName, gestureImageExtension);

                // Attempt to generate landmarks for the image
                NovaHandResult handResult = null;
                yield return mediapipeHandler.TryGenerateLandmarks(predefinedTexture, result =>
                {
                    handResult = result;
                });

                if (handResult == null || !handResult.Success)
                {
                    Debug.LogError($"Failed to generate landmarks for gesture '{gestureName}'. Error: {handResult?.Error}");
                    continue;
                }

                // Retrieve the distances from the GestureRecognizer
                var distances = GestureRecognizer.GetNormalizedLandmarkDistances(handResult.Normalized);
                var directions = GestureRecognizer.GetGestureDirections(handResult.Normalized);

                yield return null; // Yield to avoid blocking

                List<LandmarkDistance> dbDistances = new();
                List<LandmarkDirection> dbDirections = new();

                // Convert to database-compatible formats
                foreach (var dist in distances)
                {
                    dbDistances.Add(new LandmarkDistance
                    {
                        Distance = dist,
                        IsPredefined = true,
                        LandmarkId = 1, // TODO: REMOVE
                        OtherLandmarkId = 2 // TODO: REMOVE
                    });
                }

                foreach (var dir in directions)
                {
                    dbDirections.Add(new LandmarkDirection
                    {
                        Direction = dir,
                        IsPredefined = true,
                        LandmarkId = 1, // TODO: REMOVE
                        OtherLandmarkId = 2 // TODO: REMOVE
                    });
                }

                // Create a new gesture with the generated landmarks
                QueryableGestureInfo qgi = new QueryableGestureInfo
                {
                    GestureName = gestureName,
                    CategoryName = GestureCategory.PredefinedCategoryName,
                    ImageName = $"{gestureName}",
                    IsPredefined = true,
                    Direction = dbDirections,
                    Distances = dbDistances,
                    Landmarks = handResult.Landmarks,
                };

                gestureSqliteHandler.AddGesture(qgi);
                yield return null; // Skip a frame after every gesture addition

                Debug.Log($"Added predefined gesture '{gestureName}' to the database.");
            }
        }
    }
}

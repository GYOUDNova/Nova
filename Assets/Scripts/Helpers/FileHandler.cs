using System.IO;
using UnityEngine;

namespace NOVA.Scripts
{
    public static class FileHandler
    {
        // Save a Texture2D to a file under Assets/Resources with the specified extension
        public static void SaveTextureToResources(Texture2D textureImage, string fileName, GestureImageExtension extension)
        {
            if (textureImage == null)
            {
                Debug.LogError("Texture is null. Cannot save to file.");
                return;
            }

            string resourcesDir = Path.Combine(Application.dataPath, HelperConstants.ResourcesDirectory);
            string fileWithExtension = $"{fileName}.{extension.GetExtension()}";
            string filePath = Path.Combine(resourcesDir, fileWithExtension);

            // Create directory if it doesn't exist
            if (!Directory.Exists(resourcesDir))
            {
                Directory.CreateDirectory(resourcesDir);
            }

            // Decide how to encode
            byte[] rawData;

            switch (extension)
            {
                case GestureImageExtension.Jpeg:
                case GestureImageExtension.Jpg:
                    rawData = textureImage.EncodeToJPG();
                    break;
                case GestureImageExtension.Png:
                    rawData = textureImage.EncodeToPNG();
                    break;
                default:
                    Debug.LogError($"Unsupported image format: {extension}");
                    return;
            }

            File.WriteAllBytes(filePath, rawData);
            Debug.Log($"Image {fileName} saved to {filePath}");
        }

        public static Texture2D LoadTextureFromResources(string fileName, GestureImageExtension extension)
        {
            string resourcesDir = Path.Combine(Application.dataPath, HelperConstants.ResourcesDirectory);
            string fileWithExtension = $"{fileName}.{extension.GetExtension()}";
            string filePath = Path.Combine(resourcesDir, fileWithExtension);

            if (!File.Exists(filePath))
            {
                Debug.LogError($"File {filePath} does not exist.");
                return null;
            }

            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new(HelperConstants.CameraHeight, HelperConstants.CameraWidth);
            texture.LoadImage(fileData);
            return texture;
        }
    }
}

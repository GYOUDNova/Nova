using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MPLandmark = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;
using NOVALandmark = NOVA.Scripts.Landmark;

namespace NOVA.Scripts
{
    public sealed class NovaHandResult
    {
        public bool Success;
        public string Error;
        public Mediapipe.NormalizedLandmarkList Normalized;
        public List<NOVALandmark> Landmarks;
    }

    /// <summary>
    /// Pure backend MediaPipe wrapper (no Editor/UI).
    /// </summary>
    public class NovaMediapipeHandler : System.IDisposable
    {
        private HandLandmarker taskApi;
        private TextureFrame textureFrame;
        private ImageProcessingOptions imageProcessingOptions;
        private readonly HandLandmarkDetectionConfig config;

        public NovaMediapipeHandler(HandLandmarkDetectionConfig overrideConfig = null)
        {
            config = overrideConfig ?? new HandLandmarkDetectionConfig();
        }

        public IEnumerator InitializeIfNeeded(int width, int height)
        {
            if (taskApi != null && textureFrame != null) yield break;

            config.RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE;
            AssetLoader.Provide(new StreamingAssetsResourceManager());
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            imageProcessingOptions = new ImageProcessingOptions(rotationDegrees: 0);
            var options = config.GetHandLandmarkerOptions(null);
            taskApi = HandLandmarker.CreateFromOptions(options);
            textureFrame = new TextureFrame(width, height, TextureFormat.RGBA32);
        }

        /// <summary>
        /// Backend-only detection
        /// </summary>
        public IEnumerator TryGenerateLandmarks(Texture2D sourceTexture, System.Action<NovaHandResult> onDone)
        {
            // Ensure frame matches texture
            if (textureFrame == null || textureFrame.width != sourceTexture.width || textureFrame.height != sourceTexture.height)
            {
                Object.DestroyImmediate(textureFrame.texture);
                textureFrame = new TextureFrame(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32);
            }

            if (taskApi == null)
            {
                yield return InitializeIfNeeded(sourceTexture.width, sourceTexture.height);
            }

            textureFrame.ReadTextureOnCPU(sourceTexture);
            var mpImage = textureFrame.BuildCPUImage();

            try
            {
                var result = HandLandmarkerResult.Alloc(2);
                if (!taskApi.TryDetect(mpImage, imageProcessingOptions, ref result))
                {
                    onDone?.Invoke(new NovaHandResult { Success = false, Error = "NO_HAND" });
                    yield break;
                }

                var hand = result.handLandmarks.FirstOrDefault();
                var normalized = HandLandmarkerRunner.ConvertToNormalizedLandmarkList(hand);
                var landmarks = Translate(hand.landmarks);

                onDone?.Invoke(new NovaHandResult
                {
                    Success = true,
                    Normalized = normalized,
                    Landmarks = landmarks
                });
            }
            finally
            {
                mpImage?.Dispose();
            }
        }

        private static List<NOVALandmark> Translate(List<MPLandmark> mpLandmarks)
        {
            var list = new List<NOVALandmark>(mpLandmarks.Count);

            for (int i = 0; i < mpLandmarks.Count; i++)
            {
                list.Add(new NOVALandmark
                {
                    X = mpLandmarks[i].x,
                    Y = mpLandmarks[i].y,
                    Z = mpLandmarks[i].z
                });
            }

            return list;
        }

        public void Dispose()
        {
            if (textureFrame != null)
            {
                Object.DestroyImmediate(textureFrame.texture);
            }

            textureFrame = null;
            taskApi = null;
        }

        public static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            if (source.width == targetWidth && source.height == targetHeight)
            {
                return source; // No resizing needed
            }

            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            Graphics.Blit(source, rt);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D result = new Texture2D(targetWidth, targetHeight);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}

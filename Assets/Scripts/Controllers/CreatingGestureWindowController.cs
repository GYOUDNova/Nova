using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public class CreatingGestureWindowController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset createGestureScreenAsset;

        /* Window Settings */
        private const float MinWindowHeight = 1920;
        private const float MinWindowLength = 1080;

        private VisualElement root;
        private WebCamTexture webCamTexture;
        private Texture2D texture;

        private EditorCoroutine edCoro;

        // The actual task API that will be used for hand landmark detection
        private HandLandmarker _taskApi;

        // A frame object to hold the texture image
        private TextureFrame _frame;

        // Reference to the MP image that will be used for processing
        private Mediapipe.Image _mpImage;

        private ImageProcessingOptions imageProcessingOptions;

        // This is will contain the basic config information for the hand landmark detection (i.e., num of hands, etc.)
        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

        [MenuItem("Window/UI Toolkit/Creating Gesture Screen")]
        public static void SetupAndShowWindow()
        {
            CreatingGestureWindowController createGestureController = GetWindow<CreatingGestureWindowController>();
            createGestureController.titleContent = new GUIContent("Create a Gesture");
            createGestureController.maxSize = new Vector2(MinWindowHeight, MinWindowLength);
            createGestureController.minSize = createGestureController.maxSize;
        }

        public void CreateGUI()
        {
            root = createGestureScreenAsset.CloneTree();
            rootVisualElement.Add(root);

            webCamTexture = new WebCamTexture(800, 600);
            webCamTexture.deviceName = "USB Camera";
            webCamTexture.Play();

            texture = new Texture2D(webCamTexture.width, webCamTexture.height);
            foreach (var device in WebCamTexture.devices)
            {
                Debug.Log(device.name);
            }


            var image = new Image();
            image.image = texture;
            image.style.width = 800;
            image.style.height = 600;

            root.Add(image);

            // Add functionality to take image and save
            var takeImage = root.Q<Button>("TakeImageButton");
            takeImage.clicked += () =>
            {
                // Use the mediapipe task API to process the image

                _frame.ReadTextureOnCPU(texture);
                _mpImage = _frame.BuildCPUImage();

                var result = HandLandmarkerResult.Alloc(2);
                if (_taskApi.TryDetect(_mpImage, imageProcessingOptions, ref result))
                {
                    foreach (var hands in result.handWorldLandmarks)
                    {
                        foreach (var handLandmark in hands.landmarks)
                        {
                            Debug.Log(handLandmark);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Error processing image texture...");
                }
            };

            edCoro = EditorCoroutineUtility.StartCoroutine(UpdateFeed(), this);
        }

        public void OnDestroy()
        {
            webCamTexture.Stop();
            webCamTexture = null;

            EditorCoroutineUtility.StopCoroutine(edCoro);
        }

        private void OnGUI()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                texture.SetPixels32(webCamTexture.GetPixels32());
                texture.Apply();
            }
        }

        private IEnumerator UpdateFeed()
        {
            config.RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE;
            AssetLoader.Provide(new StreamingAssetsResourceManager());
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);
            var options = config.GetHandLandmarkerOptions(null);
            _taskApi = HandLandmarker.CreateFromOptions(options);

            _frame = new(1920, 1080, TextureFormat.RGBA32);

            // Continue updating the feed until the window is closed
            while (hasFocus)
            {
                Repaint();
                yield return null;
            }
        }
    }
}

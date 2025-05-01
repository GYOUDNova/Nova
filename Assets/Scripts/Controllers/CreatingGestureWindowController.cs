using System;
using System.Collections;
using System.Linq;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
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

        private const float MinWindowHeight = 1280;
        private const float MinWindowLength = 720;

        private const string WindowName = "Create a Gesture";
        private const string CameraFeedSelector = "camera-feed";
        private const string TakeImageButtonName = "TakeImageButton";
        private const string DropdownMenuName = "CameraDropdown";
        private const string ErrorLabelName = "ErrorLabel";

        private DropdownField DropdownField;
        private Label ErrorLabel;

        private VisualElement root;

        /* Camera Settings */

        private const int CameraWidth = 600;
        private const int CameraHeight = 600;
        private WebCamTexture webCamTexture;
        private Texture2D texture;
        private EditorCoroutine edCoro;

        // The actual task API that will be used for hand landmark detection
        private HandLandmarker _taskApi;

        // A frame object to hold the texture image
        private TextureFrame _frame;

        // Reference to the MP image that will be used for processing
        private Mediapipe.Image _mpImage;

        // Image processing options for the hand landmark detection
        private ImageProcessingOptions imageProcessingOptions;

        // This is will contain the basic config information for the hand landmark detection (i.e., num of hands, etc.)
        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

        [MenuItem("Window/UI Toolkit/Creating Gesture Screen")]
        public static void SetupAndShowWindow()
        {
            CreatingGestureWindowController createGestureController = GetWindow<CreatingGestureWindowController>();
            createGestureController.titleContent = new GUIContent(WindowName);
            createGestureController.maxSize = new Vector2(MinWindowHeight, MinWindowLength);
            createGestureController.minSize = createGestureController.maxSize;
        }


        /// <summary>
        /// Creates the GUI for the window
        /// </summary>
        public void CreateGUI()
        {
            root = createGestureScreenAsset.CloneTree();
            rootVisualElement.Add(root);

            webCamTexture = new WebCamTexture(CameraWidth, CameraHeight);
           

            texture = new Texture2D(640, 480);
            DropdownField = root.Q<DropdownField>(DropdownMenuName);
            ErrorLabel = root.Q<Label>(ErrorLabelName);

            foreach (var device in WebCamTexture.devices)
            {
                DropdownField.choices.Add(device.name);
            }
            // Register a callback for when the dropdown value changes
            DropdownField.RegisterValueChangedCallback(evt => OnCameraSelected(evt.newValue));

            // Add the camera feed to the UI
            var image = new Image();
            image.image = texture;
            image.AddToClassList(CameraFeedSelector);
            root.Add(image);

            // Add functionality to take image and save
            var takeImage = root.Q<Button>(TakeImageButtonName);
            takeImage.clicked += () =>
            {
                // Use the mediapipe task API to process the image

                _frame.ReadTextureOnCPU(texture);
                _mpImage = _frame.BuildCPUImage();

                var result = HandLandmarkerResult.Alloc(2);
                if (_taskApi.TryDetect(_mpImage, imageProcessingOptions, ref result))
                {
                    // Placeholder: Log the results
                    // TODO: Replace with actual logic to process landmarks
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
        }

        private void OnCameraSelected(string newValue)
        {
            if (WebCamTexture.devices.Any(device => device.name == newValue))
            {

            }
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
              webCamTexture = new WebCamTexture(480, 620);
            webCamTexture.deviceName = newValue;
            webCamTexture.Play();
            edCoro = EditorCoroutineUtility.StartCoroutine(UpdateFeed(), this);

        }

        /// <summary>
        /// Cleanup resources when the window is closed.
        /// </summary>
        public void OnDestroy()
        {
            webCamTexture.Stop();
            webCamTexture = null;

            EditorCoroutineUtility.StopCoroutine(edCoro);
        }

        /// <summary>
        /// This method is called every frame to update the camera feed
        /// </summary>
        private void OnGUI()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                Debug.Log(webCamTexture.GetPixels32().Length);
                Debug.Log(webCamTexture.width + "|" + webCamTexture.height);
                Debug.Log(texture.width + "|" + texture.height);
                texture.SetPixels32(webCamTexture.GetPixels32());
                texture.Apply();
            }
        }

        /// <summary>
        /// Coroutine to update the camera feed and process the image
        /// </summary>
        private IEnumerator UpdateFeed()
        {
            config.RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE;
            AssetLoader.Provide(new StreamingAssetsResourceManager());
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            imageProcessingOptions = new ImageProcessingOptions(rotationDegrees: 0);
            var options = config.GetHandLandmarkerOptions(null);
            _taskApi = HandLandmarker.CreateFromOptions(options);

            _frame = new(CameraWidth, CameraHeight, TextureFormat.RGBA32);

            // Continue updating the feed until the window is closed
            while (hasFocus)
            {
                Repaint();
                yield return null;
            }
        }
    }
}

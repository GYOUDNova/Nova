#if UNITY_EDITOR

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
        private const string MessageLabelName = "MessageLabel";
        private const float MessageLabelTimer = 5f;

        private DropdownField dropdownField;
        private Label messageText;
        private Button saveImageButton;
        private VisualElement root;
        private VisualElement savingGestureContainer;

        /* Camera Settings */
        private const int CameraWidth = 640;
        private const int CameraHeight = 480;
        private WebCamTexture webCamTexture;
        private Texture2D texture;
        private EditorCoroutine edCoro;

        // The actual task API that will be used for hand landmark detection
        private HandLandmarker taskApi;

        // A frame object to hold the texture image
        private TextureFrame textureFrame;

        // Reference to the MP image that will be used for processing
        private Mediapipe.Image mpImage;

        // Image processing options for the hand landmark detection
        private ImageProcessingOptions imageProcessingOptions;

        // This will contain the basic config information for the hand landmark detection (i.e., num of hands, etc.)[
        public readonly HandLandmarkDetectionConfig Config = new HandLandmarkDetectionConfig();

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

            savingGestureContainer = root.Q<VisualElement>("SavingGestureContainer");
            saveImageButton = root.Q<Button>("SaveGestureButton");
            saveImageButton.RegisterCallback<ClickEvent>(evt => SaveImage(evt));
            dropdownField = root.Q<DropdownField>(DropdownMenuName);
            dropdownField.RegisterValueChangedCallback(evt => OnCameraSelected(evt.newValue));
            foreach (var device in WebCamTexture.devices)
            {
                dropdownField.choices.Add(device.name);
            }
            messageText = root.Q<Label>(MessageLabelName);

            webCamTexture = new WebCamTexture(CameraWidth, CameraHeight);
            texture = new Texture2D(CameraWidth, CameraHeight);

            var image = new Image();
            image.image = texture;
            image.AddToClassList(CameraFeedSelector);
            root.Add(image);

            // Add functionality to take image and save
            var takeImage = root.Q<Button>(TakeImageButtonName);
            takeImage.clicked += () =>
            {
                // Use the mediapipe task API to process the image

                textureFrame.ReadTextureOnCPU(texture);
                mpImage = textureFrame.BuildCPUImage();

                var result = HandLandmarkerResult.Alloc(2);
                if (taskApi.TryDetect(mpImage, imageProcessingOptions, ref result))
                {
                    DisplayMessage("Gesture data received. Please name the gesture and then save", Color.green, 10f);
                    savingGestureContainer.style.display = DisplayStyle.Flex;
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
                    DisplayMessage("Unable to detect gesture. Please try again", Color.red, 5f);
                }
            };
        }

        /// <summary>
        /// Callback for when a camera is picked in the dropdown
        /// </summary>
        /// <param name="selectedCamera"></param>
        private void OnCameraSelected(string selectedCamera)
        {
            if (!WebCamTexture.devices.Any(device => device.name == selectedCamera))
            {
                DisplayMessage($"Unable to find the given camera: {selectedCamera}", Color.red, 5f);
            }
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }

            webCamTexture = new WebCamTexture(CameraWidth, CameraHeight);
            webCamTexture.deviceName = selectedCamera;
            webCamTexture.Play();

            if (webCamTexture.isPlaying)
            {
                edCoro = EditorCoroutineUtility.StartCoroutine(UpdateFeed(), this);
            }
            else
            {
                DisplayMessage($"There was a problem setting up and playing the camera: {selectedCamera}", Color.red, 5f);
            }
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
                texture.SetPixels32(webCamTexture.GetPixels32());
                texture.Apply();
            }
        }

        /// <summary>
        /// Coroutine to update the camera feed and process the image
        /// </summary>
        private IEnumerator UpdateFeed()
        {
            Config.RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE;
            AssetLoader.Provide(new StreamingAssetsResourceManager());
            yield return AssetLoader.PrepareAssetAsync(Config.ModelPath);

            imageProcessingOptions = new ImageProcessingOptions(rotationDegrees: 0);
            var options = Config.GetHandLandmarkerOptions(null);
            taskApi = HandLandmarker.CreateFromOptions(options);

            textureFrame = new(CameraWidth, CameraHeight, TextureFormat.RGBA32);

            // Continue updating the feed until the window is closed
            while (hasFocus)
            {
                Repaint();
                yield return null;
            }
        }

        private void DisplayMessage(string text, Color messageColor, float messageDisplayTime)
        {
            messageText.style.color = messageColor;
            messageText.text = text;
            EditorCoroutineUtility.StartCoroutine(ClearErrorMessage(messageDisplayTime), this);
        }

        private IEnumerator ClearErrorMessage(float time)
        {
            yield return new EditorWaitForSeconds(time);
            messageText.text = string.Empty;
        }

        private void SaveImage(ClickEvent evt)
        {
            //TODO: Implement the saving to the database here...
            Debug.Log("Not implemented yet...");
        }
    }
}
#endif

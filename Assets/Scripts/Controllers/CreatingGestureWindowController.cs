#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
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
using Color = UnityEngine.Color;
using Image = UnityEngine.UIElements.Image;
using MPLandmark = Mediapipe.Tasks.Components.Containers.Landmark;
using NOVALandmark = NOVA.Scripts.Landmark;

namespace NOVA.Scripts
{
    public class CreatingGestureWindowController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset createGestureScreenAsset;

        private const string WindowName = "Create a Gesture";
        private const string CameraFeedSelector = "camera-feed";
        private const string TakeImageButtonName = "TakeImageButton";
        private const string DropdownMenuName = "CameraDropdown";
        private const string MessageLabelName = "MessageLabel";

        private DropdownField dropdownField;
        private Label messageText;
        private Button saveGestureButton;
        private Button takeImageButton;
        private VisualElement root;
        private VisualElement savingGestureContainer;
        private TextField savingGestureTextField;
        private TextField gestureCategoryTextfield;

        /* Camera Settings */
        private WebCamTexture webCamTexture;
        private Texture2D texture;
        private Texture2D savingTexture;
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

        // Landmarks list
        public List<NOVALandmark> Landmarks { get; private set; } = new();

        // Distances list
        public List<LandmarkDistance> Distances { get; private set; } = new();

        [MenuItem("Window/UI Toolkit/Creating Gesture Screen")]
        public static void SetupAndShowWindow()
        {
            CreatingGestureWindowController createGestureController = GetWindow<CreatingGestureWindowController>();
            createGestureController.titleContent = new GUIContent(WindowName);
            createGestureController.maxSize = new Vector2(HelperConstants.MinWindowHeight, HelperConstants.MinWindowLength);
            createGestureController.minSize = new Vector2(HelperConstants.MinWindowHeight + 1, HelperConstants.MinWindowLength + 1);
        }

        /// <summary>
        /// Creates the GUI for the window
        /// </summary>
        public void CreateGUI()
        {
            root = createGestureScreenAsset.CloneTree();
            rootVisualElement.Add(root);

            savingGestureContainer = root.Q<VisualElement>("SavingGestureContainer");
            savingGestureTextField = root.Q<TextField>("SaveGestureTextField");
            gestureCategoryTextfield = root.Q<TextField>("GestureCategoryTextField");
            savingGestureContainer.style.display = DisplayStyle.None; // Ensure the container is hidden until an image is taken
            saveGestureButton = root.Q<Button>("SaveGestureButton");
            saveGestureButton.RegisterCallback<ClickEvent>(evt => SaveGesture(evt));
            saveGestureButton.style.display = DisplayStyle.None; // Ensure the button is hidden until an image is taken
            dropdownField = root.Q<DropdownField>(DropdownMenuName);
            dropdownField.RegisterValueChangedCallback(evt => OnCameraSelected(evt.newValue));

            foreach (var device in WebCamTexture.devices)
            {
                dropdownField.choices.Add(device.name);
            }

            messageText = root.Q<Label>(MessageLabelName);

            webCamTexture = new WebCamTexture(HelperConstants.CameraWidth, HelperConstants.CameraHeight);
            texture = new Texture2D(HelperConstants.CameraWidth, HelperConstants.CameraHeight);
            savingTexture = new Texture2D(HelperConstants.CameraWidth, HelperConstants.CameraHeight);

            var image = new Image();
            image.image = texture;
            image.AddToClassList(CameraFeedSelector);
            root.Add(image);

            // Add functionality to take image and save
            takeImageButton = root.Q<Button>(TakeImageButtonName);
            takeImageButton.style.display = DisplayStyle.None; // Ensure the button is hidden
            takeImageButton.clicked += () =>
            {
                ResetSaveContainer();

                // Use the mediapipe task API to process the image

                savingTexture.LoadRawTextureData(texture.GetRawTextureData()); // Save the current for image saving
                textureFrame.ReadTextureOnCPU(texture);
                mpImage = textureFrame.BuildCPUImage();

                var result = HandLandmarkerResult.Alloc(2);
                if (taskApi.TryDetect(mpImage, imageProcessingOptions, ref result))
                {
                    var handWorldLandmarks = result.handWorldLandmarks.FirstOrDefault();
                    EditorCoroutineUtility.StartCoroutine(TranslateMPLandmarks(handWorldLandmarks.landmarks), this);
                    EditorTextHandler.DisplayMessage("Gesture data received. Please name the gesture and then save", Color.green, messageText);
                    savingGestureContainer.style.display = DisplayStyle.Flex;
                    saveGestureButton.style.display = DisplayStyle.Flex;
                    takeImageButton.style.display = DisplayStyle.None;
                }
                else
                {
                    EditorTextHandler.DisplayMessage("Unable to detect gesture. Please try again", Color.red, messageText);
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
                EditorTextHandler.DisplayMessage($"Unable to find the given camera: {selectedCamera}", Color.red, messageText);
            }
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }

            webCamTexture = new WebCamTexture(HelperConstants.CameraWidth, HelperConstants.CameraHeight);
            webCamTexture.deviceName = selectedCamera;
            webCamTexture.Play();

            if (webCamTexture.isPlaying)
            {
                edCoro = EditorCoroutineUtility.StartCoroutine(UpdateFeed(), this);
                takeImageButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                EditorTextHandler.DisplayMessage($"There was a problem setting up and playing the camera: {selectedCamera}", Color.red, messageText);
            }
        }

        /// <summary>
        /// Cleanup resources when the window is closed.
        /// </summary>
        public void OnDestroy()
        {
            if (webCamTexture == null) return;

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

            textureFrame = new(HelperConstants.CameraWidth, HelperConstants.CameraHeight, TextureFormat.RGBA32);

            // Continue updating the feed until the window is closed
            while (hasFocus)
            {
                Repaint();
                yield return null;
            }
        }

        private void SaveGesture(ClickEvent evt)
        {
            string gestureName = savingGestureTextField.value;
            string gestureCategory = gestureCategoryTextfield.value;

            if (string.IsNullOrEmpty(gestureName))
            {
                EditorTextHandler.DisplayMessage("Please enter a name for the gesture", Color.red, messageText);
                return;
            }

            if (string.IsNullOrEmpty(gestureCategory))
            {
                EditorTextHandler.DisplayMessage("Please enter a category for the gesture", Color.red, messageText);
                return;
            }

            // Normalize the gesture name and category to Title Case (e.g., "My Gesture")
            gestureName = StringHandler.GetNormalizedString(gestureName);
            gestureCategory = StringHandler.GetNormalizedString(gestureCategory);

            var dbHandler = GestureSqliteHandler.Instance();

            if (dbHandler.GestureExists(gestureName))
            {
                EditorTextHandler.DisplayMessage($"{gestureName} already exists, please enter a different name", Color.red, messageText);
                return;
            }

            // Save image locally
            var activeConfig = dbHandler.GetActiveConfiguration();
            var ext = activeConfig.ImageExtension;
            FileHandler.SaveTextureToResources(savingTexture, gestureName, ext);

            // Create queryable gesture info
            QueryableGestureInfo qgi = new QueryableGestureInfo
            {
                GestureName = gestureName,
                IsPredefined = false,
                ImageName = gestureName,
                CategoryName = gestureCategory,
                Landmarks = this.Landmarks
            };

            var distances = GestureRecognizer.GetLandmarkDistances(qgi.Landmarks);
            Distances.Clear();

            foreach (var distance in distances)
            {
                Distances.Add(new LandmarkDistance
                {
                    Distance = distance,
                    LandmarkId = 1, // TODO: REMOVE
                    OtherLandmarkId = 2 // TODO: REMOVE
                });
            }

            qgi.Distances = Distances;
            dbHandler.AddGesture(qgi);

            //  Internal check
            var gestureInfo = dbHandler.GetGestureInfo(gestureName);
            if (gestureInfo.Equals(qgi))
            {
                saveGestureButton.style.display = DisplayStyle.None;
                savingGestureContainer.style.display = DisplayStyle.None;
                takeImageButton.style.display = DisplayStyle.Flex;
                EditorTextHandler.DisplayMessage($"{gestureName} was successfully created! Check Gesture List for more info", Color.green, messageText);
            }
            else
            {
                EditorTextHandler.DisplayMessage($"There was an error saving the gesture {gestureName}. Please review logs", Color.red, messageText);
            }
        }

        private void ResetSaveContainer()
        {
            savingGestureContainer.style.display = DisplayStyle.None;
            savingGestureTextField.value = string.Empty;
            messageText.text = string.Empty;
        }

        private IEnumerator TranslateMPLandmarks(List<MPLandmark> mpLandmarks)
        {
            Landmarks.Clear();

            for (int i = 0; i < mpLandmarks.Count; i++)
            {
                NOVALandmark novaLandmark = new()
                {
                    X = mpLandmarks[i].x,
                    Y = mpLandmarks[i].y,
                    Z = mpLandmarks[i].z
                };

                Landmarks.Add(novaLandmark);
            }

            yield return null;
        }
    }
}
#endif

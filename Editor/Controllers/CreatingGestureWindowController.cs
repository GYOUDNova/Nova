#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using Image = UnityEngine.UIElements.Image;
using NOVALandmark = NOVA.Scripts.Landmark;

namespace NOVA.Scripts
{
    internal enum GestureInputMode
    {
        None,
        ImageMode,
        CameraMode
    }

    public class CreatingGestureWindowController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset createGestureScreenAsset;

        private const string WindowName = "Create a Gesture";
        private const string CameraFeedSelector = "webcam-feed";
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

        /* Camera/Image Settings and Textures */
        private WebCamTexture webCamTexture;
        private Texture2D texture;
        private Texture2D savingTexture;
        private EditorCoroutine edCoro;

        private VisualElement uploadContainer;
        private Texture2D uploadedTexture;
        private bool suppressCameraSelection = false;

        // Flags to track the current mode
        private GestureInputMode currentMode = GestureInputMode.None;

        // Backend handler (no UI inside)
        private NovaMediapipeHandler mp;

        // Landmarks list
        public List<NOVALandmark> Landmarks { get; private set; } = new();

        // Normalized landmarks list (kept as window state for saving/metrics)
        public Mediapipe.NormalizedLandmarkList NormalizedLandmarks { get; private set; } = new();

        // Distances list
        public List<LandmarkDistance> Distances { get; private set; } = new();

        // Angles list
        public List<LandmarkAngle> Angles { get; private set; } = new();

        // Direction List
        public List<LandmarkDirection> Directions { get; private set; } = new();

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

            uploadContainer = root.Q<VisualElement>("UploadContainer");

            var imageSizeHint = uploadContainer.Q<Label>("ImageSizeHint");
            imageSizeHint.text = $"Min {HelperConstants.CameraWidth}x{HelperConstants.CameraHeight}";

            var uploadImageButton = root.Q<Button>("UploadImageButton");
            uploadImageButton.RegisterCallback<ClickEvent>(evt => UploadImage());

            foreach (var device in WebCamTexture.devices)
            {
                dropdownField.choices.Add(device.name);
            }

            messageText = root.Q<Label>(MessageLabelName);

            webCamTexture = new WebCamTexture(HelperConstants.CameraWidth, HelperConstants.CameraHeight);
            texture = new Texture2D(HelperConstants.CameraWidth, HelperConstants.CameraHeight);
            savingTexture = new Texture2D(HelperConstants.CameraWidth, HelperConstants.CameraHeight);

            var image = new Image();
            image.AddToClassList(CameraFeedSelector);
            root.Add(image);

            // Backend handler
            mp = new NovaMediapipeHandler();

            // Add functionality to take image and save
            takeImageButton = root.Q<Button>(TakeImageButtonName);
            takeImageButton.style.display = DisplayStyle.None; // Ensure the button is hidden
            takeImageButton.clicked += () =>
            {
                EditorCoroutineUtility.StartCoroutine(ProcessGestureFromTexture(texture), this);
            };
        }

        private void UploadImage()
        {
            string imagePath = EditorUtility.OpenFilePanel("Select Image", "", "png,jpg,jpeg");

            if (string.IsNullOrEmpty(imagePath))
                return;

            try
            {
                if (uploadedTexture != null)
                {
                    DestroyImmediate(uploadedTexture);
                    uploadedTexture = null;
                }

                // Load the image
                byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
                uploadedTexture = new Texture2D(HelperConstants.CameraWidth, HelperConstants.CameraHeight);

                if (uploadedTexture.LoadImage(imageData))
                {
                    // Check minimum image dimensions
                    if (uploadedTexture.width < HelperConstants.CameraWidth || uploadedTexture.height < HelperConstants.CameraHeight)
                    {
                        EditorTextHandler.DisplayMessage($"Image too small ({uploadedTexture.width}x{uploadedTexture.height}). " +
                                                        $"Minimum size is {HelperConstants.CameraWidth}x{HelperConstants.CameraHeight}.",
                                                        Color.red, messageText);
                        return;
                    }

                    // Resize to match camera dimensions if needed
                    uploadedTexture = NovaMediapipeHandler.ResizeTexture(uploadedTexture, HelperConstants.CameraWidth, HelperConstants.CameraHeight);

                    // Update the display
                    var image = root.Q<Image>();
                    image.image = uploadedTexture;

                    // Switch to image mode
                    EnterImageMode();

                    // Process the uploaded image immediately
                    EditorCoroutineUtility.StartCoroutine(ProcessUploadedImage(), this);
                }
                else
                {
                    EditorTextHandler.DisplayMessage("Failed to load the selected image", Color.red, messageText);
                }
            }
            catch (System.Exception e)
            {
                throw new System.Exception($"Error loading image: {e.Message}", e);
            }
        }

        private IEnumerator ProcessUploadedImage()
        {
            const string uploadSuccessMessage = "Gesture data received from uploaded image. Please name the gesture and then save";
            yield return ProcessGestureFromTexture(uploadedTexture, uploadSuccessMessage);
        }

        private IEnumerator ProcessGestureFromTexture(Texture2D sourceTexture, string successMessage = "Gesture data received. Please name the gesture and then save")
        {
            // Ensure backend is ready for this texture size
            yield return mp.InitializeIfNeeded(sourceTexture.width, sourceTexture.height);

            ResetSaveContainer();

            // Copy source texture to saving texture for later use (UI concern)
            if (savingTexture == null || savingTexture.width != sourceTexture.width || savingTexture.height != sourceTexture.height)
            {
                if (savingTexture != null)
                {
                    DestroyImmediate(savingTexture);
                }

                savingTexture = new Texture2D(sourceTexture.width, sourceTexture.height, sourceTexture.format, false);
            }

            Graphics.CopyTexture(sourceTexture, savingTexture);

            // Backend detection
            NovaHandResult res = null;
            yield return mp.TryGenerateLandmarks(sourceTexture, r => res = r);

            if (res != null && res.Success)
            {
                // Update window state
                NormalizedLandmarks = res.Normalized;
                Landmarks = res.Landmarks;

                // UI success
                EditorTextHandler.DisplayMessage(successMessage, Color.green, messageText);
                savingGestureContainer.style.display = DisplayStyle.Flex;
                saveGestureButton.style.display = DisplayStyle.Flex;

                // Hide the trigger buttons based on current mode
                if (currentMode.Equals(GestureInputMode.CameraMode))
                {
                    takeImageButton.style.display = DisplayStyle.None;
                }
                else if (currentMode.Equals(GestureInputMode.ImageMode))
                {
                    uploadContainer.style.display = DisplayStyle.None;
                }
            }
            else
            {
                string errorMessage = currentMode.Equals(GestureInputMode.ImageMode) ?
                    "Unable to detect gesture in uploaded image. Please try a different image with a clear hand gesture!" :
                    "Unable to detect gesture. Please try again with a clear hand gesture";
                EditorTextHandler.DisplayMessage(errorMessage, Color.red, messageText);
            }
        }

        private void EnterImageMode()
        {
            currentMode = GestureInputMode.ImageMode;

            // Hide camera controls
            dropdownField.style.display = DisplayStyle.None;
            takeImageButton.style.display = DisplayStyle.None;

            // Stop camera if running
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
                if (edCoro != null)
                {
                    EditorCoroutineUtility.StopCoroutine(edCoro);
                }
            }

            // Show upload button (keep it visible for switching images)
            uploadContainer.style.display = DisplayStyle.Flex;
        }

        private void EnterCameraMode()
        {
            currentMode = GestureInputMode.CameraMode;

            // Show camera controls
            dropdownField.style.display = DisplayStyle.Flex;
            uploadContainer.style.display = DisplayStyle.None;

            // Clear uploaded texture
            if (uploadedTexture != null)
            {
                DestroyImmediate(uploadedTexture);
                uploadedTexture = null;
            }

            var image = root.Q<Image>();
            image.image = texture;
        }

        /// <summary>
        /// Callback for when a camera is picked in the dropdown
        /// </summary>
        private void OnCameraSelected(string selectedCamera)
        {
            // Prevent camera selection in image mode
            if (currentMode.Equals(GestureInputMode.ImageMode)) { return; }

            // Prevent clearing the dropdown from triggering this method
            if (suppressCameraSelection)
            {
                suppressCameraSelection = false;
                return;
            }

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
                EnterCameraMode(); // Switch to camera mode
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
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
            webCamTexture = null;

            if (edCoro != null)
            {
                EditorCoroutineUtility.StopCoroutine(edCoro);
                edCoro = null;
            }

            mp?.Dispose();
            mp = null;
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
            // Initialize backend if not already done
            yield return mp.InitializeIfNeeded(HelperConstants.CameraWidth, HelperConstants.CameraHeight);

            // Continue updating the feed until the window is closed
            while (hasFocus && currentMode.Equals(GestureInputMode.CameraMode))
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

            var distances = GestureRecognizer.GetNormalizedLandmarkDistances(NormalizedLandmarks);
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

            var directions = GestureRecognizer.GetGestureDirections(NormalizedLandmarks);
            Directions.Clear();

            foreach (var direction in directions)
            {
                Directions.Add(new LandmarkDirection
                {
                    Direction = direction,
                    LandmarkId = 1, // TODO: REMOVE
                    OtherLandmarkId = 2 // TODO: REMOVE
                });
            }

            qgi.Distances = Distances;
            qgi.Direction = Directions;
            dbHandler.AddGesture(qgi);

            //  Internal check
            var gestureInfo = dbHandler.GetGestureInfo(gestureName);
            if (gestureInfo.Equals(qgi))
            {
                saveGestureButton.style.display = DisplayStyle.None;
                savingGestureContainer.style.display = DisplayStyle.None;
                takeImageButton.style.display = DisplayStyle.Flex;
                ResetToNormalMode();
                EditorTextHandler.DisplayMessage($"{gestureName} was successfully created! Check Gesture List for more info", Color.green, messageText);
            }
            else
            {
                EditorTextHandler.DisplayMessage($"There was an error saving the gesture {gestureName}. Please review logs", Color.red, messageText);
            }
        }

        private void ResetToNormalMode()
        {
            // Stop camera if running
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }

            if (edCoro != null)
            {
                EditorCoroutineUtility.StopCoroutine(edCoro);
                edCoro = null;
            }

            // Clear the image display
            var image = root.Q<Image>();
            image.image = null;

            // Clear uploaded texture
            if (uploadedTexture != null)
            {
                DestroyImmediate(uploadedTexture);
                uploadedTexture = null;
            }

            // Reset UI elements
            saveGestureButton.style.display = DisplayStyle.None;
            savingGestureContainer.style.display = DisplayStyle.None;
            takeImageButton.style.display = DisplayStyle.None;
            uploadContainer.style.display = DisplayStyle.Flex;
            dropdownField.style.display = DisplayStyle.Flex;

            // Clear text fields
            savingGestureTextField.value = string.Empty;
            gestureCategoryTextfield.value = string.Empty;

            // Reset dropdown selection only in camera mode
            if (currentMode.Equals(GestureInputMode.CameraMode))
            {
                suppressCameraSelection = true; // Prevents triggering OnCameraSelected
                dropdownField.value = string.Empty;
            }

            currentMode = GestureInputMode.None;
        }

        private void ResetSaveContainer()
        {
            savingGestureContainer.style.display = DisplayStyle.None;
            savingGestureTextField.value = string.Empty;
            messageText.text = string.Empty;
        }
    }
}
#endif

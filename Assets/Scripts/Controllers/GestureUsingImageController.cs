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
    public class GestureUsingImageController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset gestureUsingImageScreenAsset;

        private VisualElement root;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 875;
        private const string Title = "Create a Gesture";

        private Button testButton;


        /*Mediapipe Stuff*/

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



        [SerializeField]
        private Texture textureImage;

        [MenuItem("Window/UI Toolkit/Create Gesture Using Image Screen")]
        public static void SetupAndShowWindow()
        {
            GestureUsingImageController gestureController = GetWindow<GestureUsingImageController>();
            gestureController.minSize = new Vector2(MinWindowLength, MinWindowHeight);
            gestureController.maxSize = new Vector2(MinWindowLength + 1, MinWindowHeight + 1);
            gestureController.titleContent = new GUIContent(Title);
        }

        public void CreateGUI()
        {
            root = gestureUsingImageScreenAsset.CloneTree();
            rootVisualElement.Clear(); 
            rootVisualElement.Add(root);

            testButton = root.Q<Button>("Test");
            testButton.RegisterCallback<ClickEvent>(evt => OnTestButtonClick(evt));
        }

        private void OnTestButtonClick(ClickEvent evt)
        {
            EditorCoroutineUtility.StartCoroutine(TakeImage(), this);
        }

        private IEnumerator TakeImage()
        {
            Debug.Log("Test");
            Config.RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE;
            AssetLoader.Provide(new StreamingAssetsResourceManager());
            yield return AssetLoader.PrepareAssetAsync(Config.ModelPath);

            var result = HandLandmarkerResult.Alloc(2);
            var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);
            var options = Config.GetHandLandmarkerOptions(null);
            taskApi = HandLandmarker.CreateFromOptions(options);

            textureFrame = new(textureImage.width, textureImage.height, TextureFormat.RGBA32);
            textureFrame.ReadTextureOnCPU(textureImage);
            mpImage = textureFrame.BuildCPUImage();
            if(taskApi.TryDetect(mpImage, imageProcessingOptions, ref result))
            {
                Debug.Log(result);
                Debug.Log("Worked");
            }
            else
            {
                Debug.Log("You Suck");
            }
        }

    }
}

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public class CreatingGestureWindowController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset createGestureScreenAsset;

        private VisualElement root;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 850;
        private const string WindowTitle = "Create a Gesture";

        [MenuItem("Window/UI Toolkit/Creating Gesture Screen")]
        public static void SetupAndShowWindow()
        {
            CreatingGestureWindowController createGestureController = GetWindow<CreatingGestureWindowController>();
            createGestureController.titleContent = new GUIContent(WindowTitle);
            createGestureController.maxSize = new Vector2(MinWindowLength, MinWindowHeight);
            createGestureController.minSize = createGestureController.maxSize;
        }

        public void CreateGUI()
        {
            root = createGestureScreenAsset.CloneTree();
            rootVisualElement.Add(root);
        }
    }
}
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    /// <summary>
    /// This class is the controller for the main screen of the settings window
    /// </summary>
    [InitializeOnLoad]
    public class MainEditorWindowController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset mainEditorTreeAsset;

        private VisualElement root;

        private Button creatingGestureButton;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 850;
        private const string WindowTitle = "NOVA Configuration";
        private const string HeaderTitle = "Hand Gesture Configuration";

        static MainEditorWindowController()
        {
            EditorApplication.delayCall += SetupAndShowWindow;
        }

        /// <summary>
        /// Called when the window is opened
        /// </summary>
        [MenuItem("Window/UI Toolkit/EditorWindowController")]
        public static void SetupAndShowWindow()
        {
            MainEditorWindowController mainWindowController = GetWindow<MainEditorWindowController>();
            mainWindowController.titleContent = new GUIContent(WindowTitle);
            mainWindowController.maxSize = new Vector2(MinWindowLength, MinWindowHeight);
            mainWindowController.minSize = mainWindowController.maxSize;
        }

        /// <summary>
        /// Creates the GUI for the main editor application
        /// </summary>
        public void CreateGUI()
        {          
            root = mainEditorTreeAsset.CloneTree();
            rootVisualElement.Add(root);

            Label label = root.Q<Label>("TitleLabel");
            label.text = HeaderTitle;

            creatingGestureButton = root.Q<Button>("CreateAGestureButton");
            creatingGestureButton.RegisterCallback<ClickEvent>(OpenCreateGestureScreen);
        }

        private void OpenCreateGestureScreen(ClickEvent evt)
        {
            CreatingGestureWindowController.SetupAndShowWindow();
        }
    }
}
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
        private Button addingCombinationButton;
        private Button gestureListButton;
        private Button settingsCalibrationButton;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 850;
        private const string Title = "NOVA Configuration";

        static MainEditorWindowController()
        {
            EditorApplication.delayCall += SetupAndShowWindow;
        }

        /// <summary>
        /// Called when the window is opened
        /// </summary>
        [MenuItem("Window/UI Toolkit/Main Screen")]
        public static void SetupAndShowWindow()
        {
            MainEditorWindowController mainWindowController = GetWindow<MainEditorWindowController>();
            mainWindowController.titleContent = new GUIContent(Title);
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
            label.text = Title;

            creatingGestureButton = root.Q<Button>("CreateAGestureButton");
            creatingGestureButton.RegisterCallback<ClickEvent>(evt => OpenSubScreen(evt, SubScreen.CreateGesture));

            addingCombinationButton = root.Q<Button>("AddACombinationButton");
            addingCombinationButton.RegisterCallback<ClickEvent>(evt => OpenSubScreen(evt, SubScreen.AddingCombination));

            gestureListButton = root.Q<Button>("GestureListButton");
            gestureListButton.RegisterCallback<ClickEvent>(evt => OpenSubScreen(evt, SubScreen.GestureList));

            settingsCalibrationButton = root.Q<Button>("SettingsCalibrationButton");
            settingsCalibrationButton.RegisterCallback<ClickEvent>(evt => OpenSubScreen(evt, SubScreen.SettingsCalibration));
        }

        private void OpenSubScreen(ClickEvent evt, SubScreen subScreenToOpen)
        {
            switch (subScreenToOpen)
            {
                case SubScreen.CreateGesture:
                    CreatingGestureWindowController.SetupAndShowWindow();
                    break;
                case SubScreen.AddingCombination:
                    AddingCombinationWindowController.SetupAndShowWindow();
                    break;
                case SubScreen.GestureList:
                    GestureListWindowController.SetupAndShowWindow();
                    break;
                case SubScreen.SettingsCalibration:
                    SettingsCalibrationWindowController.SetupAndShowWindow();
                    break;
                default:
                    break;
            }
        }
    }
}
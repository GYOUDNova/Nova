#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public class SettingsCalibrationWindowController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset settingsScreenAsset;

        private VisualElement root;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 850;
        private const string Title = "Settings & Calibration";

        [MenuItem("Window/UI Toolkit/Settings Calibration")]
        public static void SetupAndShowWindow()
        {
            SettingsCalibrationWindowController settingsController = GetWindow<SettingsCalibrationWindowController>();
            settingsController.titleContent = new GUIContent(Title);
            settingsController.maxSize = new Vector2(MinWindowLength, MinWindowHeight);
            settingsController.minSize = settingsController.maxSize;
        }

        public void CreateGUI()
        {
            root = settingsScreenAsset.CloneTree();
            rootVisualElement.Add(root);

            Label label = root.Q<Label>("TitleLabel");
            label.text = Title;
        }
    }
}

#endif

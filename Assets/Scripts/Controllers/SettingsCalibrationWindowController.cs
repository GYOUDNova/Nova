#if UNITY_EDITOR

using System;
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
        private Button saveConfigurationButton;
        private Label errorText;

        /*Configuration fields*/
        private TextField configuratrionName;
        private SliderInt gamma;
        private FloatField chainTimer;
        private FloatField landmarkTolerance;
        private DropdownField dropdownField;

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

            errorText = root.Q<Label>("ErrorLabel");
            configuratrionName = root.Q<TextField>("ConfigurationName");
            gamma = root.Q<SliderInt>("Gamma");
            chainTimer = root.Q<FloatField>("ChainTimer");
            landmarkTolerance = root.Q<FloatField>("LandmarkTolerance");

            dropdownField = root.Q<DropdownField>("ImageType");
            dropdownField.value = GestureImageExtension.Jpeg.ToString();
            foreach (var extension in Enum.GetValues(typeof(GestureImageExtension)))
            {
                dropdownField.choices.Add(extension.ToString());
            }

            saveConfigurationButton = root.Q<Button>("SaveConfigurationButton");
            saveConfigurationButton.RegisterCallback<ClickEvent>(evt => OnSaveConfiguration(evt));
        }

        private void OnSaveConfiguration(ClickEvent evt)
        {
            if (String.IsNullOrEmpty(configuratrionName.text))
            {
                TextHandler.DisplayMessage("Configuration must include a name", Color.red, errorText);
            }
            else
            {
                Enum.TryParse(dropdownField.value.ToString(), out GestureImageExtension extension);

                Configuration configuration = new();
                configuration.Name = configuratrionName.text;
                configuration.Gamma = gamma.value;
                configuration.ChainTimer = chainTimer.value;
                configuration.LandmarkTolerance = landmarkTolerance.value;
                configuration.ImageExtension = extension;


                try
                {
                    var handler = GestureSqliteHandler.Instance();
                    var currAvtive = handler.GetActiveConfiguration();

                    //TODO: Function that swaps actives

                    handler.AddItemByName(configuration, configuration.Name);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
#endif

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
        private Label statusText;

        /*Configuration fields*/
        private TextField configuratrionName;
        private SliderInt gamma;
        private FloatField chainTimer;
        private FloatField landmarkTolerance;
        private DropdownField dropdownField;
        private RadioButtonGroup radioButtonGroup;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 850;
        private const int RadioButtonYes = 0;
        private const int RadioButtonNo = 0;
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

            statusText = root.Q<Label>("StatusLabel");
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

            radioButtonGroup = root.Q<RadioButtonGroup>("SetActiveConfigurationButtons");

            saveConfigurationButton = root.Q<Button>("SaveConfigurationButton");
            saveConfigurationButton.RegisterCallback<ClickEvent>(evt => OnSaveConfiguration(evt));
        }

        private void OnSaveConfiguration(ClickEvent evt)
        {
            if (String.IsNullOrEmpty(configuratrionName.text))
            {
                TextHandler.DisplayMessage("Configuration must include a name", Color.red, statusText);
                return;
            }
            if (radioButtonGroup.value == -1)
            {
                TextHandler.DisplayMessage("Please choose whether or not to set this confiuration as active", Color.red, statusText);
                return;
            }

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
                if (radioButtonGroup.value == RadioButtonYes)
                {
                    var activeConfiguration = handler.GetActiveConfiguration();
                    handler.SetCurrentActiveConfigToFalse(activeConfiguration);
                    configuration.Active = true;
                }
                else if (radioButtonGroup.value == RadioButtonNo)
                {
                    configuration.Active = false;
                }
                handler.AddItemByName(configuration, configuration.Name);
                TextHandler.DisplayMessage($"Configuration: {configuration.Name} was sucsesfully added!", Color.green, statusText);
            }
            catch (Exception exception) when (exception is ItemAlreadyExistsException || exception is ItemNotFoundException || exception is TableNotFoundException)
            {
                TextHandler.DisplayMessage(exception.Message, Color.red, statusText);
            }
        }
    }
}
#endif

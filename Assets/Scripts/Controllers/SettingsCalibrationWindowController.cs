#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
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
        private Label creatingConfigStatusText;
        private Label editingConfigStatusText;

        /*Configuration fields*/
        private Label titleLabel;
        private TextField configuratrionName;
        private SliderInt gamma;
        private FloatField chainTimer;
        private FloatField landmarkTolerance;
        private DropdownField imageExtensionsDropdown;
        private DropdownField configurationsDropdown;
        private RadioButtonGroup radioButtonGroup;
        private Button setActiveConfigurationButton;
        private Button deleteConfigurationButton;

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
            SetupUI();
        }

        private void SetupUI()
        {
            //Setup root
            root = settingsScreenAsset.CloneTree();
            rootVisualElement.Add(root);

            //Setup all UI references
            titleLabel = root.Q<Label>("TitleLabel");
            creatingConfigStatusText = root.Q<Label>("CreatingConfigStatusLabel");
            editingConfigStatusText = root.Q<Label>("EditingConfigStatusLabel");
            configuratrionName = root.Q<TextField>("ConfigurationName");
            gamma = root.Q<SliderInt>("Gamma");
            chainTimer = root.Q<FloatField>("ChainTimer");
            landmarkTolerance = root.Q<FloatField>("LandmarkTolerance");
            imageExtensionsDropdown = root.Q<DropdownField>("ImageType");
            radioButtonGroup = root.Q<RadioButtonGroup>("SetActiveConfigurationButtons");
            saveConfigurationButton = root.Q<Button>("SaveConfigurationButton");
            configurationsDropdown = root.Q<DropdownField>("ListOfConfigs");
            setActiveConfigurationButton = root.Q<Button>("MakeActiveButton");
            deleteConfigurationButton = root.Q<Button>("DeleteConfigurationButton");


            //Setup events + UI data
            saveConfigurationButton.RegisterCallback<ClickEvent>(evt => OnSaveConfiguration(evt));
            setActiveConfigurationButton.RegisterCallback<ClickEvent>(evt => OnChangeActiveConfiguration(evt));
            deleteConfigurationButton.RegisterCallback<ClickEvent>(evt => OnDeleteConfiguration(evt));
            titleLabel.text = Title;
            imageExtensionsDropdown.value = GestureImageExtension.Jpeg.ToString();
            foreach (var extension in Enum.GetValues(typeof(GestureImageExtension)))
            {
                imageExtensionsDropdown.choices.Add(extension.ToString());
            }
            RefreshConfigurationDropdown();
        }

        private void RefreshConfigurationDropdown()
        {
            var handler = GestureSqliteHandler.Instance();
            List<Configuration> allConfigurations = handler.GetObjects<Configuration>();
            if (allConfigurations.Any())
            {
                configurationsDropdown.choices.Clear();
                foreach (var configuration in allConfigurations)
                {
                    configurationsDropdown.choices.Add(configuration.Name);
                }
                configurationsDropdown.index = 0;
            }
        }

        private void OnSaveConfiguration(ClickEvent evt)
        {
            if (String.IsNullOrEmpty(configuratrionName.text))
            {
                TextHandler.DisplayMessage("Configuration must include a name", Color.red, creatingConfigStatusText);
                return;
            }
            if (radioButtonGroup.value == -1)
            {
                TextHandler.DisplayMessage("Please choose whether or not to set this confiuration as active", Color.red, creatingConfigStatusText);
                return;
            }

            Enum.TryParse(imageExtensionsDropdown.value.ToString(), out GestureImageExtension extension);
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
                TextHandler.DisplayMessage($"Configuration: {configuration.Name} was sucsesfully added!", Color.green, creatingConfigStatusText);
            }
            catch (Exception exception) when (exception is ItemAlreadyExistsException || exception is ItemNotFoundException || exception is TableNotFoundException)
            {
                TextHandler.DisplayMessage(exception.Message, Color.red, creatingConfigStatusText);
            }
        }

        private void OnDeleteConfiguration(ClickEvent evt)
        {
            string configurationName = configurationsDropdown.value;
            var handler = GestureSqliteHandler.Instance();
            var configuration = handler.GetObjectByName<Configuration>(configurationName);

            if (configuration is null)
            {
                TextHandler.DisplayMessage($"No configuration named: {configurationName} exists", Color.red, editingConfigStatusText);
                return;
            }

            if (configuration.Active)
            {
                TextHandler.DisplayMessage($"Cannot delete configuration while it is active. Please make another one active", Color.red, editingConfigStatusText);
                return;
            }
            handler.DeleteConfiguration(configurationName);
            TextHandler.DisplayMessage($"Configuration: {configurationName} was sucsesfully deleted!", Color.green, editingConfigStatusText);
            RefreshConfigurationDropdown();
        }

        private void OnChangeActiveConfiguration(ClickEvent evt)
        {

        }
    }
}
#endif

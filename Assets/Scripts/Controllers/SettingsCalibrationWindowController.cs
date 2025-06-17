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

        /*All fields from UI*/
        private VisualElement root;

        private Label creatingConfigStatusText;
        private Label editingConfigStatusText;
        private Label titleLabel;

        private Button saveConfigurationButton;
        private Button setActiveConfigurationButton;
        private Button deleteConfigurationButton;

        private TextField configuratrionName;

        private SliderInt gamma;

        private FloatField chainTimer;
        private FloatField landmarkTolerance;

        private DropdownField imageExtensionsDropdown;
        private DropdownField configurationsDropdown;

        private RadioButtonGroup radioButtonGroup;
        private const int RadioButtonYes = 0;
        private const int RadioButtonNo = 0;

        private Label gammaValueDisplay;

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
            SetupUIReferences();
            SetupUIEvents();
            RefreshConfigurationDropdown();
        }

        private void SetupUIReferences()
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
            gammaValueDisplay = root.Q<Label>("GammaValueDisplay");
        }

        private void SetupUIEvents()
        {
            //Setup events + UI data
            gamma.RegisterValueChangedCallback(evt => { gammaValueDisplay.text = $"{evt.newValue}%"; });

            saveConfigurationButton.RegisterCallback<ClickEvent>(evt => OnSaveConfiguration(evt));
            setActiveConfigurationButton.RegisterCallback<ClickEvent>(evt => OnChangeActiveConfiguration(evt));
            deleteConfigurationButton.RegisterCallback<ClickEvent>(evt => OnDeleteConfiguration(evt));

            titleLabel.text = Title;

            imageExtensionsDropdown.value = GestureImageExtension.Jpeg.ToString();
            foreach (var extension in Enum.GetValues(typeof(GestureImageExtension)))
            {
                imageExtensionsDropdown.choices.Add(extension.ToString());
            }
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
            if (!CheckValidInput() || !Enum.TryParse(imageExtensionsDropdown.value.ToString(), out GestureImageExtension extension)) { return; }

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
                    handler.SetActivePropertyToFalse(activeConfiguration);
                    configuration.Active = true;
                }
                else if (radioButtonGroup.value == RadioButtonNo)
                {
                    configuration.Active = false;
                }
                handler.AddItemByName(configuration, configuration.Name);
                TextHandler.DisplayMessage($"Configuration: {configuration.Name} was sucsesfully added!", Color.green, creatingConfigStatusText);
                RefreshConfigurationDropdown();
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
            string configurationName = configurationsDropdown.value;
            var handler = GestureSqliteHandler.Instance();
            var activeConfiguration = handler.GetActiveConfiguration();
            var configurationToBeActive = handler.GetObjectByName<Configuration>(configurationName);

            if (configurationToBeActive.Active)
            {
                TextHandler.DisplayMessage($"Cofiguration {configurationToBeActive.Name} is already active", Color.red, editingConfigStatusText);
                return;
            }
            handler.SetActivePropertyToFalse(activeConfiguration);
            handler.SetActivePropertyToTrue(configurationToBeActive);
            TextHandler.DisplayMessage($"Active configuration was sucsessfully changed to {configurationToBeActive.Name}", Color.green, editingConfigStatusText);
        }

        private bool CheckValidInput()
        {
            string errorMessage = String.Empty;
            if (String.IsNullOrEmpty(configuratrionName.text) || configuratrionName.text.Length > 25)
            {
                errorMessage = "Configuration must include a name and must be between 1-25 characters long";
            }
            else if (chainTimer.value < 1 || chainTimer.value > 10)
            {
                errorMessage = "Chain timer must be a value between 1-10 (in seconds)";
            }
            else if (landmarkTolerance.value < 1 || landmarkTolerance.value > 10)
            {
                errorMessage = "Landmark tolerance must be a value between 1-10";
            }
            else if (radioButtonGroup.value == -1)
            {
                errorMessage = "Please choose whether or not to set this confiuration as active";
            }

            if (errorMessage != String.Empty)
            {
                TextHandler.DisplayMessage(errorMessage, Color.red, creatingConfigStatusText);
                return false;
            }
            return true;
        }
    }
}
#endif

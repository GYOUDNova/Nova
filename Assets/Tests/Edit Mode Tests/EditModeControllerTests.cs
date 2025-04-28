using System.Collections;
using NOVA.Scripts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

using UnityEngine.UIElements;


public class EditModeControllerTests
{
    /// <summary>
    /// This test ensures that when an instance of the window is created/opened, it is actually created and opened.
    /// </summary>
    [UnityTest]
    public IEnumerator EditorWindow_OpenWindow_EnsureWindowIsOpen()
    {
        //Arrange
        bool result = false;

        //Act
        EditorWindow.GetWindow<MainEditorWindowController>();
        yield return null;
        result = EditorWindow.HasOpenInstances<MainEditorWindowController>();

        //Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Test ensures that when the Add a Combination button is clicked in the editor, that the menu opens
    /// </summary>
    [UnityTest]
    public IEnumerator EditorWindow_OpenAddCombinationWindow_SubWindowOpens()
    {
        //Arrange
        Button button = new();
        bool buttonClicked = false;
        var mainWindow = EditorWindow.GetWindow<MainEditorWindowController>();
        var subMenu = EditorWindow.GetWindow<AddingCombinationWindowController>();

        //Act   
        button = mainWindow.rootVisualElement.Q<Button>("AddACombinationButton");

        TestUtils.ClickOnButton(button);
        buttonClicked = EditorWindow.HasOpenInstances<AddingCombinationWindowController>();
        yield return null;

        //Assert
        Assert.IsTrue(buttonClicked);
    }

    /// <summary>
    /// Test ensures that when the Create Gesture button is clicked in the editor, that the menu opens
    /// </summary>
    [UnityTest]
    public IEnumerator EditorWindow_OpenCreateGestureWindow_SubWindowOpens()
    {
        //Arrange
        Button button = new();
        bool buttonClicked = false;
        var mainWindow = EditorWindow.GetWindow<MainEditorWindowController>();
        var subMenu = EditorWindow.GetWindow<CreatingGestureWindowController>();

        //Act   
        button = mainWindow.rootVisualElement.Q<Button>("CreateAGestureButton");

        TestUtils.ClickOnButton(button);
        buttonClicked = EditorWindow.HasOpenInstances<CreatingGestureWindowController>();
        yield return null;

        //Assert
        Assert.IsTrue(buttonClicked);
    }

    /// <summary>
    /// Test ensures that when the Add a Gesture List button is clicked in the editor, that the menu opens
    /// </summary>
    [UnityTest]
    public IEnumerator EditorWindow_OpenGestureListWindow_SubWindowOpens()
    {
        //Arrange
        Button button = new();
        bool buttonClicked = false;
        var mainWindow = EditorWindow.GetWindow<MainEditorWindowController>();
        var subMenu = EditorWindow.GetWindow<GestureListWindowController>();

        //Act   
        button = mainWindow.rootVisualElement.Q<Button>("GestureListButton");

        TestUtils.ClickOnButton(button);
        buttonClicked = EditorWindow.HasOpenInstances<GestureListWindowController>();
        yield return null;

        //Assert
        Assert.IsTrue(buttonClicked);
    }

    /// <summary>
    /// Test ensures that when the Settings & Calibration button is clicked in the editor, that the menu opens
    /// </summary>
    [UnityTest]
    public IEnumerator EditorWindow_OpenSettingsWindow_SubWindowOpens()
    {
        //Arrange
        Button button = new();
        bool buttonClicked = false;
        var mainWindow = EditorWindow.GetWindow<MainEditorWindowController>();
        var subMenu = EditorWindow.GetWindow<SettingsCalibrationWindowController>();

        //Act   
        button = mainWindow.rootVisualElement.Q<Button>("SettingsCalibrationButton");

        TestUtils.ClickOnButton(button);
        buttonClicked = EditorWindow.HasOpenInstances<SettingsCalibrationWindowController>();
        yield return null;

        //Assert
        Assert.IsTrue(buttonClicked);
    }
}

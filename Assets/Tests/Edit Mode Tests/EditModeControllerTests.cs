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
    /// Test ensures that when a button on the menu is clicked, it is actually triggered.
    /// </summary>
    [UnityTest]
    public IEnumerator EditorWindow_ClickButton_ButtonWorks()
    {
        //Arrange
        Button button = new();
        bool buttonClicked = false;
        var window = EditorWindow.GetWindow<MainEditorWindowController>();

        //Act   
        button = window.rootVisualElement.Q<Button>("CreateAGestureButton");
        button.clicked += () => buttonClicked = true;

        TestUtils.ClickOnButton(button);
        yield return null;

        //Assert
        Assert.IsTrue(buttonClicked);
    }
}
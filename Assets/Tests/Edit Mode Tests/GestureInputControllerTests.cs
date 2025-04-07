using System.Collections;
using System.Collections.Generic;
using NOVA.Scripts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

public class GestureInputControllerTests
{
    private GestureInputController _gestureInputController;

    private string _outputText;

    //set up gesture input controller
    [SetUp]
    public void Setup()
    {
        // Create a new GameObject and add the GestureInputController component
        GameObject gameObject = new GameObject();
        _gestureInputController = gameObject.AddComponent<GestureInputController>();

        // Initialize the GestureDictionary with some test data
        _gestureInputController.gestureDictionary = new GestureDictionary
        {
            gestureInputs = new List<GestureInput>
            {
                new GestureInput { gestureName = "TestGesture", gestureEvent = new UnityEvent() }
            }
        };

        // Initialize the GestureInputMapping dictionary
        _gestureInputController.gestureInputMapping = _gestureInputController.gestureDictionary.ToDictionary();

        // Set up a listener for the UnityEvent to capture output
        _gestureInputController.gestureDictionary.gestureInputs[0].gestureEvent.AddListener(() => _outputText = "TestGesture Activated");

        // default value for output text
        _outputText = string.Empty;
    }


    //
    [UnityTest]
    public IEnumerator GestureInputControllerTest_ActivateTestGesture()
    {
        // Arrange
        string gestureName = "TestGesture";

        // Act
        _gestureInputController.ActivateGestureInput(gestureName);

        // Wait for the UnityEvent to be invoked
        yield return null;

        // Assert
        Assert.AreEqual("TestGesture Activated", _outputText, "The gesture input was not activated correctly.");
    }
}

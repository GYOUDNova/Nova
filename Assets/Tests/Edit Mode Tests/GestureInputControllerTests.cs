using System.Collections;
using System.Collections.Generic;
using NOVA.Scripts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

public class GestureInputControllerTests
{
    private GestureInputController gestureInputController;

    private string outputText;

    //set up gesture input controller
    [SetUp]
    public void Setup()
    {
        // Create a new GameObject and add the GestureInputController component
        GameObject gameObject = new GameObject();
        gestureInputController = gameObject.AddComponent<GestureInputController>();

        // Initialize the GestureDictionary with some test data
        gestureInputController.GestureDictionary = new GestureDictionary
        {
            GestureInputs = new List<GestureInput>
            {
                new GestureInput { GestureName = "TestGesture", GestureEvent = new UnityEvent() }
            }
        };

        // Initialize the GestureInputMapping dictionary
        gestureInputController.GestureInputMapping = gestureInputController.GestureDictionary.ToDictionary();

        // Set up a listener for the UnityEvent to capture output
        gestureInputController.GestureDictionary.GestureInputs[0].GestureEvent.AddListener(() => outputText = "TestGesture Activated");

        // default value for output text
        outputText = string.Empty;
    }


    //
    [UnityTest]
    public IEnumerator GestureInputControllerTest_ActivateTestGesture()
    {
        // Arrange
        string gestureName = "TestGesture";

        // Act
        gestureInputController.ActivateGestureInput(gestureName);

        // Wait for the UnityEvent to be invoked
        yield return null;

        // Assert
        Assert.AreEqual("TestGesture Activated", outputText, "The gesture input was not activated correctly.");
    }
}

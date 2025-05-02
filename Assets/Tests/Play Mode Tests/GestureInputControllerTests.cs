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

        gestureInputController.GestureChainDictionary = new GestureChainDictionary
        {
            GestureChainInputs = new List<GestureChainInput>
            {
                new GestureChainInput { GestureChainNames = new List<string> { "TestGesture" , "TestGesture2"}, GestureEvent = new UnityEvent() }
            }
        };

        // Initialize the GestureInputMapping dictionary
        gestureInputController.SingleGestureInputMapping = gestureInputController.GestureDictionary.ToDictionary();

        gestureInputController.GestureChainMapping = gestureInputController.GestureChainDictionary.ToDictionary();

        gestureInputController.ResetLongestChainLength();

        // Set up a listener for the UnityEvent to capture output
        gestureInputController.GestureDictionary.GestureInputs[0].GestureEvent.AddListener(() => outputText = "TestGesture Activated");

        // Set up a listener for the UnityEvent to capture output for chain
        gestureInputController.GestureChainDictionary.GestureChainInputs[0].GestureEvent.AddListener(() => outputText = "TestGestureChain Activated");

        gestureInputController.GestureChainMapping["TestGestureTestGesture2"].AddListener(() => outputText = "TestGestureChain Activated");

        gestureInputController.SingleGestureInputMapping["TestGesture"].AddListener(() => outputText = "TestGesture Activated");

        // default value for output text
        outputText = string.Empty;
    }


    //
    [UnityTest]
    public IEnumerator GestureInputControllerTest_ActivateTestGesture()
    {
        // Arrange
        string GestureName = "TestGesture";

        // Act
        gestureInputController.ActivateGestureInput(GestureName);

        // Wait for the UnityEvent to be invoked
        yield return null;

        // Assert
        Assert.AreEqual("TestGesture Activated", outputText, "The gesture input was not activated correctly.");
    }

    [UnityTest]
    public IEnumerator GestureInputControllerTest_ActivateTestGestureChain()
    {
        // Arrange
        string gesture1Name = "TestGesture";
        string gesture2Name = "TestGesture2";

        // Act
        gestureInputController.AddGestureToChain(gesture1Name);
        gestureInputController.AddGestureToChain(gesture2Name);

        // Wait for 2 seconds
        yield return new WaitForSeconds(3f);

        // Assert
        Assert.AreEqual("TestGestureChain Activated", outputText, "The gesture input was not activated correctly.");
    }

    [UnityTest]
    public IEnumerator GestureInputControllerTest_ActivateTestGestureChainToSingle()
    {
        // Arrange
        string gesture1Name = "TestGesture";
        string gesture2Name = "TestGesture2";

        // Act
        gestureInputController.AddGestureToChain(gesture1Name);
        gestureInputController.AddGestureToChain(gesture2Name);
        gestureInputController.AddGestureToChain(gesture1Name);

        // Wait for 2 seconds
        yield return new WaitForSeconds(2f);

        // Assert
        Assert.AreEqual("TestGesture Activated", outputText, "The gesture input was not activated correctly.");
    }
}

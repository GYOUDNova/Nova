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

        _gestureInputController.gestureChainDictionary = new GestureChainDictionary
        {
            gestureChainInputs = new List<GestureChainInput>
            {
                new GestureChainInput { gestureChainNames = new List<string> { "TestGesture" , "TestGesture2"}, gestureEvent = new UnityEvent() }
            }
        };

        // Initialize the GestureInputMapping dictionary
        _gestureInputController.singleGestureInputMapping = _gestureInputController.gestureDictionary.ToDictionary();

        _gestureInputController.gestureChainMapping = _gestureInputController.gestureChainDictionary.ToDictionary();

        _gestureInputController.ResetLongestChainLength();

        // Set up a listener for the UnityEvent to capture output
        _gestureInputController.gestureDictionary.gestureInputs[0].gestureEvent.AddListener(() => _outputText = "TestGesture Activated");

        // Set up a listener for the UnityEvent to capture output for chain
        _gestureInputController.gestureChainDictionary.gestureChainInputs[0].gestureEvent.AddListener(() => _outputText = "TestGestureChain Activated");

        _gestureInputController.gestureChainMapping["TestGestureTestGesture2"].AddListener(() => _outputText = "TestGestureChain Activated");

        _gestureInputController.singleGestureInputMapping["TestGesture"].AddListener(() => _outputText = "TestGesture Activated");

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

    [UnityTest]
    public IEnumerator GestureInputControllerTest_ActivateTestGestureChain()
    {
        // Arrange
        string gesture1Name = "TestGesture";
        string gesture2Name = "TestGesture2";

        // Act
        _gestureInputController.AddGestureToChain(gesture1Name);
        _gestureInputController.AddGestureToChain(gesture2Name);

        // Wait for 2 seconds
        yield return new WaitForSecondsRealtime(3f);

        // Assert
        Assert.AreEqual("TestGestureChain Activated", _outputText, "The gesture input was not activated correctly.");
    }

    [UnityTest]
    public IEnumerator GestureInputControllerTest_ActivateTestGestureChainToSingle()
    {
        // Arrange
        string gesture1Name = "TestGesture";
        string gesture2Name = "TestGesture2";

        // Act
        _gestureInputController.AddGestureToChain(gesture1Name);
        _gestureInputController.AddGestureToChain(gesture2Name);
        _gestureInputController.AddGestureToChain(gesture1Name);

        // Wait for 2 seconds
        yield return new WaitForSecondsRealtime(2f);

        // Assert
        Assert.AreEqual("TestGesture Activated", _outputText, "The gesture input was not activated correctly.");
    }
}

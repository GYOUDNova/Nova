using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public class EditorWindowController : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset visualTreeAsset;

    private const float minWindowHeight = 400;
    private const float minWindowLength = 800;

    static EditorWindowController()
    {
        EditorApplication.delayCall += ShowWindow;
    }

    /// <summary>
    /// Called when the window is opened
    /// </summary>
    [MenuItem("Window/UI Toolkit/EditorWindowController")]
    public static void ShowWindow()
    {
        EditorWindowController windowController = GetWindow<EditorWindowController>();
        windowController.minSize = new Vector2(minWindowLength, minWindowHeight);
        windowController.titleContent = new GUIContent("NOVA Configuration");     
    }

    public void CreateGUI()
    {
        VisualElement root = visualTreeAsset.CloneTree();
        rootVisualElement.Add(root);


        Label label = root.Q<Label>("TitleLabel");
        label.text = "Hand Gesture Configuration";
    }
}

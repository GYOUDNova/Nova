using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public class AddingCombinationWindowController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset addingCombinationScreenAsset;

        private VisualElement root;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 850;
        private const string Title = "Add a Combination";

        [MenuItem("Window/UI Toolkit/Adding Combination Screen")]
        public static void SetupAndShowWindow()
        {
            AddingCombinationWindowController addingCombinationController = GetWindow<AddingCombinationWindowController>();
            addingCombinationController.titleContent = new GUIContent(Title);
            addingCombinationController.maxSize = new Vector2(MinWindowLength, MinWindowHeight);
            addingCombinationController.minSize = addingCombinationController.maxSize;
        }

        public void CreateGUI()
        {
            root = addingCombinationScreenAsset.CloneTree();
            rootVisualElement.Add(root);

            Label label = root.Q<Label>("TitleLabel");
            label.text = Title;
        }
    }
}
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public class GestureListWindowController : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset gestureListScreenAsset;

        private VisualElement root;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 850;
        private const string Title = "Gesture List";

        [MenuItem("Window/UI Toolkit/Gesture List Screen")]
        public static void SetupAndShowWindow()
        {
            GestureListWindowController gestureListController = GetWindow<GestureListWindowController>();
            gestureListController.titleContent = new GUIContent(Title);
            gestureListController.maxSize = new Vector2(MinWindowLength, MinWindowHeight);
            gestureListController.minSize = gestureListController.maxSize;
        }

        public void CreateGUI()
        {
            root = gestureListScreenAsset.CloneTree();
            rootVisualElement.Add(root);

            Label label = root.Q<Label>("TitleLabel");
            label.text = Title;
        }
    }
}

#endif

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

        [SerializeField]
        private VisualTreeAsset gestureCard;

        private VisualElement root;
        private ScrollView scrollView;

        /*Window Settings*/
        private const float MinWindowHeight = 600;
        private const float MinWindowLength = 850;
        private const string Title = "Gesture List";

        [MenuItem("Window/UI Toolkit/Gesture List Screen")]
        public static void SetupAndShowWindow()
        {
            GestureListWindowController gestureListController = GetWindow<GestureListWindowController>();
            gestureListController.minSize = new Vector2(MinWindowLength, MinWindowHeight);
            gestureListController.maxSize = new Vector2(MinWindowLength + 1, MinWindowHeight + 1);
            gestureListController.titleContent = new GUIContent(Title);
        }

        public void CreateGUI()
        {
            root = gestureListScreenAsset.CloneTree();
            rootVisualElement.Add(root);

            Label label = root.Q<Label>("TitleLabel");
            label.text = Title;

            scrollView = root.Q<ScrollView>("ListOfGestures");
            VisualElement card1 = gestureCard.CloneTree();
            Label label1 = card1.Q<Label>("GestureDetails");
            label1.text = "1";
            scrollView.Add(card1);

            VisualElement card2 = gestureCard.CloneTree();
            Label label2 = card2.Q<Label>("GestureDetails");
            label2.text = "2";
            scrollView.Add(card2);

            VisualElement card3 = gestureCard.CloneTree();
            Label label3 = card3.Q<Label>("GestureDetails");
            label3.text = "3";
            scrollView.Add(card3);

            VisualElement card4 = gestureCard.CloneTree();
            Label label4 = card4.Q<Label>("GestureDetails");
            label4.text = "4";
            scrollView.Add(card4);

            VisualElement card5 = gestureCard.CloneTree();
            Label label5 = card5.Q<Label>("GestureDetails");
            label4.text = "5";
            scrollView.Add(card5);

        }

        private void OnEnable()
        {
            minSize = new Vector2(MinWindowLength, MinWindowHeight);
            maxSize = new Vector2(MinWindowLength, MinWindowHeight);
        }
    }
}

#endif

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
        private const float MinWindowLength = 875;
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

            var gestureSqliteHandler = GestureSqliteHandler.Instance();
            var gestureList = gestureSqliteHandler.GetAllUIGestures();

            if (gestureList.Count == 0)
            {
                Debug.LogWarning("No gestures found in the database.");
                return;
            }

            // Populate the UI with gesture data
            scrollView = root.Q<ScrollView>("ListOfGestures");

            foreach (var gesture in gestureList)
            {
                VisualElement card = gestureCard.CloneTree();
                Label cardLabel = card.Q<Label>("GestureDetails");
                Label typeLabel = card.Q<Label>("GestureType");
                cardLabel.text = $"{gesture.GestureName} of category {gesture.Category.Name}";

                if (gesture.Data.IsPredefined)
                {
                    typeLabel.text = "Predefined";
                    typeLabel.style.color = new StyleColor(Color.yellow);
                }
                else
                {
                    typeLabel.text = "Custom";
                    typeLabel.style.color = new StyleColor(Color.green);
                }

                // Load the image from the expected path and set it to the Image component
                VisualElement image = card.Q<VisualElement>("GestureImage");
                Texture2D imageTexture = FileHandler.LoadTextureFromResources(gesture.Image.Name, gesture.Image.FileExtension);

                // Add that texture to the image element
                if (imageTexture != null)
                {
                    image.style.backgroundImage = new StyleBackground(imageTexture);
                }
                else
                {
                    Debug.LogWarning($"Image {gesture.Image.Name} not found in resources.");
                }

                scrollView.Add(card);
            }
        }
    }
}

#endif

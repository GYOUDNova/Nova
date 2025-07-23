#if UNITY_EDITOR

using System.Linq;
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
        private DropdownField categoryOptions;
        private DropdownField sortOptions;
        private Button filterButton;
        private Button resetButton;

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
            rootVisualElement.Clear();
            rootVisualElement.Add(root);

            Label label = root.Q<Label>("TitleLabel");
            label.text = Title;

            filterButton = root.Q<Button>("FilterButton");
            filterButton.RegisterCallback<ClickEvent>(evt => OnFilterButtonClick(evt));

            resetButton = root.Q<Button>("ResetButton");
            resetButton.RegisterCallback<ClickEvent>(evt => OnResetButtonClick(evt));

            categoryOptions = root.Q<DropdownField>("CategoryOptions");
            categoryOptions.choices.Clear();

            sortOptions = root.Q<DropdownField>("SortOptions");
            sortOptions.choices.Clear();

            scrollView = root.Q<ScrollView>("ListOfGestures");

            PopulateDropdowns();
            PopulateUI();
        }

        private void PopulateDropdowns()
        {
            var gestureSqliteHandler = GestureSqliteHandler.Instance();
            var allCategories = gestureSqliteHandler.GetObjects<GestureCategory>();
            foreach (var category in allCategories)
            {
                categoryOptions.choices.Add(category.Name);
            }

            foreach (var option in HelperConstants.SortingOptions)
            {
                sortOptions.choices.Add(option);
            }
        }

        private void PopulateUI(string categoryFilter = HelperConstants.GestureListNoFilters,
                                string sortOption = HelperConstants.NoSorting)
        {
            ResetGUI();

            var gestureSqliteHandler = GestureSqliteHandler.Instance();

            categoryOptions.choices.Add(HelperConstants.GestureListNoFilters);
            categoryOptions.value = categoryFilter == HelperConstants.GestureListNoFilters ? HelperConstants.GestureListNoFilters : categoryFilter;
            sortOptions.value = sortOption == HelperConstants.NoSorting ? HelperConstants.NoSorting : sortOption;

            var gestures = categoryFilter == HelperConstants.GestureListNoFilters
                ? gestureSqliteHandler.GetAllUIGestures() : gestureSqliteHandler.GetUIGesturesByCategory(categoryFilter);

            string selectedSortOption = sortOptions.value;
            switch (selectedSortOption)
            {
                case HelperConstants.SortAlphabetically:
                    gestures = gestures.OrderBy(g => g.GestureName).ToList();
                    break;
                case HelperConstants.SortInReverse:
                    gestures = gestures.OrderByDescending(g => g.GestureName).ToList();
                    break;
                default:
                    break;
            }

            foreach (var gesture in gestures)
            {
                VisualElement card = gestureCard.CloneTree();
                Label cardLabel = card.Q<Label>("GestureDetails");
                Label typeLabel = card.Q<Label>("GestureType");
                Button deleteGestureButton = card.Q<Button>("DeleteGestureButton");
                deleteGestureButton.clicked += () => OnDeleteButtonClick(gesture.GestureName);
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

        private void ResetGUI()
        {
            // Clear the scroll view and reset dropdowns
            scrollView.Clear();
            categoryOptions.value = HelperConstants.GestureListNoFilters;
            sortOptions.value = HelperConstants.NoSorting;
        }

        #region Button Events
        private void OnFilterButtonClick(ClickEvent evt)
        {
            string filterOption = categoryOptions.value;
            string sortOption = sortOptions.value;
            PopulateUI(filterOption, sortOption);
        }

        private void OnResetButtonClick(ClickEvent evt)
        {
            PopulateUI();
        }

        private void OnDeleteButtonClick(string gestureName)
        {
            var gestureSqliteHandler = GestureSqliteHandler.Instance();

            try
            {
                gestureSqliteHandler.DeleteGesture(gestureName);
                Debug.Log($"Gesture {gestureName} deleted successfully.");
                ResetGUI(); // Reset the UI after deletion
                PopulateUI();
            }
            catch (ItemNotFoundException ex)
            {
                Debug.LogError($"Failed to delete gesture {gestureName}: {ex.Message}");
            }
            catch (DatabaseException ex)
            {
                Debug.LogError($"Database error while deleting gesture {gestureName}: {ex.Message}");
            }
        }
        #endregion
    }
}
#endif

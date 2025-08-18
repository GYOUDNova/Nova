using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public class InfoWindowPresenter : MonoBehaviour
    {
        private VisualElement informationView;
        private VisualElement welcomeView;
        private TextElement informationText;
        private TextElement inforHeader;

        private VisualElement upCard;
        private VisualElement confirmCard;
        private VisualElement downCard;


        [SerializeField]
        private StyleSheet informationStyleSheet;

        public List<InformationWindowText> ButtonData;
        public ScrollView ScrollButtonView;

        private int selectedIndex = 3;

        private const int DefaultSelectedIndex = 3;

        private Coroutine scrollCoroutine;

        [SerializeField]
        private Texture2D backgroundMapTexture;

        private VisualElement informationImageWindow;

        private const string PulseClass = "pulse-color";
        private Color pulseColor = new Color(0.98f, 0.79f, 0.04f, 0.5f); // yellow w/ alpha
        private Color originalColor = new Color(0.133f, 0.133f, 0.133f);


        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(informationStyleSheet);
            ScrollButtonView = root.Q<ScrollView>("ButtonScroll");
            informationView = root.Q<VisualElement>("InformationView");
            informationImageWindow = informationView.Q<VisualElement>("InformationContainer");
            upCard = informationView.Q<VisualElement>("UpCard");
            confirmCard = informationView.Q<VisualElement>("ConfirmCard");
            downCard = informationView.Q<VisualElement>("DownCard");
            welcomeView = root.Q<VisualElement>("WelcomeView");
            informationText = root.Q<TextElement>("InformationText");
            inforHeader = root.Q<TextElement>("InfoHeader");

            ScrollButtonView.Clear();

            // Initialize buttons
            foreach (var data in ButtonData)
            {
                var button = new Button();
                button.text = data.ButtonName;
                button.AddToClassList("button");
                ScrollButtonView.Add(button);
            }

            HideInformationView();
        }

        public void ShowInformationView()
        {
            informationView.style.display = DisplayStyle.Flex;
            welcomeView.style.display = DisplayStyle.None;
            SetInformationText("To read information on possible use cases please match one of the gestures on the left", " ");
            // Highlight new button
            var centerButton = (Button)ScrollButtonView.ElementAt(selectedIndex);
            HighlightButton(centerButton);

            // Center the selected button in the scroll view
            ScrollToCenter(centerButton);

        }

        public void HideInformationView()
        {
            // Reset selection by taking the total cound and current index and math it to get back to 3
            MoveSelection(DefaultSelectedIndex - selectedIndex + 1);

            SetInformationText("To read information on possible use cases please match one of the gestures on the left", " ");
            informationView.style.display = DisplayStyle.None;
            welcomeView.style.display = DisplayStyle.Flex;
            selectedIndex = DefaultSelectedIndex;
        }

        public void HighlightButton(Button button)
        {
            // Remove highlight + glow from all buttons
            foreach (var child in ScrollButtonView.Children())
            {
                if (child is Button b)
                {
                    b.RemoveFromClassList("button-hover");
                    b.RemoveFromClassList("button-glow");
                }
            }

            // Add highlight + glow to the current one
            button.AddToClassList("button-hover");
            button.AddToClassList("button-glow");
        }

        public void MoveSelection(int direction)
        {
            if (direction >= 0)
            {
                PlayPulse(downCard);
            }
            else
            {
                PlayPulse(upCard);
            }

            // Wrap index instead of clamping
            selectedIndex += direction;
            if (selectedIndex < 0) selectedIndex = ScrollButtonView.childCount - 1;
            else if (selectedIndex >= ScrollButtonView.childCount) selectedIndex = 0;

            // Highlight new button
            var newButton = (Button)ScrollButtonView.ElementAt(selectedIndex);
            HighlightButton(newButton);

            // Center the selected button in the scroll view
            SmoothScrollToCenter(newButton);

        }

        public void ConfirmSelection()
        {
            // Clear the background image for any other button
            informationImageWindow.style.backgroundImage = null;

            if (informationView.style.display == DisplayStyle.None)
            {
                ShowInformationView();
                return;
            }

            PlayPulse(confirmCard);

            if (selectedIndex >= 0 && selectedIndex < ButtonData.Count)
            {
                // if info text is "Return" hide the information view
                if (ButtonData[selectedIndex].InfoText == "Return")
                {
                    HideInformationView();
                    return;
                }
                else if (ButtonData[selectedIndex].InfoText == "MAP")
                {
                    if (backgroundMapTexture != null)
                    {
                        informationImageWindow.style.backgroundImage = new StyleBackground(backgroundMapTexture);
                        SetInformationText("", " ");
                        return;
                    }
                }

                SetInformationText(ButtonData[selectedIndex].InfoText, ButtonData[selectedIndex].ButtonName);

                var selectedButton = (Button)ScrollButtonView.ElementAt(selectedIndex);
            }
        }


        public void SetInformationText(string text, string header)
        {
            if (informationView.style.display == DisplayStyle.Flex)
            {
                informationText.text = text;
                inforHeader.text = header;
            }
        }

        public void PlayPulse(VisualElement card)
        {
            // Set pulse color
            card.style.backgroundColor = pulseColor;

            // Schedule reset back to original
            card.schedule.Execute(() =>
            {
                card.style.backgroundColor = originalColor;
            }).StartingIn(300); // 300ms
        }

        // function to scroll the selected button to the center of the scroll view
        private void ScrollToCenter(Button button)
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
            }
            scrollCoroutine = StartCoroutine(ScrollToCenterCoroutine(button));
        }

        private void SmoothScrollToCenter(Button button)
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
            }
            scrollCoroutine = StartCoroutine(SmoothScrollToCenterCoroutine(button));
        }

        private IEnumerator ScrollToCenterCoroutine(Button button)
        {
            // Wait for the next frame to ensure the button is fully rendered
            yield return null;
            // Calculate the position of the button in the scroll view
            float buttonPosition = button.resolvedStyle.top + button.resolvedStyle.height / 2;
            float scrollViewHeight = ScrollButtonView.resolvedStyle.height;
            // Calculate the new scroll position to center the button
            float newScrollPosition = buttonPosition - scrollViewHeight / 2;
            // Smoothly scroll to the new position
            ScrollButtonView.scrollOffset = new Vector2(0, newScrollPosition);
        }

        // coroutine to smoothly scroll the scroll view to the center of the selected button
        private IEnumerator SmoothScrollToCenterCoroutine(Button button)
        {
            float targetPosition = button.resolvedStyle.top + button.resolvedStyle.height / 2;
            float scrollViewHeight = ScrollButtonView.resolvedStyle.height;
            float startPosition = ScrollButtonView.scrollOffset.y;
            float newScrollPosition = targetPosition - scrollViewHeight / 2;
            float duration = 0.25f; // Duration of the scroll
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                ScrollButtonView.scrollOffset = new Vector2(0, Mathf.Lerp(startPosition, newScrollPosition, t));
                yield return null;
            }
            ScrollButtonView.scrollOffset = new Vector2(0, newScrollPosition);
        }
    }
}

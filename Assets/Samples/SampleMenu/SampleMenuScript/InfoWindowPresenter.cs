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

        [SerializeField]
        private StyleSheet informationStyleSheet;

        public List<InformationWindowText> ButtonData;
        public ScrollView ScrollButtonView;

        private int selectedIndex = -1;

        private const int DefaultSelectedIndex = -1;

        private Coroutine scrollCoroutine;

        [SerializeField]
        private Texture2D backgroundMapTexture;

        private VisualElement informationImageWindow;


        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            root.styleSheets.Add(informationStyleSheet);
            ScrollButtonView = root.Q<ScrollView>("ButtonScroll");
            informationView = root.Q<VisualElement>("InformationView");
            informationImageWindow = informationView.Q<VisualElement>("InformationContainer");
            welcomeView = root.Q<VisualElement>("WelcomeView");
            informationText = root.Q<TextElement>("InformationText");

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

            SetInformationText("To read information on possible use cases please match one of the gestures on the left");
            informationView.style.display = DisplayStyle.Flex;
            welcomeView.style.display = DisplayStyle.None;

        }

        public void HideInformationView()
        {
            // Reset selection by taking the total cound and current index and math it to get back to 1
            MoveSelection(DefaultSelectedIndex - selectedIndex + 1);

            SetInformationText("To read information on possible use cases please match one of the gestures on the left");
            informationView.style.display = DisplayStyle.None;
            welcomeView.style.display = DisplayStyle.Flex;
            selectedIndex = -1;
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
            // Wrap index instead of clamping
            selectedIndex += direction;
            if (selectedIndex < 0) selectedIndex = ScrollButtonView.childCount - 1;
            else if (selectedIndex >= ScrollButtonView.childCount) selectedIndex = 0;

            // Highlight new button
            var newButton = (Button)ScrollButtonView.ElementAt(selectedIndex);
            HighlightButton(newButton);

            // Smooth scroll to it
            //if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
            //scrollCoroutine = StartCoroutine(SmoothScrollTo(newButton));

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
                        SetInformationText("");
                    }
                }

                SetInformationText(ButtonData[selectedIndex].InfoText);

                var selectedButton = (Button)ScrollButtonView.ElementAt(selectedIndex);
                //selectedButton.AddToClassList("button-confirmed");
            }
        }


        public void SetInformationText(string text)
        {
            if (informationView.style.display == DisplayStyle.Flex)
            {
                informationText.text = text;
            }
        }


        private IEnumerator SmoothScrollTo(Button targetButton, float duration = 0.25f)
        {
            // Position of the button inside the scroll content
            float targetY = targetButton.layout.y
                          - (ScrollButtonView.layout.height / 2f)
                          + (targetButton.layout.height / 2f);

            // Clamp so we don’t overscroll
            targetY = Mathf.Clamp(targetY, 0, ScrollButtonView.contentContainer.layout.height - ScrollButtonView.layout.height);

            Vector2 start = ScrollButtonView.scrollOffset;
            Vector2 end = new Vector2(0, targetY);

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, time / duration);
                ScrollButtonView.scrollOffset = Vector2.Lerp(start, end, t);
                yield return null;
            }

            ScrollButtonView.scrollOffset = end;
        }

    }
}

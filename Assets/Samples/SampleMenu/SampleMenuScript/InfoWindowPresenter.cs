using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public class InfoWindowPresenter : MonoBehaviour
    {
        private VisualElement informationView;
        private VisualElement welcomeView;
        private TextElement informationText;


        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            informationView = root.Q<VisualElement>("InformationView");
            welcomeView = root.Q<VisualElement>("WelcomeView");
            informationText = root.Q<TextElement>("InformationText");
        }

        public void ShowInformationView()
        {
            informationView.style.display = DisplayStyle.Flex;
            welcomeView.style.display = DisplayStyle.None;
        }

        public void HideInformationView()
        {
            SetInformationText(0);
            informationView.style.display = DisplayStyle.None;
            welcomeView.style.display = DisplayStyle.Flex;

        }

        public void SetInformationText(int index = 0)
        {
            if (informationView.style.display == DisplayStyle.Flex)
            {
                switch (index)
                {
                    case 0:
                        informationText.text = "To read information on possible use cases please match one of the gestures on the left";
                        break;
                    case 1:
                        informationText.text = "Our Unity plugin enables intuitive hand gesture recognition, making it ideal for modern kiosk technologies. It allows users to interact with digital content in a completely touchless manner, which is especially valuable in public environments where hygiene is a concern. This can be applied to interactive information kiosks in museums, airports, or retail settings, where users can browse content, navigate menus, or trigger actions using simple gestures. Additionally, the plugin supports integration with projector-based or holographic displays, enabling users to control immersive content in mid-air without physical contact, enhancing accessibility and creating futuristic, engaging user experiences.";
                        break;
                    case 2:
                        informationText.text = "Our Unity plugin opens up powerful possibilities in the realm of sign language recognition and learning. By accurately detecting and interpreting hand gestures, it can be used to create real-time translation tools that convert sign language into text or speech, improving communication accessibility for Deaf and hard-of-hearing individuals. Additionally, it can support immersive educational applications, allowing users to learn sign language through interactive lessons, visual feedback, and guided practice in a virtual environment. This makes it a valuable tool for both assistive technology and inclusive education.";
                        break;
                    default:
                        informationText.text = "To read information on possible use cases please match one of the gestures on the left";
                        break;
                }
            }
        }

    }
}

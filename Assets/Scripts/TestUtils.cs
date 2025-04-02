using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public static class TestUtils
    {
        public static void ClickOnButton(Button button)
        {
            // Ensure that the button is enabled in the hierarchy
            button.SetEnabled(true);

            var navigationEvent = new NavigationSubmitEvent() { target = button };
            button.SendEvent(navigationEvent);
        }
    }
}
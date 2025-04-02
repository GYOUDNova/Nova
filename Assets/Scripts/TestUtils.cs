using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public static class TestUtils
    {
        /// <summary>
        ///     This coroutine is used to simulate clicking on a button on a test case
        ///     Taken from Trades Electrical. Thank you to them :)
        /// </summary>
        /// <param name="button">The button to click</param>
        public static void ClickOnButton(Button button)
        {
            // Ensure that the button is enabled in the hierarchy
            button.SetEnabled(true);

            var navigationEvent = new NavigationSubmitEvent() { target = button };
            button.SendEvent(navigationEvent);
        }
    }
}

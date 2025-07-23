#if UNITY_EDITOR

using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NOVA.Scripts
{
    public static class EditorTextHandler
    {
        private static float messageLabelTimer = 5f;

        private static EditorCoroutine currentCoroutine;

        public static void DisplayMessage(string text, Color messageColor, Label label)
        {
            if (currentCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(currentCoroutine);
            }

            label.style.color = messageColor;
            label.text = text;
            currentCoroutine = EditorCoroutineUtility.StartCoroutineOwnerless(ClearErrorMessage(label));
        }

        private static IEnumerator ClearErrorMessage(Label labelToClear)
        {
            yield return new EditorWaitForSeconds(messageLabelTimer);
            labelToClear.text = string.Empty;
        }
    }
}

#endif

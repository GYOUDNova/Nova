using UnityEngine;

[CreateAssetMenu(fileName = "InformationWindowText", menuName = "Scriptable Objects/InformationWindowText")]
public class InformationWindowText : ScriptableObject
{
    // variable for name
    [Tooltip("The name of the button")]
    public string ButtonName;

    // variable for text
    [Tooltip("The text to display in the information window")]
    public string InfoText;

}

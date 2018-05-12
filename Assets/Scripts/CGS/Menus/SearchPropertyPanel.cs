using CardGameDef;
using UnityEngine;
using UnityEngine.UI;

public class SearchPropertyPanel : MonoBehaviour
{
    public PropertyType type;
    public Text nameLabelText;
    public InputField stringInputField;
    public Text stringPlaceHolderText;
    public InputField integerMinInputField;
    public InputField integerMaxInputField;
    public RectTransform enumContent;
    public Toggle enumToggle;
}
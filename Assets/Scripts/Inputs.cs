public static class Inputs
{
    public const string Cancel = "Cancel";
    public const string CardViewer = "CardViewer";
    public const string Column = "Column";
    public const string Delete = "Delete";
    public const string Draw = "Draw";
    public const string Filter = "Filter";
    public const string FocusName = "FocusName";
    public const string FocusText = "FocusText";
    public const string Horizontal = "Horizontal";
    public const string Load = "Load";
    public const string New = "New";
    public const string No = "No";
    public const string Page = "Page";
    public const string Save = "Save";
    public const string Sort = "Sort";
    public const string Submit = "Submit";
    public const string Vertical = "Vertical";

    public static char FilterFocusNameInput(char charToValidate)
    {
        if (charToValidate == '`')
            charToValidate = '\0';
        return charToValidate;
    }
}

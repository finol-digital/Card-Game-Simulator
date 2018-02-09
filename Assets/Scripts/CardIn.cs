public static class CardIn
{
    public const string CancelInput = "Cancel";
    public const string CardViewerInput = "CardViewer";
    public const string ColumnInput = "Column";
    public const string DeleteInput = "Delete";
    public const string DrawInput = "Draw";
    public const string FilterInput = "Filter";
    public const string FocusNameInput = "FocusName";
    public const string FocusTextInput = "FocusText";
    public const string HorizontalInput = "Horizontal";
    public const string LoadInput = "Load";
    public const string NewInput = "New";
    public const string NoInput = "No";
    public const string PageInput = "Page";
    public const string SaveInput = "Save";
    public const string SortInput = "Sort";
    public const string SubmitInput = "Submit";
    public const string VerticalInput = "Vertical";

    public static char FilterFocusNameInput(char charToValidate)
    {
        if (charToValidate == '`')
            charToValidate = '\0';
        return charToValidate;
    }
}

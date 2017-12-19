using UnityEngine;

public class SearchResultsLayout : MonoBehaviour
{
    public const float WidthCheck = 1000f;
    public static readonly Vector2 SearchNamePortraitPosition = new Vector2(150f, 450f);
    public static readonly Vector2 SearchNameLandscapePosition = new Vector2(150f, 367.5f);

    public RectTransform searchName;
    public SearchResults searchResults;

    void OnRectTransformDimensionsChange()
    {
        if (!gameObject.activeInHierarchy)
            return;

        searchName.anchoredPosition = GetComponent<RectTransform>().rect.width < WidthCheck ? SearchNamePortraitPosition : SearchNameLandscapePosition;
        searchResults.CurrentPageIndex = 0;
        searchResults.UpdateSearchResultsPanel();

        CardInfoViewer.Instance.IsVisible = false;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public delegate void OnSearchDelegate(List<Card> searchResults);

public class SearchMenu : MonoBehaviour
{
    public RectTransform filterContentView;
    public RectTransform nameProperty;
    public RectTransform idProperty;
    public RectTransform setProperty;
    public RectTransform propertyTemplate;

    public OnSearchDelegate SearchCallback { get; private set; }

    private string _nameFilter;
    private string _idFilter;
    private string _setCodeFilter;
    private Dictionary<string, string> _propertyFilters;
    private List<Card> _results;

    public void Show(OnSearchDelegate searchCallback)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();
        SearchCallback = searchCallback;

        propertyTemplate.gameObject.SetActive(true);
        nameProperty.SetParent(this.transform);
        idProperty.SetParent(this.transform);
        setProperty.SetParent(this.transform);
        propertyTemplate.SetParent(this.transform);
        filterContentView.DestroyAllChildren();
        nameProperty.SetParent(filterContentView);
        idProperty.SetParent(filterContentView);
        setProperty.SetParent(filterContentView);
        propertyTemplate.SetParent(filterContentView);
        Vector2 pos = propertyTemplate.localPosition;
        foreach (PropertyDef prop in CardGameManager.Current.CardProperties) {
            GameObject newProp = Instantiate(propertyTemplate.gameObject, propertyTemplate.position, propertyTemplate.rotation, filterContentView) as GameObject;
            newProp.transform.localPosition = pos;
            PropertyEditor editor = newProp.GetComponent<PropertyEditor>();
            editor.nameLabel.text = prop.Name;
            UnityAction<string> textChange = new UnityAction<string>(text => SetPropertyFilter(prop.Name, text));
            editor.inputField.onValueChanged.AddListener(textChange);
            editor.placeHolderText.text = "Enter " + prop.Name + "...";
            pos.y -= propertyTemplate.rect.height;
        }
        propertyTemplate.gameObject.SetActive(false);
        filterContentView.sizeDelta = new Vector2(filterContentView.sizeDelta.x, propertyTemplate.rect.height * CardGameManager.Current.CardProperties.Count + propertyTemplate.rect.height * 3);
    }

    public void SetPropertyFilter(string key, string val)
    {
        PropertyFilters [key] = val;
    }

    public void ClearFilters()
    {
        foreach (InputField input in GetComponentsInChildren<InputField>())
            input.text = string.Empty;
        PropertyFilters.Clear();
    }

    public void ClearSearch()
    {
        ClearFilters();
        Search();
    }

    public void Search()
    {
        Results.Clear();
        IEnumerable<Card> cardSearcher = CardGameManager.Current.FilterCards(IdFilter, NameFilter, SetCodeFilter, PropertyFilters);
        foreach (Card card in cardSearcher)
            Results.Add(card);
        SearchCallback(Results);
    }

    void Update()
    {
        if (Input.GetButtonDown("Submit")) {
            Search();
            Hide();
        }
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    public string NameFilter {
        get {
            if (_nameFilter == null)
                _nameFilter = string.Empty;
            return _nameFilter;
        }
        set {
            _nameFilter = value;
            Search();
        }
    }

    public string IdFilter {
        get {
            if (_idFilter == null)
                _idFilter = string.Empty;
            return _idFilter;
        }
        set {
            _idFilter = value;
            Search();
        }
    }

    public string SetCodeFilter {
        get {
            if (_setCodeFilter == null)
                _setCodeFilter = string.Empty;
            return _setCodeFilter;
        }
        set {
            _setCodeFilter = value;
            Search();
        }
    }

    public Dictionary<string, string> PropertyFilters {
        get {
            if (_propertyFilters == null)
                _propertyFilters = new Dictionary<string, string>();
            return _propertyFilters;
        }
    }

    public List<Card> Results {
        get {
            if (_results == null)
                _results = new List<Card>();
            return _results;
        }
    }
}

public class PropertyEditor : MonoBehaviour
{
    public Text nameLabel;
    public InputField inputField;
    public Text placeHolderText;
    public Text valueText;
}

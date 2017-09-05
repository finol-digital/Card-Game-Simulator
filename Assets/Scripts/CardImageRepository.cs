using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardImageRepository
{
    private static Dictionary<string, Sprite> allCardImages = new Dictionary<string, Sprite>();

    // TODO: MAKE THIS BE DETERMINED DIFFERENTLY BASED OFF THE CARD GAME; WOULD LIKE TO DELETE THIS METHOD ENTIRELY
    public static string GetCardImageName(Card card)
    {
        char[] cardNameAlphaNum = card.Name.Where(c => (char.IsLetterOrDigit(c) ||
                                  char.IsWhiteSpace(c) ||
                                  c == '-')).ToArray(); 
        string cardImageName = new string(cardNameAlphaNum);
        cardImageName = cardImageName.Replace(" ", "_").Replace("-", "_").ToLower() + ".jpg";
        return cardImageName;
    }

    public static string GetCardImageFilePath(Card card)
    {
        string imageFilePath = CardGameManager.Current.FilePathBase + "/" + card.SetCode + "/" + GetCardImageName(card);
        return imageFilePath;
    }

    public static bool TryGetCachedCardImage(Card card, out Sprite image)
    {
        string imageFilePath = GetCardImageFilePath(card);
        bool imageCached = allCardImages.TryGetValue(imageFilePath, out image);
        return imageCached;
    }

    public static IEnumerator GetAndCacheCardImage(Card card)
    {
        Debug.Log("Getting and caching card image for: " + card.Name);
        string imageFilePath = GetCardImageFilePath(card);
        string imageWebURL = CardGameManager.Current.CardImageURLBase + GetCardImageName(card);

        Sprite cardImage = null;
        yield return UnityExtensionMethods.RunOutputCoroutine<Sprite>(UnityExtensionMethods.LoadOrGetImage(imageFilePath, imageWebURL), (output) => cardImage = output);
        if (cardImage != null)
            allCardImages [imageFilePath] = cardImage;
        else
            Debug.LogWarning("Failed to get and cache card image for " + card.Name + "!");
    }

    public static Sprite DefaultImage {
        get { return CardGameManager.Current.CardBackImage; }
    }
}

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
        string imageFilePath = CardGameManager.CurrentCardGame.FilePathBase + "/" + card.SetCode + "/" + GetCardImageName(card);
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
        string imageFilePath = GetCardImageFilePath(card);
        Debug.Log("Loading and caching card image: " + imageFilePath);

        WWW loadImage = null;
        bool imageCached = File.Exists(imageFilePath);
        if (imageCached) { 
            string imageFileURL = "file://" + imageFilePath;
            Debug.Log("Attempting to load card image from: " + imageFileURL);
            loadImage = new WWW(imageFileURL);
            yield return loadImage;
        }

        if (loadImage == null || !string.IsNullOrEmpty(loadImage.error)) {
            string imageWebURL = CardGameManager.CurrentCardGame.CardImageBaseURL + GetCardImageName(card);
            // TODO: BETTER HANDLING OF HOW TO DETERMINE WHAT THE NAME OF THE IMAGE FOR THE CARD IS, AND HOW TO LOAD IT FROM A URL
            Debug.Log("Attempting to load card image from: " + imageWebURL);
            loadImage = new WWW(imageWebURL);
            yield return loadImage;

            if (!string.IsNullOrEmpty(loadImage.error)) {
                Debug.LogWarning("Had an error loading from web: " + loadImage.error);
                // TODO: HANDLING FOR WHEN WE FAIL TO LOAD FROM WEB
            }

            Debug.Log("Saving loaded" + imageFilePath + " to file");
            if (!Directory.Exists(imageFilePath.Substring(0, imageFilePath.LastIndexOf('/')))) {
                Debug.Log("Image file directory for " + imageFilePath + " does not exist, so creating it");
                Directory.CreateDirectory(imageFilePath);
            }
            File.WriteAllBytes(imageFilePath, loadImage.bytes);
            Debug.Log(imageFilePath + " saved to file");
        }

        Sprite cardImage = Sprite.Create(loadImage.texture, new Rect(0, 0, loadImage.texture.width, loadImage.texture.height), new Vector2(0.5f, 0.5f));
        allCardImages [imageFilePath] = cardImage;
        Debug.Log("Finalized load of " + imageFilePath);
    }

    public static Sprite DefaultImage {
        get { return CardGameManager.CurrentCardGame.CardBackImage; }
    }
}

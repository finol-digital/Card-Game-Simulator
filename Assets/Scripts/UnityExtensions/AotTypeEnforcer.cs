using Newtonsoft.Json.Utilities;
using UnityEngine;

public class AotTypeEnforcer : MonoBehaviour
{
    public void Awake()
    {
        AotHelper.EnsureType<Newtonsoft.Json.Converters.StringEnumConverter>();
        AotHelper.EnsureType<CardGameDef.DeckUrl>();
        AotHelper.Ensure(() => { var enumDef = new CardGameDef.EnumDef(string.Empty, null); });
        AotHelper.Ensure(() => { var gameBoard = new CardGameDef.GameBoard(string.Empty, Vector2.zero, Vector2.zero); });
        AotHelper.EnsureType<CardGameDef.GameBoardUrl>();
    }
}

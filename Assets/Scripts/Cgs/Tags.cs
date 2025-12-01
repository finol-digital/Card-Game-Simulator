/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace Cgs
{
    public static class Tags
    {
        public const string CardGameManager = "CardGameManager";
        public const string CardViewer = "CardViewer";
        public const string PlayableViewer = "PlayableViewer";

        public const int MainMenuSceneIndex = 1;
        public const int PlayModeSceneIndex = 2;
        public const int DeckEditorSceneIndex = 3;
        public const int CardsExplorerSceneIndex = 4;
        public const int SettingsSceneIndex = 5;

        public const string PlayerMove = "Player/Move";
        public const string PlayerPage = "Player/Page";

        public const string PlayerCancel = "Player/Cancel";
        public const string PlayerDelete = "Player/Delete";
        public const string PlayerSubmit = "Player/Submit";

        public const string PlayGameMenu = "PlayGame/Menu";
        public const string PlayGameSettings = "PlayGame/Settings";
        public const string PlayGameDie = "PlayGame/Die";
        public const string PlayGameToken = "PlayGame/Token";
        public const string PlayGameToggleZoomRotation = "PlayGame/ToggleZoomRotation";

        public const string CardZoom = "Card/Zoom";
        public const string CardSelectPrevious = "Card/SelectPrevious";
        public const string CardSelectNext = "Card/SelectNext";
        public const string CardMove = "Card/Move";
        public const string CardRotate = "Card/Rotate";
        public const string CardTap = "Card/Tap";
        public const string CardFlip = "Card/Flip";

        public const string CardsNew = "Cards/New";
        public const string CardsEdit = "Cards/Edit";
        public const string CardsFilter = "Cards/Filter";
        public const string CardsSort = "Cards/Sort";

        public const string DecksSave = "Decks/Save";
        public const string DecksLoad = "Decks/Load";
        public const string DecksNew = "Decks/New";

        public const string MainMenuSettings = "MainMenu/Settings";
        public const string MainMenuGamesManagementMenu = "MainMenu/GamesManagementMenu";
        public const string MainMenuSelectPrevious = "MainMenu/SelectPrevious";
        public const string MainMenuSelectNext = "MainMenu/SelectNext";
        public const string MainMenuStartGame = "MainMenu/StartGame";
        public const string MainMenuJoinGame = "MainMenu/JoinGame";
        public const string MainMenuEditDeck = "MainMenu/EditDeck";
        public const string MainMenuExploreCards = "MainMenu/ExploreCards";

        public const string SubMenuMenu = "SubMenu/Menu";
        public const string SubMenuFocusPrevious = "SubMenu/FocusPrevious";
        public const string SubMenuFocusNext = "SubMenu/FocusNext";
        public const string SubMenuPrint = "SubMenu/Print";
        public const string SubMenuShare = "SubMenu/Share";

        public const string SettingsToolTips = "Settings/ToolTips";
        public const string SettingsPreviewMouseOver = "Settings/PreviewMouseOver";
        public const string SettingsHideReprints = "Settings/HideReprints";
        public const string SettingsDeveloperMode = "Settings/DeveloperMode";

        public const string StandardPlayingCardsDirectoryName = "Standard Playing Cards@www.cardgamesimulator.com";

        public const string CgsWebsite = "https://www.cardgamesimulator.com/";
        public const string DominoesUrl = "https://www.cardgamesimulator.com/games/Dominoes/cgs.json";
        public const string MahjongUrl = "https://www.cardgamesimulator.com/games/Mahjong/cgs.json";
        public const string StandardPlayingCardsUrl = "https://www.cardgamesimulator.com/games/Standard/cgs.json";
        public const string CgsGamesBrowseUrl = "https://cgs.games/browse";
        public const string CgsGamesBrowseApiUrl = "https://cgs.games/api/browse";
        public const string NativeUri = "cardgamesim://main?url=";

        public static char FilterFocusInput(char charToValidate)
        {
            if (charToValidate == '`')
                charToValidate = '\0';
            return charToValidate;
        }
    }
}

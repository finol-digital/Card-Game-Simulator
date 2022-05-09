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

        public const string GameListUrl = "https://www.reddit.com/r/CardGameSimulator/comments/fl1p5z/official_games_list/";
        public const string DynamicLinkUriDomain = "https://cgs.link";
        public const string CgsWebsite = "https://www.cardgamesimulator.com/";

        public const string StandardPlayingCardsDirectoryName = "Standard Playing Cards@www.cardgamesimulator.com";

#if UNITY_WEBGL
        public const string StandardPlayingCardsJsonFileName = "Standard Playing Cards.json";

        public const string StandPlayingCardsJsonFileContent =
            "{\"name\":\"Standard Playing Cards\",\"autoUpdateUrl\":\"https://www.cardgamesimulator.com/games/Standard/Standard.json\"}";

        public const string DominoesDirectoryName = "Dominoes@www.cardgamesimulator.com";
        public const string DominoesJsonFileName = "Dominoes.json";

        public const string DominoesJsonFileContent =
            "{\"name\":\"Dominoes\",\"autoUpdateUrl\":\"https://www.cardgamesimulator.com/games/Dominoes/Dominoes.json\"}";

        public const string DominoesCardBackUrl = "https://www.cardgamesimulator.com/games/Dominoes/CardBack.png";
        public const string MahjongDirectoryName = "Mahjong@www.cardgamesimulator.com";
        public const string MahjongJsonFileName = "Mahjong.json";

        public const string MahjongJsonFileContent =
            "{\"name\":\"Mahjong\",\"autoUpdateUrl\":\"https://www.cardgamesimulator.com/games/Mahjong/Mahjong.json\"}";

        public const string MahjongCardBackUrl = "https://www.cardgamesimulator.com/games/Mahjong/CardBack.png";
        public const string ArcmageDirectoryName = "Arcmage@www.cardgamesimulator.com";
        public const string ArcmageJsonFileName = "Arcmage.json";

        public const string ArcmageJsonFileContent =
            "{\"name\":\"Arcmage\",\"autoUpdateUrl\":\"https://www.cardgamesimulator.com/games/Arcmage/Arcmage.json\"}";

        public const string ArcmageCardBackUrl = "https://www.cardgamesimulator.com/games/Arcmage/CardBack.png";
#endif
    }
}

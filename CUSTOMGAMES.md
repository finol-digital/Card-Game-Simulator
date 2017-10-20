# Defining Custom Games
Card Game Simulator allows users to download custom card games to use within the application. The process for downloading a custom card game through the ui is documented in the [main documentation](README.md).

A custom card game can also be manually addded by creating a new folder within Card Game Simulator's persistent game data directory. The location of this directory varies depending on platform. Some examples:
- Android: /Data/Data/com.finoldigital.cardgamesim/files/games/
- Windows: C:/Users/<Username>/AppData/LocalLow/Finol Digital/Card Game Simulator/games/
- Mac: ~/Library/Application Support/Finol Digital/Card Game Simulator/games/

The structure of this custom game folder is:
-<Name>/
 -<Name>.json
 -[AllSets.json]
 -[AllCards.json]
 -[Background.<BackgroundImageFileType>]
 -[CardBack.<CardBackImageFileType>]
 -[decks/]
 -sets/
  -<SetCode>/
   -<CardId>.<CardImageFileType>
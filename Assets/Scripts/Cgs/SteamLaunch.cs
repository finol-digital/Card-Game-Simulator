/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Linq;
using Cgs.Performance;

namespace Cgs
{
    /// <summary>Decides whether and when the optional Steam integration starts.</summary>
    public static class SteamLaunch
    {
        // Identifies CGS for local development; Steam supplies the App ID to the games it launches
        public const string AppIdFile = "steam_appid.txt";

        private const string SteamDeckVariable = "SteamDeck";

        // Steam sets these for every game it launches, on every desktop platform
        private static readonly string[] LaunchVariables = { "SteamAppId", "SteamGameId" };

        /// <summary>Whether Steam launched CGS, or a development App ID file is present.</summary>
        /// <remarks>Without either, the Steam API cannot start, so other builds skip it entirely.</remarks>
        public static bool IsDetected(Func<string, string> environment, Func<string, bool> fileExists) =>
            LaunchVariables.Any(name => !string.IsNullOrEmpty(environment(name))) || fileExists(AppIdFile);

        public static bool IsDetected() => IsDetected(Environment.GetEnvironmentVariable, File.Exists);

        /// <summary>Steam reports a Steam Deck before its API starts.</summary>
        public static bool IsSteamDeck(Func<string, string> environment) => environment(SteamDeckVariable) == "1";

        /// <summary>Steam starts once the launch is usable, so it never delays the first screen.</summary>
        /// <remarks>
        /// That is when the Main Menu becomes interactive or the launch summary times out, or after the first frame
        /// when a launch link opened another screen.
        /// </remarks>
        public static bool IsReadyToStart(StartupTimelineSession session, int activeSceneIndex) =>
            session == null || session.IsLaunchSummaryWritten ||
            session.HasMark(StartupMilestone.FirstFrameDrawn) && activeSceneIndex != Tags.MainMenuSceneIndex;
    }
}

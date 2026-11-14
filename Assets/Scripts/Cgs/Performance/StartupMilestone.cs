/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace Cgs.Performance
{
    /// <summary>
    /// In-app startup milestones, in the order a launch normally reaches them.
    /// </summary>
    public enum StartupMilestone
    {
        EngineReady,
        FirstSceneLoaded,
        FirstFrameDrawn,
        MainMenuInteractive,
        StartGameChosen,
        PlaySceneLoaded,
        TablePlayable,
        FirstCardAction
    }

    public enum StartupSummaryKind
    {
        Launch,
        Play
    }
}

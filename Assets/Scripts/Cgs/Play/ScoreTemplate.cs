/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using UnityEngine;
using UnityEngine.UI;

namespace Cgs.Play
{
    public class ScoreTemplate : MonoBehaviour
    {
        [SerializeField] Text nameText;
        [SerializeField] Text pointsText;
        [SerializeField] Text handCountText;

        public Text NameText => nameText;

        public Text PointsText => pointsText;

        public Text HandCountText => handCountText;
    }
}

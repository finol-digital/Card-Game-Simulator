// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using UnityEngine;

namespace GooglePlayInstant.LoadingScreen
{
    /// <summary>
    /// Presents download progress to the user
    /// </summary>
    [ExecuteInEditMode]
    public class LoadingBar : MonoBehaviour
    {
        public float OutlineWidth = 6f;
        public float InnerBorderWidth = 6f;

        [Tooltip("If true, " +
                 "the Outline and Background RectTransforms will update to match the outline and border width")]
        public bool ResizeAutomatically = true;

        [Tooltip("Asset Bundle download and install progress. The value set in the Editor is ignored at runtime.")]
        [Range(0f, 1f)]
        public float Progress = 0.25f;

        public RectTransform Background;
        public RectTransform Outline;
        public RectTransform ProgressHolder;
        public RectTransform ProgressFill;

        [Tooltip("Proportion of the loading bar allocated to the asset bundle downloading process. " +
                 "The rest is allocated to installing.")]
        [Range(0f, 1f)]
        public float AssetBundleDownloadToInstallRatio = 0.8f;

        private void Update()
        {
            if (ResizeAutomatically)
            {
                ApplyBorderWidth();
                SetProgress(Progress);
            }
        }

        public void ApplyBorderWidth()
        {
            Outline.anchorMin = Vector3.zero;
            Outline.anchorMax = Vector3.one;
            Outline.sizeDelta = Vector2.one * (OutlineWidth + InnerBorderWidth);

            Background.anchorMin = Vector3.zero;
            Background.anchorMax = Vector3.one;
            Background.sizeDelta = Vector2.one * (InnerBorderWidth);
        }

        public void SetProgress(float proportionOfLoadingBar)
        {
            Progress = proportionOfLoadingBar;

            if (ProgressFill != null)
                ProgressFill.anchorMax = new Vector2(proportionOfLoadingBar, ProgressFill.anchorMax.y);
        }

        /// <summary>
        /// Updates a loading bar by the progress made by an asynchronous operation.
        /// The bar will interpolate between startingFillProportion and endingFillProportion as the operation progresses.
        /// </summary>
        /// <param name="skipFinalUpdate">
        /// If true, the bar will only fill before the operation has finished.
        /// This is useful in cases where an async operation will set its progress to 1, even when it has failed.
        /// </param>
        public IEnumerator FillUntilDone(AsyncOperation operation, float startingFillProportion,
            float endingFillProportion, bool skipFinalUpdate)
        {
            var previousFillProportion = startingFillProportion;
            var isDone = false;
            while (!isDone)
            {
                if (operation.isDone)
                {
                    isDone = true;
                }
                else
                {
                    var fillProportion = Mathf.Lerp(startingFillProportion, endingFillProportion, operation.progress);
                    fillProportion = Mathf.Max(previousFillProportion, fillProportion); // Progress can only increase.
                    SetProgress(fillProportion);
                    previousFillProportion = fillProportion;
                }

                yield return null;
            }

            if (!skipFinalUpdate)
            {
                var finalFillProportion = Mathf.Lerp(startingFillProportion, endingFillProportion, operation.progress);
                finalFillProportion = Mathf.Max(previousFillProportion, finalFillProportion);
                SetProgress(finalFillProportion);
            }
        }
    }
}
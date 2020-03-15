using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>FFT analyzer for an audio channel.</summary>
  public class FFTAnalyzer : MonoBehaviour
   {
      #region Variables

      ///<summary>Array for the samples. More samples mean better accuracy but it also needs more performance (default: 256).</summary>
      [Tooltip("Array for the samples. More samples mean better accuracy but it also needs more performance (default: 256)")]
      public float[] Samples = new float[256];

      ///<summary>Analyzed channel (0 = right, 1 = left, default: 0).</summary>
      [Tooltip("Analyzed channel (0 = right, 1 = left, default: 0).")] [Range(0, 1)] public int Channel = 0;

      ///<summary>FFT-algorithm to analyze the audio (default: BlackmanHarris).</summary>
      [Tooltip("FFT-algorithm to analyze the audio (default: BlackmanHarris).")] public FFTWindow FFTMode = FFTWindow.BlackmanHarris;

      #endregion


      #region MonoBehaviour methods

      public void Update()
      {
         AudioListener.GetSpectrumData(Samples, Channel, FFTMode);
      }

      #endregion
   }
}
// © 2015-2020 crosstales LLC (https://www.crosstales.com)
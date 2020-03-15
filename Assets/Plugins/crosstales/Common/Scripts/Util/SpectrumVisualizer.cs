using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Simple spectrum visualizer.</summary>
   public class SpectrumVisualizer : MonoBehaviour
   {
      #region Variables

      ///<summary>FFT-analyzer with the spectrum data.</summary>
      [Tooltip("FFT-analyzer with the spectrum data.")] public FFTAnalyzer Analyzer;

      ///<summary>Prefab for the frequency representation.</summary>
      [Tooltip("Prefab for the frequency representation.")] public GameObject VisualPrefab;

      ///<summary>Width per prefab.</summary>
      [Tooltip("Width per prefab.")] public float Width = 0.075f;

      ///<summary>Gain-power for the frequency.</summary>
      [Tooltip("Gain-power for the frequency.")] public float Gain = 70f;

      ///<summary>Frequency band from left-to-right (default: true).</summary>
      [Tooltip("Frequency band from left-to-right (default: true).")] public bool LeftToRight = true;

      ///<summary>Opacity of the material of the prefab (default: 1).</summary>
      [Tooltip("Opacity of the material of the prefab (default: 1).")] [Range(0f, 1f)] public float Opacity = 1f;

      private Transform tf;
      private Transform[] visualTransforms;

      private Vector3 visualPos = Vector3.zero;

      private int samplesPerChannel;

      #endregion


      #region MonoBehaviour methods

      public void Start()
      {
         tf = transform;
         samplesPerChannel = Analyzer.Samples.Length / 2;
         visualTransforms = new Transform[samplesPerChannel];

         for (int ii = 0; ii < samplesPerChannel; ii++)
         {
            //cut the upper frequencies >11000Hz
            GameObject tempCube;

            if (LeftToRight)
            {
               var position = tf.position;
               tempCube = Instantiate(VisualPrefab, new Vector3(position.x + ii * Width, position.y, position.z), Quaternion.identity);
            }
            else
            {
               var position = tf.position;
               tempCube = Instantiate(VisualPrefab, new Vector3(position.x - ii * Width, position.y, position.z), Quaternion.identity);
            }

            tempCube.GetComponent<Renderer>().material.color = BaseHelper.HSVToRGB(360f / samplesPerChannel * ii, 1f, 1f, Opacity);

            visualTransforms[ii] = tempCube.GetComponent<Transform>();
            visualTransforms[ii].parent = tf;
         }
      }

      public void Update()
      {
         for (int ii = 0; ii < visualTransforms.Length; ii++)
         {
            visualPos.Set(Width, Analyzer.Samples[ii] * Gain, Width);
            visualTransforms[ii].localScale = visualPos;
         }
      }

      #endregion
   }
}
// © 2015-2020 crosstales LLC (https://www.crosstales.com)
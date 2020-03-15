using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Enables or disable game objects for a given platform.</summary>
 public class PlatformController : MonoBehaviour
   {
      #region Variables

      ///<summary>Selected platforms for the controller.</summary>
      [Header("Configuration")] [Tooltip("Selected platforms for the controller.")] public System.Collections.Generic.List<Model.Enum.Platform> Platforms;

      ///<summary>Enable or disable the 'Objects' for the selected 'Platforms' (default: true).</summary>
      [Tooltip("Enable or disable the 'Objects' for the selected 'Platforms' (default: true).")] public bool Active = true;


      ///<summary>Selected objects for the controller.</summary>
      [Header("Objects")] [Tooltip("Selected objects for the controller.")] public GameObject[] Objects;

      protected Model.Enum.Platform currentPlatform;

      #endregion


      #region MonoBehaviour methods

      public virtual void Start()
      {
         selectPlatform();
      }

      #endregion


      #region Private methods

      protected void selectPlatform()
      {
         currentPlatform = BaseHelper.CurrentPlatform;

         activateGO();
      }

      protected void activateGO()
      {
         bool active = Platforms.Contains(currentPlatform) ? Active : !Active;

         foreach (var go in Objects.Where(go => go != null))
         {
            go.SetActive(active);
         }
      }

      #endregion
   }
}
// © 2017-2020 crosstales LLC (https://www.crosstales.com)
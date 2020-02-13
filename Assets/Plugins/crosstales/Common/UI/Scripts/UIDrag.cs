using UnityEngine;

namespace Crosstales.UI
{
   /// <summary>Allow to Drag the Windows around.</summary>
   public class UIDrag : MonoBehaviour
   {
      #region Variables

      private float offsetX;
      private float offsetY;

      private Transform tf;

      #endregion


      #region MonoBehaviour methods

      public void Start()
      {
         tf = transform;
      }

      #endregion


      #region Public methods

      ///<summary>Drag started.</summary>
      public void BeginDrag()
      {
         var position = tf.position;
         offsetX = position.x - Input.mousePosition.x;
         offsetY = position.y - Input.mousePosition.y;
      }

      ///<summary>While dragging.</summary>
      public void OnDrag()
      {
         tf.position = new Vector3(offsetX + Input.mousePosition.x, offsetY + Input.mousePosition.y);
      }

      #endregion
   }
}
// © 2017-2020 crosstales LLC (https://www.crosstales.com)
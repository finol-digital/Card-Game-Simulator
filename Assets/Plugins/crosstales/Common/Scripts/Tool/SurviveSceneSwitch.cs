using System.Linq;
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Allows any Unity gameobject to survive a scene switch. This is especially useful to keep the music playing while loading a new scene.</summary>
   [DisallowMultipleComponent]
   public class SurviveSceneSwitch : MonoBehaviour
   {
      #region Variables

      ///<summary>Objects which have to survive a scene switch.</summary>
      [Tooltip("Objects which have to survive a scene switch.")] public GameObject[] Survivors; //any object, like a RadioPlayer

      /// <summary>Don't destroy gameobject during scene switches (default: true).</summary>
      [Tooltip("Don't destroy gameobject during scene switches (default: true).")] public bool DontDestroy = true;

      private const float ensureParentTime = 1.5f;
      private float ensureParentTimer = 0f;

      private static SurviveSceneSwitch instance;

      //private static GameObject go;
      private static Transform tf;
      private static bool loggedOnlyOneInstance = false;

      #endregion


      #region MonoBehaviour methods

      public void OnEnable()
      {
         if (instance == null)
         {
            instance = this;
            //go = gameObject;
            tf = transform;

            if (!BaseHelper.isEditorMode && DontDestroy)
               DontDestroyOnLoad(tf.root.gameObject);

            //Debug.LogWarning("Using new instance!");
         }
         else
         {
            if (!BaseHelper.isEditorMode && DontDestroy && instance != this)
            {
               if (!loggedOnlyOneInstance)
               {
                  Debug.LogWarning("Only one active instance of 'SurviveSceneSwitch' allowed in all scenes!" + System.Environment.NewLine + "This object and all survivors will now be destroyed.");

                  loggedOnlyOneInstance = true;
               }

               foreach (var _go in Survivors.Where(_go => _go != null))
               {
                  Destroy(_go);
               }

               Destroy(gameObject, 0.2f);
            }

            //Debug.LogWarning("Using old instance!");
         }
      }

      public void Start()
      {
         ensureParentTimer = ensureParentTime;
      }

      public void Update()
      {
         ensureParentTimer += Time.deltaTime;

         if (Survivors != null && ensureParentTimer > ensureParentTime)
         {
            ensureParentTimer = 0f;

            foreach (var _go in Survivors.Where(_go => _go != null))
            {
               _go.transform.SetParent(tf);
            }
         }
      }

      #endregion
   }
}
// © 2016-2020 crosstales LLC (https://www.crosstales.com)
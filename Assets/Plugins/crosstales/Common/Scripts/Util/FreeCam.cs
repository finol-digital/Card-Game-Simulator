using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>
   /// A simple free camera to be added to a Unity game object.
   /// 
   /// Keys:
   ///	wasd / arrows	- movement
   ///	q/e 			- up/down (local space)
   ///	r/f 			- up/down (world space)
   ///	pageup/pagedown	- up/down (world space)
   ///	hold shift		- enable fast movement mode
   ///	right mouse  	- enable free look
   ///	mouse			- free look / rotation
   /// </summary>
 public class FreeCam : MonoBehaviour
   {
      #region Variables

      /// <summary>Normal speed of camera movement.</summary>
      public float MovementSpeed = 10f;

      /// <summary>Speed of camera movement when shift is held down.</summary>
      public float FastMovementSpeed = 100f;

      /// <summary>Sensitivity for free look.</summary>
      public float FreeLookSensitivity = 3f;

      /// <summary>Amount to zoom the camera when using the mouse wheel.</summary>
      public float ZoomSensitivity = 10f;

      /// <summary>Amount to zoom the camera when using the mouse wheel (fast mode).</summary>
      public float FastZoomSensitivity = 50f;

      private Transform tf;
      private bool looking = false;

      #endregion


      #region MonoBehaviour methods

      public void Start()
      {
         tf = transform;
      }

      public void Update()
      {
         bool fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
         float movementSpeed = fastMode ? FastMovementSpeed : MovementSpeed;

         if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
         {
            tf.position += Time.deltaTime * movementSpeed * -tf.right;
         }

         if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
         {
            tf.position += Time.deltaTime * movementSpeed * tf.right;
         }

         if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
         {
            tf.position += Time.deltaTime * movementSpeed * tf.forward;
         }

         if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
         {
            tf.position += Time.deltaTime * movementSpeed * -tf.forward;
         }

         if (Input.GetKey(KeyCode.Q))
         {
            tf.position += Time.deltaTime * movementSpeed * tf.up;
         }

         if (Input.GetKey(KeyCode.E))
         {
            tf.position += Time.deltaTime * movementSpeed * -tf.up;
         }

         if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
         {
            tf.position += Time.deltaTime * movementSpeed * Vector3.up;
         }

         if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
         {
            tf.position += Time.deltaTime * movementSpeed * -Vector3.up;
         }

         if (looking)
         {
            var localEulerAngles = tf.localEulerAngles;
            float newRotationX = localEulerAngles.y + Input.GetAxis("Mouse X") * FreeLookSensitivity;
            float newRotationY = localEulerAngles.x - Input.GetAxis("Mouse Y") * FreeLookSensitivity;
            localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            tf.localEulerAngles = localEulerAngles;
         }

         float axis = Input.GetAxis("Mouse ScrollWheel");
         if (Mathf.Abs(axis) > BaseConstants.FLOAT_TOLERANCE)
         {
            var zoomSensitivity = fastMode ? FastZoomSensitivity : ZoomSensitivity;
            tf.position += zoomSensitivity * axis * tf.forward;
         }

         if (Input.GetKeyDown(KeyCode.Mouse1))
         {
            StartLooking();
         }
         else if (Input.GetKeyUp(KeyCode.Mouse1))
         {
            StopLooking();
         }
      }

      public void OnDisable()
      {
         StopLooking();
      }

      #endregion


      #region Public methods

      /// <summary>
      /// Enable free looking.
      /// </summary>
      public void StartLooking()
      {
         looking = true;
         Cursor.visible = false;
         Cursor.lockState = CursorLockMode.Locked;
      }

      /// <summary>
      /// Disable free looking.
      /// </summary>
      public void StopLooking()
      {
         looking = false;
         Cursor.visible = true;
         Cursor.lockState = CursorLockMode.None;
      }

      #endregion
   }
}
// © 2019-2020 crosstales LLC (https://www.crosstales.com)
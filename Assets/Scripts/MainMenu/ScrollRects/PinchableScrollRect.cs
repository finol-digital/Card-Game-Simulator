using System.Collections;
 using System.Collections.Generic;
 using UnityEngine;
 using UnityEngine.UI;
 public class PinchableScrollRect : SecondaryScrollView
 {
     float _minZoom = .75f;
     float _maxZoom = 1.5f;
     float _zoomLerpSpeed = 1f;
     float _currentZoom = 1;
     bool _isPinching = false;
     float _startPinchDist;
     float _startPinchZoom;
     Vector2 _startPinchCenterPosition;
     Vector2 _startPinchScreenPosition;
     float _mouseWheelSensitivity = .1f;
     bool blockPan = false;
 
     protected override void Awake()
     {
         Input.multiTouchEnabled = true;
     }
 
     private void Update()
     {
         if (Input.touchCount == 2)
         {
             if (!_isPinching)
             {
                 _isPinching = true;
                 OnPinchStart();
             }
             OnPinch();
         }
         else
         {
             _isPinching = false;
             if (Input.touchCount == 0)
             {
                 blockPan = false;
             }
         }
         //pc input
         float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
         if (Mathf.Abs(scrollWheelInput) > float.Epsilon)
         {
             _currentZoom *= 1 + scrollWheelInput * _mouseWheelSensitivity;
             _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
             _startPinchScreenPosition = (Vector2)Input.mousePosition;
             RectTransformUtility.ScreenPointToLocalPointInRectangle(content, _startPinchScreenPosition, null, out _startPinchCenterPosition);
             Vector2 pivotPosition = new Vector3(content.pivot.x * content.rect.size.x, content.pivot.y * content.rect.size.y);
             Vector2 posFromBottomLeft = pivotPosition + _startPinchCenterPosition;
             SetPivot(content, new Vector2(posFromBottomLeft.x / content.rect.width, posFromBottomLeft.y / content.rect.height));
         }
         //pc input end
 
         if (Mathf.Abs(content.localScale.x - _currentZoom) > 0.001f)
             content.localScale = Vector3.Lerp(content.localScale, Vector3.one * _currentZoom, _zoomLerpSpeed * Time.deltaTime);
     }
 
     protected override void SetContentAnchoredPosition(Vector2 position)
     {
         if (_isPinching || blockPan) return;
         base.SetContentAnchoredPosition(position);
     }
 
     void OnPinchStart()
     {
         Vector2 pos1 = Input.touches[0].position;
         Vector2 pos2 = Input.touches[1].position;
 
         _startPinchDist = Distance(pos1, pos2) * content.localScale.x;
         _startPinchZoom = _currentZoom;
         _startPinchScreenPosition = (pos1 + pos2) / 2;
         RectTransformUtility.ScreenPointToLocalPointInRectangle(content, _startPinchScreenPosition, null, out _startPinchCenterPosition);
 
         Vector2 pivotPosition = new Vector3(content.pivot.x * content.rect.size.x, content.pivot.y * content.rect.size.y);
         Vector2 posFromBottomLeft = pivotPosition + _startPinchCenterPosition;
 
         SetPivot(content, new Vector2(posFromBottomLeft.x / content.rect.width, posFromBottomLeft.y / content.rect.height));
         blockPan = true;
     }
 
     void OnPinch()
     {
         float currentPinchDist = Distance(Input.touches[0].position, Input.touches[1].position) * content.localScale.x;
         _currentZoom = (currentPinchDist / _startPinchDist) * _startPinchZoom;
         _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
     }
 
     float Distance(Vector2 pos1, Vector2 pos2)
     {
         RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos1, null, out pos1);
         RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos2, null, out pos2);
         return Vector2.Distance(pos1, pos2);
     }
 
     static void SetPivot(RectTransform rectTransform, Vector2 pivot)
     {
         if (rectTransform == null) return;
 
         Vector2 size = rectTransform.rect.size;
         Vector2 deltaPivot = rectTransform.pivot - pivot;
         Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y) * rectTransform.localScale.x;
         rectTransform.pivot = pivot;
         rectTransform.localPosition -= deltaPosition;
     }
 }
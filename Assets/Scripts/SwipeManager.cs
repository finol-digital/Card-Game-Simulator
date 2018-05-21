using UnityEngine;
using System.Collections.Generic;
using System.Linq;
 
class CardinalDirection
{
    public static readonly Vector2 Up = new Vector2 (0, 1);
    public static readonly Vector2 Down = new Vector2 (0, -1);
    public static readonly Vector2 Right = new Vector2 (1, 0);
    public static readonly Vector2 Left = new Vector2 (-1, 0);
    public static readonly Vector2 UpRight = new Vector2 (1, 1);
    public static readonly Vector2 UpLeft = new Vector2 (-1, 1);
    public static readonly Vector2 DownRight = new Vector2 (1, -1);
    public static readonly Vector2 DownLeft = new Vector2 (-1, -1);
}
 
public enum Swipe
{
    None,
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight
};
 
public class SwipeManager : MonoBehaviour
{
    #region Inspector Variables
 
    [Tooltip("Min swipe distance (inches) to register as swipe")]
    [SerializeField] float minSwipeLength = 0.5f;
 
    [Tooltip("If true, a swipe is counted when the min swipe length is reached. If false, a swipe is counted when the touch/click ends.")]
    [SerializeField] bool triggerSwipeAtMinLength = false;
 
    [Tooltip("Whether to detect eight or four cardinal directions")]
    [SerializeField] bool useEightDirections = false;
 
    #endregion
 
    const float eightDirAngle = 0.906f;
    const float fourDirAngle = 0.5f;
    const float defaultDPI = 72f;
    const float dpcmFactor = 2.54f;
 
    static Dictionary<Swipe, Vector2> cardinalDirections = new Dictionary<Swipe, Vector2> ()
    {
        { Swipe.Up,         CardinalDirection.Up                 },
        { Swipe.Down,         CardinalDirection.Down             },
        { Swipe.Right,         CardinalDirection.Right             },
        { Swipe.Left,         CardinalDirection.Left             },
        { Swipe.UpRight,     CardinalDirection.UpRight             },
        { Swipe.UpLeft,     CardinalDirection.UpLeft             },
        { Swipe.DownRight,     CardinalDirection.DownRight         },
        { Swipe.DownLeft,     CardinalDirection.DownLeft         }
    };
 
    public delegate void OnSwipeDetectedHandler(Swipe swipeDirection, Vector2 swipeVelocity);
 
    static OnSwipeDetectedHandler _OnSwipeDetected;
    public static event OnSwipeDetectedHandler OnSwipeDetected
    {
        add {
            _OnSwipeDetected += value;
            autoDetectSwipes = true;
        }
        remove {
            _OnSwipeDetected -= value;
        }
    }
 
    public static Vector2 swipeVelocity;
 
    static float dpcm;
    static float swipeStartTime;
    static float swipeEndTime;
    static bool autoDetectSwipes;
    static bool swipeEnded;
    static Swipe swipeDirection;
    static Vector2 firstPressPos;
    static Vector2 secondPressPos;
    static SwipeManager instance;
 
 
    void Awake ()
    {
        instance = this;
        float dpi = (Screen.dpi == 0) ? defaultDPI : Screen.dpi;
        dpcm = dpi / dpcmFactor;
    }
 
    void Update ()
    {
        if (autoDetectSwipes) {
            DetectSwipe();
        }
    }
 
    /// <summary>
    /// Attempts to detect the current swipe direction.
    /// Should be called over multiple frames in an Update-like loop.
    /// </summary>
    static void DetectSwipe ()
    {
        if (GetTouchInput() || GetMouseInput()) {
            // Swipe already ended, don't detect until a new swipe has begun
            if (swipeEnded) {
                return;
            }
 
            Vector2 currentSwipe =  secondPressPos - firstPressPos;
            float swipeCm = currentSwipe.magnitude / dpcm;
 
            // Check the swipe is long enough to count as a swipe (not a touch, etc)
            if (swipeCm < instance.minSwipeLength) {
                // Swipe was not long enough, abort
                if (!instance.triggerSwipeAtMinLength) {
                    if (Application.isEditor) {
                        Debug.Log("[SwipeManager] Swipe was not long enough.");
                    }
 
                    swipeDirection = Swipe.None;
                }
 
                return;
            }
 
            swipeEndTime = Time.time;
            swipeVelocity = currentSwipe * (swipeEndTime - swipeStartTime);
            swipeDirection = GetSwipeDirByTouch(currentSwipe);
            swipeEnded = true;
 
            if (_OnSwipeDetected != null) {
                _OnSwipeDetected(swipeDirection, swipeVelocity);
            }
        } else {
            swipeDirection = Swipe.None;
        }
    }
 
	public static bool IsSwiping			 () {	  return swipeDirection != Swipe.None; 			}
    public static bool IsSwipingRight        () {     return IsSwipingDirection(Swipe.Right);   	}
    public static bool IsSwipingLeft         () {     return IsSwipingDirection(Swipe.Left);    	}
    public static bool IsSwipingUp           () {     return IsSwipingDirection(Swipe.Up);          }
    public static bool IsSwipingDown         () {     return IsSwipingDirection(Swipe.Down);        }
    public static bool IsSwipingDownLeft     () {     return IsSwipingDirection(Swipe.DownLeft);    }
    public static bool IsSwipingDownRight    () {     return IsSwipingDirection(Swipe.DownRight);   }
    public static bool IsSwipingUpLeft       () {     return IsSwipingDirection(Swipe.UpLeft);      }
    public static bool IsSwipingUpRight      () {     return IsSwipingDirection(Swipe.UpRight);     }
 
    #region Helper Functions
 
    static bool GetTouchInput ()
    {
        if (Input.touches.Length > 0) {
            Touch t = Input.GetTouch(0);
 
            // Swipe/Touch started
            if (t.phase == TouchPhase.Began) {
                firstPressPos = t.position;
                swipeStartTime = Time.time;
                swipeEnded = false;
            // Swipe/Touch ended
            } else if (t.phase == TouchPhase.Ended) {
                secondPressPos = t.position;
                return true;
            // Still swiping/touching
            } else {
                // Could count as a swipe if length is long enough
                if (instance.triggerSwipeAtMinLength) {
                    return true;
                }
            }
        }
 
        return false;
    }
 
    static bool GetMouseInput ()
    {
        // Swipe/Click started
        if (Input.GetMouseButtonDown(0)) {
            firstPressPos = (Vector2)Input.mousePosition;
            swipeStartTime = Time.time;
			swipeEnded = false;
        // Swipe/Click ended
        } else if (Input.GetMouseButtonUp(0)) {
            secondPressPos = (Vector2)Input.mousePosition;
            return true;
        // Still swiping/clicking
        } else {
            // Could count as a swipe if length is long enough
            if (instance.triggerSwipeAtMinLength) {
                return true;
            }
        }
 
        return false;
    }
 
    static bool IsDirection (Vector2 direction, Vector2 cardinalDirection)
    {
        var angle = instance.useEightDirections ? eightDirAngle : fourDirAngle;
        return Vector2.Dot(direction, cardinalDirection) > angle;
    }
 
    static Swipe GetSwipeDirByTouch (Vector2 currentSwipe)
    {
        currentSwipe.Normalize();
        var swipeDir = cardinalDirections.FirstOrDefault(dir => IsDirection(currentSwipe, dir.Value));
        return swipeDir.Key;
    }
 
    static bool IsSwipingDirection (Swipe swipeDir)
    {
        DetectSwipe();
        return swipeDirection == swipeDir;
    }
 
    #endregion
}
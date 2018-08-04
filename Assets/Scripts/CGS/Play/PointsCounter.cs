using CGS.Play.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace CGS.Play
{
    public class PointsCounter : MonoBehaviour
    {
        public Text pointsText;

        protected int Count
        {
            get { return CGSNetManager.Instance.LocalPlayer.CurrentScore; }
            set { CGSNetManager.Instance.LocalPlayer.RequestScoreUpdate(value); }
        }

        public void Decrement()
        {
            Count--;
        }

        public void Increment()
        {
            Count++;
        }

        public void UpdateText()
        {
            pointsText.text = Count.ToString();
        }
    }
}

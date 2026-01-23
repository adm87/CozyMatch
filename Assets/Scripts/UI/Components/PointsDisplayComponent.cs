using UnityEngine;

namespace Cozy.Match.UI.Components
{
    public class PointDisplayComponent : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI pointsText;

        private int currentPoints = 0;
        private int displayedPoints = 0;

        private void Awake()
        {
            if (pointsText != null)
            {
                pointsText.text = "0";
            }
        }

        public void AddPoints(int points)
        {
            currentPoints += points;
        }

        private void Update()
        {
            if (displayedPoints < currentPoints)
            {
                displayedPoints += Mathf.CeilToInt((currentPoints - displayedPoints) * 0.1f);
                if (displayedPoints > currentPoints)
                {
                    displayedPoints = currentPoints;
                }
                pointsText.text = displayedPoints.ToString("N0");
            }
        }
    }
}
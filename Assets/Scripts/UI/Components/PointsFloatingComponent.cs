using System;
using UnityEngine;

namespace Cozy.Match.UI.Components
{
    public class PointsFloatingComponent : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI pointsText;

        [SerializeField]
        private CanvasGroup canvasGroup;

        public float Alpha
        {
            get => canvasGroup.alpha;
            set => canvasGroup.alpha = value;
        }

        public void SetPoints(int points)
        {
            if (pointsText != null)
            {
                pointsText.text = points.ToString();
            }
        }

        private void Update()
        {
            transform.LookAt(Camera.main.transform);
        }
    }
}
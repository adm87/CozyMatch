using UnityEngine;
using UnityEngine.UI;

namespace Cozy.Match.UI.Components
{
    public class PieceDisplayComponent : MonoBehaviour
    {
        [SerializeField]
        private Image currentImage;

        [SerializeField]
        private Image nextImage;

        public void SetCurrentPiece(Sprite sprite)
        {
            if (currentImage != null)
            {
                currentImage.sprite = sprite;
            }
        }

        public void SetNextPiece(Sprite sprite)
        {
            if (nextImage != null)
            {
                nextImage.sprite = sprite;
            }
        }
    }
}
using Cozy.Hexagons;
using UnityEngine;

namespace Cozy.Match.Puzzle.Components
{
    public class PieceComponent : MonoBehaviour
    {
        public int PieceID { get; set; }

        public Hexagon Hexagon { get; set; }
    }
}
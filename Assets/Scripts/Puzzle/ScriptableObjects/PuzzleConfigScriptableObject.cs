using Cozy.Hexagons.ScriptableObjects;
using UnityEngine;

namespace Cozy.Match.Puzzle.ScriptableObjects
{
    [CreateAssetMenu(fileName = "PuzzleConfig", menuName = "Cozy/Match/Puzzle/Puzzle Config")]
    public class PuzzleConfigScriptableObject : HexagonConfigScriptableObject
    {
        [SerializeField]
        private uint initialPieceCount;
        
        public uint InitialPieceCount => initialPieceCount;

        [SerializeField]
        private PuzzlePieceConfigScriptableObject puzzlePieceConfig;

        public PuzzlePieceConfigScriptableObject PuzzlePieceConfig => puzzlePieceConfig;
    }
}
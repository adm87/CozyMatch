using System;
using UnityEngine;

namespace Cozy.Match.Puzzle.ScriptableObjects
{
    [Serializable]
    public class PuzzlePieceConfig
    {
        public GameObject PiecePrefab;

        public PieceSpawnRate SpawnRate;
    }

    [CreateAssetMenu(fileName = "PuzzlePieceConfig", menuName = "Cozy/Match/Puzzle/Puzzle Piece Config")]
    public class PuzzlePieceConfigScriptableObject : ScriptableObject
    {
        [SerializeField]
        private PuzzlePieceConfig[] configuration;

        public PuzzlePieceConfig[] Configuration => configuration;
    }
}
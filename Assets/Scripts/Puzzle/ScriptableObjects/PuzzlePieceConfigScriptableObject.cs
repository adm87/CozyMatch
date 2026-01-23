using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cozy.Match.Puzzle.ScriptableObjects
{
    [Serializable]
    public class PuzzlePieceConfig
    {
        public GameObject PiecePrefab;

        public Sprite Icon;

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
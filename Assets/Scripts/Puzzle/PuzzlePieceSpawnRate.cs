using UnityEngine;

namespace Cozy.Match.Puzzle
{    
    [System.Serializable]
    public struct PieceSpawnRate
    {
        public AnimationCurve InitialSpawnRate;

        public AnimationCurve SpawnRate;
    }
}
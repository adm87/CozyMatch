using System.Linq;
using UnityEngine;

namespace Cozy.Match.Puzzle
{
    public class PuzzlePieceSpawner
    {
        private readonly PieceSpawnRate[] spawnRates;

        public PuzzlePieceSpawner(PieceSpawnRate[] spawnRates)
        {
            this.spawnRates = spawnRates;
        }
        
        public uint SelectInitialPieceID(float time)
        {
            var curves = spawnRates.Select(rate => rate.InitialSpawnRate).ToArray();
            return SelectPieceID(curves, time);
        }

        public uint SelectPieceID(float time)
        {
            var curves = spawnRates.Select(rate => rate.SpawnRate).ToArray();
            return SelectPieceID(curves, time);
        }

        private uint SelectPieceID(AnimationCurve[] curves, float time)
        {
            float totalRate = 0f;
            foreach (var curve in curves)
            {
                totalRate += curve.Evaluate(time);
            }

            float randomValue = Random.Range(0f, totalRate);
            float cumulativeRate = 0f;

            for (uint i = 0; i < curves.Length; i++)
            {
                if (curves[i].keys.Length == 0)
                {
                    continue;
                }

                cumulativeRate += curves[i].Evaluate(time);
                if (randomValue <= cumulativeRate)
                {
                    return i;
                }
            }

            return (uint)curves.Length - 1;
        }
    }
}
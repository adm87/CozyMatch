using System.Collections.Generic;
using System.Linq;
using Cozy.Hexagons;

namespace Cozy.Match.Puzzle
{
    public class Puzzle
    {
        public const uint RequiredMatchCount = 3;

        private HexagonGrid grid;

        public HexagonGrid Grid => grid;

        private Dictionary<long, uint> pieces;

        public Dictionary<long, uint> Pieces => pieces;

        private PuzzlePieceSpawner pieceSpawner;

        public PuzzlePieceSpawner PieceSpawner => pieceSpawner;

        private HexagonConfiguration hexConfig;

        public Puzzle(HexagonConfiguration config, PieceSpawnRate[] spawnRates)
        {
            grid = new HexagonGrid();
            grid.BuildFromConfiguration(config);

            pieceSpawner = new PuzzlePieceSpawner(spawnRates);

            hexConfig = config;
        }

        public void SpawnBoard(uint count, Puzzle puzzle)
        {
            pieces = new();

            List<Hexagon> availableHexagons = new();
            puzzle.Grid.ForEach(hexagon =>
            {
                availableHexagons.Add(hexagon);
                return true;
            });

            for (uint i = 0; i < count && availableHexagons.Count > 0; i++)
            {
                float time = (float)i / count;
                uint selectedPieceID = pieceSpawner.SelectInitialPieceID(time);

                if (!TryPlacePiece(availableHexagons, selectedPieceID, out Hexagon placedHexagon))
                {
                    continue;
                }

                long hexId = HexagonEncoder.Encode(placedHexagon);
                pieces[hexId] = selectedPieceID;
            }
        }

        private bool TryPlacePiece(List<Hexagon> availableHexagons, uint pieceID, out Hexagon placedHexagon)
        {
            List<Hexagon> candidateHexagons = new(availableHexagons);

            while (candidateHexagons.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidateHexagons.Count);
                Hexagon hexagon = candidateHexagons[randomIndex];
                candidateHexagons.RemoveAt(randomIndex);

                if (SimulateMatch(hexagon, pieceID).Count() < RequiredMatchCount - 1)
                {
                    placedHexagon = hexagon;
                    availableHexagons.Remove(hexagon);
                    return true;
                }
            }

            placedHexagon = default;
            return false;
        }

        public IEnumerable<Hexagon> SimulateMatch(Hexagon hexagon, uint pieceId)
        {
            HashSet<Hexagon> matched = new();
            HashSet<long> visited = new();

            foreach (var (dq, dr) in GetNeighborOffsets(hexConfig))
            {
                Hexagon neighbor = new(hexagon.Q + dq, hexagon.R + dr); 
                InternalMatchSearch(neighbor, pieceId, matched, visited);
            }

            return matched;
        }
    
        public void InternalMatchSearch(Hexagon hexagon, uint pieceId, HashSet<Hexagon> matched, HashSet<long> visited)
        {
            long hexId = HexagonEncoder.Encode(hexagon);

            if (visited.Contains(hexId))
            {
                return;
            }

            visited.Add(hexId);

            if (!pieces.TryGetValue(hexId, out uint existingPieceId) || existingPieceId != pieceId)
            {
                return;
            }

            matched.Add(hexagon);

            foreach (var (dq, dr) in GetNeighborOffsets(hexConfig))
            {
                Hexagon neighbor = new(hexagon.Q + dq, hexagon.R + dr);
                InternalMatchSearch(neighbor, pieceId, matched, visited);
            }
        }

        private static (int q, int r)[] GetNeighborOffsets(HexagonConfiguration config)
        {
            return config.CoordinateSystem switch
            {
                HexagonCoordinateSystem.Offset => HexagonMath.OffsetNeighbors[config.Orientation][config.OffsetGrid.OffsetParity],
                HexagonCoordinateSystem.Axial => HexagonMath.AxialNeighbors,
                _ => throw new System.Exception("Unsupported coordinate system"),
            };
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Cozy.Match.Puzzle.Components
{
    public class PiecePoolComponent : MonoBehaviour
    {
        private class PiecePool
        {
            public GameObject PiecePrefab;

            public GameObject PieceContainer;

            public List<GameObject> PooledPieces;
        }

        private Dictionary<int, PiecePool> piecePools;

        public void AddPiecePool(int pieceId, GameObject piecePrefab)
        {
            piecePools ??= new Dictionary<int, PiecePool>();

            if (!piecePools.ContainsKey(pieceId))
            {
                var pool = new PiecePool
                {
                    PiecePrefab = piecePrefab,
                    PooledPieces = new List<GameObject>(),
                    PieceContainer = new GameObject($"PiecePool_{pieceId}")
                };
                pool.PieceContainer.transform.SetParent(transform);
                piecePools.Add(pieceId, pool);
            }
        }

        public GameObject GetPiece(int pieceId)
        {
            if (piecePools != null && piecePools.TryGetValue(pieceId, out var pool))
            {
                GameObject piece;
                if (pool.PooledPieces.Count > 0)
                {
                    piece = pool.PooledPieces[pool.PooledPieces.Count - 1];
                    pool.PooledPieces.RemoveAt(pool.PooledPieces.Count - 1);
                    piece.SetActive(true);
                }
                else
                {
                    piece = Instantiate(pool.PiecePrefab);
                    piece.GetComponent<PieceComponent>().PieceID = pieceId;
                }

                return piece;
            }

            return null;
        }

        public void ReturnPiece(int pieceId, GameObject piece)
        {
            if (piecePools != null && piecePools.TryGetValue(pieceId, out var pool))
            {
                piece.SetActive(false);
                piece.transform.SetParent(pool.PieceContainer.transform);
                pool.PooledPieces.Add(piece);
            }
        }
    }
}
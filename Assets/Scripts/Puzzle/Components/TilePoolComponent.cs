using System.Collections.Generic;
using UnityEngine;

namespace Cozy.Match.Puzzle.Components
{
    public class TilePoolComponent : MonoBehaviour
    {
        [SerializeField]
        private GameObject tilePrefab;

        private List<GameObject> pooledTiles;

        private void Awake()
        {
            pooledTiles = new List<GameObject>();
        }

        public GameObject GetTile()
        {
            GameObject tile;
            if (pooledTiles.Count > 0)
            {
                tile = pooledTiles[pooledTiles.Count - 1];
                pooledTiles.RemoveAt(pooledTiles.Count - 1);
                tile.SetActive(true);
            }
            else
            {
                tile = Instantiate(tilePrefab);
            }

            return tile;
        }

        public void ReturnTile(GameObject tile)
        {
            tile.SetActive(false);
            pooledTiles.Add(tile);
        }
    }
}
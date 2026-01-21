using System.Linq;
using Cozy.Hexagons;
using Cozy.Match.Puzzle.ScriptableObjects;
using UnityEngine;

namespace Cozy.Match.Puzzle.Components
{
    public class PuzzleComponent : MonoBehaviour, IPuzzleInputReceiver
    {       
        [SerializeField]
        private PuzzleConfigScriptableObject puzzleConfig;

        [SerializeField]
        private GameObject cursor;

        [SerializeField]
        private PuzzleInputComponent inputComponent;

        [SerializeField]
        private TilePoolComponent tilePool;

        [SerializeField]
        private PiecePoolComponent piecePool;

        [SerializeField]
        private uint spawnRateRange;

        private GameObject tileContainer;
        private GameObject pieceContainer;
        private GameObject nextPiece;

        private Puzzle puzzle;

        private uint spawnIndex;
        private bool canPlacePiece;

        private void Awake()
        {
            tileContainer = new GameObject("Tiles");
            tileContainer.transform.SetParent(transform);

            pieceContainer = new GameObject("Pieces");
            pieceContainer.transform.SetParent(transform);

            for (int i = 0; i < puzzleConfig.PuzzlePieceConfig.Configuration.Length; i++)
            {
                var config = puzzleConfig.PuzzlePieceConfig.Configuration[i];
                piecePool.AddPiecePool(i, config.PiecePrefab);
            }
        }

        private void Start()
        {
            InitializePuzzleGrid();
            RefreshPieceView();
            SetNextPiece();
        }

        private void OnEnable()
        {
            inputComponent.Subscribe(this);
        }

        private void OnDisable()
        {
            inputComponent.Unsubscribe(this);
        }
        
        private void InitializePuzzleGrid()
        {
            var spawnRates = puzzleConfig.PuzzlePieceConfig.Configuration.Select(config => config.SpawnRate).ToArray();
            var orientation = puzzleConfig.Configuration.Orientation;
            var radius = puzzleConfig.Configuration.HexRadius;
            var rotY = orientation == HexagonOrientation.FlatTop ? 30f : 0f;
            var angles = Quaternion.Euler(0f, rotY, 0f);

            puzzle = new Puzzle(puzzleConfig.Configuration, spawnRates);
            puzzle.Grid.ForEach(hexagon =>
            {
                var tile = tilePool.GetTile();
                
                var (x, y) = HexagonMath.FromHex[orientation](hexagon, radius);
                tile.transform.position = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + y);
                tile.transform.SetParent(tileContainer.transform);
                tile.transform.rotation = angles;
                return true;
            });
            puzzle.SpawnBoard(puzzleConfig.InitialPieceCount, puzzle);

            cursor.transform.rotation = angles;
            cursor.SetActive(false);
        }

        private void RefreshPieceView()
        {
            ClearPieces();

            foreach (var kvp in puzzle.Pieces)
            {
                var hexagonID = kvp.Key;
                var pieceID = kvp.Value;

                var hexagon = HexagonEncoder.Decode(hexagonID);
                var orientation = puzzleConfig.Configuration.Orientation;
                var radius = puzzleConfig.Configuration.HexRadius;
                var (x, y) = HexagonMath.FromHex[orientation](hexagon, radius);

                var piece = piecePool.GetPiece((int)pieceID);
                piece.transform.position = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + y);
                piece.transform.SetParent(pieceContainer.transform);
                piece.GetComponent<PieceComponent>().Hexagon = hexagon;
            }
        }

        private void SetNextPiece()
        {
            if (nextPiece != null)
            {
                ClearNextPiece();
            }

            float time = (float)spawnIndex / spawnRateRange;
            uint selectedPieceID = puzzle.PieceSpawner.SelectPieceID(time);

            nextPiece = piecePool.GetPiece((int)selectedPieceID);
            nextPiece.transform.SetParent(transform);
            nextPiece.transform.position = cursor.transform.position;
            nextPiece.SetActive(false);

            spawnIndex++;

            if (spawnIndex >= spawnRateRange)
            {
                spawnIndex = 0;
            }
        }

        private void ClearNextPiece()
        {
            if (nextPiece != null)
            {
                var pieceComponent = nextPiece.GetComponent<PieceComponent>();
                piecePool.ReturnPiece(pieceComponent.PieceID, nextPiece);
                nextPiece = null;
            }
        }

        private void ClearPieces()
        {
            foreach (Transform child in pieceContainer.transform)
            {
                if (child.TryGetComponent<PieceComponent>(out var pieceComponent))
                {
                    piecePool.ReturnPiece(pieceComponent.PieceID, child.gameObject);
                }
            }
        }

        public void OnInputDown(PuzzleInputState inputState)
        {
            
        }

        public void OnInputUp(PuzzleInputState inputState)
        {
            
        }

        public void OnInputClicked(PuzzleInputState inputState)
        {
            if (!canPlacePiece)
            {
                return;
            }

            Debug.Log("Placing Piece");
        }

        public void OnInputMove(PuzzleInputState inputState)
        {
            canPlacePiece = false;

            Hexagon hexagon = HexagonMath.ToHex[puzzleConfig.Configuration.Orientation](
                inputState.Position.x - transform.position.x,
                inputState.Position.z - transform.position.z,
                puzzleConfig.Configuration.HexRadius
            );

            var hexId = HexagonEncoder.Encode(hexagon);
            if (puzzle.Pieces.ContainsKey(hexId))
            {
                cursor.SetActive(false);
                nextPiece.SetActive(false);
                return;
            }

            if (puzzle.Grid.TryGetHexagon(hexagon, out Hexagon foundHexagon))
            {
                var (x, y) = HexagonMath.FromHex[puzzleConfig.Configuration.Orientation](foundHexagon, puzzleConfig.Configuration.HexRadius);
                cursor.transform.position = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + y);
                cursor.SetActive(true);

                nextPiece.transform.position = cursor.transform.position;
                nextPiece.SetActive(true);

                canPlacePiece = true;
            }
            else
            {
                cursor.SetActive(false);
                nextPiece.SetActive(false);
            }
        }

        private void OnDrawGizmos()
        {
            if (puzzleConfig == null)
            {
                return;
            }

            var orientation = puzzleConfig.Configuration.Orientation;
            var radius = puzzleConfig.Configuration.HexRadius;

            puzzle = new Puzzle(puzzleConfig.Configuration, puzzleConfig.PuzzlePieceConfig.Configuration.Select(config => config.SpawnRate).ToArray());
            puzzle.Grid.BuildFromConfiguration(puzzleConfig.Configuration);

            puzzle.Grid.ForEach(hexagon =>
            {
                var (x, y) = HexagonMath.FromHex[orientation](hexagon, radius);
                DrawHexOutline(x, y, radius, orientation);
                return true;
            });
        }

        private void DrawHexOutline(float xHex, float yHex, float radius, HexagonOrientation orientation)
        {
            Vector3 position = transform.position;
            for (int corner = 0; corner < 6; corner++)
            {
                var (x1, y1) = HexagonMath.GetCorner(radius, corner, orientation);
                var (x2, y2) = HexagonMath.GetCorner(radius, (corner + 1) % 6, orientation);

                Gizmos.DrawLine(
                    new Vector3(position.x + xHex + x1, position.y, position.z + yHex + y1),
                    new Vector3(position.x + xHex + x2, position.y, position.z + yHex + y2)
                );
            }
        }
    }
}
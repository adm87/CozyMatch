using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cozy.Hexagons;
using Cozy.Match.Puzzle.ScriptableObjects;
using Cozy.Match.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cozy.Match.Puzzle.Components
{
    public class PuzzleComponent : MonoBehaviour, IPuzzleInputReceiver
    {
        private static readonly WaitForSeconds waitForSeconds0_2 = new(0.2f);
        private static readonly WaitForEndOfFrame waitForEndOfFrame = new();

        [SerializeField]
        private PuzzleConfigScriptableObject puzzleConfig;

        [SerializeField]
        private PieceDisplayComponent pieceDisplay;

        [SerializeField]
        private PointDisplayComponent pointsDisplay;

        [SerializeField]
        private FloatingPointSpawnerComponent floatingPointSpawner;

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

        [SerializeField]
        private List<Hexagon> potentialPieces;

        private Coroutine potentialPieceCoroutine;

        private GameObject tileContainer;
        private GameObject pieceContainer;
        private GameObject currentPiece;

        private Hexagon? hoveredHex;

        private Puzzle puzzle;

        private uint spawnIndex;
        private bool canPlacePiece;
        private bool isPlacingPiece;

        private uint nextPieceID;

        private readonly Dictionary<long, GameObject> pieceViews = new();

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
            SetCurrentPiece();

            inputComponent.Subscribe(this);
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

            nextPieceID = puzzle.PieceSpawner.SelectPieceID(0f);

            cursor.transform.rotation = angles;
            cursor.SetActive(false);
        }

        private void RefreshPieceView()
        {
            ClearPieceViews();

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

                pieceViews[hexagonID] = piece;
            }
        }

        private void SetCurrentPiece()
        {
            if (currentPiece != null)
            {
                ClearCurrentPiece();
            }

            currentPiece = piecePool.GetPiece((int)nextPieceID);
            currentPiece.transform.SetParent(transform);
            currentPiece.transform.position = cursor.transform.position;
            currentPiece.SetActive(false);

            pieceDisplay.SetCurrentPiece(puzzleConfig.PuzzlePieceConfig.Configuration[(int)nextPieceID].Icon);

            spawnIndex++;

            if (spawnIndex >= spawnRateRange)
            {
                spawnIndex = 0;
            }

            float time = (float)spawnIndex / spawnRateRange;
            nextPieceID = puzzle.PieceSpawner.SelectPieceID(time);

            pieceDisplay.SetNextPiece(puzzleConfig.PuzzlePieceConfig.Configuration[(int)nextPieceID].Icon);
        }

        private void ClearCurrentPiece()
        {
            if (currentPiece != null)
            {
                var pieceComponent = currentPiece.GetComponent<PieceComponent>();
                piecePool.ReturnPiece(pieceComponent.PieceID, currentPiece);
                currentPiece = null;
            }
        }

        private void ClearPieceViews()
        {
            foreach (var kvp in pieceViews)
            {
                piecePool.ReturnPiece(kvp.Value.GetComponent<PieceComponent>().PieceID, kvp.Value);
            }

            pieceViews.Clear();
        }

        private void ClearPiece(long hexId)
        {
            if (puzzle.Pieces.ContainsKey(hexId))
            {
                puzzle.Pieces.Remove(hexId);

                if (pieceViews.TryGetValue(hexId, out GameObject pieceView))
                {
                    piecePool.ReturnPiece(pieceView.GetComponent<PieceComponent>().PieceID, pieceView);
                    pieceViews.Remove(hexId);
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
            if (!canPlacePiece || isPlacingPiece)
            {
                return;
            }

            Hexagon hexagon = HexagonMath.ToHex[puzzleConfig.Configuration.Orientation](
                inputState.Position.x - transform.position.x,
                inputState.Position.z - transform.position.z,
                puzzleConfig.Configuration.HexRadius
            );

            if (puzzle.Pieces.ContainsKey(HexagonEncoder.Encode(hexagon)))
            {
                return;
            }            

            isPlacingPiece = true;

            ClearPotentialPieces();

            uint pieceID = (uint)currentPiece.GetComponent<PieceComponent>().PieceID;

            inputComponent.ToggleInput(false);
            StartCoroutine(ProcessPiecePlacement(hexagon, pieceID, () => {
                SetCurrentPiece();

                inputComponent.ToggleInput(true);

                isPlacingPiece = false;
            }));
        }

        public void OnInputMove(PuzzleInputState inputState)
        {
            if (isPlacingPiece)
            {
                return;
            }

            canPlacePiece = false;

            Hexagon hexagon = HexagonMath.ToHex[puzzleConfig.Configuration.Orientation](
                inputState.Position.x - transform.position.x,
                inputState.Position.z - transform.position.z,
                puzzleConfig.Configuration.HexRadius
            );

            if (puzzle.Grid.TryGetHexagon(hexagon, out Hexagon foundHexagon))
            {
                var hexId = HexagonEncoder.Encode(foundHexagon);
                if (puzzle.Pieces.ContainsKey(hexId))
                {
                    ClearPotentialPieces();
                    cursor.SetActive(false);
                    currentPiece.SetActive(false);

                    hoveredHex = null;
                    return;
                }

                canPlacePiece = true;

                if (hoveredHex.HasValue && hoveredHex.Value.Equals(foundHexagon))
                {
                    return;
                }
                hoveredHex = foundHexagon;

                var (x, y) = HexagonMath.FromHex[puzzleConfig.Configuration.Orientation](foundHexagon, puzzleConfig.Configuration.HexRadius);
                cursor.transform.position = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + y);
                cursor.SetActive(true);

                currentPiece.transform.position = cursor.transform.position;
                currentPiece.SetActive(true);

                ClearPotentialPieces();
                uint pieceID = (uint)currentPiece.GetComponent<PieceComponent>().PieceID;

                CollectMatchingPieces(foundHexagon, pieceID, ref potentialPieces);
                if (potentialPieces.Count >= Puzzle.RequiredMatchCount - 1)
                {
                    potentialPieceCoroutine = StartCoroutine(AnimatePotentialPieces(foundHexagon));
                }
            }
            else
            {
                cursor.SetActive(false);
                currentPiece.SetActive(false);
            }
        }

        private IEnumerator ProcessPiecePlacement(Hexagon hexagon, uint pieceID, Action onComplete = null)
        {
            List<Hexagon> matched = new();

            long hexId = HexagonEncoder.Encode(hexagon);
            uint evolvedPieceId = CollectMatchingPieces(hexagon, pieceID, ref matched);

            if (matched.Count < Puzzle.RequiredMatchCount - 1)
            {
                PlacePiece(hexId, pieceID);
                onComplete?.Invoke();
                yield break;
            }

            yield return AnimationMatch(hexagon, matched);

            currentPiece.SetActive(false);
            
            if (evolvedPieceId >= puzzleConfig.PuzzlePieceConfig.Configuration.Length)
            {
                onComplete?.Invoke();
                yield break;
            }

            int points = (int)(evolvedPieceId * (matched.Count + 1) * 10);
            pointsDisplay.AddPoints(points);

            (float x, float y) = HexagonMath.FromHex[puzzleConfig.Configuration.Orientation](hexagon, puzzleConfig.Configuration.HexRadius);
            floatingPointSpawner.Spawn(new Vector3(x, transform.position.y + 0.5f, y), points);

            PlacePiece(hexId, evolvedPieceId);
            yield return AnimateEvolution(hexagon);

            onComplete?.Invoke();
        }

        private uint CollectMatchingPieces(Hexagon hexagon, uint pieceId, ref List<Hexagon> matched)
        {
            List<Hexagon> collection = puzzle.SimulateMatch(hexagon, pieceId).ToList();
            if (collection.Count < Puzzle.RequiredMatchCount - 1)
            {
                return pieceId;
            }

            matched.AddRange(collection);
            matched = matched.Distinct().ToList();

            return CollectMatchingPieces(hexagon, pieceId + 1, ref matched);
        }

        private IEnumerator AnimationMatch(Hexagon targetHex, List<Hexagon> hexagons)
        {
            float elapsed = 0f;
            float duration = 0.25f;

            var (x, y) = HexagonMath.FromHex[puzzleConfig.Configuration.Orientation](targetHex, puzzleConfig.Configuration.HexRadius);

            HashSet<long> completed = new();
            while (completed.Count() != hexagons.Count())
            {
                foreach (var hexagon in hexagons)
                {
                    long hexId = HexagonEncoder.Encode(hexagon);
                    if (completed.Contains(hexId))
                    {
                        continue;
                    }

                    if (pieceViews.TryGetValue(hexId, out GameObject pieceView))
                    {
                        float t = elapsed / duration;
                        float eased = t * t * t;

                        float scale = Mathf.Lerp(1f, 0f, eased);                        
                        pieceView.transform.position = Vector3.Lerp(pieceView.transform.position, new Vector3(transform.position.x + x, transform.position.y, transform.position.z + y), eased);

                        if (elapsed >= duration)
                        {
                            ClearPiece(hexId);
                            completed.Add(hexId);
                        }
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator AnimateEvolution(Hexagon hexagon)
        {
            float elapsed = 0f;
            float duration = 0.5f;

            long hexId = HexagonEncoder.Encode(hexagon);
            if (!pieceViews.TryGetValue(hexId, out GameObject pieceView))
            {
                yield break;
            }

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float c4 = 2f * Mathf.PI / 3f;

                float eased = t == 0f ? 0f 
                            : t == 1f ? 1f 
                            : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;

                float scale = Mathf.Lerp(1.2f, 1f, eased);
                pieceView.transform.localScale = new Vector3(scale, scale, scale);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator AnimatePotentialPieces(Hexagon target)
        {
            (float xTarget, float yTarget) = HexagonMath.FromHex[puzzleConfig.Configuration.Orientation](target, puzzleConfig.Configuration.HexRadius);

            float elapsed = 0f;
            float duration = 0.5f;
            float distance = 0.1f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float eased = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f; // EaseInOut

                foreach (var piece in potentialPieces)
                {
                    (float x, float y) = HexagonMath.FromHex[puzzleConfig.Configuration.Orientation](piece, puzzleConfig.Configuration.HexRadius);

                    Vector2 delta = new Vector2(xTarget - x, yTarget - y).normalized;
                    
                    float lerpX = x + delta.x * eased * distance;
                    float lerpY = y + delta.y * eased * distance;

                    long hexId = HexagonEncoder.Encode(piece);
                    pieceViews[hexId].transform.position = new Vector3(transform.position.x + lerpX, transform.position.y, transform.position.z + lerpY);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
            yield break;
        }

        private void ClearPotentialPieces()
        {
            if (potentialPieceCoroutine != null)
            {
                StopCoroutine(potentialPieceCoroutine);
                potentialPieceCoroutine = null;
            }

            foreach (var piece in potentialPieces)
            {
                (float x, float y) = HexagonMath.FromHex[puzzleConfig.Configuration.Orientation](piece, puzzleConfig.Configuration.HexRadius);

                long hexId = HexagonEncoder.Encode(piece);
                pieceViews[hexId].transform.position = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + y);
            }

            potentialPieces.Clear();
        }

        private void PlacePiece(long hexId, uint pieceID)
        {
            puzzle.Pieces[hexId] = pieceID;
            RefreshPieceView();
            ClearCurrentPiece();
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
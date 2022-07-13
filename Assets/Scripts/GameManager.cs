using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Grid grid;
    
    [Header("Piece Setup")]
    public GameObject pieceBackground;
    public GameObject piecePrefab;
    public Sprite[] normalPieceSprites;

    public bool preFillGrid;
    
    private bool _allowInput;
    private Camera _camera;

    private Piece _heldPiece;
    private Piece _slidingPiece;

    private static readonly Vector2Int[] AdjacentSides = {
        new (1, 0),
        new (-1, 0),
        new (0, 1),
        new (0, -1)
    };
    
    private void Start()
    {
        grid.Initialise(piecePrefab, normalPieceSprites);
        _camera = Camera.main;
        
        // Creates initial grid
        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                Instantiate(pieceBackground, grid.GetWorldPosition(x, y), Quaternion.identity, grid.transform);
                if (preFillGrid) grid.CreateRandomPiece(x, y);
            }
        }
        
        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        if (!_allowInput) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = _camera.ScreenToWorldPoint(Input.mousePosition);

            if (grid.XBounds.x < mousePos.x && mousePos.x < grid.XBounds.y && 
                grid.YBounds.x < mousePos.y && mousePos.y < grid.YBounds.y)
            {
                Vector2Int gridPos = grid.GetGridPosition(mousePos);
                _heldPiece = grid[gridPos.x, gridPos.y];
            }
        }

        if (Input.GetMouseButtonUp(0) && _heldPiece != null)
        {
            if (_slidingPiece != null)
            {
                // Swaps pieces
                Vector2Int originalPos = new(_heldPiece.X, _heldPiece.Y);
                Vector2Int slidingPos = new(_slidingPiece.X, _slidingPiece.Y);
                grid[originalPos.x, originalPos.y] = _slidingPiece;
                grid[slidingPos.x, slidingPos.y] = _heldPiece;

                List<Piece> matches = new();
                matches.AddRange(grid.GetMatches(slidingPos.x, slidingPos.y));
                matches.AddRange(grid.GetMatches(originalPos.x, originalPos.y));

                // Return pieces to original position if there are no available matches
                if (matches.Count == 0 && 
                    _heldPiece.Type != PieceType.Rainbow && 
                    _slidingPiece.Type != PieceType.Rainbow)
                {
                    grid[originalPos.x, originalPos.y] = _heldPiece;
                    grid[slidingPos.x, slidingPos.y] = _slidingPiece;
                    grid.StopAllCoroutines();
                    StartCoroutine(grid.ResetPiece(_slidingPiece));
                }

                if (_slidingPiece.Type == PieceType.Rainbow && _heldPiece.Type == PieceType.Rainbow)
                {
                    for (int x = 0; x < grid.width; x++)
                    for (int y = 0; y < grid.height; y++)
                    {
                        grid.RemovePiece(x, y);
                    }
                    
                    grid.RemovePiece(originalPos.x, originalPos.y);
                    grid.RemovePiece(slidingPos.x, slidingPos.y);
                }
                else if (_slidingPiece.Type == PieceType.Rainbow)
                {
                    for (int x = 0; x < grid.width; x++)
                    for (int y = 0; y < grid.height; y++)
                    {
                        if (grid[x, y].Colour == _heldPiece.Colour) 
                            grid.RemovePiece(x, y);
                    }   
                    
                    grid.RemovePiece(originalPos.x, originalPos.y);
                }
                else if (_heldPiece.Type == PieceType.Rainbow)
                {
                    for (int x = 0; x < grid.width; x++)
                    for (int y = 0; y < grid.height; y++)
                    {
                        if (grid[x, y].Colour == _slidingPiece.Colour) 
                            grid.RemovePiece(x, y);
                    } 
                    
                    grid.RemovePiece(slidingPos.x, slidingPos.y);
                }
            }
            
            // Resets held and sliding piece
            _heldPiece.transform.position = grid.GetWorldPosition(_heldPiece.X, _heldPiece.Y);
            _heldPiece = null;
            _slidingPiece = null;
            _allowInput = false;
        }

        if (_heldPiece != null)
        {
            // Move held piece to mouse position
            Vector3 mousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = -1;
            _heldPiece.transform.position = mousePos;

            // Calculate relative position from mouse to piece location
            Vector3 relativePosition = mousePos - grid.GetWorldPosition(_heldPiece.X, _heldPiece.Y);

            // Reset sliding piece if held piece too close
            if (Mathf.Abs(relativePosition.x) < .5f && Mathf.Abs(relativePosition.y) < .5f)
            {
                if (_slidingPiece == null) return;
                
                grid.StopAllCoroutines();
                StartCoroutine(grid.ResetPiece(_slidingPiece));
                _slidingPiece = null;
                return;
            }
            
            // Finds the current side that the sliding piece should be on
            int sideIndex;
            if (Mathf.Abs(relativePosition.x) > Mathf.Abs(relativePosition.y))
            {
                sideIndex = relativePosition.x > 0 ? 0 : 1;
            }
            else
            {
                sideIndex = relativePosition.y > 0 ? 2 : 3;
            }
            Vector2Int sidePieceCoords = new Vector2Int(_heldPiece.X, _heldPiece.Y) + AdjacentSides[sideIndex];

            // Returns if side of sliding piece is out of bounds
            if (sidePieceCoords.x < 0 || sidePieceCoords.x >= grid.width ||
                sidePieceCoords.y < 0 || sidePieceCoords.y >= grid.height)
            {
                return;
            }

            // Finds the new sliding piece, and slides it accordingly, resetting the previous piece if necessary
            Piece newSlidingPiece = grid[sidePieceCoords.x, sidePieceCoords.y];
            if (_slidingPiece != null && _slidingPiece != newSlidingPiece)
            {
                grid.StopAllCoroutines();
                StartCoroutine(grid.ResetPiece(_slidingPiece));
                _slidingPiece = null;
            }
            _slidingPiece = newSlidingPiece;
            grid.StartCoroutine(grid.MovePiece(newSlidingPiece, _heldPiece.X, _heldPiece.Y));
        }
    }

    private IEnumerator GameLoop()
    {
        while (true)
        {
            if (!grid.DropPieces())
            {
                List<(int count, List<Piece> matches, Piece root)> gridMatchData = new();
                List<Piece> usedPieces = new();

                bool matchesFound = false;
                
                for (int x = 0; x < grid.width; x++)
                for (int y = 0; y < grid.height; y++)
                {
                    List<Piece> matches = grid.GetMatches(x, y);

                    if (matches.Count == 0) continue;
                    
                    matchesFound = true;
                    gridMatchData.Add((matches.Count, matches, grid[x, y]));
                }

                foreach ((int count, List<Piece> matches, Piece root) data in 
                    gridMatchData.OrderByDescending(m => m.count))
                {

                    if (!usedPieces.Contains(data.root))
                    {
                        grid.RemovePiece(data.root.X, data.root.Y);
                        usedPieces.Add(data.root);
                        
                        switch (data.count)
                        {
                            case 4:
                                grid.CreatePiece(data.root.X, data.root.Y, data.root.Colour,
                                    Random.Range(0, 2) == 0 ? PieceType.Horizontal : PieceType.Vertical);
                                break;
                            case 5:
                                grid.CreatePiece(data.root.X, data.root.Y, data.root.Colour, PieceType.Rainbow);
                                break;
                        }
                    }
                    
                    foreach (Piece match in data.matches.Where(match => !usedPieces.Contains(match)))
                    {
                        grid.RemovePiece(match.X, match.Y);
                        usedPieces.Add(match);
                    }
                }
                
                _allowInput = !matchesFound;
            }
            else
            {
                _allowInput = false;
            }
            yield return new WaitForSeconds(grid.fillTime);
        }
    }
}

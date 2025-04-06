using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    [Header("Manager References")] public BuildingShopManager shopManager;
    public GridManager gridManager;
    public GridDragAndDropManager dragAndDropManager;

    [Header("UI References")] public GameObject winPanel;
    public GameObject losePanel;

    [Header("Game Events")] public UnityEvent OnGameWin;
    public UnityEvent OnGameLose;

    [Header("Game Settings")] [Tooltip("Time to wait after placement before checking game state")]
    public float checkDelay = 0.5f;

    [Tooltip("Should we automatically check for potential placement after each building is placed?")]
    public bool autoCheckAfterPlacement = true;

    private bool _gameEnded = false;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // Auto-find references if needed
        if (shopManager == null) shopManager = FindObjectOfType<BuildingShopManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (dragAndDropManager == null) dragAndDropManager = FindObjectOfType<GridDragAndDropManager>();

        // Listen for building placement events
        if (dragAndDropManager != null)
        {
            dragAndDropManager.OnBuildingPlaced.AddListener(OnBuildingPlaced);
        }

        // Listen for building purchase events to check if there are more buildings
        if (shopManager != null)
        {
            shopManager.OnBuildingPurchased.AddListener(OnBuildingPurchased);
        }
    }

    private void OnBuildingPurchased(BuildingShopManager.BuildingOption building)
    {
        // When a building is purchased, we'll check for placement possibilities after it's placed
        if (autoCheckAfterPlacement)
        {
            StartCoroutine(CheckAfterPlacementDelay());
        }
    }

    private IEnumerator CheckAfterPlacementDelay()
    {
        // Wait a moment to allow the placement to complete
        yield return new WaitForSeconds(checkDelay);

        // Check if the building was successfully placed
        if (shopManager._currentPlacementObject == null)
        {
            CheckForVictory();

            if (!_gameEnded)
            {
                CheckCanPlaceCurrentBuilding();
            }
        }
    }

    // This should be called from GridDragAndDropManager after a building is placed
    public void OnBuildingPlaced()
    {
        StartCoroutine(DelayedCheckAfterPlacement());
    }

    private IEnumerator DelayedCheckAfterPlacement()
    {
        // Wait a moment for everything to update
        yield return new WaitForSeconds(0.5f);

        //Check if the building was actually placed
        if (shopManager._currentPlacementObject == null)
        {
            CheckForVictory();
        }

        // If we haven't won, check if the next building can be placed
        if (!_gameEnded)
        {
            CheckCanPlaceCurrentBuilding();
        }
    }

    private void CheckForVictory()
    {
        if (shopManager != null &&
            shopManager._currentBuildingIndex >= shopManager._buildingSequence.Count &&
            !shopManager.loopBuildings &&
            shopManager._currentPlacementObject == null)
        {
            Win();
        }
    }

    public void CheckGameState()
    {
        if (_gameEnded) return;

        CheckForVictory();

        if (!_gameEnded)
        {
            CheckCanPlaceCurrentBuilding();
        }
    }

    private void CheckCanPlaceCurrentBuilding()
    {
        if (_gameEnded || shopManager == null || gridManager == null) return;

        // Get current building
        BuildingShopManager.BuildingOption currentBuilding = shopManager._currentDisplayedBuilding;
        if (currentBuilding == null || currentBuilding.buildingPrefab == null) return;

        Debug.Log($"Checking if {currentBuilding.displayName} can be placed...");

        // Create test building
        GameObject testObj = Instantiate(currentBuilding.buildingPrefab);
        testObj.SetActive(false);
        GridObject gridObject = testObj.GetComponent<GridObject>();

        if (gridObject == null)
        {
            Destroy(testObj);
            return;
        }

        // Check if this is the first building (special case)
        bool isFirstBuilding = gridManager.GetAllPlacedObjects().Count == 0;

        // If not the first building, get all cells adjacent to existing buildings
        HashSet<Vector2Int> adjacentCells = new HashSet<Vector2Int>();

        if (!isFirstBuilding)
        {
            // Collect all cells occupied by existing buildings
            List<Vector2Int> existingBuildingCells = new List<Vector2Int>();
            foreach (var kvp in gridManager.GetAllPlacedObjects())
            {
                existingBuildingCells.AddRange(kvp.Value);
            }

            // Find all adjacent cells (that aren't already occupied)
            foreach (var cell in existingBuildingCells)
            {
                // Check in all four directions
                Vector2Int[] directions = new Vector2Int[]
                {
                    new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(0, 1), new Vector2Int(0, -1)
                };

                foreach (var dir in directions)
                {
                    Vector2Int adjacent = cell + dir;

                    // Only add if within bounds and not already occupied
                    if (gridManager.IsWithinGridBounds(adjacent) &&
                        gridManager.IsCellValid(adjacent) &&
                        !existingBuildingCells.Contains(adjacent))
                    {
                        adjacentCells.Add(adjacent);
                    }
                }
            }

            Debug.Log($"Found {adjacentCells.Count} cells adjacent to existing buildings");
        }

        // Try all rotations and valid positions
        bool canPlace = false;

        // Try each rotation
        for (int rot = 0; rot < 4; rot++)
        {
            gridObject.rotationIndex = rot;
            gridObject.CalculateRelativeCellPositions();

            if (isFirstBuilding)
            {
                // For first building, check the entire grid
                for (int x = 0; x < gridManager.maxWidth; x++)
                {
                    for (int y = 0; y < gridManager.maxDepth; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        List<Vector2Int> occupiedCells = gridObject.GetOccupiedCells(pos);

                        if (gridManager.IsValidPlacement(occupiedCells))
                        {
                            canPlace = true;
                            break;
                        }
                    }

                    if (canPlace) break;
                }
            }
            else
            {
                // For subsequent buildings, only check adjacent cells
                foreach (var adjacentCell in adjacentCells)
                {
                    List<Vector2Int> occupiedCells = gridObject.GetOccupiedCells(adjacentCell);

                    if (gridManager.IsValidPlacement(occupiedCells))
                    {
                        canPlace = true;
                        break;
                    }
                }
            }

            if (canPlace) break;
        }

        Destroy(testObj);

        // Trigger lose condition if building can't be placed
        if (!canPlace)
        {
            Debug.Log($"GAME OVER: Building '{currentBuilding.displayName}' cannot be placed!");
            Lose();
        }
    }
    public void VisualizeValidPlacements()
{
    // Clear any existing markers
    foreach(var marker in GameObject.FindGameObjectsWithTag("DebugMarker"))
    {
        Destroy(marker);
    }
    
    // First visualize the grid bounds to see the actual grid
    VisualizeGridBounds();
    
    // Get the current building
    BuildingShopManager.BuildingOption currentBuilding = shopManager._currentDisplayedBuilding;
    if (currentBuilding == null)
    {
        Debug.LogError("No current building to check!");
        return;
    }
    
    Debug.Log($"Checking valid placements for: {currentBuilding.displayName}");
    
    // Create a test object
    GameObject testObj = Instantiate(currentBuilding.buildingPrefab);
    testObj.SetActive(false);
    GridObject gridObject = testObj.GetComponent<GridObject>();
    
    if (gridObject == null)
    {
        Debug.LogError("Building has no GridObject component!");
        Destroy(testObj);
        return;
    }
    
    // Get existing buildings
    var placedObjects = gridManager.GetAllPlacedObjects();
    bool isFirstBuilding = placedObjects.Count == 0;
    
    // Find cells adjacent to existing buildings
    HashSet<Vector2Int> adjacentCells = new HashSet<Vector2Int>();
    
    if (!isFirstBuilding)
    {
        // Collect all cells occupied by existing buildings
        List<Vector2Int> existingBuildingCells = new List<Vector2Int>();
        foreach (var kvp in placedObjects)
        {
            existingBuildingCells.AddRange(kvp.Value);
        }
        
        // Find all adjacent cells (that aren't already occupied)
        foreach (var cell in existingBuildingCells)
        {
            // Check in all four directions
            Vector2Int[] directions = new Vector2Int[] {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };
            
            foreach (var dir in directions)
            {
                Vector2Int adjacent = cell + dir;
                
                // Only add if within bounds and not already occupied
                if (gridManager.IsWithinGridBounds(adjacent) && 
                    gridManager.IsCellValid(adjacent) &&
                    !existingBuildingCells.Contains(adjacent))
                {
                    adjacentCells.Add(adjacent);
                }
            }
        }
        
        Debug.Log($"Found {adjacentCells.Count} cells adjacent to existing buildings");
    }
    
    // Track valid positions
    List<Vector3> validWorldPositions = new List<Vector3>();
    HashSet<string> uniqueValidPositions = new HashSet<string>();
    
    // Try each rotation
    for (int rot = 0; rot < 4; rot++)
    {
        gridObject.rotationIndex = rot;
        gridObject.CalculateRelativeCellPositions();
        
        if (isFirstBuilding)
        {
            // For first building, check entire grid
            for (int x = 0; x < gridManager.maxWidth; x++)
            {
                for (int y = 0; y < gridManager.maxDepth; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    // Skip if not a valid cell
                    if (!gridManager.IsWithinGridBounds(pos) || !gridManager.IsCellValid(pos))
                    {
                        continue;
                    }
                    
                    List<Vector2Int> occupiedCells = gridObject.GetOccupiedCells(pos);
                    
                    // Double-check each cell in the building is valid
                    bool allCellsValid = true;
                    foreach (var cell in occupiedCells)
                    {
                        if (!gridManager.IsWithinGridBounds(cell) || !gridManager.IsCellValid(cell))
                        {
                            allCellsValid = false;
                            break;
                        }
                    }
                    
                    if (allCellsValid && gridManager.IsValidPlacement(occupiedCells))
                    {
                        // Use a hash to avoid duplicates
                        string cellsHash = string.Join(",", occupiedCells.OrderBy(c => c.x).ThenBy(c => c.y));
                        
                        if (!uniqueValidPositions.Contains(cellsHash))
                        {
                            uniqueValidPositions.Add(cellsHash);
                            validWorldPositions.Add(gridManager.GetWorldPosition(pos));
                            Debug.Log($"Valid placement at ({x},{y}) with rotation {rot}");
                        }
                    }
                }
            }
        }
        else
        {
            // For subsequent buildings, only check adjacent to existing
            foreach (var basePos in adjacentCells)
            {
                List<Vector2Int> occupiedCells = gridObject.GetOccupiedCells(basePos);
                
                // Check if all cells are valid
                bool allCellsValid = true;
                foreach (var cell in occupiedCells)
                {
                    if (!gridManager.IsWithinGridBounds(cell) || !gridManager.IsCellValid(cell))
                    {
                        allCellsValid = false;
                        break;
                    }
                }
                
                if (allCellsValid && gridManager.IsValidPlacement(occupiedCells))
                {
                    // Use a hash to avoid duplicates
                    string cellsHash = string.Join(",", occupiedCells.OrderBy(c => c.x).ThenBy(c => c.y));
                    
                    if (!uniqueValidPositions.Contains(cellsHash))
                    {
                        uniqueValidPositions.Add(cellsHash);
                        validWorldPositions.Add(gridManager.GetWorldPosition(basePos));
                        Debug.Log($"Valid placement at {basePos} with rotation {rot}");
                    }
                }
            }
        }
    }
    
    // Create markers for valid positions
    foreach (var worldPos in validWorldPositions)
    {
        // Double check this position is within grid bounds
        Vector2Int gridPos = gridManager.GetGridPosition(worldPos);
        if (!gridManager.IsWithinGridBounds(gridPos) || !gridManager.IsCellValid(gridPos))
        {
            Debug.LogWarning($"Position {worldPos} converts to invalid grid position {gridPos} - skipping");
            continue;
        }
        
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = worldPos + Vector3.up * 0.5f;
        marker.transform.localScale = Vector3.one * 0.3f;
        marker.GetComponent<Renderer>().material.color = Color.green;
        marker.tag = "DebugMarker";
    }
    
    Debug.Log($"Found {uniqueValidPositions.Count} unique valid positions");
    
    if (uniqueValidPositions.Count == 0)
    {
        Debug.Log("NO VALID PLACEMENTS - Should trigger game over!");
    }
    
    Destroy(testObj);
}

    public void VisualizeGridBounds()
    {
        // Clear existing grid markers
        foreach (var marker in GameObject.FindGameObjectsWithTag("GridMarker"))
        {
            Destroy(marker);
        }

        Debug.Log("Visualizing grid boundaries...");

        // Create a marker for each valid grid cell
        for (int x = 0; x < gridManager.maxWidth; x++)
        {
            for (int y = 0; y < gridManager.maxDepth; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);

                if (gridManager.IsWithinGridBounds(gridPos))
                {
                    bool isValid = gridManager.IsCellValid(gridPos);
                    Vector3 worldPos = gridManager.GetWorldPosition(gridPos);

                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    marker.transform.position = worldPos;
                    marker.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);

                    // Blue for valid cells, red for invalid
                    marker.GetComponent<Renderer>().material.color = isValid
                        ? new Color(0, 0, 1, 0.3f)
                        : // Transparent blue
                        new Color(1, 0, 0, 0.3f); // Transparent red

                    marker.tag = "GridMarker";
                }
            }
        }
    }

    private void Win()
    {
        if (_gameEnded) return;
        
        _gameEnded = true;
        
        // Show win UI
        if (winPanel != null) winPanel.SetActive(true);
        
        // Trigger event
        OnGameWin?.Invoke();
    }
    
    private void Lose()
    {
        if (_gameEnded) return;
        
        _gameEnded = true;
        
        // Show lose UI
        if (losePanel != null) losePanel.SetActive(true);
        
        // Trigger event
        OnGameLose?.Invoke();
    }
}
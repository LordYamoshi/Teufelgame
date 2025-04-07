using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    [Header("Manager References")] 
    public BuildingShopManager shopManager;
    public GridManager gridManager;
    public GridDragAndDropManager dragAndDropManager;

    [Header("UI References")] 
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Game Events")] 
    public UnityEvent OnGameWin;
    public UnityEvent OnGameLose;
    public UnityEvent<bool> OnGameStateChanged;

    [Header("Game Settings")] 
    [Tooltip("Time to wait after placement before checking game state")]
    public float checkDelay = 0.5f;

    [Tooltip("Should we automatically check for potential placement after each building is placed?")]
    public bool autoCheckAfterPlacement = true;
    
    [Tooltip("Whether to check win conditions after placing the final building")]
    public bool checkWinOnFinalBuilding = true;

    [Header("Debug")]
    [Tooltip("Enable detailed logging")]
    public bool verboseLogging = false;
    
    [Tooltip("Show debug visualization of possible placements")]
    public bool showDebugVisualization = false;

    private bool _gameEnded = false;
    private BuildingShopManager.BuildingOption _lastPlacedBuilding = null;
    private int _checkCount = 0; // Used to track delayed checks
    private bool _isCheckingGameState = false; // Flag to prevent overlapping checks

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // Auto-find references if needed
        if (shopManager == null) shopManager = FindObjectOfType<BuildingShopManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (dragAndDropManager == null) dragAndDropManager = FindObjectOfType<GridDragAndDropManager>();

        // Register event listeners
        if (dragAndDropManager != null)
        {
            dragAndDropManager.OnBuildingPlaced.AddListener(OnBuildingPlaced);
        }

        if (shopManager != null)
        {
            shopManager.OnBuildingPurchased.AddListener(OnBuildingPurchased);
        }
        
        if (verboseLogging)
        {
            Debug.Log("GameStateManager initialized. Auto-check: " + autoCheckAfterPlacement);
        }
    }

    private void OnDestroy()
    {
        // Unregister event listeners
        if (dragAndDropManager != null)
        {
            dragAndDropManager.OnBuildingPlaced.RemoveListener(OnBuildingPlaced);
        }

        if (shopManager != null)
        {
            shopManager.OnBuildingPurchased.RemoveListener(OnBuildingPurchased);
        }
    }

    private void OnBuildingPurchased(BuildingShopManager.BuildingOption building)
    {
        _lastPlacedBuilding = building;
        
        if (verboseLogging)
        {
            Debug.Log($"Building purchased: {building.displayName}");
        }
        
        // Don't check immediately - wait for placement
    }

    private void OnBuildingPlaced()
    {
        if (_gameEnded) return;

        // After a building is placed, check game state with delay
        StartCoroutine(DelayedGameStateCheck());
    }

    private IEnumerator DelayedGameStateCheck()
    {
        // Generate a unique check ID
        int thisCheckID = ++_checkCount;
        
        // Wait for a moment to ensure everything is settled
        yield return new WaitForSeconds(checkDelay);
        
        // Only proceed if this is still the most recent check 
        // (prevents multiple overlapping checks)
        if (thisCheckID != _checkCount) yield break;
        
        // Make sure placement is complete
        if (shopManager._currentPlacementObject != null)
        {
            if (verboseLogging)
            {
                Debug.Log("Skipping game state check - building still being placed");
            }
            yield break;
        }

        // Check if this was potentially the final placement
        bool isFinalPlacement = shopManager._currentBuildingIndex >= shopManager._buildingSequence.Count &&
                               !shopManager.loopBuildings;

        if (verboseLogging)
        {
            Debug.Log($"Checking game state... Final placement? {isFinalPlacement}");
        }

        // Check for win condition if appropriate
        if (checkWinOnFinalBuilding && isFinalPlacement)
        {
            if (verboseLogging)
            {
                Debug.Log("All buildings placed. Checking for victory condition.");
            }
            CheckForVictory();
        }

        // Don't do additional checks if game has ended
        if (_gameEnded) yield break;

        // Check if next building can be placed
        if (autoCheckAfterPlacement)
        {
            CheckCanPlaceCurrentBuilding();
        }
    }

    public void CheckGameState()
    {
        if (_gameEnded || _isCheckingGameState) return;
        
        _isCheckingGameState = true;
        
        try 
        {
            CheckForVictory();

            if (!_gameEnded)
            {
                CheckCanPlaceCurrentBuilding();
            }
        }
        finally
        {
            _isCheckingGameState = false;
        }
    }

    private void CheckForVictory()
    {
        if (_gameEnded) return;

        bool hasWon = false;
        
        // Has completed all buildings and they are placed?
        if (shopManager != null &&
            shopManager._currentBuildingIndex >= shopManager._buildingSequence.Count &&
            !shopManager.loopBuildings &&
            shopManager._currentPlacementObject == null)
        {
            if (verboseLogging)
            {
                Debug.Log("Victory condition met: All buildings used and placed.");
            }
            hasWon = true;
        }
        
        // Add any additional victory conditions here
        // ...

        if (hasWon)
        {
            Win();
        }
    }

    private void CheckCanPlaceCurrentBuilding()
    {
        if (_gameEnded || shopManager == null || gridManager == null) return;

        // Get current building
        BuildingShopManager.BuildingOption currentBuilding = shopManager._currentDisplayedBuilding;
        if (currentBuilding == null || currentBuilding.buildingPrefab == null) 
        {
            if (verboseLogging)
            {
                Debug.Log("No current building to check for placement");
            }
            return;
        }

        if (verboseLogging)
        {
            Debug.Log($"Checking if {currentBuilding.displayName} can be placed...");
        }

        // Create test building
        GameObject testObj = Instantiate(currentBuilding.buildingPrefab);
        testObj.SetActive(false);
        GridObject gridObject = testObj.GetComponent<GridObject>();

        if (gridObject == null)
        {
            Debug.LogWarning("Building prefab has no GridObject component!");
            Destroy(testObj);
            return;
        }

        try
        {
            // Check if we can place it somewhere
            bool canPlace = CanPlaceBuilding(gridObject);
        
            if (verboseLogging)
            {
                Debug.Log($"Can place {currentBuilding.displayName}: {canPlace}");
            }
        
            // Show debug visualization if enabled
            if (showDebugVisualization)
            {
                VisualizeValidPlacements();
            }

            if (!canPlace)
            {
                Debug.Log($"GAME OVER: Building '{currentBuilding.displayName}' cannot be placed!");
                Lose();
            }
            else if (verboseLogging)
            {
                Debug.Log($"Building '{currentBuilding.displayName}' can be placed. Game continues.");
            }
        }
        finally
        {
            Destroy(testObj);
        }
    }

    /// <summary>
    /// Checks if a building can be placed somewhere on the grid
    /// </summary>
    private bool CanPlaceBuilding(GridObject gridObject)
    {
        if (verboseLogging)
        {
            Debug.Log($"Checking placement for building: {gridObject.gameObject.name}");
        }

        // First, check if we can clear any deletable pre-placed objects
        bool canClearPreplacedObjects = CanClearPreplacedObjectsForPlacement(gridObject);

        if (!canClearPreplacedObjects)
        {
            if (verboseLogging)
            {
                Debug.Log("Cannot place building: No way to clear pre-placed objects");
            }

            return false;
        }

        bool isFirstBuilding = !gridManager.FirstObjectPlaced || gridManager.GetPlayerPlacedObjectCount() == 0;

        // Try all possible rotations
        for (int rot = 0; rot < 4; rot++)
        {
            gridObject.rotationIndex = rot;
            gridObject.CalculateRelativeCellPositions();

            if (isFirstBuilding)
            {
                // For first building, scan entire grid
                if (TryScanEntireGrid(gridObject))
                {
                    return true;
                }
            }
            else
            {
                // Get adjacent cells from existing player-placed buildings
                HashSet<Vector2Int> adjacentCells = GetAdjacentCells();

                foreach (var adjacentCell in adjacentCells)
                {
                    // Get the cells this object would occupy at this position
                    List<Vector2Int> occupiedCells = gridObject.GetOccupiedCells(adjacentCell);

                    bool canPlace = true;
                    foreach (var cell in occupiedCells)
                    {
                        // Check if cell is within grid bounds
                        if (!gridManager.IsWithinGridBounds(cell))
                        {
                            canPlace = false;
                            break;
                        }

                        // Check if cell is valid for placement
                        // Allow placement if the cell is occupied by a deletable pre-placed object
                        if (!gridManager.IsCellValid(cell))
                        {
                            // Check if the occupying object is a pre-placed, deletable object
                            var occupyingObject = gridManager._cells[cell].OccupyingObject;
                            if (occupyingObject == null ||
                                !gridManager.IsPreplacedObject(occupyingObject) ||
                                !occupyingObject.GetComponent<GridObject>().isDestructible)
                            {
                                canPlace = false;
                                break;
                            }
                        }
                    }

                    // If all cells are valid or can be cleared, this is a potential placement
                    if (canPlace)
                    {
                        if (verboseLogging)
                        {
                            Debug.Log($"Valid placement found at {adjacentCell} with rotation {rot}");
                        }

                        return true;
                    }
                }
            }
        }

        // No valid placement found
        if (verboseLogging)
        {
            Debug.Log("No valid placement found for this building");
        }

        return false;
    }

    private bool CanClearPreplacedObjectsForPlacement(GridObject gridObjectToBePlaced)
    {
        // Count of deletable pre-placed objects
        int deletablePreplacedObjectCount = 0;

        // Try all rotations of the object
        for (int rot = 0; rot < 4; rot++)
        {
            gridObjectToBePlaced.rotationIndex = rot;
            gridObjectToBePlaced.CalculateRelativeCellPositions();

            // Check entire grid for potential placements after clearing pre-placed objects
            for (int x = 0; x < gridManager.maxWidth; x++)
            {
                for (int y = 0; y < gridManager.maxDepth; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    List<Vector2Int> occupiedCells = gridObjectToBePlaced.GetOccupiedCells(pos);

                    // Track deletable pre-placed objects
                    HashSet<GameObject> deletableObjects = new HashSet<GameObject>();
                    bool canPlace = true;

                    foreach (var cell in occupiedCells)
                    {
                        // Check if cell is within grid bounds
                        if (!gridManager.IsWithinGridBounds(cell))
                        {
                            canPlace = false;
                            break;
                        }

                        // If cell is not valid, check if it's a deletable pre-placed object
                        if (!gridManager.IsCellValid(cell))
                        {
                            var occupyingObject = gridManager._cells[cell].OccupyingObject;

                            // Check if object is pre-placed and deletable
                            if (occupyingObject == null ||
                                !gridManager.IsPreplacedObject(occupyingObject) ||
                                !occupyingObject.GetComponent<GridObject>().isDestructible)
                            {
                                canPlace = false;
                                break;
                            }

                            // Add to deletable objects
                            deletableObjects.Add(occupyingObject);
                        }
                    }

                    // If placement is possible after clearing pre-placed objects
                    if (canPlace)
                    {
                        if (verboseLogging)
                        {
                            Debug.Log($"Potential placement found at {pos} with rotation {rot}. " +
                                      $"Deletable pre-placed objects: {deletableObjects.Count}");
                        }

                        return true;
                    }
                }
            }
        }

        if (verboseLogging)
        {
            Debug.Log("No placement possible even after clearing pre-placed objects");
        }

        return false;
    }


    /// <summary>
    /// Scans the entire grid for a valid placement position
    /// </summary>
    private bool TryScanEntireGrid(GridObject gridObject)
    {
        // Scan entire grid for first building or when no adjacent cells are found
        for (int x = 0; x < gridManager.maxWidth; x++)
        {
            for (int y = 0; y < gridManager.maxDepth; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                List<Vector2Int> occupiedCells = gridObject.GetOccupiedCells(pos);

                // Check if all cells for this building's layout can be placed
                bool canPlace = true;
                foreach (var cell in occupiedCells)
                {
                    if (!gridManager.IsWithinGridBounds(cell) || !gridManager.IsCellValid(cell))
                    {
                        canPlace = false;
                        break;
                    }
                }

                if (canPlace)
                {
                    if (verboseLogging)
                    {
                        Debug.Log($"First building can be placed at {pos}");
                    }
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Gets all cells adjacent to existing player-placed buildings
    /// </summary>
    private HashSet<Vector2Int> GetAdjacentCells()
    {
        HashSet<Vector2Int> adjacentCells = new HashSet<Vector2Int>();

        // Collect all cells occupied by existing player-placed buildings
        List<Vector2Int> existingBuildingCells = new List<Vector2Int>();
        Dictionary<GameObject, List<Vector2Int>> placedObjects = gridManager.GetAllPlacedObjects();

        // Count of player-placed buildings (for debugging)
        int playerPlacedCount = 0;

        foreach (var kvp in placedObjects)
        {
            // Skip preplaced objects when checking for adjacency
            if (gridManager.IsPreplacedObject(kvp.Key))
            {
                if (verboseLogging)
                {
                    Debug.Log($"Skipping pre-placed object {kvp.Key.name} with {kvp.Value.Count} cells");
                }

                continue;
            }

            playerPlacedCount++;
            existingBuildingCells.AddRange(kvp.Value);

            if (verboseLogging)
            {
                Debug.Log($"Adding player-placed object {kvp.Key.name} with {kvp.Value.Count} cells");
            }
        }

        if (verboseLogging)
        {
            Debug.Log(
                $"Found {playerPlacedCount} player-placed objects with a total of {existingBuildingCells.Count} cells");
        }

        // Let's manually handle the first building edge case
        // If there are truly no player-placed buildings (including the incorrect case)
        if (existingBuildingCells.Count == 0)
        {
            if (verboseLogging)
            {
                Debug.Log("No player-placed objects found, treating as first building placement");
            }

            // Return empty set - caller will scan entire grid
            return adjacentCells;
        }

        // Find all adjacent cells (that aren't already occupied)
        foreach (var cell in existingBuildingCells)
        {
            // Check the four cardinal directions
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

        if (verboseLogging)
        {
            Debug.Log($"Found {adjacentCells.Count} cells adjacent to player-placed buildings");
        }

        return adjacentCells;
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
            Debug.LogWarning("No current building to check!");
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
            // Use the helper method
            adjacentCells = GetAdjacentCells();
            
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
                                if (verboseLogging)
                                {
                                    Debug.Log($"Valid placement at ({x},{y}) with rotation {rot}");
                                }
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
                            if (verboseLogging)
                            {
                                Debug.Log($"Valid placement at {basePos} with rotation {rot}");
                            }
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
            Debug.Log("NO VALID PLACEMENTS - Triggering game over");
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
                        ? new Color(0, 0, 1, 0.3f) // Transparent blue
                        : new Color(1, 0, 0, 0.3f); // Transparent red

                    marker.tag = "GridMarker";
                }
            }
        }
    }

    private void Win()
    {
        if (_gameEnded) return;
        
        _gameEnded = true;
        
        Debug.Log("GAME WIN!");
        
        // Show win UI
        if (winPanel != null) winPanel.SetActive(true);
        
        // Trigger events
        OnGameWin?.Invoke();
        OnGameStateChanged?.Invoke(true);
    }
    
    private void Lose()
    {
        if (_gameEnded) return;
        
        _gameEnded = true;
        
        Debug.Log("GAME OVER!");
        
        // Show lose UI
        if (losePanel != null) losePanel.SetActive(true);
        
        // Trigger events
        OnGameLose?.Invoke();
        OnGameStateChanged?.Invoke(false);
    }

    // Public method for restarting the game
    public void RestartGame()
    {
        // Add restart logic here
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            RestartGame();
        }
    }
}
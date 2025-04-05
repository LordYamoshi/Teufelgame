using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Manages a grid-based system for placing, moving, and interacting with grid objects
/// </summary>
public class GridManager : MonoBehaviour
{
    #region Properties and Fields
    [Header("Grid Settings")]
    [Tooltip("Maximum width of the grid in cells")]
    public int maxWidth = 20;
    
    [Tooltip("Maximum depth of the grid in cells")]
    public int maxDepth = 20;
    
    [Tooltip("Size of each grid cell in world units")]
    public float cellSize = 1.0f;
    
    [Tooltip("Origin point of the grid in world space")]
    public Vector3 gridOrigin = Vector3.zero;
    
    [Header("Visualization")]
    [Tooltip("Prefab for visualizing each cell")]
    public GameObject cellVisualPrefab;
    
    [Tooltip("Material for normal/empty cells")]
    public Material normalCellMaterial;
    
    [Tooltip("Material for cells with valid placement")]
    public Material validPlacementMaterial;
    
    [Tooltip("Material for cells with invalid placement")]
    public Material invalidPlacementMaterial;
    
    [Tooltip("Material for occupied cells")]
    public Material occupiedCellMaterial;
    
    [Header("Gameplay Rules")]
    [Tooltip("Require connectivity to existing buildings")]
    public bool requireEdgeConnectivity = true;
    
    [Tooltip("Show only tiles that will be used by the object")]
    public bool showOnlyUsedTiles = true;
    
    [Header("Serialization")]
    [Tooltip("Folder path for saving grid layouts")]
    public string saveFolder = "GridLayouts";
    
    [Tooltip("Default grid layout to load on start")]
    public string defaultGridLayout = "";
    
    // Grid data storage - key is grid position, value is the cell
    private Dictionary<Vector2Int, GridCell> _cells = new Dictionary<Vector2Int, GridCell>();
    
    // Parent transform for all cell visuals
    public Transform cellContainer;
    
    // Track all placed objects and their occupied cells
    private Dictionary<GameObject, List<Vector2Int>> _placedObjects = new Dictionary<GameObject, List<Vector2Int>>();
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        // Create cell container if not provided
        if (cellContainer == null)
        {
            GameObject container = new GameObject("Cell Container");
            container.transform.parent = transform;
            cellContainer = container.transform;
        }
        
        // Create save directory if needed
        EnsureSaveDirectoryExists();
    }

    private void Start()
    {
        // Load default grid layout if specified
        if (!string.IsNullOrEmpty(defaultGridLayout))
        {
            LoadGridLayout(defaultGridLayout);
        }
        
        // Auto-create grid if none exists
        if (_cells.Count == 0)
        {
            CreateRectangularGrid(maxWidth, maxDepth);
        }
        
        // Apply materials to cells if set
        if (normalCellMaterial != null)
        {
            ApplyDefaultMaterialToAllCells();
        }
    }
    #endregion
    
    #region Grid Creation and Management
    
    /// <summary>
    /// Creates a rectangular grid with the specified width and depth
    /// </summary>
    public void CreateRectangularGrid(int width, int depth)
    {
        Debug.Log($"Creating rectangular grid: {width}x{depth}");
        ClearGrid();
        
        // Update grid properties
        maxWidth = width;
        maxDepth = depth;
        
        // Create all cells
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                AddCell(gridPos, true);
            }
        }
        
        Debug.Log($"Rectangular grid created with {_cells.Count} cells");
    }
    
    /// <summary>
    /// Creates a custom grid based on a 2D boolean array
    /// </summary>
    public void CreateCustomGrid(bool[,] gridLayout)
    {
        if (gridLayout == null)
        {
            Debug.LogError("Cannot create custom grid with null layout!");
            return;
        }
        
        int width = gridLayout.GetLength(0);
        int depth = gridLayout.GetLength(1);
        
        Debug.Log($"Creating custom grid: {width}x{depth}");
        ClearGrid();
        
        // Update grid properties
        maxWidth = width;
        maxDepth = depth;
        
        // Create cells according to layout
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                AddCell(gridPos, gridLayout[x, z]);
            }
        }
        
        Debug.Log($"Custom grid created with {_cells.Count} cells");
    }
    
    /// <summary>
    /// Adds a cell to the grid at the specified position
    /// </summary>
    public GridCell AddCell(Vector2Int gridPos, bool isPlaceable)
    {
        // Calculate world position for this cell
        Vector3 worldPos = GetWorldPosition(gridPos);
        
        // Create a new cell
        GridCell cell = new GridCell(gridPos, worldPos, cellVisualPrefab, cellContainer);
        cell.SetPlaceable(isPlaceable);
        
        // Set initial material if available
        if (cell.CellVisual != null && normalCellMaterial != null)
        {
            Renderer renderer = cell.CellVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = normalCellMaterial;
            }
        }
        
        // Add to dictionary
        _cells[gridPos] = cell;
        
        return cell;
    }
    
    /// <summary>
    /// Removes a cell from the grid at the specified position
    /// </summary>
    public void RemoveCell(Vector2Int gridPos)
    {
        if (_cells.TryGetValue(gridPos, out GridCell cell))
        {
            if (cell.CellVisual != null)
            {
                Destroy(cell.CellVisual);
            }
            _cells.Remove(gridPos);
        }
    }
    
    /// <summary>
    /// Clears the entire grid
    /// </summary>
    public void ClearGrid()
    {
        Debug.Log($"Clearing grid with {_cells.Count} cells");
        
        foreach (var cell in _cells.Values)
        {
            if (cell.CellVisual != null)
            {
                Destroy(cell.CellVisual);
            }
        }
        
        _cells.Clear();
        _placedObjects.Clear();
    }
    
    /// <summary>
    /// Returns the total number of cells in the grid
    /// </summary>
    public int GetCellCount()
    {
        return _cells.Count;
    }
    
    /// <summary>
    /// Apply the default material to all cells
    /// </summary>
    private void ApplyDefaultMaterialToAllCells()
    {
        foreach (var cell in _cells.Values)
        {
            if (cell.CellVisual != null)
            {
                Renderer renderer = cell.CellVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = normalCellMaterial;
                }
            }
        }
    }
    #endregion
    
    #region Grid Position Conversion
    
    /// <summary>
    /// Converts a grid position to a world position
    /// </summary>
    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        float offsetX = -maxWidth * cellSize / 2;
        float offsetZ = -maxDepth * cellSize / 2;
        
        return new Vector3(
            offsetX + gridPos.x * cellSize + cellSize / 2,
            gridOrigin.y,
            offsetZ + gridPos.y * cellSize + cellSize / 2
        ) + new Vector3(gridOrigin.x, 0, gridOrigin.z);
    }
 
    /// <summary>
    /// Converts a world position to a grid position
    /// </summary>
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        float offsetX = -maxWidth * cellSize / 2;
        float offsetZ = -maxDepth * cellSize / 2;
        
        // Subtract only X and Z components of grid origin
        Vector3 localPosition = new Vector3(
            worldPosition.x - gridOrigin.x,
            worldPosition.y, // Y doesn't matter for grid position
            worldPosition.z - gridOrigin.z
        );
        
        int x = Mathf.FloorToInt((localPosition.x - offsetX) / cellSize);
        int z = Mathf.FloorToInt((localPosition.z - offsetZ) / cellSize);
        
        return new Vector2Int(x, z);
    }
    
    /// <summary>
    /// Checks if the position is within the grid bounds
    /// </summary>
    public bool IsWithinGridBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < maxWidth && 
               gridPos.y >= 0 && gridPos.y < maxDepth;
    }
    
    /// <summary>
    /// Clamps a position to ensure it's within grid bounds
    /// </summary>
    public Vector2Int ClampToGridBounds(Vector2Int gridPos)
    {
        return new Vector2Int(
            Mathf.Clamp(gridPos.x, 0, maxWidth - 1),
            Mathf.Clamp(gridPos.y, 0, maxDepth - 1)
        );
    }
    #endregion
    
    #region Object Placement and Validation
    
    /// <summary>
    /// Checks if the specified grid position contains a valid, placeable cell
    /// </summary>
    public bool IsCellValid(Vector2Int gridPos)
    {
        // Check if cell exists in our grid
        if (!_cells.TryGetValue(gridPos, out GridCell cell))
        {
            return false; // Cell doesn't exist in our grid
        }
    
        // Check if cell is placeable and not occupied
        if (!cell.IsPlaceable || cell.OccupyingObject != null)
        {
            return false;
        }
    
        return true;
    }
    
    /// <summary>
    /// Places an object on the grid at the specified positions
    /// </summary>
    public bool PlaceObject(GameObject obj, List<Vector2Int> occupiedCells)
    {
        if (obj == null || occupiedCells == null || occupiedCells.Count == 0)
        {
            Debug.LogError("Invalid parameters for PlaceObject");
            return false;
        }
        
        // Double check that all cells are valid before placement
        foreach (var pos in occupiedCells)
        {
            if (!IsCellValid(pos))
            {
                Debug.LogError($"Cannot place object at {pos} - cell is invalid or occupied");
                return false;
            }
        }
    
        // All cells are valid, so place the object
        foreach (var pos in occupiedCells)
        {
            if (_cells.TryGetValue(pos, out GridCell cell))
            {
                cell.SetOccupyingObject(obj);
            }
        }
        
        // Track this object and its cells
        _placedObjects[obj] = new List<Vector2Int>(occupiedCells);
    
        Debug.Log($"Object {obj.name} placed on {occupiedCells.Count} cells");
        return true;
    }

    /// <summary>
    /// Removes an object from the grid, properly clearing cell states
    /// </summary>
    public void RemoveObject(GameObject obj, List<Vector2Int> occupiedCells)
    {
        if (obj == null) return;
    
        // Check if the object is movable
        GridObject gridObj = obj.GetComponent<GridObject>();
        if (gridObj != null && !gridObj.isMovable)
        {
            Debug.LogWarning($"Cannot move object {obj.name} as it is marked as immovable");
            return;
        }
    
        int cellsCleared = 0;

        // First, reset all cell visuals to ensure proper state transition
        ResetAllCellVisuals();
    
        // Now clear the cells one by one
        foreach (var pos in occupiedCells)
        {
            if (_cells.TryGetValue(pos, out GridCell cell))
            {
                if (cell.ClearOccupyingObject(obj))
                {
                    // Explicitly update this cell to ensure it appears as unoccupied
                    cell.UpdateVisual(false, true, false);
                    cellsCleared++;
                }
            }
        }
    
        // Remove from tracked objects
        if (_placedObjects.ContainsKey(obj))
        {
            _placedObjects.Remove(obj);
        }

        Debug.Log($"Removed object {obj.name} from {cellsCleared} cells");
    }
    
    /// <summary>
    /// Destroys an object on the grid
    /// </summary>
    public bool DestroyObject(GameObject obj)
    {
        if (!_placedObjects.TryGetValue(obj, out List<Vector2Int> occupiedCells))
        {
            Debug.LogWarning($"Object {obj.name} is not placed on the grid");
            return false;
        }
        
        GridObject gridObj = obj.GetComponent<GridObject>();
        if (gridObj != null && !gridObj.isDestructible)
        {
            Debug.LogWarning($"Cannot destroy object {obj.name} as it is marked as indestructible");
            return false;
        }
        
        // Remove from grid
        RemoveObject(obj, occupiedCells);
        
        // Destroy the GameObject
        Destroy(obj);
        
        return true;
    }
    
    /// <summary>
    /// Checks if a placement is valid for all specified cells
    /// </summary>
    public bool IsValidPlacement(List<Vector2Int> cellsToCheck)
    {
        if (cellsToCheck == null || cellsToCheck.Count == 0)
        {
            return false;
        }
        
        // Basic cell validity check
        foreach (var pos in cellsToCheck)
        {
            // First check if position is within grid bounds
            if (!IsWithinGridBounds(pos))
            {
                return false;
            }
            
            // Then check if the cell is valid at that position
            if (!IsCellValid(pos))
            {
                return false;
            }
        }
        
        // If we don't require edge connectivity, or this is the first object, we're done
        if (!requireEdgeConnectivity || _placedObjects.Count == 0)
        {
            return true;
        }
        
        // Check edge connectivity - at least one edge must be adjacent to an existing object
        return HasEdgeConnectivity(cellsToCheck);
    }
    
    /// <summary>
    /// Checks if the specified cells have at least one edge connected to an existing object
    /// </summary>
    public bool HasEdgeConnectivity(List<Vector2Int> cellsToCheck)
    {
        // For each cell in the placement, check if any adjacent cell is occupied by another object
        foreach (var pos in cellsToCheck)
        {
            // Check all adjacent cells (up, down, left, right)
            Vector2Int[] adjacentCells = new Vector2Int[]
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };
            
            foreach (var adjacentPos in adjacentCells)
            {
                // Skip if the adjacent cell is part of our placement
                if (cellsToCheck.Contains(adjacentPos))
                {
                    continue;
                }
                
                // Skip if the adjacent cell is out of bounds
                if (!IsWithinGridBounds(adjacentPos))
                {
                    continue;
                }
                
                // Check if the adjacent cell exists and is occupied
                if (_cells.TryGetValue(adjacentPos, out GridCell cell) && cell.OccupyingObject != null)
                {
                    return true; // Found a connection!
                }
            }
        }
        
        return false; // No connectivity found
    }

    /// <summary>
    /// Get all currently placed objects
    /// </summary>
    public Dictionary<GameObject, List<Vector2Int>> GetAllPlacedObjects()
    {
        return new Dictionary<GameObject, List<Vector2Int>>(_placedObjects);
    }
    
    /// <summary>
    /// Gets all adjacent occupied cells to the specified positions
    /// </summary>
    public List<Vector2Int> GetAdjacentOccupiedCells(List<Vector2Int> positions)
    {
        List<Vector2Int> adjacentOccupied = new List<Vector2Int>();
        
        foreach (var pos in positions)
        {
            // Check all adjacent cells (up, down, left, right)
            Vector2Int[] adjacentCells = new Vector2Int[]
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };
            
            foreach (var adjacentPos in adjacentCells)
            {
                // Skip if the adjacent cell is part of our positions
                if (positions.Contains(adjacentPos) || adjacentOccupied.Contains(adjacentPos))
                {
                    continue;
                }
                
                // Check if the adjacent cell exists and is occupied
                if (_cells.TryGetValue(adjacentPos, out GridCell cell) && cell.OccupyingObject != null)
                {
                    adjacentOccupied.Add(adjacentPos);
                }
            }
        }
        
        return adjacentOccupied;
    }
    #endregion
    
    #region Cell Visualization
    
    /// <summary>
    /// Updates the visual state of the specified cells
    /// </summary>
    /// <param name="cellPositions">Cells to update</param>
    /// <param name="isValid">Whether the placement is valid</param>
    /// <param name="isDragging">Whether the update is during a drag operation</param>
    public void UpdateCellsVisual(List<Vector2Int> cellPositions, bool isValid, bool isDragging = false)
    {
        // In Clash of Clans style, we only highlight the cells that will be used
        if (showOnlyUsedTiles)
        {
            foreach (var pos in cellPositions)
            {
                if (_cells.TryGetValue(pos, out GridCell cell))
                {
                    cell.UpdateVisual(true, isValid, isDragging);
                }
            }
        }
        else
        {
            // Highlight all cells to show valid and invalid placements
            foreach (var cell in _cells.Values)
            {
                bool isBeingUsed = cellPositions.Contains(cell.GridPosition);
                cell.UpdateVisual(isBeingUsed, isValid, isDragging);
            }
        }
    }

    /// <summary>
    /// Resets the visual state of all cells
    /// </summary>
    public void ResetAllCellVisuals()
    {
        foreach (var cell in _cells.Values)
        {
            cell.UpdateVisual(false, true, false); // Explicitly reset dragging state
        }
    }

    /// <summary>
    /// Set custom colors for cell visualizations
    /// </summary>
    public void SetCellVisualizationColors(Color normalColor, Color validColor, Color invalidColor, Color occupiedColor)
    {
        foreach (var cell in _cells.Values)
        {
            cell.SetVisualizationColors(normalColor, validColor, invalidColor, occupiedColor);
        }
    }
    #endregion
    
    #region Grid Serialization
    
    /// <summary>
    /// Ensures that the save directory exists
    /// </summary>
    private void EnsureSaveDirectoryExists()
    {
        string savePath = Path.Combine(Application.persistentDataPath, saveFolder);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }
    
    /// <summary>
    /// Saves the current grid layout to a file
    /// </summary>
    public bool SaveGridLayout(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            Debug.LogError("Cannot save grid with empty filename");
            return false;
        }
        
        try
        {
            EnsureSaveDirectoryExists();
            
            // Create serializable grid data
            GridLayoutData layoutData = new GridLayoutData
            {
                width = maxWidth,
                height = maxDepth,
                cellSize = cellSize,
                originX = gridOrigin.x,
                originY = gridOrigin.y,
                originZ = gridOrigin.z,
                cells = new List<CellData>()
            };
            
            // Add all cells
            foreach (var cellPair in _cells)
            {
                Vector2Int pos = cellPair.Key;
                GridCell cell = cellPair.Value;
                
                layoutData.cells.Add(new CellData
                {
                    x = pos.x,
                    y = pos.y,
                    isPlaceable = cell.IsPlaceable
                });
            }
            
            // Convert to JSON
            string json = JsonUtility.ToJson(layoutData, true);
            
            // Save to file
            string savePath = Path.Combine(Application.persistentDataPath, saveFolder);
            string filePath = Path.Combine(savePath, $"{filename}.json");
            File.WriteAllText(filePath, json);
            
            Debug.Log($"Grid layout saved to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving grid layout: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Loads a grid layout from a file
    /// </summary>
    public bool LoadGridLayout(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            Debug.LogError("Cannot load grid with empty filename");
            return false;
        }
        
        try
        {
            // Check if file exists
            string savePath = Path.Combine(Application.persistentDataPath, saveFolder);
            string filePath = Path.Combine(savePath, $"{filename}.json");
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Grid layout file not found: {filePath}");
                return false;
            }
            
            // Read JSON
            string json = File.ReadAllText(filePath);
            GridLayoutData layoutData = JsonUtility.FromJson<GridLayoutData>(json);
            
            if (layoutData == null || layoutData.cells == null)
            {
                Debug.LogError("Failed to parse grid layout data");
                return false;
            }
            
            // Clear current grid
            ClearGrid();
            
            // Set grid properties
            maxWidth = layoutData.width;
            maxDepth = layoutData.height;
            cellSize = layoutData.cellSize;
            gridOrigin = new Vector3(layoutData.originX, layoutData.originY, layoutData.originZ);
            
            // Create cells
            foreach (var cellData in layoutData.cells)
            {
                Vector2Int pos = new Vector2Int(cellData.x, cellData.y);
                AddCell(pos, cellData.isPlaceable);
            }
            
            Debug.Log($"Grid layout loaded from {filePath} with {_cells.Count} cells");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading grid layout: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets a list of all saved grid layouts
    /// </summary>
    public string[] GetSavedGridLayouts()
    {
        List<string> layouts = new List<string>();
        
        try
        {
            string savePath = Path.Combine(Application.persistentDataPath, saveFolder);
            
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                return layouts.ToArray();
            }
            
            // Get all JSON files
            string[] files = Directory.GetFiles(savePath, "*.json");
            
            foreach (string file in files)
            {
                layouts.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting saved layouts: {e.Message}");
        }
        
        return layouts.ToArray();
    }
    #endregion
    
    #region Debug Methods
    
    /// <summary>
    /// Outputs debug information about the grid to the console
    /// </summary>
    public void DebugLogGridInfo()
    {
        Debug.Log($"=== GRID DEBUG INFO ===");
        Debug.Log($"Grid dimensions: {maxWidth}x{maxDepth}");
        Debug.Log($"Cell size: {cellSize}");
        Debug.Log($"Grid origin: {gridOrigin}");
        Debug.Log($"Total cells in grid: {_cells.Count}");
        Debug.Log($"Total placed objects: {_placedObjects.Count}");
        
        // Log some cell positions for verification
        Debug.Log("Sample of cells in grid:");
        int count = 0;
        foreach (var pos in _cells.Keys)
        {
            Debug.Log($"  Cell at {pos} exists");
            count++;
            if (count >= 10) break;
        }
        
        Debug.Log("=== END GRID DEBUG ===");
    }
    
    /// <summary>
    /// Draw debug gizmos in the scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw grid bounds
        Gizmos.color = Color.yellow;
        
        float width = maxWidth * cellSize;
        float depth = maxDepth * cellSize;
        
        Vector3 center = gridOrigin;
        Vector3 size = new Vector3(width, 0.1f, depth);
        
        Gizmos.DrawWireCube(center, size);
        
        // Draw a small marker at the grid origin
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(gridOrigin, 0.2f);
    }
    #endregion
}

/// <summary>
/// Data class for serializing grid layouts
/// </summary>
[System.Serializable]
public class GridLayoutData
{
    public int width;
    public int height;
    public float cellSize;
    public float originX;
    public float originY;
    public float originZ;
    public List<CellData> cells = new List<CellData>();
}

/// <summary>
/// Data class for serializing individual cells
/// </summary>
[System.Serializable]
public class CellData
{
    public int x;
    public int y;
    public bool isPlaceable;
}
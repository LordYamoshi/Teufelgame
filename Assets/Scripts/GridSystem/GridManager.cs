using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
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
    
    private Dictionary<Vector2Int, GridCell> cells = new Dictionary<Vector2Int, GridCell>();
    
    private Transform cellContainer;

    private void Awake()
    {
        cellContainer = new GameObject("Cell Container").transform;
        cellContainer.SetParent(transform);
        cellContainer.localPosition = Vector3.zero;
    }

    void Start()
    {
        CreateRectangularGrid(maxWidth, maxDepth);
    }
    
    
    /// <summary>
    /// Creates a rectangular grid with the specified width and depth
    /// </summary>
    /// <param name="width"></param>
    /// <param name="depth"></param>
    public void CreateRectangularGrid(int width, int depth)
    {
        ClearGrid();
        
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                AddCell(new Vector2Int(x, z), true);
            }
        }
    }
    /// <summary>
    /// Creates a custom shapes grid based on a 2D boolean array
    /// </summary>
    /// <param name="gridLayout"></param>
    public void CreateCustomGrid(bool[,] gridLayout)
    {
        ClearGrid();
        
        int width = gridLayout.GetLength(0);
        int depth = gridLayout.GetLength(1);
        
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (gridLayout[x, z])
                {
                    AddCell(new Vector2Int(x, z), true);
                }
            }
        }
    }
    
    /// <summary>
    /// Adds a cell to the grid at the specified position
    /// </summary>
    /// <param name="gridPos"></param>
    /// <param name="isPlaceable"></param>
    /// <returns></returns>
    public GridCell AddCell(Vector2Int gridPos, bool isPlaceable)
    {
        // Calculate world position for this cell
        Vector3 worldPos = GetWorldPosition(gridPos);
        
        // Create a new cell
        GridCell cell = new GridCell(gridPos, worldPos, cellVisualPrefab, cellContainer);
        cell.SetPlaceable(isPlaceable);
        
        // Add it to our dictionary
        cells[gridPos] = cell;
        
        return cell;
    }
    
    /// <summary>
    /// Removes a cell from the grid at the specified position
    /// </summary>
    /// <param name="gridPos"></param>
    public void RemoveCell(Vector2Int gridPos)
    {
        if (cells.TryGetValue(gridPos, out GridCell cell))
        {
            if (cell.cellVisual != null)
            {
                Destroy(cell.cellVisual);
            }
            cells.Remove(gridPos);
        }
    }
    
    /// <summary>
    /// Clears the entire grid
    /// </summary>
    public void ClearGrid()
    {
        foreach (var cell in cells.Values)
        {
            if (cell.cellVisual != null)
            {
                Destroy(cell.cellVisual);
            }
        }
        
        cells.Clear();
    }
    
    
    /// <summary>
    /// Converts a grid position to a world position
    /// </summary>
    /// <param name="gridPos"></param>
    /// <returns></returns>
    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        float offsetX = -maxWidth * cellSize / 2;
        float offsetZ = -maxDepth * cellSize / 2;
        
        return new Vector3(
            offsetX + gridPos.x * cellSize + cellSize / 2,
            0,
            offsetZ + gridPos.y * cellSize + cellSize / 2
        ) + gridOrigin;
    }
 
    /// <summary>
    /// Converts a world position to a grid position
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        float offsetX = -maxWidth * cellSize / 2;
        float offsetZ = -maxDepth * cellSize / 2;
        
        Vector3 localPosition = worldPosition - gridOrigin;
        
        int x = Mathf.FloorToInt((localPosition.x - offsetX) / cellSize);
        int z = Mathf.FloorToInt((localPosition.z - offsetZ) / cellSize);
        
        return new Vector2Int(x, z);
    }
    
    
    /// <summary>
    /// Checks if the specified grid position contains a valid, placeable cell
    /// </summary>
    /// <param name="gridPos"></param>
    /// <returns></returns>
    public bool IsCellValid(Vector2Int gridPos)
    {
        // Check for if the cell cell exists in our grid
        if (!cells.TryGetValue(gridPos, out GridCell cell))
        {
            return false; // Cell doesn't exist in our grid
        }
    
        // Check for if the cell is placeable and not occupied
        if (!cell.isPlaceable || cell.occupyingObject != null)
        {
            return false; // Cell is either not placeable or already occupied
        }
    
        return true;
    }
    
    /// <summary>
    /// Places an object on the grid at the specified position
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="occupiedCells"></param>
    /// <returns></returns>
    public bool PlaceObject(GameObject obj, List<Vector2Int> occupiedCells)
    {
        // Double check that all cells are valid before placement
        foreach (var pos in occupiedCells)
        {
            if (!IsCellValid(pos))
            {
                Debug.LogError($"Attempting to place object on invalid cell at {pos}");
                return false; // Fail fast if any cell is invalid
            }
        }
    
        // All cells are valid, so place the object
        foreach (var pos in occupiedCells)
        {
            if (cells.TryGetValue(pos, out GridCell cell))
            {
                cell.SetOccupyingObject(obj);
            }
        }
    
        Debug.Log($"Object {obj.name} placed on {occupiedCells.Count} cells");
        return true;
    }
    
    
    /// <summary>
    /// Removes an object from the grid
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="occupiedCells"></param>
    public void RemoveObject(GameObject obj, List<Vector2Int> occupiedCells)
    {
        int cellsCleared = 0;
    
        foreach (var pos in occupiedCells)
        {
            if (cells.TryGetValue(pos, out GridCell cell))
            {
                if (cell.ClearOccupyingObject(obj))
                {
                    cellsCleared++;
                }
            }
        }
    
        Debug.Log($"Removed object {obj.name} from {cellsCleared} cells");
    }
    
    /// <summary>
    /// Checks if a palcement is valid for all specified cells
    /// </summary>
    /// <param name="cellsToCheck"></param>
    /// <returns></returns>
    public bool IsValidPlacement(List<Vector2Int> cellsToCheck)
    {
        foreach (var pos in cellsToCheck)
        {
            if (!IsCellValid(pos))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Updates the visual state of the specified cells
    /// </summary>
    /// <param name="cellPositions"></param>
    /// <param name="isValid"></param>
    public void UpdateCellsVisual(List<Vector2Int> cellPositions, bool isValid)
    {
        foreach (var pos in cellPositions)
        {
            if (cells.TryGetValue(pos, out GridCell cell))
            {
                cell.UpdateVisual(true, isValid);
            }
        }
    }
    
    
    /// <summary>
    /// Resets the visual state of all cells
    /// </summary>
    public void ResetAllCellVisuals()
    {
        foreach (var cell in cells.Values)
        {
            cell.UpdateVisual();
        }
    }
    
    
}

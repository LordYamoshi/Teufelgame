using System.Collections.Generic;
using UnityEngine;

public class GridObject : MonoBehaviour
{
    [Header("Grid Properties")]
    [Tooltip("Layout of the object represented as a 2D array. Use 1 for occupied cells, 0 for empty spaces.")]
    [SerializeField] private int[,] objectLayout = { { 1 } }; // Default is a 1x1 object
    
    [Tooltip("The cell in the layout that serves as the reference point (pivot)")]
    [SerializeField] private Vector2Int pivotCell = Vector2Int.zero;
    
    [Header("Visualization")]
    [Tooltip("Material to use when the object placement is valid")]
    public Material validPlacementMaterial;
    
    [Tooltip("Material to use when the object placement is invalid")]
    public Material invalidPlacementMaterial;
    
    [Tooltip("Material to use when the object is selected")]
    public Material selectedMaterial;
    
    // List of relative cell positions this object occupies
    private List<Vector2Int> relativeCellPositions = new List<Vector2Int>();
    
    // List of world positions this object occupies
    private List<Vector2Int> currentGridPositions = new List<Vector2Int>();
    
    // Original materials for restoration
    private Material[] originalMaterials;
    private Renderer[] objectRenderers;
    
    
    private void Awake()
    {
        objectRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[objectRenderers.Length];
        
        for (int i = 0; i < objectRenderers.Length; i++)
        {
            originalMaterials[i] = objectRenderers[i].material;
        }
        
        // Calculate the relative cell positions from the object layout
        CalculateRelativeCellPositions();
        
        SetObjectLayout(new int[2,2],Vector2Int.zero);
    }
    
    
    /// <summary>
    /// Calculates all the relative grid cells this object occupies based on it's layout
    /// </summary>
    private void CalculateRelativeCellPositions()
    {
        relativeCellPositions.Clear();
        
        // If the layout isn't specified, default to a 1x1 object
        if (objectLayout == null)
        {
            relativeCellPositions.Add(Vector2Int.zero);
            return;
        }
        
        int width = objectLayout.GetLength(0);
        int height = objectLayout.GetLength(1);
        
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (objectLayout[x, z] == 1)
                {
                    // Calculate position relative to the pivot
                    Vector2Int relativePos = new Vector2Int(x, z) - pivotCell;
                    relativeCellPositions.Add(relativePos);
                }
            }
        }
    }
     
    /// <summary>
    /// Sets the object's grid layout
    /// </summary>
    /// <param name="newLayout"></param>
    /// <param name="newPivot"></param>
    public void SetObjectLayout(int[,] newLayout, Vector2Int newPivot)
    {
        objectLayout = newLayout;
        pivotCell = newPivot;
        CalculateRelativeCellPositions();
    }
    
    //Gets the world position of the object
    public Vector3 GetCenterOffset(float cellSize)
    {
        if (relativeCellPositions.Count == 0)
        {
            return Vector3.zero;
        }
        
        // Calculate average position
        Vector2 sum = Vector2.zero;
        foreach (var pos in relativeCellPositions)
        {
            sum += new Vector2(pos.x, pos.y);
        }
        
        Vector2 average = sum / relativeCellPositions.Count;
        return new Vector3(average.x * cellSize, 0, average.y * cellSize);
    }
    
    /// <summary>
    /// Gets the grid cells this object would occupy if placed at the specified grid position
    /// </summary>
    /// <param name="baseGridPosition"></param>
    /// <returns></returns>
    public List<Vector2Int> GetOccupiedCells(Vector2Int baseGridPosition)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        
        foreach (var relativePos in relativeCellPositions)
        {
            Vector2Int worldGridPos = baseGridPosition + relativePos;
            result.Add(worldGridPos);
        }
        
        return result;
    }
    
    /// <summary>
    /// Updates the current occupied grid positions
    /// </summary>
    /// <param name="baseGridPosition"></param>
    public void UpdateCurrentGridPositions(Vector2Int baseGridPosition)
    {
        currentGridPositions = GetOccupiedCells(baseGridPosition);
    }
    
    /// <summary>
    /// Gets the current grid positions of the object
    /// </summary>
    /// <returns></returns>
    public List<Vector2Int> GetCurrentGridPositions()
    {
        return currentGridPositions;
    }
    
    /// <summary>
    /// Sets all renderer's materials
    /// </summary>
    /// <param name="material"></param>
    public void SetAllMaterials(Material material)
    {
        foreach (var renderer in objectRenderers)
        {
            renderer.material = material;
        }
    }
    
    /// <summary>
    /// Restores the original materials of the object
    /// </summary>
    public void RestoreOriginalMaterials()
    {
        for (int i = 0; i < objectRenderers.Length; i++)
        {
            objectRenderers[i].material = originalMaterials[i];
        }
    }
    
    /// <summary>
    /// Creates common object shapes for convenience
    /// </summary>
    /// <param name="shapeType">Type of shape</param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static int[,] CreateShape(ShapeType shapeType, int size = 2)
    {
        switch (shapeType)
        {
            case ShapeType.Square:
                return CreateSquareShape(size);
            case ShapeType.Rectangle:
                return CreateRectangleShape(size, size + 1);
            case ShapeType.L:
                return CreateLShape(size);
            case ShapeType.T:
                return CreateTShape(size);
            case ShapeType.Cross:
                return CreateCrossShape(size);
            default:
                return new int[,] { { 1 } };
        }
    }
    
      private static int[,] CreateSquareShape(int size)
    {
        int[,] layout = new int[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                layout[x, z] = 1;
            }
        }
        return layout;
    }
    
    private static int[,] CreateRectangleShape(int width, int height)
    {
        int[,] layout = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                layout[x, z] = 1;
            }
        }
        return layout;
    }
    
    private static int[,] CreateLShape(int size)
    {
        int[,] layout = new int[size, size];
        
        // Create the L shape
        for (int x = 0; x < size; x++)
        {
            layout[0, x] = 1; // Vertical part
            
            if (x == 0)
            {
                for (int z = 0; z < size; z++)
                {
                    layout[z, 0] = 1; // Horizontal part
                }
            }
        }
        
        return layout;
    }
    
    private static int[,] CreateTShape(int size)
    {
        int[,] layout = new int[size, size];
        
        // Create the T shape
        for (int x = 0; x < size; x++)
        {
            // Top bar of the T
            layout[x, size - 1] = 1;
            
            // Stem of the T
            if (x == size / 2)
            {
                for (int z = 0; z < size - 1; z++)
                {
                    layout[x, z] = 1;
                }
            }
        }
        
        return layout;
    }
    
    private static int[,] CreateCrossShape(int size)
    {
        int[,] layout = new int[size, size];
        int center = size / 2;
        
        // Create the cross shape
        for (int x = 0; x < size; x++)
        {
            layout[x, center] = 1; // Horizontal bar
            layout[center, x] = 1; // Vertical bar
        }
        
        return layout;
    }
    
    [SerializeField] public enum ShapeType
    {
        Square,
        Rectangle,
        L,
        T,
        Cross
    }
}

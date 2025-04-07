using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Represents objects that can be placed on the grid with specific shapes and properties
/// </summary>
public class GridObject : MonoBehaviour
{
    private Vector2Int[] rotationMatrices = new Vector2Int[]
    {
        new Vector2Int(1, 0), 
        new Vector2Int(0, 1),  
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1)  
    };

    
    #region Properties and Fields
    [Header("Grid Properties")]
    [Tooltip("Layout of the object represented as a 2D array. 1 for occupied cells, 0 for empty spaces.")]
    [SerializeField] private int[,] objectLayout = {{1}};
    
    [Tooltip("The cell in the layout that serves as the reference point (pivot)")]
    [SerializeField] private Vector2Int pivotCell = Vector2Int.zero;
    
    [Header("Shape Settings")]
    [Tooltip("Current shape type for this object")]
    public ShapeType currentShapeType = ShapeType.Square;
    
    [Tooltip("Size of the shape (number of cells)")]
    [Range(2, 5)]
    public int shapeSize = 3;
    
    [Tooltip("Apply shape changes automatically in Play mode")]
    public bool autoUpdateShape = false;
    
    [Header("Object Properties")]
    [Tooltip("Can this object be moved after placement")]
    public bool isMovable = true;
    
    [Tooltip("Can this object be destroyed")]
    public bool isDestructible = true;
    
    [Tooltip("Current rotation of the object in 90-degree increments (0-3)")]
    [Range(0, 3)]
    public int rotationIndex = 0;
    
    [Header("Visualization")]
    [Tooltip("Material to use when the object placement is valid")]
    public Material validPlacementMaterial;
    
    [Tooltip("Material to use when the object placement is invalid")]
    public Material invalidPlacementMaterial;
    
    [Tooltip("Material to use when the object is selected")]
    public Material selectedMaterial;
    
    [Header("Shape Serialization")]
    [Tooltip("Folder to save/load shapes")]
    public string saveFolder = "GridShapes";
    
    [Tooltip("Shape to load on start (if any)")]
    public string loadShapeOnStart = "";
    
    private List<Vector2Int> _relativeCellPositions = new List<Vector2Int>();

    private List<Vector2Int> _currentGridPositions = new List<Vector2Int>();
    
    private Material[] _originalMaterials;
    private Renderer[] _objectRenderers;

    [HideInInspector] public Vector3 visualOffset = Vector3.zero;
    private bool _isInitialized = false;
    
    private int[,] _originalLayout;
    private Vector2Int _originalPivot;
    private int _originalRotation;
    
    private ShapeType _lastShapeType;
    private int _lastShapeSize;
    private int _lastRotationIndex;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        // Cache all renderers and their materials
        _objectRenderers = GetComponentsInChildren<Renderer>();
        _originalMaterials = new Material[_objectRenderers.Length];
    
        for (int i = 0; i < _objectRenderers.Length; i++)
        {
            _originalMaterials[i] = _objectRenderers[i].sharedMaterial;
        }
    
        // Store initial values for change detection
        _lastShapeType = currentShapeType;
        _lastShapeSize = shapeSize;
        _lastRotationIndex = rotationIndex;
    
        // Calculate the relative cell positions from the object layout
        CalculateRelativeCellPositions();
        
        // Create save directory if needed
        EnsureSaveDirectoryExists();
    }
    
    private void Start()
    {
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, rotationIndex * 90, 0);
        
        // Apply the initial shape if none is set
        if (objectLayout.Length <= 1 && currentShapeType != ShapeType.Custom)
        { 
            ApplyCurrentShape();
        }
        
        // Load shape if specified
        if (!string.IsNullOrEmpty(loadShapeOnStart))
        {
            LoadShape(loadShapeOnStart);
        }
    }
    
    private void Update()
    {
        if (!autoUpdateShape) return;
        
        // Check for shape or rotation changes
        if (currentShapeType != _lastShapeType || 
            shapeSize != _lastShapeSize || 
            rotationIndex != _lastRotationIndex)
        {
            ApplyCurrentShape();
            
            // Update last values
            _lastShapeType = currentShapeType;
            _lastShapeSize = shapeSize;
            _lastRotationIndex = rotationIndex;
        }
    }
    #endregion
    
    
    #region Layout and Rotation Methods
    
    private void InitializeWithDefaults()
    {
        if (_isInitialized) return;
    
        // Store initial values for layout and pivot
        _originalLayout = GetCurrentLayout();
        _originalPivot = GetCurrentPivot();
        _originalRotation = rotationIndex;
    
        // Mark as initialized
        _isInitialized = true;
    
        Debug.Log($"Initialized {gameObject.name} with rotation {rotationIndex}, pivot {pivotCell}");
    }
    
    /// <summary>
    /// Calculates all the relative grid cells this object occupies based on its layout and rotation
    /// </summary>
    public void CalculateRelativeCellPositions()
    {
        _relativeCellPositions.Clear();

        // If the layout isn't specified, default to a 1x1 object
        if (objectLayout == null)
        {
            _relativeCellPositions.Add(Vector2Int.zero);
            return;
        }

        int width = objectLayout.GetLength(0);
        int height = objectLayout.GetLength(1);
    
        Debug.Log($"Calculating positions for rotation {rotationIndex} (shape: {currentShapeType})");

        // For each cell in the layout
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (objectLayout[x, y] == 1)
                {
                    // Get the local position relative to pivot
                    Vector2Int localPos = new Vector2Int(x - pivotCell.x, y - pivotCell.y);
                
                    // Apply rotation using our fixed rotation function
                    Vector2Int rotatedPos = RotatePosition(localPos, rotationIndex);
                
                    _relativeCellPositions.Add(rotatedPos);
                
                    Debug.Log($"Cell {x},{y} → local {localPos.x},{localPos.y} → " +
                              $"rotated {rotationIndex*90}° = {rotatedPos.x},{rotatedPos.y}");
                }
            }
        }

        Debug.Log($"Calculated {_relativeCellPositions.Count} cell positions");
    }
    
    private Vector2 ApplyRotation(Vector2 position, int rotIndex)
    {
        switch (rotIndex)
        {
            case 0: // 0 degrees
                return position;
            case 1: // 90 degrees
                return new Vector2(-position.y, position.x);
            case 2: // 180 degrees
                return new Vector2(-position.x, -position.y);
            case 3: // 270 degrees
                return new Vector2(position.y, -position.x);
            default:
                Debug.LogError($"Invalid rotation index: {rotIndex}");
                return position;
        }
    }
    
    /// <summary>
    /// Rotates the object by 90 degrees clockwise
    /// </summary>
    public void RotateClockwise()
    {
        // Increment rotation index
        rotationIndex = (rotationIndex + 1) % 4;
    
        // Apply visual rotation
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, rotationIndex * 90, 0);
    
        // Recalculate the cell positions with the new rotation
        CalculateRelativeCellPositions();
    
        Debug.Log($"Rotated to {rotationIndex*90}° (index {rotationIndex})");
    
        // Add extra visual debug for rotation alignment
#if UNITY_EDITOR
        // Draw direction arrows in the scene view to show axes
        Vector3 position = transform.position;
        Debug.DrawLine(position, position + transform.right, Color.red, 2.0f);   // X axis
        Debug.DrawLine(position, position + transform.forward, Color.blue, 2.0f); // Z axis
        Debug.DrawLine(position, position + transform.up, Color.green, 2.0f);     // Y axis
#endif
    }
    
    public Vector2Int[] GetAbsoluteOccupiedPositions(Vector2Int basePosition)
    {
        // Calculate rotated positions
        List<Vector2Int> positions = GetOccupiedCells(basePosition);
        return positions.ToArray();
    }
    
    public Vector2Int RotatePosition(Vector2Int position, int rotationIdx)
    {
        // Flip the rotation direction (counterclockwise instead of clockwise)
        switch (rotationIdx)
        {
            case 0: // 0 degrees
                return position;
            
            case 1:
                return new Vector2Int(position.y, -position.x);
            
            case 2: // 180 degrees
                return new Vector2Int(-position.x, -position.y);
            
            case 3: 
                return new Vector2Int(-position.y, position.x);
            
            default:
                Debug.LogError($"Invalid rotation index: {rotationIdx}");
                return position;
        }
    }
    
    /// <summary>
    /// Sets the object's grid layout
    /// </summary>
    public void SetObjectLayout(int[,] newLayout, Vector2Int newPivot)
    {
        if (newLayout == null || newLayout.Length == 0)
        {
            Debug.LogError("Cannot set null or empty layout");
            return;
        }
        
        objectLayout = newLayout;
        pivotCell = newPivot;
        CalculateRelativeCellPositions();
    }
    
    /// <summary>
    /// Applies the current shape settings to create a new layout
    /// </summary>
    public void ApplyCurrentShape()
    {
        // Create the shape layout
        int[,] layout = CreateShape(currentShapeType, shapeSize);

        // Calculate default pivot (center of the shape)
        int width = layout.GetLength(0);
        int height = layout.GetLength(1);
        Vector2Int defaultPivot = new Vector2Int(width / 2, height / 2);
        
        // Set the layout
        SetObjectLayout(layout, defaultPivot);
    }
    
    /// <summary>
    /// Apply a specific shape type
    /// </summary>
    public void SetShapeType(ShapeType shapeType, bool applyImmediately = true)
    {
        currentShapeType = shapeType;
        
        if (applyImmediately)
        {
            ApplyCurrentShape();
        }
    }
    
    
    
    /// <summary>
    /// Set the shape size and optionally apply it immediately
    /// </summary>
    public void SetShapeSize(int size, bool applyImmediately = true)
    {
        shapeSize = Mathf.Clamp(size, 2, 5);
        
        if (applyImmediately)
        {
            ApplyCurrentShape();
        }
    }
    #endregion
    
    #region Grid Positioning Methods
    
    /// <summary>
    /// Gets the world position offset from the base grid position
    /// </summary>
    public Vector3 GetCenterOffset(float cellSize)
    {
        // For a rotated object, we need to account for both the visual and logical centers
        Vector3 offset = Vector3.zero;
    
        // Find the center of all occupied cells
        if (_relativeCellPositions.Count > 0)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
        
            foreach (var pos in _relativeCellPositions)
            {
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }
        
            // Get center of the bounding box
            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;
        
            offset = new Vector3(centerX * cellSize, 0, centerY * cellSize);
        }
    
        // For rectangular shapes, apply additional visual offset based on rotation
        if (visualOffset != Vector3.zero)
        {
            Vector3 rotatedVisualOffset = ApplyRotationToVector3(visualOffset, rotationIndex);
            offset += rotatedVisualOffset * cellSize;
        }

        return offset;
    }
    private Vector3 ApplyRotationToVector3(Vector3 position, int rotIndex)
    {
        switch (rotIndex)
        {
            case 0: // 0 degrees
                return position;
            case 1: // 90 degrees
                return new Vector3(-position.z, position.y, position.x);
            case 2: // 180 degrees
                return new Vector3(-position.x, position.y, -position.z);
            case 3: // 270 degrees
                return new Vector3(position.z, position.y, -position.x);
            default:
                Debug.LogError($"Invalid rotation index: {rotIndex}");
                return position;
        }
    }
    
    /// <summary>
    /// Gets the grid cells this object would occupy if placed at the specified grid position
    /// </summary>
    public List<Vector2Int> GetOccupiedCells(Vector2Int baseGridPosition)
    {
        List<Vector2Int> result = new List<Vector2Int>();
    
        foreach (var relativePos in _relativeCellPositions)
        {
            Vector2Int worldGridPos = baseGridPosition + relativePos;
            result.Add(worldGridPos);
        }
    
        return result;
    }

    /// <summary>
    /// Gets the edge cells of this object at the specified position
    /// </summary>
    public List<Vector2Int> GetEdgeCells(Vector2Int baseGridPosition)
    {
        List<Vector2Int> occupiedCells = GetOccupiedCells(baseGridPosition);
        List<Vector2Int> edgeCells = new List<Vector2Int>();
        
        // For each occupied cell, check if any of its 4 neighbors is not occupied by this object
        foreach (var cell in occupiedCells)
        {
            // Check each of the 4 adjacent cells
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(cell.x + 1, cell.y),
                new Vector2Int(cell.x - 1, cell.y),
                new Vector2Int(cell.x, cell.y + 1),
                new Vector2Int(cell.x, cell.y - 1)
            };
            
            bool isEdge = false;
            
            foreach (var neighbor in neighbors)
            {
                // If the neighbor is not in our occupied list, this cell is an edge
                if (!occupiedCells.Contains(neighbor))
                {
                    isEdge = true;
                    break;
                }
            }
            
            if (isEdge)
            {
                edgeCells.Add(cell);
            }
        }
        
        return edgeCells;
    }
    
    public void MarkAsImmovable()
    {
        isMovable = false;
    
        // Store original materials if needed for restoration later
        if (_originalMaterials == null)
        {
            _objectRenderers = GetComponentsInChildren<Renderer>();
            _originalMaterials = new Material[_objectRenderers.Length];
        
            for (int i = 0; i < _objectRenderers.Length; i++)
            {
                _originalMaterials[i] = _objectRenderers[i].sharedMaterial;
            }
        }
    
        // Apply visual indicator through material properties
        foreach (var renderer in _objectRenderers)
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", Color.red * 0.3f);
        
            if (renderer.material.HasProperty("_RimColor"))
            {
                renderer.material.SetColor("_RimColor", Color.red);
                renderer.material.SetFloat("_RimPower", 3.0f);
            }
        
            // Option 3: Tint the existing material slightly
            Color baseColor = renderer.material.color;
            renderer.material.color = Color.Lerp(baseColor, Color.red, 0.2f);
        }
    }
    
    /// <summary>
    /// Updates the current occupied grid positions
    /// </summary>
    public void UpdateCurrentGridPositions(Vector2Int baseGridPosition)
    {
        _currentGridPositions = GetOccupiedCells(baseGridPosition);
    }
    
    /// <summary>
    /// Gets the current grid positions of the object
    /// </summary>
    public List<Vector2Int> GetCurrentGridPositions()
    {
        return new List<Vector2Int>(_currentGridPositions);
    }
    #endregion
    
    #region Visualization Methods
    
    /// <summary>
    /// Sets all renderer's materials
    /// </summary>
    public void SetAllMaterials(Material material)
    {
        if (material == null) return;
        
        foreach (var renderer in _objectRenderers)
        {
            renderer.material = material;
        }
    }
    
    /// <summary>
    /// Restores the original materials of the object
    /// </summary>
    public void RestoreOriginalMaterials()
    {
        for (int i = 0; i < _objectRenderers.Length; i++)
        {
            if (i < _originalMaterials.Length)
            {
                _objectRenderers[i].material = _originalMaterials[i];
            }
        }
    }
    #endregion
    
    #region Layout Access Methods
    
    /// <summary>
    /// Gets a copy of the current layout
    /// </summary>
    public int[,] GetCurrentLayout()
    {
        if (objectLayout == null)
        {
            return new int[,] { { 1 } };
        }
    
        int width = objectLayout.GetLength(0);
        int height = objectLayout.GetLength(1);
        int[,] layoutCopy = new int[width, height];
    
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                layoutCopy[x, y] = objectLayout[x, y];
            }
        }
    
        return layoutCopy;
    }

    /// <summary>
    /// Gets the current pivot cell position
    /// </summary>
    public Vector2Int GetCurrentPivot()
    {
        return pivotCell;
    }

    /// <summary>
    /// Copies the layout from another GridObject
    /// </summary>
    public void CopyLayoutFrom(GridObject otherGridObject)
    {
        if (otherGridObject == null) return;
    
        // Copy shape properties
        currentShapeType = otherGridObject.currentShapeType;
        shapeSize = otherGridObject.shapeSize;
        rotationIndex = otherGridObject.rotationIndex;
    
        // Copy the layout
        int[,] sourceLayout = otherGridObject.GetCurrentLayout();
        Vector2Int sourcePivot = otherGridObject.GetCurrentPivot();
    
        // Apply to this object
        SetObjectLayout(sourceLayout, sourcePivot);
    }
    #endregion
    
    #region Object Management Methods
    
    /// <summary>
    /// Destroys the object and updates the grid
    /// </summary>
    public void DestroyObject(GridManager gridManager)
    {
        if (gridManager == null)
        {
            Debug.LogError("Cannot destroy object without GridManager reference");
            return;
        }
        
        if (!isDestructible)
        {
            Debug.LogWarning($"Cannot destroy {gameObject.name} as it is marked as indestructible");
            return;
        }
        
        // Remove from grid
        gridManager.RemoveObject(gameObject, _currentGridPositions);
        
        // Destroy the GameObject
        Destroy(gameObject);
    }
    #endregion
    
    #region Shape Creation Methods
    
    /// <summary>
    /// Creates common object shapes
    /// </summary>
    public static int[,] CreateShape(ShapeType shapeType, int size = 3)
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
        size = Mathf.Max(2, size); // Ensure minimum size
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
        width = Mathf.Max(2, width);
        height = Mathf.Max(2, height);
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
        size = Mathf.Max(2, size);
        int[,] layout = new int[size, size];
        
        // Create the L shape
        for (int x = 0; x < size; x++)
        {
            // Vertical part
            layout[0, x] = 1;
        }
        
        // Horizontal part
        for (int x = 0; x < size; x++)
        {
            layout[x, 0] = 1;
        }
        
        return layout;
    }
    
    private static int[,] CreateTShape(int size)
    {
        size = Mathf.Max(3, size);
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
        size = Mathf.Max(3, size);
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
    
    /// <summary>
    /// Applies a custom layout to the object
    /// </summary>
    public void SetCustomLayout(int[,] customLayout, Vector2Int pivot)
    {
        if (customLayout == null || customLayout.Length == 0)
        {
            Debug.LogError("Cannot set null or empty custom layout");
            return;
        }
        
        // Set the shape type to Custom since we're applying a custom layout
        currentShapeType = ShapeType.Custom;
        
        // Apply the layout
        SetObjectLayout(customLayout, pivot);
    }
    #endregion
    
    #region Shape Serialization
    
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
    /// Saves the current shape to a file
    /// </summary>
    public bool SaveShape(string shapeName)
    {
        if (string.IsNullOrEmpty(shapeName))
        {
            Debug.LogError("Cannot save shape with empty name");
            return false;
        }
    
        try
        {
            // Ensure we have a valid layout
            if (objectLayout == null || objectLayout.Length <= 1)
            {
                Debug.LogError("Cannot save empty or invalid layout");
                return false;
            }
        
            EnsureSaveDirectoryExists();
        
            // Get dimensions first
            int width = objectLayout.GetLength(0);
            int height = objectLayout.GetLength(1);
        
            // Create shape data for serialization
            ShapeData data = new ShapeData
            {
                width = width,
                height = height,
                pivotX = pivotCell.x,
                pivotY = pivotCell.y,
                shapeType = (int)currentShapeType,
                rotationIndex = rotationIndex,
                layoutData = new int[width * height]
            };
        
            // Flatten the layout
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    data.layoutData[index] = objectLayout[x, y];
                }
            }
        
            // Serialize to JSON
            string json = JsonUtility.ToJson(data, true);
        
            // Save to file
            string savePath = Path.Combine(Application.persistentDataPath, saveFolder);
            string filePath = Path.Combine(savePath, $"{shapeName}.json");
            File.WriteAllText(filePath, json);
        
            Debug.Log($"Shape saved to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving shape: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Loads a shape from a file
    /// </summary>
    public bool LoadShape(string shapeName)
    {
        if (string.IsNullOrEmpty(shapeName))
        {
            Debug.LogError("Cannot load shape with empty name");
            return false;
        }
        
        try
        {
            string savePath = Path.Combine(Application.persistentDataPath, saveFolder);
            string filePath = Path.Combine(savePath, $"{shapeName}.json");
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Shape file not found: {filePath}");
                return false;
            }
            
            // Read JSON
            string json = File.ReadAllText(filePath);
            ShapeData data = JsonUtility.FromJson<ShapeData>(json);
            
            if (data == null || data.layoutData == null)
            {
                Debug.LogError("Failed to parse shape data");
                return false;
            }
            
            // Create the layout
            int[,] layout = new int[data.width, data.height];
            
            // Unflatten the layout
            for (int y = 0; y < data.height; y++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    int index = y * data.width + x;
                    if (index < data.layoutData.Length)
                    {
                        layout[x, y] = data.layoutData[index];
                    }
                }
            }
            
            // Set the pivot
            Vector2Int pivot = new Vector2Int(data.pivotX, data.pivotY);
            
            // Apply to grid object
            SetObjectLayout(layout, pivot);
            
            // Set shape type and rotation
            currentShapeType = (ShapeType)data.shapeType;
            rotationIndex = data.rotationIndex;
            
            // Apply rotation visually
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, rotationIndex * 90, 0);
            
            Debug.Log($"Shape '{shapeName}' loaded successfully");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading shape: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets a list of all saved shapes
    /// </summary>
    public string[] GetSavedShapes()
    {
        List<string> shapes = new List<string>();
        
        try
        {
            string savePath = Path.Combine(Application.persistentDataPath, saveFolder);
            
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                return shapes.ToArray();
            }
            
            // Get all JSON files
            string[] files = Directory.GetFiles(savePath, "*.json");
            
            foreach (string file in files)
            {
                shapes.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting saved shapes: {e.Message}");
        }
        
        return shapes.ToArray();
    }
    #endregion
    
    /// <summary>
    /// Shape types available for grid objects
    /// </summary>
    [System.Serializable]
    public enum ShapeType
    {
        Square,
        Rectangle,
        L,
        T,
        Cross,
        Custom
    }
}

/// <summary>
/// Data class for serializing shapes
/// </summary>
[System.Serializable]
public class ShapeData
{
    public int width;
    public int height;
    public int pivotX;
    public int pivotY;
    public int shapeType;
    public int rotationIndex;
    public int[] layoutData;
}
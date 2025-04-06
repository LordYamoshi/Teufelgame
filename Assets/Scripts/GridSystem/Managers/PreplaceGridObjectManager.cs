using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manages pre-placed grid objects for level design. Allows you to create
/// immovable or destroy-only objects in the scene that will be recognized by the GridManager.
/// </summary>
public class PreplacedGridObjectsManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Reference to the GridManager")]
    public GridManager gridManager;

    [Header("Preoccupying Objects")]
    [Tooltip("List of objects to place on the grid when the level starts")]
    public List<PreplacedGridObjectData> preplacedObjects = new List<PreplacedGridObjectData>();

    [Header("Prefabs")]
    [Tooltip("Prefabs for creating new preoccupying objects")]
    public List<GameObject> gridObjectPrefabs = new List<GameObject>();

    private void Start()
    {
        // Auto-find grid manager if not assigned
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("No GridManager found in the scene.");
                return;
            }
        }

        PlacePreoccupyingObjects();
    }

    /// <summary>
    /// Places all preoccupying objects on the grid
    /// </summary>
    public void PlacePreoccupyingObjects()
    {
        foreach (var objectData in preplacedObjects)
        {
            if (objectData.prefab == null) continue;

            // Instantiate the object
            GameObject gridObj = Instantiate(objectData.prefab);
            gridObj.name = objectData.prefab.name + " (Preplaced)";
            gridObj.transform.parent = transform;

            // Get and configure the GridObject component
            GridObject grid = gridObj.GetComponent<GridObject>();
            if (grid == null)
            {
                Debug.LogWarning($"Prefab {objectData.prefab.name} doesn't have a GridObject component.");
                Destroy(gridObj);
                continue;
            }

            // Apply object restrictions
            grid.isMovable = objectData.isMovable;
            grid.isDestructible = objectData.isDestructible;

            // Calculate world position from grid position with height offset
            Vector3 worldPos;

            // Check if we should apply height offset to preplaced objects
            if (gridManager.applyHeightOffsetToPreplaced)
            {
                worldPos = gridManager.GetWorldPositionWithOffset(objectData.gridPosition);
            }
            else
            {
                worldPos = gridManager.GetWorldPosition(objectData.gridPosition);
            }

            gridObj.transform.position = worldPos;

            gridObj.transform.rotation = Quaternion.Euler(
                gridObj.transform.rotation.eulerAngles.x,
                objectData.rotationIndex * 90,
                0
            );
            grid.rotationIndex = objectData.rotationIndex;

            // If it's a custom shape, try to load it
            if (objectData.useCustomShape && !string.IsNullOrEmpty(objectData.customShapeName))
            {
                grid.LoadShape(objectData.customShapeName);
            }

            // Get the cells this object would occupy
            List<Vector2Int> occupiedCells = grid.GetOccupiedCells(objectData.gridPosition);

            // Place on grid - pass isPreplaced = true to indicate this is a preplaced object
            gridManager.PlaceObject(gridObj, occupiedCells, true);

            // Update the grid object's record
            grid.UpdateCurrentGridPositions(objectData.gridPosition);
        }

        // Log the result for debugging
        int playerObjCount = gridManager.GetPlayerPlacedObjectCount();
        int preplacedCount = preplacedObjects.Count;
        Debug.Log(
            $"Placed {preplacedCount} preplaced objects - Player objects: {playerObjCount} - FirstObjectPlaced: {gridManager.FirstObjectPlaced}");
    }


#if UNITY_EDITOR
    /// <summary>
    /// Adds a new preoccupying object to the list
    /// </summary>
    public void AddPreplacedObject(GameObject prefab, Vector2Int gridPosition, int rotationIndex, 
                                  bool isMovable, bool isDestructible, 
                                  bool useCustomShape = false, string customShapeName = "")
    {
        PreplacedGridObjectData newObject = new PreplacedGridObjectData
        {
            prefab = prefab,
            gridPosition = gridPosition,
            rotationIndex = rotationIndex,
            isMovable = isMovable,
            isDestructible = isDestructible,
            useCustomShape = useCustomShape,
            customShapeName = customShapeName
        };

        preplacedObjects.Add(newObject);
        EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// Previews where a preplaced object would be placed
    /// </summary>
    public void PreviewPreplacedObject(GameObject prefab, Vector2Int gridPosition, int rotationIndex)
    {
        if (gridManager == null || prefab == null) return;

        // Create a temporary grid object to get its shape
        GameObject tempObj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        GridObject grid = tempObj.GetComponent<GridObject>();
        
        if (grid == null)
        {
            DestroyImmediate(tempObj);
            return;
        }

        // Set rotation
        grid.rotationIndex = rotationIndex;
        
        // Get the cells this object would occupy
        List<Vector2Int> occupiedCells = grid.GetOccupiedCells(gridPosition);
        
        // Preview in the grid
        gridManager.UpdateCellsVisual(occupiedCells, true);
        
        // Destroy the temporary object
        DestroyImmediate(tempObj);
    }

    /// <summary>
    /// Removes a preplaced object at the specified index
    /// </summary>
    public void RemovePreplacedObject(int index)
    {
        if (index >= 0 && index < preplacedObjects.Count)
        {
            preplacedObjects.RemoveAt(index);
            EditorUtility.SetDirty(this);
        }
    }

    /// <summary>
    /// Clears all preplaced objects
    /// </summary>
    public void ClearPreplacedObjects()
    {
        preplacedObjects.Clear();
        EditorUtility.SetDirty(this);
    }
#endif
}

/// <summary>
/// Data for a preplaced grid object
/// </summary>
[System.Serializable]
public class PreplacedGridObjectData
{
    [Tooltip("Prefab to instantiate")] public GameObject prefab;

    [Tooltip("Position on the grid")] public Vector2Int gridPosition;

    [Tooltip("Rotation index (0-3, multiplied by 90 degrees)")] [Range(0, 3)]
    public int rotationIndex;

    [Tooltip("Can this object be moved after placement?")]
    public bool isMovable = false;

    [Tooltip("Can this object be destroyed?")]
    public bool isDestructible = true;

    [Tooltip("Use a custom shape instead of the prefab's default")]
    public bool useCustomShape = false;

    [Tooltip("Name of the custom shape to load")]
    public string customShapeName = "";
}
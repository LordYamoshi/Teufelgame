using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int gridPosition { get; private set; }
    public Vector3 worldPosition { get; private set; }
    public bool isPlaceable { get; private set; } = true;
    public GameObject occupyingObject { get; private set; }
    public GameObject cellVisual { get; private set; }


    /// <summary>
    /// Create a new GridCell
    /// </summary>
    /// <param name="gridPos">gets the position in the grid</param>
    /// <param name="worldPos">gets the world position to place the grid in the world</param>
    /// <param name="cellVisualPrefab">the prefab for visualizing the cell</param>
    /// <param name="parent">to get the parent object of this cell</param>
    public GridCell(Vector2Int gridPos, Vector3 worldPos, GameObject cellVisualPrefab, Transform parent)
    {
        gridPosition = gridPos;
        worldPosition = worldPos;
        
        if (cellVisualPrefab != null)
        {
            cellVisual = Instantiate(cellVisualPrefab, worldPos, Quaternion.identity, parent);
            cellVisual.name = $"Cell {gridPos.x}, {gridPos.y}";
        }
    }
    
    /// <summary>
    /// Sets the object to occupy the cell
    /// </summary>
    /// <param name="obj">Object that's occupying the cell</param>
    public void SetOccupyingObject(GameObject obj)
    {
        occupyingObject = obj;
        UpdateVisual();
    
        // Debug verification
        if (obj != null)
        {
            Debug.Log($"Cell {gridPosition} is now occupied by {obj.name}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Returns true if something is occupying other wise it's false</returns>
    public bool ClearOccupyingObject(GameObject obj)
    {
        if (occupyingObject == obj)
        {
            occupyingObject = null;
            UpdateVisual();
            return true;
        }
        return false;
    }



    /// <summary>
    /// Updates the visual of the cell based on the current state
    /// </summary>
    /// <param name="isBeingHovered"></param>
    /// <param name="isValid"></param>
    public void UpdateVisual(bool isBeingHovered = false, bool isValid = true)
    {
        if (cellVisual == null) return;

        Renderer renderer = cellVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color cellColor = Color.white;

            if (occupyingObject != null)
            {
                cellColor = Color.grey;
            }
            else if (isBeingHovered)
            {
                cellColor = isValid ? Color.green : Color.red;
            }
            else if (isPlaceable)
            {
                cellColor = Color.cyan;
            }
            else
            {
                cellColor = Color.black;
            }

            renderer.material.color = cellColor;
        }
    }

    /// <summary>
    /// Sets whether this cell is placable or not   
    /// </summary>
    /// <param name="placable"></param>
    public void SetPlaceable(bool placeable)
    {
        isPlaceable = placeable;
        UpdateVisual();
        
        //Hide non placable cells
        if(cellVisual != null)
        {
            cellVisual.SetActive(placeable);
        }
    }
    
}

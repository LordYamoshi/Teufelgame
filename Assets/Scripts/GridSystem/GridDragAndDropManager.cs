using System.Collections.Generic;
using UnityEngine;

public class GridDragAndDropManager : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GridManager gridManager;
    
    [Header("Materials")]
    public Material highlightMaterial;
    public Material validPositionMaterial;
    public Material invalidPositionMaterial;
    
    // Currently selected object
    private GameObject selectedObject;
    private GridObject selectedGridObject;
    
    // Preview object during dragging
    private GameObject previewObject;
    
    // Dragging state
    private bool isDragging = false;
    private Vector2Int originalGridPosition;
    private List<Vector2Int> originalOccupiedCells = new List<Vector2Int>();
    
        private void Update()
    {
        if (isDragging)
        {
            UpdateDraggingPosition();
            if (Input.GetMouseButtonUp(0))
            {
                FinalizePlacement();
            }
        }
        else
        {
            TrySelectObject();
        }
    }
    
    /// <summary>
    /// Tries to select an object from the grid
    /// </summary>
    private void TrySelectObject()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            int layerMask = LayerMask.GetMask("Objects");
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                // Try to get the object and its grid properties
                selectedObject = hit.collider.gameObject;
                selectedGridObject = selectedObject.GetComponent<GridObject>();
                
                if (selectedGridObject != null)
                {
                    // Get the current position in the grid
                    originalGridPosition = gridManager.GetGridPosition(selectedObject.transform.position);
                    
                    // Store the cells this object currently occupies
                    originalOccupiedCells = selectedGridObject.GetCurrentGridPositions();
                    
                    StartDragging();
                }
                else
                {
                    selectedObject = null;
                }
            }
        }
    }
    
    /// <summary>
    /// Starts the dragging process
    /// </summary>
    private void StartDragging()
    {
        isDragging = true;
        
        // Highlight the selected object
        selectedGridObject.SetAllMaterials(highlightMaterial);
        
        // Create a preview object
        previewObject = GameObject.Instantiate(selectedObject);
        previewObject.SetActive(true);
        
        // Configure the preview object
        GridObject previewGridObject = previewObject.GetComponent<GridObject>();
        previewGridObject.SetAllMaterials(validPositionMaterial);
        
        // Disable colliders on the preview
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Remove the original object from the grid
        gridManager.RemoveObject(selectedObject, originalOccupiedCells);
        
        // Reset cell visuals
        gridManager.ResetAllCellVisuals();
    }
    
    /// <summary>
    /// Updates the position and visual feedback during dragging
    /// </summary>
    private void UpdateDraggingPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, gridManager.gridOrigin.y, 0));
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            Vector2Int gridPosition = gridManager.GetGridPosition(worldPoint);
            
            // Get the cells this object would occupy at the current position
            List<Vector2Int> occupiedCells = selectedGridObject.GetOccupiedCells(gridPosition);
            
            // Check if the placement is valid
            bool isValidPlacement = gridManager.IsValidPlacement(occupiedCells);
            
            // Update the preview object's position and material
            GridObject previewGridObject = previewObject.GetComponent<GridObject>();
            previewGridObject.SetAllMaterials(isValidPlacement ? validPositionMaterial : invalidPositionMaterial);
            
            // Calculate the center position for the preview
            Vector3 centerPosition = gridManager.GetWorldPosition(gridPosition);
            Vector3 offset = selectedGridObject.GetCenterOffset(gridManager.cellSize);
            previewObject.transform.position = centerPosition + offset;
            
            // Visualize the affected cells
            gridManager.ResetAllCellVisuals();
            gridManager.UpdateCellsVisual(occupiedCells, isValidPlacement);
        }
    }
    
    /// <summary>
    /// Finalizes the placement of the dragged object
    /// </summary>
    private void FinalizePlacement()
    {
        // Get the final grid position
        Vector2Int gridPosition = gridManager.GetGridPosition(previewObject.transform.position);
    
        // Get the cells this object would occupy
        List<Vector2Int> occupiedCells = selectedGridObject.GetOccupiedCells(gridPosition);
    
        bool wasPlaced = false;
    
        // Try to place the object at the new position
        if (gridManager.IsValidPlacement(occupiedCells))
        {
            // Place at new position
            gridManager.PlaceObject(selectedObject, occupiedCells);
        
            // Update the object's position in the world
            Vector3 centerPosition = gridManager.GetWorldPosition(gridPosition);
            Vector3 offset = selectedGridObject.GetCenterOffset(gridManager.cellSize);
            selectedObject.transform.position = centerPosition + offset;
        
            // Update the object's record of occupied cells
            selectedGridObject.UpdateCurrentGridPositions(gridPosition);
        
            wasPlaced = true;
            Debug.Log($"Object placed at {gridPosition}, occupying {occupiedCells.Count} cells");
        }
        else
        {
            Debug.LogWarning($"Invalid placement at {gridPosition}!");
        }
    
        // If placement failed, return to original position
        if (!wasPlaced)
        {
            gridManager.PlaceObject(selectedObject, originalOccupiedCells);
            selectedGridObject.UpdateCurrentGridPositions(originalGridPosition);
            Debug.Log($"Object returned to original position at {originalGridPosition}");
        }
        // Restore original materials
        selectedGridObject.RestoreOriginalMaterials();
        
        // Clean up the preview object and reset state
        Destroy(previewObject);
        gridManager.ResetAllCellVisuals();
        
        isDragging = false;
        selectedObject = null;
        selectedGridObject = null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Manages the drag and drop interaction with grid objects
/// </summary>
[RequireComponent(typeof(GridManager))]
public class GridDragAndDropManager : MonoBehaviour
{
    #region Properties and Fields
    [Header("References")]
    [Tooltip("Camera used for raycasting")]
    public Camera mainCamera;
    
    [Tooltip("Reference to the grid manager")]
    public GridManager gridManager;
    
    [Header("Materials")]
    [Tooltip("Material to use when an object is selected")]
    public Material highlightMaterial;
    
    [Tooltip("Material to use when placement is valid")]
    public Material validPositionMaterial;
    
    [Tooltip("Material to use when placement is invalid")]
    public Material invalidPositionMaterial;
    
    [Header("Placement Settings")]
    [Tooltip("Enable rotation during placement")]
    public bool enableRotation = true;
    
    [Tooltip("Key for rotating objects during placement")]
    public KeyCode rotateKey = KeyCode.R;
    
    [Tooltip("Key for placement confirmation")]
    public KeyCode confirmPlacementKey = KeyCode.Space;
    
    [Tooltip("Key for canceling placement")]
    public KeyCode cancelPlacementKey = KeyCode.Escape;
    
    [Tooltip("Key for destroying selected objects")]
    public KeyCode destroyKey = KeyCode.Delete;
    
    [Header("UI Feedback")]
    [Tooltip("Text element for showing placement status")]
    public TMP_Text placementStatusText;
    
    [Tooltip("UI container for placement options")]
    public GameObject placementUI;

    [Header("Visual Effects")]
    [Tooltip("Particle effect for successful placement")]
    public GameObject placementSuccessEffect;

    public AudioClip destroyAudioClip;

    [Tooltip("Audio clip for successful placement")]
    public AudioClip placementSuccessSound;
    
    [Tooltip("Audio clip for invalid placement")]
    public AudioClip placementErrorSound;
    
    [Tooltip("Height offset for placed objects")]
    public float objectHeightOffset = 0.1f;
    
    [Header("Events")]
    public UnityEvent OnBuildingPlaced;
    
    [Header("Rotation Offset Correction")]
    [Tooltip("Enable manual offset correction for specific rotations")]
    public bool enableRotationOffsetCorrection = true;

    [Tooltip("X offset for rotation 0 (0 degrees)")]
    public float rotation0OffsetX = 0f;
    [Tooltip("Z offset for rotation 0 (0 degrees)")]
    public float rotation0OffsetZ = 0f;

    [Tooltip("X offset for rotation 1 (90 degrees)")]
    public float rotation1OffsetX = -1f;
    [Tooltip("Z offset for rotation 1 (90 degrees)")]
    public float rotation1OffsetZ = 0f;

    [Tooltip("X offset for rotation 2 (180 degrees)")]
    public float rotation2OffsetX = 0f;
    [Tooltip("Z offset for rotation 2 (180 degrees)")]
    public float rotation2OffsetZ = 0f;

    [Tooltip("X offset for rotation 3 (270 degrees)")]
    public float rotation3OffsetX = 0f;
    [Tooltip("Z offset for rotation 3 (270 degrees)")]
    public float rotation3OffsetZ = 0f;
    
    private GameObject _selectedObject;
    private GridObject _selectedGridObject;
    private GameObject _previewObject;
    private GridObject _previewGridObject;
    private GameObject _currentlySelectedObject;
    private GameObject _placementObject;
    
    private bool _isDragging = false;
    private Vector2Int _originalGridPosition;
    private List<Vector2Int> _originalOccupiedCells = new List<Vector2Int>();

    private bool _isInPlacementMode = false;
    private bool _isValidPlacement = false;
    
    private int[,] _originalLayout;
    private Vector2Int _originalPivot;
    private int _originalRotation;
    
    private AudioSource _audioSource;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        // Auto-reference grid manager if not set
        if (gridManager == null)
        {
            gridManager = GetComponent<GridManager>();
        }
        
        // Setup audio source for feedback
        SetupAudioSource();
    }
    
    private void Start()
    {
        // Initialize UI
        InitializeUI();
        
        // Make sure we have a camera reference
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    private void Update()
    {
        if (_isDragging)
        {
            HandleDraggingUpdate();
        }
        else if (_isInPlacementMode)
        {
            HandlePlacementModeUpdate();
        }
        else
        {
            // Handle object selection and destruction
            TrySelectObject();
            
            if (_currentlySelectedObject != null && Input.GetKeyDown(destroyKey))
            {
                TryDestroySelectedObject();
            }
        }
    }
    #endregion
    
    #region Initialization
    private void SetupAudioSource()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;  // 2D sound
    }
    
    private void InitializeUI()
    {
        if (placementUI != null)
        {
            placementUI.SetActive(false);
        }
        
        if (placementStatusText != null)
        {
            placementStatusText.text = "";
        }
    }
    #endregion
    
    #region Drag and Drop Handling
    private void HandleDraggingUpdate()
    {
        // Update dragging position
        UpdateDraggingPosition();
        
        // Handle rotation during dragging
        if (enableRotation && Input.GetKeyDown(rotateKey))
        {
            RotatePreview();
        }
        
        // Handle placement confirmation or cancellation
        if (Input.GetMouseButtonUp(0) || Input.GetKeyDown(confirmPlacementKey))
        {
            FinalizePlacement();
        }
        else if (Input.GetKeyDown(cancelPlacementKey))
        {
            CancelPlacement();
        }
    }
    
    public void StartDraggingExistingObject(GameObject existingObject)
    {
        if (existingObject == null)
        {
            Debug.LogError("Cannot start dragging null object");
            return;
        }

        // Cancel any existing placement or dragging
        CancelPlacement();
    
        // Set as the selected object
        _selectedObject = existingObject;
        _selectedGridObject = existingObject.GetComponent<GridObject>();
    
        if (_selectedGridObject == null)
        {
            Debug.LogError("Selected object does not have a GridObject component");
            return;
        }
    
        // Store original grid position and occupied cells
        _originalGridPosition = gridManager.GetGridPosition(_selectedObject.transform.position);
        _originalOccupiedCells = _selectedGridObject.GetCurrentGridPositions();
    
        // Start dragging the object
        StartDragging();
    }
    
    private void HandlePlacementModeUpdate()
    {
        // Update placement preview
        UpdatePlacementPreview();
        
        // Handle rotation during placement
        if (enableRotation && Input.GetKeyDown(rotateKey))
        {
            RotatePreview();
        }
        
        // Handle placement confirmation or cancellation
        if (Input.GetMouseButtonDown(0) && _isValidPlacement)
        {
            FinalizePlacement();
        }
        else if (Input.GetKeyDown(cancelPlacementKey))
        {
            CancelPlacement();
        }
    }
    
    /// <summary>
    /// Tries to select an object from the grid
    /// </summary>
    private void TrySelectObject()
    {
        if (!Input.GetMouseButtonDown(0)) return;
    
        // Clear current selection
        ClearCurrentSelection();
    
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        int layerMask = LayerMask.GetMask("Objects");
    
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            // Try to get the object and its grid properties
            GameObject hitObject = hit.collider.gameObject;
            GridObject gridObj = hitObject.GetComponent<GridObject>();
        
            if (gridObj != null)
            {
                // Set as currently selected
                _currentlySelectedObject = hitObject;
            
                // Always highlight selected object, even if it can't be moved
                gridObj.SetAllMaterials(highlightMaterial);
            
                // If movable, start dragging
                if (gridObj.isMovable)
                {
                    _selectedObject = hitObject;
                    _selectedGridObject = gridObj;
                
                    // Get the current position in the grid
                    _originalGridPosition = gridManager.GetGridPosition(_selectedObject.transform.position);
                
                    // Store the cells this object currently occupies
                    _originalOccupiedCells = _selectedGridObject.GetCurrentGridPositions();
                
                    StartDragging();
                }
                else
                {
                    // Show feedback for immovable object
                    ShowStatusMessage("This object cannot be moved after placement");
                
                    // Play error sound
                    PlaySound(placementErrorSound);
                
                    // Add visual shake effect to indicate it's immovable
                    StartCoroutine(ShakeObject(hitObject));
                }
            }
        }
    }
    
    private IEnumerator ShakeObject(GameObject obj)
    {
        if (obj == null) yield break;
    
        Vector3 originalPosition = obj.transform.position;
        Quaternion originalRotation = obj.transform.rotation;
        float duration = 0.5f;
        float magnitude = 0.1f;
        float elapsed = 0;
    
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude;
        
            obj.transform.position = originalPosition + new Vector3(x, 0, z);
        
            elapsed += Time.deltaTime;
            yield return null;
        }
    
        // Restore original position
        obj.transform.position = originalPosition;
        obj.transform.rotation = originalRotation;
    }
    
    private void ClearCurrentSelection()
    {
        if (_currentlySelectedObject != null)
        {
            GridObject gridObj = _currentlySelectedObject.GetComponent<GridObject>();
            if (gridObj != null)
            {
                gridObj.RestoreOriginalMaterials();
            }
            _currentlySelectedObject = null;
        }
    }
    
    private void StartDragging()
    {
        _isDragging = true;

        // Store the original layout and rotation
        _originalLayout = _selectedGridObject.GetCurrentLayout();
        _originalPivot = _selectedGridObject.GetCurrentPivot();
        _originalRotation = _selectedGridObject.rotationIndex;

        Debug.Log($"Dragging object: Original layout {_originalLayout.GetLength(0)}x{_originalLayout.GetLength(1)} with pivot at {_originalPivot}, rotation {_originalRotation}");

        // Highlight the selected object
        _selectedGridObject.SetAllMaterials(highlightMaterial);

        // Create a preview object
        _previewObject = Instantiate(_selectedObject);
        _previewObject.SetActive(true);

        // Get the preview object's GridObject component
        _previewGridObject = _previewObject.GetComponent<GridObject>();
    
        // Apply the stored layout to the preview object
        _previewGridObject.SetObjectLayout(_originalLayout, _originalPivot);
        _previewGridObject.rotationIndex = _originalRotation;
        
        // Set the material
        _previewGridObject.SetAllMaterials(validPositionMaterial);

        // Disable colliders on the preview
        DisableColliders(_previewObject);
        
        gridManager.ResetAllCellVisuals();
    
        // This ensures that the cells it previously occupied now appear as unoccupied (white)
        gridManager.RemoveObject(_selectedObject, _originalOccupiedCells);
    
        // Show placement UI
        ShowPlacementUI("Moving object: drag to place" + (enableRotation ? $", press {rotateKey} to rotate" : ""));
    }

    /// <summary>
    /// Updates the position and visual feedback during dragging
    /// </summary>
    private void UpdateDraggingPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, gridManager.gridOrigin.y + 0.1f, 0));

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);

            // Use the precise grid position calculation
            Vector2Int gridPosition = GetPreciseGridPosition(worldPoint);

            // Debug the exact mouse position
            Debug.Log($"Mouse world point: {worldPoint}, Grid position: {gridPosition}");

            // Ensure position is within grid bounds
            gridPosition = ClampPositionToGrid(gridPosition);

            // Get the cells this object would occupy at the current position
            List<Vector2Int> occupiedCells = _previewGridObject.GetOccupiedCells(gridPosition);

            // Check if the placement is valid
            _isValidPlacement = gridManager.IsValidPlacement(occupiedCells);

            // Update the preview object's material
            _previewGridObject.SetAllMaterials(_isValidPlacement ? validPositionMaterial : invalidPositionMaterial);

            // Update preview position - use precise positioning
            DirectPositioning(_previewObject, _previewGridObject, gridPosition);

            // Visualize the affected cells
            gridManager.ResetAllCellVisuals();
            gridManager.UpdateCellsVisual(occupiedCells, _isValidPlacement);

            // Update status text
            UpdatePlacementStatusText(_isValidPlacement, occupiedCells);
        }
    }

    private Vector2Int ClampPositionToGrid(Vector2Int position)
    {
        if (!gridManager.IsWithinGridBounds(position))
        {
            return gridManager.ClampToGridBounds(position);
        }
        return position;
    }

    private void DirectPositioning(GameObject obj, GridObject gridObj, Vector2Int gridPosition)
    {
        // First, apply the rotation-specific offset to the grid position if enabled
        Vector2Int offsetGridPosition = gridPosition;
        Quaternion originalRotation = obj.transform.rotation;

        if (enableRotationOffsetCorrection)
        {
            // Get the appropriate offsets based on rotation
            float offsetX = 0f;
            float offsetZ = 0f;

            switch (gridObj.rotationIndex)
            {
                case 0:
                    offsetX = rotation0OffsetX;
                    offsetZ = rotation0OffsetZ;
                    break;
                case 1:
                    offsetX = rotation1OffsetX;
                    offsetZ = rotation1OffsetZ;
                    break;
                case 2:
                    offsetX = rotation2OffsetX;
                    offsetZ = rotation2OffsetZ;
                    break;
                case 3:
                    offsetX = rotation3OffsetX;
                    offsetZ = rotation3OffsetZ;
                    break;
            }

            // Apply the offset (only if non-zero)
            if (offsetX != 0 || offsetZ != 0)
            {
                offsetGridPosition = new Vector2Int(
                    gridPosition.x + Mathf.RoundToInt(offsetX),
                    gridPosition.y + Mathf.RoundToInt(offsetZ)
                );

                Debug.Log($"Applied rotation {gridObj.rotationIndex} offset: X={offsetX}, Z={offsetZ}");
                Debug.Log($"Grid position adjusted from {gridPosition} to {offsetGridPosition}");
            }
        }

        // Get all the occupied grid positions based on the adjusted grid position
        List<Vector2Int> occupiedPositions = gridObj.GetOccupiedCells(offsetGridPosition);

        if (occupiedPositions.Count == 0) return;

        // Calculate the center position
        Vector3 worldCenter;

        // Find center of occupied cells
        Vector3 sum = Vector3.zero;
        foreach (var pos in occupiedPositions)
        {
            sum += gridManager.GetWorldPosition(pos);
        }

        worldCenter = sum / occupiedPositions.Count;

        // Use the gridManager's height offset instead of our local one
        float heightOffset = gridManager.objectHeightOffset;

        // Position the object
        obj.transform.position = new Vector3(
            worldCenter.x,
            worldCenter.y + heightOffset,
            worldCenter.z
        );
        
        obj.transform.rotation = originalRotation;
        Debug.Log($"Final position: {obj.transform.position} for {obj.name} with rotation {gridObj.rotationIndex}");
    }

    private Vector2Int GetPreciseGridPosition(Vector3 worldPoint)
    {
        // Get the raw grid position first
        Vector2Int rawGridPos = gridManager.GetGridPosition(worldPoint);
    
        // Debug the conversion process
        Debug.Log($"Raw world point: {worldPoint}, converted to grid: {rawGridPos}");
    
        // Verify the conversion by getting the world position of the grid cell
        Vector3 gridWorldPos = gridManager.GetWorldPosition(rawGridPos);
        Debug.Log($"Grid position {rawGridPos} converts to world: {gridWorldPos}");
    
        // Calculate distances to adjacent cells to find the closest one
        float cellSize = gridManager.cellSize;
    
        return rawGridPos;
    }
    
    

    private void UpdatePlacementStatusText(bool isValid, List<Vector2Int> occupiedCells)
    {
        if (placementStatusText == null) return;

        if (isValid)
        {
            placementStatusText.text = "Valid placement - release to place";
        }
        else
        {
            // Determine reason for invalid placement
            if (gridManager.requireEdgeConnectivity && !gridManager.FirstObjectPlaced && gridManager.exemptFirstObject)
            {
                // First object placement
                placementStatusText.text = "First building - can be placed anywhere on grid";
            }
            else if (gridManager.requireEdgeConnectivity && !gridManager.HasEdgeConnectivity(occupiedCells))
            {
                // Need connectivity but don't have it - specifically clarify it needs to connect to player buildings
                placementStatusText.text = "Invalid placement - must connect to another player-placed building";
            }
            else
            {
                // Other placement issues
                placementStatusText.text = "Invalid placement - cells already occupied or out of bounds";
            }
        }
    }
    
    
    /// <summary>
    /// Rotates the preview object during placement
    /// </summary>
    private void RotatePreview()
    {
        if (_previewGridObject == null) return;
    
        // Rotate the preview
        _previewGridObject.RotateClockwise();
    
        // Important: RotateClockwise already calls CalculateRelativeCellPositions,
        // so the GridObject component has already updated its internal layout
    
        // Update placement validation and visuals
        if (_isDragging)
        {
            UpdateDraggingPosition();
        }
        else if (_isInPlacementMode)
        {
            UpdatePlacementPreview();
        }
    }

    
    /// <summary>
    /// Starts placement mode for a new object
    /// </summary>
    public void StartPlacementMode(GameObject objectPrefab)
    {
        if (objectPrefab == null)
        {
            Debug.LogError("Cannot start placement mode with null prefab");
            return;
        }
        
        // Cancel any existing placement or dragging
        CancelPlacement();
        
        // Create the placement object
        _placementObject = Instantiate(objectPrefab);
        _placementObject.SetActive(true);
        
        // Get the grid object component
        _previewGridObject = _placementObject.GetComponent<GridObject>();
        if (_previewGridObject == null)
        {
            Debug.LogError("Placement object must have a GridObject component");
            Destroy(_placementObject);
            return;
        }
        
        // Set initial material
        _previewGridObject.SetAllMaterials(validPositionMaterial);
        
        // Disable colliders during placement
        DisableColliders(_placementObject);
        
        // Set placement mode
        _isInPlacementMode = true;
        _previewObject = _placementObject;
        
        // Show UI
        ShowPlacementUI("Placing new object: click to place" + (enableRotation ? $", press {rotateKey} to rotate" : ""));
    }
    
    private void DisableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }
    
    private void ShowPlacementUI(string message)
    {
        if (placementUI != null)
        {
            placementUI.SetActive(true);
        }
        
        if (placementStatusText != null)
        {
            placementStatusText.text = message;
        }
    }
    
    private void ShowStatusMessage(string message)
    {
        if (placementStatusText != null)
        {
            placementStatusText.text = message;
        }
    }
    
    /// <summary>
    /// Updates the position of the placement preview
    /// </summary>
    private void UpdatePlacementPreview()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, gridManager.gridOrigin.y, 0));
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            Vector2Int gridPosition = gridManager.GetGridPosition(worldPoint);
            
            // Ensure position is within grid bounds
            gridPosition = ClampPositionToGrid(gridPosition);
            
            // Get the cells this object would occupy at the current position
            List<Vector2Int> occupiedCells = _previewGridObject.GetOccupiedCells(gridPosition);
            
            // Check if the placement is valid
            _isValidPlacement = gridManager.IsValidPlacement(occupiedCells);
            
            // Update the preview object's material
            _previewGridObject.SetAllMaterials(_isValidPlacement ? validPositionMaterial : invalidPositionMaterial);
            
            // Calculate the center position for the preview
            Vector3 centerPosition = gridManager.GetWorldPosition(gridPosition);
            Vector3 offset = _previewGridObject.GetCenterOffset(gridManager.cellSize);
            _placementObject.transform.position = centerPosition + offset + new Vector3(0, objectHeightOffset, 0);
            
            // Visualize the affected cells
            gridManager.ResetAllCellVisuals();
            gridManager.UpdateCellsVisual(occupiedCells, _isValidPlacement);
            
            // Update status text
            UpdatePlacementStatusText(_isValidPlacement, occupiedCells);
        }
    }
    
    /// <summary>
    /// Finalizes the placement of the dragged or placed object
    /// </summary>
    private void FinalizePlacement()
    {
        if (_isDragging)
        {
            // Handle existing object placement finalization
            FinalizeExistingObjectPlacement();
        }
        else if (_isInPlacementMode)
        {
            // Handle new object placement finalization
            FinalizeNewObjectPlacement();
        }
        
        // Hide UI
        HidePlacementUI();
        
        // Reset cell visuals
        gridManager.ResetAllCellVisuals();
    }
    
    private void HidePlacementUI()
    {
        if (placementUI != null)
        {
            placementUI.SetActive(false);
        }
        
        if (placementStatusText != null)
        {
            placementStatusText.text = "";
        }
    }

    /// <summary>
    /// Finalizes placement of an existing object being moved
    /// </summary>
    private void FinalizeExistingObjectPlacement()
    {
        // Get the grid position
        Vector2Int gridPosition = gridManager.GetGridPosition(_previewObject.transform.position);

        // Get the cells this object would occupy with the current rotation
        List<Vector2Int> occupiedCells = _previewGridObject.GetOccupiedCells(gridPosition);

        bool wasPlaced = false;

        // Try to place the object at the new position
        if (_isValidPlacement)
        {
            // Apply the new rotation to the original object if changed
            if (_selectedGridObject.rotationIndex != _previewGridObject.rotationIndex)
            {
                _selectedGridObject.rotationIndex = _previewGridObject.rotationIndex;
                _selectedGridObject.CalculateRelativeCellPositions();


            }

            // Place at new position
            gridManager.PlaceObject(_selectedObject, occupiedCells);

            // Position the object using our direct positioning method
            DirectPositioning(_selectedObject, _selectedGridObject, gridPosition);

            // Update the object's record of occupied cells
            _selectedGridObject.UpdateCurrentGridPositions(gridPosition);

            // Make the object immovable after placement
            _selectedGridObject.MarkAsImmovable();

            wasPlaced = true;
            Debug.Log(
                $"Object placed at {gridPosition}, occupying {occupiedCells.Count} cells with rotation {_selectedGridObject.rotationIndex * 90}°");

            // Play success sound and effect
            PlaySound(placementSuccessSound);
            SpawnPlacementEffect(_selectedObject.transform.position);
            


            // Fire the building placed event
            OnBuildingPlaced?.Invoke();

            // Notify the BuildingIncomeGenerator that the object has been placed
            if (wasPlaced)
            {
                BuildingIncomeGenerator incomeGenerator = _selectedObject.GetComponent<BuildingIncomeGenerator>();
                if (incomeGenerator != null)
                {
                    incomeGenerator.OnPlacedOnGrid();
                }
            }
        }
        else
        {
            Debug.LogWarning($"Invalid placement at {gridPosition}!");
            // Apply visual rotation
            _selectedObject.transform.rotation = Quaternion.Euler(
                _selectedObject.transform.rotation.eulerAngles.x,
                _selectedGridObject.rotationIndex * 90,
                0
            );
            PlaySound(placementErrorSound);
        }

        // If placement failed, return to original position
        if (!wasPlaced)
        {
            gridManager.PlaceObject(_selectedObject, _originalOccupiedCells);
            _selectedGridObject.rotationIndex = _originalRotation;
            _selectedGridObject.CalculateRelativeCellPositions();
            _selectedGridObject.UpdateCurrentGridPositions(_originalGridPosition);

            // Reset rotation transform
            _selectedObject.transform.rotation =
                Quaternion.Euler(transform.rotation.eulerAngles.x, _originalRotation * 90, 0);

            // Position back at original location
            DirectPositioning(_selectedObject, _selectedGridObject, _originalGridPosition);

            Debug.Log($"Object returned to original position at {_originalGridPosition}");
        }

        // Restore original materials
        _selectedGridObject.RestoreOriginalMaterials();

        // Clean up the preview object and reset state
        Destroy(_previewObject);

        _isDragging = false;
        _selectedObject = null;
        _selectedGridObject = null;
        _previewObject = null;
        _previewGridObject = null;
        _originalLayout = null;
    }

    /// <summary>
    /// Finalizes placement of a new object
    /// </summary>
    private void FinalizeNewObjectPlacement()
    {
        // Get the final grid position
        Vector2Int gridPosition = gridManager.GetGridPosition(_placementObject.transform.position);

        // Get the cells this object would occupy
        List<Vector2Int> occupiedCells = _previewGridObject.GetOccupiedCells(gridPosition);

        if (_isValidPlacement)
        {
            // Make sure rotation is set correctly
            _placementObject.transform.rotation = Quaternion.Euler(
                _placementObject.transform.rotation.eulerAngles.x,
                _previewGridObject.rotationIndex * 90,
                0
            );

            // Enable colliders on the placed object
            Collider[] colliders = _placementObject.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = true;
            }

            // Place the object on the grid
            gridManager.PlaceObject(_placementObject, occupiedCells);

            // Position using direct positioning
            DirectPositioning(_placementObject, _previewGridObject, gridPosition);

            OnBuildingPlaced?.Invoke();

            // Update the object's record of occupied cells
            _previewGridObject.UpdateCurrentGridPositions(gridPosition);

            // Make the object immovable after placement
            _previewGridObject.isMovable = false;

            // Restore original materials
            _previewGridObject.RestoreOriginalMaterials();

            // Notify the BuildingIncomeGenerator that the object has been placed
            BuildingIncomeGenerator incomeGenerator = _placementObject.GetComponent<BuildingIncomeGenerator>();
            if (incomeGenerator != null)
            {
                incomeGenerator.OnPlacedOnGrid();
            }

            Debug.Log(
                $"New object placed at {gridPosition}, occupying {occupiedCells.Count} cells with rotation {_previewGridObject.rotationIndex * 90}°");

            // Play success sound and effect
            PlaySound(placementSuccessSound);
            SpawnPlacementEffect(_placementObject.transform.position);

            // Reset placement mode but keep the object
            _previewObject = null;
            _previewGridObject = null;
            _placementObject = null;
        }
        else
        {
            // Invalid placement, destroy the preview
            Destroy(_placementObject);
            Debug.LogWarning($"Invalid placement at {gridPosition}, canceled placement");
            // Apply visual rotation
            _selectedObject.transform.rotation = Quaternion.Euler(
                _selectedObject.transform.rotation.eulerAngles.x,
                _selectedGridObject.rotationIndex * 90,
                0
            );
            // Play error sound
            PlaySound(placementErrorSound);
        }

        _isInPlacementMode = false;
    }

    /// <summary>
    /// Cancels the current placement operation
    /// </summary>
    public void CancelPlacement()
    {
        if (_isDragging)
        {
            CancelDragging();
        }
        else if (_isInPlacementMode)
        {
            CancelPlacementMode();
        }
        
        // Reset cell visuals
        gridManager.ResetAllCellVisuals();
        
        // Hide UI
        HidePlacementUI();
    }
    
    private void CancelDragging()
    {

        // Store the original full rotation
        Quaternion originalRotation = _selectedObject.transform.rotation;

        gridManager.PlaceObject(_selectedObject, _originalOccupiedCells);
        _selectedGridObject.UpdateCurrentGridPositions(_originalGridPosition);
        _selectedGridObject.RestoreOriginalMaterials();
    
        // Explicitly restore rotation
        _selectedObject.transform.rotation = Quaternion.Euler(
            _selectedGridObject.transform.eulerAngles.x,
            _selectedGridObject.rotationIndex * 90,
            0
        );
        _selectedGridObject.CalculateRelativeCellPositions();
        
        // Clean up
        Destroy(_previewObject);
        _isDragging = false;
        _selectedObject = null;
        _selectedGridObject = null;
        _previewObject = null;
        _previewGridObject = null;

    }
    
    private void CancelPlacementMode()
    {
        // Destroy the placement preview
        Destroy(_placementObject);
        _isInPlacementMode = false;
        _placementObject = null;
        _previewObject = null;
        _previewGridObject = null;
        
    }
    
    /// <summary>
    /// Try to destroy the currently selected object
    /// </summary>
    private void TryDestroySelectedObject()
    {
        if (_currentlySelectedObject == null) return;
    
        GridObject gridObj = _currentlySelectedObject.GetComponent<GridObject>();
        if (gridObj == null) return;
    
        // First check if it's a pre-placed object
        if (!gridManager.IsPreplacedObject(_currentlySelectedObject))
        {
            // Show feedback for player-placed objects
            ShowStatusMessage("Cannot delete player-built structures");
        
            // Play error sound
            PlaySound(placementErrorSound);
        
            // Add visual shake effect to indicate it's not deletable
            StartCoroutine(ShakeObject(_currentlySelectedObject));
            return;
        }
    
        if (!gridObj.isDestructible)
        {
            // Show feedback for indestructible object
            ShowStatusMessage("This pre-placed object cannot be destroyed");
        
            // Play error sound
            PlaySound(placementErrorSound);
            return;
        }
    
        // Destroy the object
        GameObject objToDestroy = _currentlySelectedObject;
        Vector3 position = objToDestroy.transform.position;
        bool destroyed = gridManager.DestroyObject(objToDestroy);


        if (destroyed)
        {
            Debug.Log($"Pre-placed object {objToDestroy.name} destroyed");
            _currentlySelectedObject = null;
        
            // Play destruction effect/sound
            SpawnPlacementEffect(position);
            PlaySound(destroyAudioClip);
            // Nog niet perfect maar goed genoeg
            gridManager.ResetAllCellVisuals();
        }
    }
    #endregion
    
    #region Visual and Audio Feedback
    /// <summary>
    /// Plays a sound effect if available
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Spawns a visual effect at the specified position
    /// </summary>
    private void SpawnPlacementEffect(Vector3 position)
    {
        if (placementSuccessEffect != null)
        {
            GameObject effect = Instantiate(placementSuccessEffect, position, Quaternion.identity);
            Destroy(effect, 2f); // Cleanup after 2 seconds
        }
    }
    #endregion
}
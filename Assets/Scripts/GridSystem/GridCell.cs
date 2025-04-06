using UnityEngine;
using System.Collections;

/// <summary>
/// Represents a single cell in the grid system, managing its visual state and properties
/// </summary>
public class GridCell
{
    // Public properties
    public Vector2Int GridPosition { get; private set; }
    public Vector3 WorldPosition { get; private set; }
    public bool IsPlaceable { get; private set; } = true;
    public GameObject OccupyingObject { get; private set; }
    public GameObject CellVisual { get; private set; }

    // Visual state constants
    private Color _normalColor = Color.black;
    private Color _validPlacementColor = Color.green;
    private Color _invalidPlacementColor = Color.red;
    private Color _occupiedColor = Color.gray;
    private Color _highlightColor = new Color(0.2f, 0.6f, 1f); // Blue highlight
    
    private const float DEFAULT_EFFECT_DURATION = 0.3f;
    
    // Visual components
    private Material _cellMaterial;
    private Coroutine _effectCoroutine;
    private MonoBehaviour _coroutineRunner;

    // Current visual state tracking
    private bool _isHighlighted = false;
    private Color _currentColor;

    /// <summary>
    /// Create a new GridCell with the specified properties
    /// </summary>
    public GridCell(Vector2Int gridPos, Vector3 worldPos, GameObject cellVisualPrefab, Transform parent)
    {
        GridPosition = gridPos;
        WorldPosition = worldPos;
        
        if (cellVisualPrefab != null)
        {
            CellVisual = GameObject.Instantiate(cellVisualPrefab, worldPos, Quaternion.identity, parent);
            CellVisual.name = $"Cell {gridPos.x}, {gridPos.y}";
            
            // Initialize the cell material
            InitializeMaterial();
        }
        
        // Set initial color
        _currentColor = _normalColor;
    }
    
    private void InitializeMaterial()
    {
        Renderer renderer = CellVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a unique material instance to avoid affecting other cells
            _cellMaterial = new Material(renderer.sharedMaterial);
            renderer.material = _cellMaterial;
            
            // Apply initial color
            _cellMaterial.color = _normalColor;
        }
    }
    
    /// <summary>
    /// Sets the object occupying this cell and updates visual state
    /// </summary>
    public void SetOccupyingObject(GameObject obj)
    {
        // Clean up existing material effects if any
        if (_effectCoroutine != null && _coroutineRunner != null)
        {
            _coroutineRunner.StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }
        
        OccupyingObject = obj;
        
        // Create visual feedback
        if (obj != null && CellVisual != null)
        {
            PulseEffect(_normalColor, _occupiedColor, DEFAULT_EFFECT_DURATION, false);
            _currentColor = _occupiedColor;
        }
        
        UpdateVisual();
    
        // Log for debugging
        if (obj != null)
        {
            Debug.Log($"Cell {GridPosition} is now occupied by {obj.name}");
        }
    }

    /// <summary>
    /// Clears the occupying object if it matches the provided object
    /// </summary>
    public bool ClearOccupyingObject(GameObject obj)
    {
        if (OccupyingObject == obj)
        {
            // Clean up existing material effects if any
            if (_effectCoroutine != null && _coroutineRunner != null)
            {
                _coroutineRunner.StopCoroutine(_effectCoroutine);
                _effectCoroutine = null;
            }
            
            // Visual effect for clearing
            if (CellVisual != null)
            {
                PulseEffect(_occupiedColor, _normalColor, DEFAULT_EFFECT_DURATION, false);
            }
            
            OccupyingObject = null;
            _currentColor = _normalColor;
            UpdateVisual();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates the cell's visual appearance based on its current state
    /// The isDragging parameter is critical for proper material handling during drag operations
    /// </summary>
    public void UpdateVisual(bool isBeingHovered = false, bool isValid = true, bool isDragging = false)
    {
        if (CellVisual == null) return;

        Renderer renderer = CellVisual.GetComponent<Renderer>();
        if (renderer == null) return;

        // Clean up existing material pulse effects if switching states
        if (_effectCoroutine != null && _coroutineRunner != null && 
            ((_isHighlighted && !isBeingHovered) || isDragging))
        {
            _coroutineRunner.StopCoroutine(_effectCoroutine);
            _effectCoroutine = null;
        }

        Color cellColor;

        // Order of priority for visual state:
        if (OccupyingObject != null)
        {
            // Rule 1: If cell is occupied AND being dragged over, show placement color
            if (isDragging && isBeingHovered)
            {
                cellColor = isValid ? _validPlacementColor : _invalidPlacementColor;
            }
            // Rule 2: Otherwise, occupied cells are always gray
            else
            {
                cellColor = _occupiedColor;
            }
            CellVisual.SetActive(true);
        }
        else if (isBeingHovered)
        {
            // Rule 3: Empty cells being hovered during placement
            cellColor = isValid ? _validPlacementColor : _invalidPlacementColor;
            CellVisual.SetActive(true);
            
            // Add pulsing effect for hover
            if (_effectCoroutine == null && !isDragging)
            {
                Color targetColor = Color.Lerp(cellColor, Color.white, 0.3f);
                PulseEffect(cellColor, targetColor, 0.5f, true);
            }
        }
        else if (IsPlaceable)
        {
            // Rule 4: Normal placeable cells
            cellColor = _normalColor;
            CellVisual.SetActive(true);
        }
        else
        {
            // Rule 5: Non-placeable cells
            cellColor = Color.clear;
            CellVisual.SetActive(false);
            return;
        }
            
        // Apply the color if no effect is running
        if (_effectCoroutine == null && renderer.material != null)
        {
            renderer.material.color = cellColor;
            _currentColor = cellColor;
        }
        
        // Update highlighting state
        _isHighlighted = isBeingHovered;
    }

    /// <summary>
    /// Sets whether this cell is placeable
    /// </summary>
    public void SetPlaceable(bool placeable)
    {
        IsPlaceable = placeable;
        UpdateVisual();
        
        // Hide non-placeable cells
        if (CellVisual != null && !placeable && OccupyingObject == null)
        {
            CellVisual.SetActive(false);
        }
    }
    
    /// <summary>
    /// Set custom colors for different cell states
    /// </summary>
    public void SetVisualizationColors(Color normal, Color valid, Color invalid, Color occupied)
    {
        _normalColor = normal;
        _validPlacementColor = valid;
        _invalidPlacementColor = invalid;
        _occupiedColor = occupied;
        
        // Update the visual to apply new colors
        UpdateVisual();
    }
    
    /// <summary>
    /// Creates a pulsing effect between colors
    /// </summary>
    public void PulseEffect(Color startColor, Color endColor, float duration, bool loop)
    {
        if (CellVisual == null) return;
        
        // Find a MonoBehaviour to run the coroutine
        if (_coroutineRunner == null)
        {
            _coroutineRunner = FindCoroutineRunner();
            if (_coroutineRunner == null) return;
        }
        
        // Stop any existing pulse
        if (_effectCoroutine != null)
        {
            _coroutineRunner.StopCoroutine(_effectCoroutine);
        }
        
        // Start a new pulse
        _effectCoroutine = _coroutineRunner.StartCoroutine(PulseCoroutine(startColor, endColor, duration, loop));
    }
    
    /// <summary>
    /// Highlights this cell with a specific color
    /// </summary>
    public void Highlight(Color color, float duration = 0f)
    {
        if (CellVisual == null) return;
        
        Renderer renderer = CellVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Store original color
            Color originalColor = _currentColor;
            
            // Apply highlight color
            renderer.material.color = color;
            CellVisual.SetActive(true);
            
            if (duration > 0)
            {
                PulseEffect(color, originalColor, duration, false);
            }
        }
    }
    
    /// <summary>
    /// Set the cell's visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (CellVisual != null)
        {
            CellVisual.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Coroutine to handle color pulsing
    /// </summary>
    private IEnumerator PulseCoroutine(Color startColor, Color endColor, float duration, bool loop)
    {
        if (CellVisual == null)
        {
            _effectCoroutine = null;
            yield break;
        }
        
        Renderer renderer = CellVisual.GetComponent<Renderer>();
        if (renderer == null || renderer.material == null)
        {
            _effectCoroutine = null;
            yield break;
        }
        
        float startTime = Time.time;
        bool reverse = false;
        
        while (true)
        {
            float elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Use a sine wave for smoother pulsing if looping
            if (loop)
            {
                t = (Mathf.Sin(t * Mathf.PI) + 1) / 2;
            }
            
            // Apply the color
            Color newColor = Color.Lerp(
                reverse ? endColor : startColor, 
                reverse ? startColor : endColor, 
                t);
                
            renderer.material.color = newColor;
            
            // Check if we've completed a cycle
            if (t >= 1.0f)
            {
                if (loop)
                {
                    // Reset for next loop, reversing direction
                    startTime = Time.time;
                    reverse = !reverse;
                }
                else
                {
                    // Store final color
                    _currentColor = endColor;
                    break;
                }
            }
            
            yield return null;
        }
        
        // Ensure we end with the correct color
        renderer.material.color = endColor;
        _effectCoroutine = null;
    }
    
    /// <summary>
    /// Finds a MonoBehaviour to use for running coroutines
    /// </summary>
    private MonoBehaviour FindCoroutineRunner()
    {
        // First try to find GridManager
        GridManager gridManager = GameObject.FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            return gridManager;
        }
        
        // Fall back to camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Get an existing MonoBehaviour or add one
            MonoBehaviour existingBehaviour = mainCamera.GetComponent<MonoBehaviour>();
            if (existingBehaviour != null)
            {
                return existingBehaviour;
            }
        }
        
        return null;
    }
}
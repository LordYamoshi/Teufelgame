using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component that allows buildings to generate income over time.
/// Attach this to buildings that should generate money.
/// </summary>
[RequireComponent(typeof(GridObject))]
public class BuildingIncomeGenerator : MonoBehaviour
{
    [Header("Income Settings")]
    [Tooltip("Whether this building can generate income")]
    public bool canGenerateIncome = true;
    
    [Tooltip("Amount of currency generated each interval")]
    public int incomeAmount = 10;
    
    [Tooltip("Time in seconds between income generation")]
    public float incomeInterval = 15f;
    
    [Tooltip("Visual effect prefab to show when income is generated")]
    public GameObject incomeEffectPrefab;
    
    [Header("Income Events")]
    [Tooltip("Event triggered when income is generated")]
    public UnityEvent<int> OnIncomeGenerated;
    
    private bool _isGeneratingIncome = false;
    private float _nextIncomeTime = 0f;
    private BuildingShopManager _shopManager;
    private GridObject _gridObject;
    private bool _isPlayerPlaced = false;
    
    void Awake()
    {
        _gridObject = GetComponent<GridObject>();
    }
    
    private bool _hasBeenPlacedOnGrid = false;
    
   void Start()
{
    // Find the shop manager if not set
    if (_shopManager == null)
    {
        _shopManager = FindObjectOfType<BuildingShopManager>();
    }
    
    // Check if this is a player-placed object and not a pre-placed object
    GridManager gridManager = FindObjectOfType<GridManager>();
    if (gridManager != null)
    {
        _isPlayerPlaced = !gridManager.IsPreplacedObject(gameObject);
        
        // Check if this object has been properly placed on the grid
        if (_isPlayerPlaced)
        {
            // If it exists in the placed objects dictionary, it's been placed
            Dictionary<GameObject, List<Vector2Int>> placedObjects = gridManager.GetAllPlacedObjects();
            _hasBeenPlacedOnGrid = placedObjects.ContainsKey(gameObject);
        }
    }
    
    // Only start generating income if it's a player-placed object, properly placed on grid, and can generate income
    if (_isPlayerPlaced && _hasBeenPlacedOnGrid && canGenerateIncome)
    {
        StartIncomeGeneration();
        Debug.Log($"Building {gameObject.name} started generating income after being placed on grid");
    }
    else
    {
        string reason = !_isPlayerPlaced ? "Not player-placed" : 
                       !_hasBeenPlacedOnGrid ? "Not placed on grid yet" : 
                       "Cannot generate income";
        
        Debug.Log($"Building {gameObject.name} will NOT generate income: {reason}");
    }
}
    
    void Update()
    {
        // Check if it's time to generate income
        if (_isGeneratingIncome && Time.time >= _nextIncomeTime)
        {
            GenerateIncome();
            _nextIncomeTime = Time.time + incomeInterval;
        }
    }
    
    /// <summary>
    /// Start the income generation cycle
    /// </summary>
    /// <summary>
    /// Notifies the income generator that the building has been successfully placed on the grid
    /// </summary>
    public void OnPlacedOnGrid()
    {
        _hasBeenPlacedOnGrid = true;
        
        // If other conditions are met, start generating income
        if (_isPlayerPlaced && canGenerateIncome && !_isGeneratingIncome)
        {
            StartIncomeGeneration();
        }
    }

    public void StartIncomeGeneration()
    {
        // Only generate income if the building can generate income AND has been placed
        if (!canGenerateIncome || !_hasBeenPlacedOnGrid) 
        {
            Debug.Log($"Cannot start income generation for {gameObject.name}: " +
                     $"Can generate = {canGenerateIncome}, Placed on grid = {_hasBeenPlacedOnGrid}");
            return;
        }
        
        _isGeneratingIncome = true;
        _nextIncomeTime = Time.time + incomeInterval;
        
        Debug.Log($"Building {gameObject.name} started generating income: {incomeAmount} every {incomeInterval} seconds");
    }
    
    /// <summary>
    /// Stop the income generation cycle
    /// </summary>
    public void StopIncomeGeneration()
    {
        _isGeneratingIncome = false;
        Debug.Log($"Building {gameObject.name} stopped generating income");
    }
    
    /// <summary>
    /// Toggle income generation
    /// </summary>
    public void ToggleIncomeGeneration()
    {
        if (_isGeneratingIncome)
            StopIncomeGeneration();
        else
            StartIncomeGeneration();
    }
    
    /// <summary>
    /// Generate income and add it to the shop manager
    /// </summary>
    private void GenerateIncome()
    {
        if (_shopManager != null)
        {
            _shopManager.AddCurrency(incomeAmount);
            
            Debug.Log($"Building {gameObject.name} generated {incomeAmount} currency");
            
            // Spawn income effect if available
            if (incomeEffectPrefab != null)
            {
                GameObject effect = Instantiate(incomeEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
                Destroy(effect, 2f); // Clean up after 2 seconds
            }
            
            // Trigger event
            OnIncomeGenerated?.Invoke(incomeAmount);
        }
        else
        {
            Debug.LogWarning("Cannot generate income: No BuildingShopManager found");
        }
    }
    
    /// <summary>
    /// Manually trigger income generation (useful for testing or special events)
    /// </summary>
    public void TriggerIncomeGeneration()
    {
        if (canGenerateIncome)
        {
            GenerateIncome();
        }
    }
    
    /// <summary>
    /// Set the income amount
    /// </summary>
    public void SetIncomeAmount(int amount)
    {
        incomeAmount = amount;
    }
    
    /// <summary>
    /// Set the income interval
    /// </summary>
    public void SetIncomeInterval(float interval)
    {
        incomeInterval = interval;
        
        // Reset the next income time
        if (_isGeneratingIncome)
        {
            _nextIncomeTime = Time.time + interval;
        }
    }
}
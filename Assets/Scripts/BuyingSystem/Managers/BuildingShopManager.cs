using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class BuildingShopManager : MonoBehaviour
{
    [System.Serializable]
    public class BuildingOption
    {
        public GameObject buildingPrefab;
        public string displayName;
        public int cost;
        public Sprite previewImage;
        [TextArea] public string description;
    
        [Header("Income Generation")]
        [Tooltip("Whether this building can generate income")]
        public bool canGenerateIncome = false;
    
        [Tooltip("Amount of currency generated each interval")]
        public int incomeAmount = 10;
    
        [Tooltip("Time in seconds between income generation")]
        public float incomeInterval = 15f;
    }

    public List<BuildingOption> availableBuildings = new List<BuildingOption>();
    public bool randomizeBuildings = false;
    public bool loopBuildings = true;
    public int startingCurrency = 100;

    public TMP_Text currencyText;
    public TMP_Text buildingNameText;
    public TMP_Text buildingCostText;
    public TMP_Text buildingDescriptionText;
    public Image buildingPreviewImage;
    public Button buyButton;
    public TMP_Text buyButtonText;
    public Transform buildingSpawnPoint;
    public GridDragAndDropManager gridDragAndDropManager;
    public Material placementMaterial;
    
    public UnityEvent<int> OnCurrencyChanged;
    public UnityEvent<BuildingOption> OnBuildingPurchased;
    public UnityEvent<BuildingOption> OnNextBuildingChanged;
    public UnityEvent<bool> OnCanAffordChanged;

    [HideInInspector] public BuildingOption _currentDisplayedBuilding = null;
    [HideInInspector] public List<int> _buildingSequence = new List<int>();
    [HideInInspector] public int _currentBuildingIndex = 0;
    [HideInInspector] public GameObject _currentPlacementObject = null;
    
    private int _currentCurrency;
    private bool _isInitialized = false;
    private bool _processingPurchase = false;


    public int CurrentCurrency
    {
        get { return _currentCurrency; }
        private set
        {
            int previous = _currentCurrency;
            _currentCurrency = value;

            if (previous != _currentCurrency)
            {
                UpdateCurrencyUI();
                OnCurrencyChanged?.Invoke(_currentCurrency);

                // Check if we can afford the current building
                CheckCanAfford();
            }
        }
    }

    private void Awake()
    {
        // Initialize the shop
        InitializeShop();
    }

    private void Start()
    {
        // Set initial currency
        CurrentCurrency = startingCurrency;
        
        // Setup UI
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(PurchaseCurrentBuilding);
        }
        
        // Initialize the grid manager reference if not set
        if (gridDragAndDropManager == null)
        {
            gridDragAndDropManager = FindObjectOfType<GridDragAndDropManager>();
        }
        
        // Make sure we're initialized before showing
        if (!_isInitialized)
        {
            InitializeShop();
        }
        
        // Show the first building
        ShowNextBuilding();
        
        Debug.Log($"Shop initialized with {availableBuildings.Count} buildings:");
        for (int i = 0; i < availableBuildings.Count; i++)
        {
            Debug.Log($"Building {i}: {availableBuildings[i].displayName}");
        }
    }
    
    
    private void InitializeShop()
    {
        if (_isInitialized) return;
        
        // Generate the building sequence
        GenerateBuildingSequence();
        
        _isInitialized = true;
    }

    private void GenerateBuildingSequence()
    {
        _buildingSequence.Clear();
        
        // Add all building indices
        for (int i = 0; i < availableBuildings.Count; i++)
        {
            _buildingSequence.Add(i);
        }
        
        // Randomize if needed - FIX: Properly assign the result of OrderBy back to _buildingSequence
        if (randomizeBuildings)
        {
            _buildingSequence = _buildingSequence.OrderBy(x => Random.value).ToList();
        }
        
        // Reset the index
        _currentBuildingIndex = 0;
        
        // Log the sequence for debugging
        string sequence = "Building sequence: ";
        foreach (var idx in _buildingSequence)
        {
            sequence += $"{idx}:{availableBuildings[idx].displayName}, ";
        }
        Debug.Log(sequence);
    }
    

    public void ShowNextBuilding()
    {
        if (availableBuildings.Count == 0)
        {
            Debug.LogWarning("No buildings available in the shop!");
            DisableBuyButton("No Buildings!");
            return;
        }
        
        // Check if we need to regenerate or loop the sequence
        if (_currentBuildingIndex >= _buildingSequence.Count)
        {
            if (!loopBuildings)
            {
                DisableBuyButton("Sold Out!");
                return;
            }
            
            // Reset to the beginning or regenerate if randomized
            if (randomizeBuildings)
            {
                GenerateBuildingSequence();
            }
            else
            {
                _currentBuildingIndex = 0;
            }
        }
        
        // Get the exact index from our sequence
        int buildingIndex = _buildingSequence[_currentBuildingIndex];
        
        // Get the building from the available buildings using that index
        _currentDisplayedBuilding = availableBuildings[buildingIndex];
        
        // Log for debugging
        Debug.Log($"Showing building: {_currentDisplayedBuilding.displayName} at sequence index {_currentBuildingIndex} (building index {buildingIndex})");
        
        // Update UI using null-conditional operators
        buildingNameText?.SetText(_currentDisplayedBuilding.displayName);
        buildingCostText?.SetText(_currentDisplayedBuilding.cost.ToString());
        buildingDescriptionText?.SetText(_currentDisplayedBuilding.description);
        
        // Update preview image
        if (buildingPreviewImage != null)
        {
            buildingPreviewImage.sprite = _currentDisplayedBuilding.previewImage;
            buildingPreviewImage.enabled = _currentDisplayedBuilding.previewImage != null;
        }
        
        // Check if the player can afford this building
        CheckCanAfford();
        
        // Trigger event
        OnNextBuildingChanged?.Invoke(_currentDisplayedBuilding);
    }
    
    public void PurchaseCurrentBuilding()
    {
        if (_currentDisplayedBuilding == null)
        {
            Debug.LogWarning("No building is currently displayed!");
            return;
        }
        
        // Don't allow purchase if we already have a building waiting for placement
        if (_currentPlacementObject != null)
        {
            Debug.LogWarning("Already have a building waiting for placement!");
            DisableBuyButton("Place Current Building!");
            return;
        }
        
        // Check if player has enough currency
        if (CurrentCurrency >= _currentDisplayedBuilding.cost)
        {
            if (_processingPurchase)
            {
                Debug.LogWarning("Already processing a purchase!");
                return;
            }
            
            _processingPurchase = true;
            
            // Capture the exact building that is displayed right now
            BuildingOption buildingToPurchase = _currentDisplayedBuilding;
            
            Debug.Log($"PRE-PURCHASE: About to purchase {buildingToPurchase.displayName}");
            
            // Deduct the cost
            CurrentCurrency -= buildingToPurchase.cost;
            
            // Call the purchase successful method with our captured reference
            PurchaseSuccessful(buildingToPurchase);
            
            // Move to the next building AFTER purchase is successful
            _currentBuildingIndex++;
            
            // Show the next building
            ShowNextBuilding();
            
            // Disable buy button until placement is complete
            DisableBuyButton("Place Current Building!");
            
            _processingPurchase = false;
        }
        else
        {
            // Update button text to show how much more currency is needed
            int needed = _currentDisplayedBuilding.cost - CurrentCurrency;
            DisableBuyButton($"Need {needed} more!");
        }
    }

    private void PurchaseSuccessful(BuildingOption building)
    {
        // Validate we have a building
        if (building == null)
        {
            Debug.LogError("Tried to purchase a null building!");
            return;
        }
        
        Debug.Log($"Purchase successful for: {building.displayName}");
        
        // Spawn the building directly with the passed reference
        SpawnPurchasedBuilding(building);
        
        // Trigger event
        OnBuildingPurchased?.Invoke(building);
    }

    private void SpawnPurchasedBuilding(BuildingOption building)
    {
        // Double check we have a valid building reference
        if (building == null || building.buildingPrefab == null)
        {
            Debug.LogError("Cannot spawn null building or prefab!");
            return;
        }

        // Clear any existing placement object
        if (_currentPlacementObject != null)
        {
            Destroy(_currentPlacementObject);
            _currentPlacementObject = null;
        }

        // Instantiate the building prefab - preserve prefab's original X rotation
        GameObject buildingObject = Instantiate(building.buildingPrefab);
        _currentPlacementObject = buildingObject;

        // Position at the spawn point with preserved X rotation
        if (buildingSpawnPoint != null)
        {
            // Apply Y offset
            float yOffset = 0f;

            // Get the grid manager to use its offset
            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                yOffset = gridManager.objectHeightOffset;
            }

            buildingObject.transform.position = new Vector3(
                buildingSpawnPoint.position.x,
                buildingSpawnPoint.position.y + yOffset,
                buildingSpawnPoint.position.z
            );

            // Use the prefab's original X rotation
            buildingObject.transform.rotation = Quaternion.Euler(
                buildingObject.transform.rotation.eulerAngles.x,
                0,
                0
            );
        }
        else
        {
            Debug.LogWarning("No building spawn point set - placing at origin");

            // Apply Y offset even at origin
            float yOffset = 0f;
            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                yOffset = gridManager.objectHeightOffset;
            }

            buildingObject.transform.position = new Vector3(0, yOffset, 0);

            // Preserve X rotation even at origin
            buildingObject.transform.rotation = Quaternion.Euler(
                buildingObject.transform.rotation.eulerAngles.x,
                0,
                0
            );
        }

        // Make it ready for placement
        MakeBuildingPlaceable(buildingObject);
    }

    private void MakeBuildingPlaceable(GameObject buildingObject)
    {
        // Check for GridObject component
        GridObject gridObject = buildingObject.GetComponent<GridObject>();
        if (gridObject == null)
        {
            Debug.LogError("Building prefab does not have a GridObject component!");
            return;
        }
        
        // Highlight the building to show it's ready for placement
        if (placementMaterial != null)
        {
            Renderer[] renderers = buildingObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // Store original materials and apply placement material
                Material[] origMaterials = renderer.sharedMaterials;
                Material[] newMaterials = new Material[origMaterials.Length];
                for (int i = 0; i < origMaterials.Length; i++)
                {
                    newMaterials[i] = placementMaterial;
                }
                renderer.materials = newMaterials;
            }
        }
        
        // Start the placement mode when this building is clicked
        buildingObject.AddComponent<BuildingPlacementTrigger>().Setup(this, gridDragAndDropManager);
    }

    
    public void OnBuildingPlaced(GameObject buildingObject)
    {
        // Clear reference if this was our current placement object
        if (buildingObject == _currentPlacementObject)
        {
            _currentPlacementObject = null;
            
            // Update the button state immediately
            CheckCanAfford();
        }
    }

    public void AddCurrency(int amount)
    {
        if (amount > 0)
        {
            CurrentCurrency += amount;
        }
    }

    public void SetCurrency(int amount)
    {
        if (amount >= 0)
        {
            CurrentCurrency = amount;
        }
    }

    private void UpdateCurrencyUI()
    {
        if (currencyText != null)
        {
            currencyText.text = _currentCurrency.ToString();
        }
    }
    

    private void CheckCanAfford()
    {
        if (availableBuildings.Count == 0 || _currentDisplayedBuilding == null)
        {
            DisableBuyButton("No Buildings");
            return;
        }
        
        // Check if we already have a building waiting for placement
        if (_currentPlacementObject != null)
        {
            DisableBuyButton("Place Current Building!");
            return;
        }
        
        // Check if we've reached the end of the sequence and not looping
        if (_currentBuildingIndex >= _buildingSequence.Count && !loopBuildings)
        {
            DisableBuyButton("Sold Out!");
            return;
        }
        
        int buildingCost = _currentDisplayedBuilding.cost;
        bool canAfford = CurrentCurrency >= buildingCost;
        
        // Update UI
        if (buyButton != null)
        {
            buyButton.interactable = canAfford;
            
            if (buyButtonText != null)
            {
                buyButtonText.text = canAfford ? "Buy" : "Need " + (buildingCost - CurrentCurrency) + " more";
            }
        }
        
        // Trigger event
        OnCanAffordChanged?.Invoke(canAfford);
    }

    private void DisableBuyButton(string reason)
    {
        if (buyButton != null)
        {
            buyButton.interactable = false;
            
            if (buyButtonText != null)
            {
                buyButtonText.text = reason;
            }
        }
    }
}

public class BuildingPlacementTrigger : MonoBehaviour
{
    private BuildingShopManager _shopManager;
    private GridDragAndDropManager _gridManager;

    public void Setup(BuildingShopManager shopManager, GridDragAndDropManager gridManager)
    {
        _shopManager = shopManager;
        _gridManager = gridManager;
    }

    private void OnMouseDown()
    {
        if (_gridManager != null)
        {
            // Use the existing object instead of creating a new one
            _gridManager.StartDraggingExistingObject(gameObject);

            // Disable income generation during the placement process
            BuildingIncomeGenerator incomeGenerator = gameObject.GetComponent<BuildingIncomeGenerator>();
            if (incomeGenerator != null)
            {
                incomeGenerator.StopIncomeGeneration();
            }

            // Notify the shop manager when placement has begun
            if (_shopManager != null)
            {
                _shopManager.OnBuildingPlaced(gameObject);
            }

            // Remove this component as it's no longer needed
            Destroy(this);
        }
    }
}
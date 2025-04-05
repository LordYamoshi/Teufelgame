using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameStateManager : MonoBehaviour
{
    [Header("Manager References")]
    public BuildingShopManager shopManager;
    public GridManager gridManager;
    public GridDragAndDropManager dragAndDropManager;
    
    [Header("UI References")]
    public GameObject winPanel;
    public GameObject losePanel;
    
    [Header("Game Events")]
    public UnityEvent OnGameWin;
    public UnityEvent OnGameLose;
    
    [Header("Game Settings")]
    [Tooltip("Time to wait after placement before checking game state")]
    public float checkDelay = 0.5f;
    [Tooltip("Should we automatically check for potential placement after each building is placed?")]
    public bool autoCheckAfterPlacement = true;

    private bool _gameEnded = false;

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        
        // Auto-find references if needed
        if (shopManager == null) shopManager = FindObjectOfType<BuildingShopManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (dragAndDropManager == null) dragAndDropManager = FindObjectOfType<GridDragAndDropManager>();
        
        // Listen for building placement events
        if (dragAndDropManager != null)
        {
            dragAndDropManager.OnBuildingPlaced.AddListener(OnBuildingPlaced);
        }
        
        // Listen for building purchase events to check if there are more buildings
        if (shopManager != null)
        {
            shopManager.OnBuildingPurchased.AddListener(OnBuildingPurchased);
        }
    }
    
    private void OnBuildingPurchased(BuildingShopManager.BuildingOption building)
    {
        // When a building is purchased, we'll check for placement possibilities after it's placed
        if (autoCheckAfterPlacement)
        {
            StartCoroutine(CheckAfterPlacementDelay());
        }
    }
    
    private IEnumerator CheckAfterPlacementDelay()
    {
        // Wait a moment to allow the placement to complete
        yield return new WaitForSeconds(checkDelay);
        
        // Check if the building was successfully placed
        if (shopManager._currentPlacementObject == null)
        {
            CheckForVictory();

            if (!_gameEnded)
            {
                CheckCanPlaceCurrentBuilding();
            }
        }
    }
    
    // This should be called from GridDragAndDropManager after a building is placed
    public void OnBuildingPlaced()
    {
        StartCoroutine(DelayedCheckAfterPlacement());
    }
    
    private IEnumerator DelayedCheckAfterPlacement()
    {
        // Wait a moment for everything to update
        yield return new WaitForSeconds(0.5f);
        
        //Check if the building was actually placed
        if (shopManager._currentPlacementObject == null)
        {
            CheckForVictory();
        }

        // If we haven't won, check if the next building can be placed
        if (!_gameEnded)
        {
            CheckCanPlaceCurrentBuilding();
        }
    }
    
    private void CheckForVictory()
    {
        if (shopManager != null && 
            shopManager._currentBuildingIndex >= shopManager._buildingSequence.Count && 
            !shopManager.loopBuildings && 
            shopManager._currentPlacementObject == null)
        {
            Win();
        }
    }
    
    public void CheckGameState()
    {
        if (_gameEnded) return;
        
        CheckForVictory();
        
        if (!_gameEnded)
        {
            CheckCanPlaceCurrentBuilding();
        }
    }
    
    private void CheckCanPlaceCurrentBuilding()
    {
        // If there are no more buildings, the player has won
        if (shopManager._currentBuildingIndex >= shopManager.availableBuildings.Count && 
            !shopManager.loopBuildings)
        {
            Win();
            return;
        }
        
        // Get current building
        BuildingShopManager.BuildingOption currentBuilding = shopManager._currentDisplayedBuilding;
        if (currentBuilding == null || currentBuilding.buildingPrefab == null)
        {
            return;
        }
        
        // Temporarily create the building prefab to check its shape
        GameObject tempBuilding = Instantiate(currentBuilding.buildingPrefab);
        tempBuilding.SetActive(false);
        
        GridObject gridObject = tempBuilding.GetComponent<GridObject>();
        if (gridObject == null)
        {
            Destroy(tempBuilding);
            return;
        }
        
        // Check all grid positions for potential placement
        bool canBePlacedAnywhere = false;
        List<Vector2Int> possiblePositions = GetAllPossiblePlacements(gridObject);
        
        // Clean up
        Destroy(tempBuilding);
        
        if (possiblePositions.Count > 0)
        {
            canBePlacedAnywhere = true;
            Debug.Log($"Building {currentBuilding.displayName} can be placed in {possiblePositions.Count} positions");
        }
        
        // If it cannot be placed anywhere, the player loses
        if (!canBePlacedAnywhere)
        {
            Lose();
        }
    }
    
    private List<Vector2Int> GetAllPossiblePlacements(GridObject gridObject)
    {
        List<Vector2Int> validPlacements = new List<Vector2Int>();
        
        // Try every grid position
        for (int x = 0; x < gridManager.maxWidth; x++)
        {
            for (int y = 0; y < gridManager.maxDepth; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                
                // Get the cells this object would occupy at this position
                List<Vector2Int> occupiedCells = gridObject.GetOccupiedCells(pos);
                
                // Check if placement is valid
                if (gridManager.IsValidPlacement(occupiedCells))
                {
                    validPlacements.Add(pos);
                }
            }
        }
        
        return validPlacements;
    }
    
    private void Win()
    {
        if (_gameEnded) return;
        
        _gameEnded = true;
        
        // Show win UI
        if (winPanel != null) winPanel.SetActive(true);
        
        // Trigger event
        OnGameWin?.Invoke();
    }
    
    private void Lose()
    {
        if (_gameEnded) return;
        
        _gameEnded = true;
        
        // Show lose UI
        if (losePanel != null) losePanel.SetActive(true);
        
        // Trigger event
        OnGameLose?.Invoke();
    }
}
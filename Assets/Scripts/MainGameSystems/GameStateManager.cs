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
        
        if (shopManager == null) shopManager = FindObjectOfType<BuildingShopManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (dragAndDropManager == null) dragAndDropManager = FindObjectOfType<GridDragAndDropManager>();
        
        if (shopManager != null)
        {
            shopManager.OnBuildingPurchased.AddListener(OnBuildingPurchased);
            shopManager.OnNextBuildingChanged.AddListener(OnNextBuildingChanged);
        }
    }
    
    private void OnBuildingPurchased(BuildingShopManager.BuildingOption building)
    {
        if (autoCheckAfterPlacement)
        {
            StartCoroutine(CheckAfterPlacementDelay());
        }
    }
    
    private void OnNextBuildingChanged(BuildingShopManager.BuildingOption building)
    {
        // Check if this is the last building and we've placed it
        if (shopManager._currentBuildingIndex >= shopManager._buildingSequence.Count && 
            !shopManager.loopBuildings)
        {
            Win();
        }
        else
        {
            StartCoroutine(CheckCanPlaceCurrentBuilding());
        }
    }
    
    private IEnumerator CheckAfterPlacementDelay()
    {
        // Wait a moment to allow the placement to complete
        yield return new WaitForSeconds(checkDelay);
        
        // Now check if the next building can be placed
        CheckCanPlaceCurrentBuilding();
    }
    
    public void CheckGameState()
    {
        if (_gameEnded) return;
        
        StartCoroutine(CheckCanPlaceCurrentBuilding());
    }
    
    private IEnumerator CheckCanPlaceCurrentBuilding()
    {
        yield return null;
        
        // If there are no more buildings, the player has won
        if (shopManager._currentBuildingIndex >= shopManager.availableBuildings.Count && 
            !shopManager.loopBuildings)
        {
            Win();
            yield break;
        }
        
        // Get current building
        BuildingShopManager.BuildingOption currentBuilding = shopManager._currentDisplayedBuilding;
        if (currentBuilding == null || currentBuilding.buildingPrefab == null)
        {
            Debug.LogWarning("No current building to check.");
            yield break;
        }
        
        // Temporarily create the building prefab to check its shape
        GameObject tempBuilding = Instantiate(currentBuilding.buildingPrefab);
        tempBuilding.SetActive(false);
        
        GridObject gridObject = tempBuilding.GetComponent<GridObject>();
        if (gridObject == null)
        {
            Debug.LogError("Building prefab does not have a GridObject component!");
            Destroy(tempBuilding);
            yield break;
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
        
        Debug.Log("Game Win! All buildings have been placed successfully.");
        _gameEnded = true;
        
        // Show win UI
        if (winPanel != null) winPanel.SetActive(true);
        
        // Trigger event
        OnGameWin?.Invoke();
    }
    
    private void Lose()
    {
        if (_gameEnded) return;
        
        Debug.Log("Game Over! Building cannot be placed anywhere on the grid.");
        _gameEnded = true;
        
        // Show lose UI
        if (losePanel != null) losePanel.SetActive(true);
        
        // Trigger event
        OnGameLose?.Invoke();
    }
    
    public void ForceCheckPlacement()
    {
        CheckGameState();
    }
}
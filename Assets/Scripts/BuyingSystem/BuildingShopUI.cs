using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BuildingShopUI : MonoBehaviour
{
    public GameObject shopPanel;
    public Image buildingPreviewImage;
    public TMP_Text buildingNameText;
    public TMP_Text buildingCostText;
    public TMP_Text buildingDescriptionText;
    public TMP_Text currencyText;
    public Button buyButton;
    public AudioClip purchaseSound;
    public AudioClip insufficientFundsSound;
    public BuildingShopManager shopManager;
    
    [Header("Currency Text Settings")]
    [Tooltip("Color when player has enough money")]
    public Color sufficientFundsColor = Color.white;
    [Tooltip("Color when player has enough money to buy current item")]
    public Color canAffordColor = Color.green;
    [Tooltip("Flash color for insufficient funds")]
    public Color insufficientFundsColor = Color.red;
    [Tooltip("Duration of each flash when funds are insufficient")]
    public float flashDuration = 0.1f;
    [Tooltip("Number of flashes when funds are insufficient")]
    public int numberOfFlashes = 3;
    
    private AudioSource _audioSource;
    private Color _originalCurrencyTextColor;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        if (shopManager == null)
        {
            shopManager = FindObjectOfType<BuildingShopManager>();
        }
        
        if (shopManager != null)
        {
            shopManager.OnBuildingPurchased.AddListener(OnBuildingPurchased);
            shopManager.OnNextBuildingChanged.AddListener(OnNextBuildingChanged);
            shopManager.OnCurrencyChanged.AddListener(OnCurrencyChanged);
            shopManager.OnCanAffordChanged.AddListener(OnCanAffordChanged);
        }
        
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
        
        if (currencyText != null)
        {
            _originalCurrencyTextColor = currencyText.color;
        }
        
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    private void Start()
    {
        UpdateCurrencyDisplay(shopManager ? shopManager.CurrentCurrency : 0);
    }
    
    private void OnBuildingPurchased(BuildingShopManager.BuildingOption building)
    {
        PlayPurchaseEffects();
    }
    
    private void OnNextBuildingChanged(BuildingShopManager.BuildingOption building)
    {
        UpdateBuildingPreview(building);
    }
    
    private void OnCurrencyChanged(int newAmount)
    {
        UpdateCurrencyDisplay(newAmount);
    }
    
    private void OnCanAffordChanged(bool canAfford)
    {
        if (buyButton != null)
        {
            buyButton.interactable = canAfford;
        }
        UpdateCurrencyColor(canAfford);
    }
    
    private void UpdateCurrencyColor(bool canAfford)
    {
        if (currencyText != null)
        {
            if (_flashCoroutine != null) return;
            
            // Set color based on whether player can afford the current building
            currencyText.color = canAfford ? canAffordColor : sufficientFundsColor;
        }
    }
    
    public void OnBuyButtonClicked()
    {
        if (shopManager != null)
        {
            shopManager.PurchaseCurrentBuilding();
        }
    }
    
    private void UpdateBuildingPreview(BuildingShopManager.BuildingOption building)
    {
        if (buildingPreviewImage != null)
        {
            buildingPreviewImage.sprite = building.previewImage;
            buildingPreviewImage.gameObject.SetActive(building.previewImage != null);
        }
    
        buildingNameText?.SetText(building.displayName);
        buildingCostText?.SetText(building.cost.ToString());
        buildingDescriptionText?.SetText(building.description);
        
        // Check if we can afford this building and update color
        if (shopManager != null)
        {
            bool canAfford = shopManager.CurrentCurrency >= building.cost;
            UpdateCurrencyColor(canAfford);
        }
    }
    
    private void UpdateCurrencyDisplay(int amount)
    {
        if (currencyText != null)
        {
            currencyText.text = amount.ToString();
            
            // Update the color if we have a current building
            if (shopManager != null && shopManager._currentDisplayedBuilding != null)
            {
                bool canAfford = amount >= shopManager._currentDisplayedBuilding.cost;
                UpdateCurrencyColor(canAfford);
            }
        }
    }
    
    private void PlayPurchaseEffects()
    {
        if (purchaseSound != null)
        {
            _audioSource?.PlayOneShot(purchaseSound);
        }
    }
    
    public void PlayInsufficientFundsEffect()
    {
        // Check if we have enough money for the current building
        if (shopManager != null && shopManager._currentDisplayedBuilding != null)
        {
            bool canAfford = shopManager.CurrentCurrency >= shopManager._currentDisplayedBuilding.cost;
            
            // Only play effect if we can't afford it
            if (!canAfford)
            {
                if (_audioSource != null && insufficientFundsSound != null)
                {
                    _audioSource.PlayOneShot(insufficientFundsSound);
                }
                
                // If we're already flashing, stop that coroutine
                if (_flashCoroutine != null)
                {
                    StopCoroutine(_flashCoroutine);
                }
                
                // Start a new flash coroutine
                if (currencyText != null)
                {
                    _flashCoroutine = StartCoroutine(FlashText(currencyText));
                }
            }
        }
    }
    
    private IEnumerator FlashText(TMP_Text text)
    {
        // Store the color we should return to
        Color returnColor;
        if (shopManager != null && shopManager._currentDisplayedBuilding != null)
        {
            bool canAfford = shopManager.CurrentCurrency >= shopManager._currentDisplayedBuilding.cost;
            returnColor = canAfford ? canAffordColor : sufficientFundsColor;
        }
        else
        {
            returnColor = sufficientFundsColor;
        }
        
        // Flash between red and return color
        for (int i = 0; i < numberOfFlashes; i++)
        {
            text.color = insufficientFundsColor;
            yield return new WaitForSeconds(flashDuration);
            text.color = returnColor;
            yield return new WaitForSeconds(flashDuration);
        }
        
        // End the coroutine
        _flashCoroutine = null;
    }
}
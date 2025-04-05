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
    
    private AudioSource _audioSource;

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
    }
    
    private void Start()
    {
        // Initialize UI
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
        // Update currency display
        UpdateCurrencyDisplay(newAmount);
    }
    
    private void OnCanAffordChanged(bool canAfford)
    {
        // Update buy button state
        if (buyButton != null)
        {
            buyButton.interactable = canAfford;
            
        }
    }
    
    private void OnBuyButtonClicked()
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
    }
    
    private void UpdateCurrencyDisplay(int amount)
    {
        if (currencyText != null)
        {
            currencyText.text = amount.ToString();
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
        if (_audioSource != null && insufficientFundsSound != null)
        {
            _audioSource.PlayOneShot(insufficientFundsSound);
        }
        
        // You could add animation or visual effects here
        if (currencyText != null)
        {
            StartCoroutine(FlashText(currencyText));
        }
    }
    
    private IEnumerator FlashText(TMP_Text text)
    {
        Color originalColor = text.color;
        
        for (int i = 0; i < 3; i++)
        {
            text.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            text.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    public void ToggleShopPanel(bool show)
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(show);
        }
    }
}

using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public BuildingShopManager shopManager;
    public int currencyAmount = 50;
    public KeyCode addCurrencyKey = KeyCode.M;
    public TMP_Text addedCurrencyText;

    void Update()
    {
        // For testing: add currency when the key is pressed
        if (Input.GetKeyDown(addCurrencyKey))
        {
            AddCurrency();
        }
    }

    public void AddCurrency()
    {
        if (shopManager != null)
        {
            shopManager.AddCurrency(currencyAmount);

            if (addedCurrencyText != null)
            {
                addedCurrencyText.text = "+" + currencyAmount;
                addedCurrencyText.gameObject.SetActive(true);
                Invoke("HideAddedText", 1.5f);
            }

            Debug.Log($"Added {currencyAmount} currency. New total: {shopManager.CurrentCurrency}");
        }
    }

    private void HideAddedText()
    {
        if (addedCurrencyText != null)
        {
            addedCurrencyText.gameObject.SetActive(false);
        }
    }
}

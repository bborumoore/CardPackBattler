using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine;
using TcgEngine.UI;

/// <summary>
/// Simple test script to verify draft card visuals are working
/// Attach this to an empty GameObject in your scene
/// </summary>
public class DraftTestSetup : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject draftCardPrefab;
    public GameObject draftSlotPrefab;
    
    [Header("Containers")]
    public Transform draftHandContainer;
    public Transform draftSlotsContainer;
    
    [Header("Test Data")]
    public DraftCardData[] testDraftCards;
    
    [Header("Layout")]
    public float cardSpacing = 150f;
    public float slotSpacing = 220f;
    
    void Start()
    {
        SetupTestScene();
    }
    
    void SetupTestScene()
    {
        // Create draft slots
        CreateDraftSlots();
        
        // Create test hand
        CreateTestHand();
    }
    
    void CreateDraftSlots()
    {
        if (draftSlotPrefab == null || draftSlotsContainer == null)
        {
            Debug.LogWarning("Missing slot prefab or container");
            return;
        }
        
        DraftSlotType[] slotTypes = {
            DraftSlotType.Head,
            DraftSlotType.Body,
            DraftSlotType.Limb,
            DraftSlotType.Power,
            DraftSlotType.Knowledge
        };
        
        float startX = -(slotTypes.Length - 1) * slotSpacing / 2f;
        
        for (int i = 0; i < slotTypes.Length; i++)
        {
            GameObject slotObj = Instantiate(draftSlotPrefab, draftSlotsContainer);
            slotObj.name = "DraftSlot_" + slotTypes[i];
            
            RectTransform rect = slotObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(startX + i * slotSpacing, 0);
            }
            
            DraftSlotUI slotUI = slotObj.GetComponent<DraftSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetSlot(slotTypes[i], 0); // Player 0 for testing
            }
        }
    }
    
    void CreateTestHand()
    {
        if (draftCardPrefab == null || draftHandContainer == null)
        {
            Debug.LogWarning("Missing card prefab or container");
            return;
        }
        
        if (testDraftCards == null || testDraftCards.Length == 0)
        {
            Debug.LogWarning("No test draft cards assigned");
            return;
        }
        
        float startX = -(testDraftCards.Length - 1) * cardSpacing / 2f;
        
        for (int i = 0; i < testDraftCards.Length; i++)
        {
            if (testDraftCards[i] == null)
                continue;
                
            GameObject cardObj = Instantiate(draftCardPrefab, draftHandContainer);
            cardObj.name = "DraftCard_" + i;
            
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(startX + i * cardSpacing, 0);
            }
            
            DraftCardUI cardUI = cardObj.GetComponent<DraftCardUI>();
            if (cardUI != null)
            {
                cardUI.SetCard(testDraftCards[i]);
            }
        }
    }
    
    [ContextMenu("Refresh Test Scene")]
    void RefreshTestScene()
    {
        // Clear existing objects
        if (draftHandContainer != null)
        {
            foreach (Transform child in draftHandContainer)
                DestroyImmediate(child.gameObject);
        }
        
        if (draftSlotsContainer != null)
        {
            foreach (Transform child in draftSlotsContainer)
                DestroyImmediate(child.gameObject);
        }
        
        // Recreate
        SetupTestScene();
    }
}
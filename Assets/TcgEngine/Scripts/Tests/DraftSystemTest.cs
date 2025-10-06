using UnityEngine;
using TcgEngine;

public class DraftSystemTest : MonoBehaviour
{
    [Header("Test Draft Cards")]
    public DraftCardData testDraftCard;
    
    void Start()
    {
        TestDraftCardGeneration();
    }
    
    void TestDraftCardGeneration()
    {
        if (testDraftCard == null)
        {
            Debug.LogError("No test draft card assigned!");
            return;
        }
        
        Debug.Log("=== DRAFT CARD TEST ===");
        Debug.Log($"Testing: {testDraftCard.title}");
        
        // Test each slot type
        DraftSlotType[] slots = { 
            DraftSlotType.Head, 
            DraftSlotType.Body, 
            DraftSlotType.Limb, 
            DraftSlotType.Power, 
            DraftSlotType.Knowledge 
        };
        
        foreach (var slot in slots)
        {
            var deckCards = testDraftCard.GenerateDeckCards(slot);
            var sideCards = testDraftCard.GenerateSideDeckCards(slot);
            
            Debug.Log($"Slot {slot}:");
            Debug.Log($"  - Deck cards: {deckCards.Count}");
            Debug.Log($"  - Side cards: {sideCards.Count}");
            
            foreach (var card in deckCards)
            {
                Debug.Log($"    Deck: {card.name}");
            }
        }
        
        Debug.Log("=== TEST COMPLETE ===");
    }
    
    [ContextMenu("Run Draft Test")]
    void RunTest()
    {
        TestDraftCardGeneration();
    }
}
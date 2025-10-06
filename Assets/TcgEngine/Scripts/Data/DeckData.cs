using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Defines all fixed deck data (for user custom decks, check UserData.cs)
    /// </summary>
    
    [CreateAssetMenu(fileName = "DeckData", menuName = "TcgEngine/DeckData", order = 7)]
    public class DeckData : ScriptableObject
    {
        public string id;

        [Header("Display")]
        public string title;

        [Header("Cards")]
        public CardData hero;
        public CardData[] cards;  // Keep as CardData[] for the inspector

        public static List<DeckData> deck_list = new List<DeckData>();
        public List<UserCardData> side_cards = new List<UserCardData>();  // Side deck cards

        public static void Load(string folder = "")
        {
            if (deck_list.Count == 0)
                deck_list.AddRange(Resources.LoadAll<DeckData>(folder));
        }

        public int GetQuantity()
        {
            // Simply return the length of the cards array
            return cards != null ? cards.Length : 0;
        }

        public int GetSideQuantity()
        {
            int quantity = 0;
            if (side_cards != null)
            {
                foreach (UserCardData card in side_cards)
                    quantity += card.quantity;
            }
            return quantity;
        }

        public int GetTotalQuantity()
        {
            return GetQuantity() + GetSideQuantity();
        }

        public bool IsValid()
        {
            return cards != null && cards.Length >= GameplayData.Get().deck_size;
        }

        // Add validation method:
        public bool IsValidSideDeck()
        {
            return GetSideQuantity() == GameplayData.Get().side_deck_size;
        }

        public static DeckData Get(string id)
        {
            foreach (DeckData deck in GetAll())
            {
                if (deck.id == id)
                    return deck;
            }
            return null;
        }

        public static List<DeckData> GetAll()
        {
            return deck_list;
        }
    }
}
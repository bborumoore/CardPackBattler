using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Defines a draft card that players use to build their deck at the start of the game
    /// Each draft card can spawn different cards based on which slot it's played into
    /// </summary>
    [CreateAssetMenu(fileName = "DraftCard", menuName = "TcgEngine/DraftCard", order = 11)]
    public class DraftCardData : ScriptableObject
    {
        [Header("Draft Card Info")]
        public string id;                      // Unique identifier
        public string title;                   // Display name
        [TextArea(3, 5)]
        public string description;             // Flavor text or description
        public Sprite card_art;                // Visual for the draft card
        
        [Header("Visual")]
        public Color card_color = Color.white; // Card background tint
        public Sprite card_frame;              // Optional custom frame
        
        [Header("Card Pools by Category")]
        // Each category contains pools of cards that can be spawned
        // When played in a slot, it spawns from general pool + matching slot pool
        public CardPoolCategory general_pool;  // Always used regardless of slot
        public CardPoolCategory head_pool;     // Used when played in Head slot
        public CardPoolCategory body_pool;     // Used when played in Body slot
        public CardPoolCategory limb_pool;     // Used when played in Limb slot
        public CardPoolCategory power_pool;    // Used when played in Power slot
        public CardPoolCategory knowledge_pool;// Used when played in Knowledge slot

        [Header("Spawn Configuration")]
        public int deck_cards_from_general = 2;    // How many deck cards from general pool
        public int deck_cards_from_slot = 1;        // How many deck cards from slot pool
        public int side_cards_from_general = 1;    // How many side cards from general pool
        public int side_cards_from_slot = 1;        // How many side cards from slot pool

        private static Dictionary<string, DraftCardData> draft_cards = new Dictionary<string, DraftCardData>();

        /// <summary>
        /// Get the appropriate card pool based on the slot this draft card is played into
        /// </summary>
        public CardPoolCategory GetPoolForSlot(DraftSlotType slot)
        {
            switch (slot)
            {
                case DraftSlotType.Head:
                    return head_pool;
                case DraftSlotType.Body:
                    return body_pool;
                case DraftSlotType.Limb:
                    return limb_pool;
                case DraftSlotType.Power:
                    return power_pool;
                case DraftSlotType.Knowledge:
                    return knowledge_pool;
                default:
                    return general_pool;
            }
        }

        /// <summary>
        /// Generate the deck cards that this draft card will spawn when played in a specific slot
        /// </summary>
        public List<CardData> GenerateDeckCards(DraftSlotType slot)
        {
            List<CardData> cards = new List<CardData>();
            
            // Add cards from general pool
            if (general_pool != null)
            {
                cards.AddRange(general_pool.GetRandomDeckCards(deck_cards_from_general));
            }
            
            // Add cards from slot-specific pool
            CardPoolCategory slotPool = GetPoolForSlot(slot);
            if (slotPool != null && slotPool != general_pool)
            {
                cards.AddRange(slotPool.GetRandomDeckCards(deck_cards_from_slot));
            }
            
            return cards;
        }

        /// <summary>
        /// Generate the side deck cards that this draft card will spawn when played in a specific slot
        /// </summary>
        public List<CardData> GenerateSideDeckCards(DraftSlotType slot)
        {
            List<CardData> cards = new List<CardData>();
            
            // Add cards from general pool
            if (general_pool != null)
            {
                cards.AddRange(general_pool.GetRandomSideCards(side_cards_from_general));
            }
            
            // Add cards from slot-specific pool
            CardPoolCategory slotPool = GetPoolForSlot(slot);
            if (slotPool != null && slotPool != general_pool)
            {
                cards.AddRange(slotPool.GetRandomSideCards(side_cards_from_slot));
            }
            
            return cards;
        }

        /// <summary>
        /// Check if this draft card is valid (has required data)
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(id))
                return false;
                
            // Must have at least general pool
            if (general_pool == null || !general_pool.IsValid())
                return false;
                
            // Check that at least one slot pool is valid
            bool hasValidSlotPool = (head_pool != null && head_pool.IsValid()) ||
                                   (body_pool != null && body_pool.IsValid()) ||
                                   (limb_pool != null && limb_pool.IsValid()) ||
                                   (power_pool != null && power_pool.IsValid()) ||
                                   (knowledge_pool != null && knowledge_pool.IsValid());
                                   
            return hasValidSlotPool;
        }

        // ----- Static Methods for Loading/Accessing Draft Cards -----

        public static void Load(string folder = "")
        {
            if (draft_cards.Count == 0)
            {
                DraftCardData[] cards = Resources.LoadAll<DraftCardData>(folder);
                foreach (DraftCardData card in cards)
                {
                    if (!draft_cards.ContainsKey(card.id))
                        draft_cards.Add(card.id, card);
                    else
                        Debug.LogError("Duplicate draft card ID: " + card.id);
                }
            }
        }

        public static DraftCardData Get(string id)
        {
            if (draft_cards.ContainsKey(id))
                return draft_cards[id];
            return null;
        }

        public static List<DraftCardData> GetAll()
        {
            return new List<DraftCardData>(draft_cards.Values);
        }

        public static void Clear()
        {
            draft_cards.Clear();
        }
    }

    /// <summary>
    /// Represents a pool of cards that can be spawned from a draft card category
    /// </summary>
    [System.Serializable]
    public class CardPoolCategory
    {
        [Header("Deck Cards")]
        public CardData[] possible_deck_cards;     // Pool of cards that can go into main deck
        public float[] deck_card_weights;          // Weights for random selection (optional)
        
        [Header("Side Deck Cards")]
        public CardData[] possible_side_cards;     // Pool of cards that can go into side deck
        public float[] side_card_weights;          // Weights for random selection (optional)

        [Header("Selection Mode")]
        public CardSelectionMode selection_mode = CardSelectionMode.Random;
        
        /// <summary>
        /// Get random cards from the deck pool
        /// </summary>
        public List<CardData> GetRandomDeckCards(int count)
        {
            return GetRandomCards(possible_deck_cards, deck_card_weights, count);
        }
        
        /// <summary>
        /// Get random cards from the side deck pool
        /// </summary>
        public List<CardData> GetRandomSideCards(int count)
        {
            return GetRandomCards(possible_side_cards, side_card_weights, count);
        }
        
        private List<CardData> GetRandomCards(CardData[] pool, float[] weights, int count)
        {
            List<CardData> selected = new List<CardData>();
            
            if (pool == null || pool.Length == 0)
                return selected;
                
            if (selection_mode == CardSelectionMode.Random)
            {
                // Simple random selection with replacement
                for (int i = 0; i < count; i++)
                {
                    CardData card = GetWeightedRandom(pool, weights);
                    if (card != null)
                        selected.Add(card);
                }
            }
            else if (selection_mode == CardSelectionMode.RandomNoDuplicates)
            {
                // Random selection without replacement
                List<CardData> availablePool = new List<CardData>(pool);
                for (int i = 0; i < count && availablePool.Count > 0; i++)
                {
                    int index = Random.Range(0, availablePool.Count);
                    selected.Add(availablePool[index]);
                    availablePool.RemoveAt(index);
                }
            }
            else if (selection_mode == CardSelectionMode.Sequential)
            {
                // Take first N cards from pool
                for (int i = 0; i < count && i < pool.Length; i++)
                {
                    selected.Add(pool[i]);
                }
            }
            
            return selected;
        }
        
        private CardData GetWeightedRandom(CardData[] pool, float[] weights)
        {
            if (pool.Length == 0)
                return null;
                
            // If no weights specified, equal probability
            if (weights == null || weights.Length == 0)
            {
                return pool[Random.Range(0, pool.Length)];
            }
            
            // Weighted random selection
            float totalWeight = 0;
            for (int i = 0; i < Mathf.Min(pool.Length, weights.Length); i++)
            {
                totalWeight += weights[i];
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0;
            
            for (int i = 0; i < Mathf.Min(pool.Length, weights.Length); i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return pool[i];
                }
            }
            
            // Fallback
            return pool[0];
        }
        
        public bool IsValid()
        {
            return (possible_deck_cards != null && possible_deck_cards.Length > 0) ||
                   (possible_side_cards != null && possible_side_cards.Length > 0);
        }
    }

    /// <summary>
    /// Defines the slots where draft cards can be played
    /// </summary>
    public enum DraftSlotType
    {
        None = 0,
        Head = 1,
        Body = 2,
        Limb = 3,
        Power = 4,
        Knowledge = 5
    }

    /// <summary>
    /// How cards are selected from the pool
    /// </summary>
    public enum CardSelectionMode
    {
        Random = 0,              // Pure random with replacement
        RandomNoDuplicates = 1,  // Random without replacement
        Sequential = 2,          // Take first N cards in order
    }
}
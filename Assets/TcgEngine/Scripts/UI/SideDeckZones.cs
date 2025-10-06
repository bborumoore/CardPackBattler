using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;
using TcgEngine.Client;  // ADDED: This is where BoardCard is located

namespace TcgEngine.UI
{
    /// <summary>
    /// Displays a player's side deck cards in the game UI
    /// Shows all cards to both players (fully visible)
    /// </summary>
    public class SideDeckZone : MonoBehaviour
    {
        public int player_id;
        public GameObject card_prefab;
        public Transform card_parent;
        
        private GameClient client;
        private List<CardUI> cards_ui = new List<CardUI>();  // CHANGED: Use CardUI instead of BoardCard

        void Start()
        {
            client = GameClient.Get();

            RefreshCards();
        }

        void OnDestroy()
        {
            // No cleanup needed
        }

        private void RefreshCards()
        {
            Game game_data = client?.GetGameData();
            if (game_data == null)
                return;

            Player player = game_data.GetPlayer(player_id);
            if (player == null)
                return;

            // Remove extra UI cards
            while (cards_ui.Count > player.cards_side.Count)
            {
                int index = cards_ui.Count - 1;
                CardUI card_ui = cards_ui[index];
                cards_ui.RemoveAt(index);
                Destroy(card_ui.gameObject);
            }

            // Add missing UI cards
            while (cards_ui.Count < player.cards_side.Count)
            {
                GameObject obj = Instantiate(card_prefab, card_parent);
                CardUI card_ui = obj.GetComponent<CardUI>();
                if (card_ui != null)
                {
                    cards_ui.Add(card_ui);
                    card_ui.onClick += OnClickSideDeckCard;
                }
            }

            // Update all cards
            for (int i = 0; i < player.cards_side.Count; i++)
            {
                Card card = player.cards_side[i];
                CardUI card_ui = cards_ui[i];
                card_ui.SetCard(card);

                // Enable/disable selection based on phase and player
                bool can_select = game_data.phase == GamePhase.SideDeckSelection 
                    && !player.side_deck_selected
                    && game_data.IsPlayerActionTurn(player);
                
                // Visual feedback for clickable state
                if (can_select)
                    card_ui.SetOpacity(1f);
                else
                    card_ui.SetOpacity(0.5f);
            }
        }

        private void OnClickSideDeckCard(CardUI card_ui)
        {
            Game game_data = client?.GetGameData();
            if (game_data == null || game_data.phase != GamePhase.SideDeckSelection)
                return;

            Player player = game_data.GetPlayer(player_id);
            if (player == null || player.side_deck_selected)
                return;

            // Find the card in the side deck
            Card card = null;
            foreach (Card side_card in player.cards_side)
            {
                if (side_card.CardData == card_ui.GetCard() && side_card.VariantData == card_ui.GetVariant())
                {
                    card = side_card;
                    break;
                }
            }

            if (card != null)
            {
                GameClient.Get().SelectSideDeckCard(card);
            }
        }

        private void Update()
        {
            // Refresh every frame to keep UI in sync
            if (client != null && client.IsReady())
            {
                RefreshCards();
            }
        }

        public Player GetPlayer()
        {
            Game game_data = client?.GetGameData();
            return game_data?.GetPlayer(player_id);
        }
    }
}
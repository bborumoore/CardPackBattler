using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Gameplay;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Panel that appears during SideDeckSelection phase
    /// Prompts player to select a card from their side deck
    /// </summary>
    public class SideDeckSelectionPanel : UIPanel
    {
        [Header("UI Elements")]
        public Text title_text;
        public Text instruction_text;
        public Text waiting_text;
        public GameObject selection_area;
        public GameObject waiting_area;

        private static SideDeckSelectionPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();
            RefreshPanel();
        }

        protected override void Update()
        {
            base.Update();

            GameClient client = GameClient.Get();
            if (client == null)
                return;

            Game game_data = client.GetGameData();
            if (game_data == null)
                return;

            // Show/hide panel based on phase
            if (game_data.phase == GamePhase.SideDeckSelection)
            {
                if (!IsVisible())
                    Show();
                RefreshPanel();
            }
            else
            {
                if (IsVisible())
                    Hide();
            }
        }

        private void RefreshPanel()
        {
            GameClient client = GameClient.Get();
            if (client == null)
                return;

            Game game_data = client.GetGameData();
            if (game_data == null)
                return;

            Player player = game_data.GetPlayer(client.GetPlayerID());
            if (player == null)
                return;

            bool has_selected = player.side_deck_selected;
            bool has_side_cards = player.HasSideCards();

            // Update UI state
            if (selection_area != null)
                selection_area.SetActive(!has_selected && has_side_cards);

            if (waiting_area != null)
                waiting_area.SetActive(has_selected || !has_side_cards);

            // Update text
            if (title_text != null)
            {
                if (has_side_cards)
                    title_text.text = "Select Side Deck Card";
                else
                    title_text.text = "No Side Deck Cards";
            }

            if (instruction_text != null && !has_selected && has_side_cards)
            {
                instruction_text.text = "Choose one card from your side deck to add to your hand";
            }

            if (waiting_text != null && has_selected)
            {
                waiting_text.text = "Waiting for opponent...";
            }
            else if (waiting_text != null && !has_side_cards)
            {
                waiting_text.text = "Your side deck is empty";
            }
        }

        public static SideDeckSelectionPanel Get()
        {
            return instance;
        }
    }
}
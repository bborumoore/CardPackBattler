using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using TcgEngine.Client;
using UnityEngine.EventSystems;

namespace TcgEngine.UI
{
    /// <summary>
    /// Visual representation of a draft slot where draft cards can be played
    /// </summary>
    public class DraftSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public DraftSlotType slot_type;
        public int player_id;
        
        [Header("UI Elements")]
        public Text slot_label;
        public Image slot_image;
        public Image highlight;
        public GameObject card_display;
        
        [Header("Colors")]
        public Color normal_color = Color.gray;
        public Color highlight_color = Color.yellow;
        public Color filled_color = Color.green;
        
        private DraftCardData current_card;
        private bool is_highlighted = false;
        
        void Awake()
        {
            if (highlight != null)
                highlight.enabled = false;
        }
        
        void Start()
        {
            UpdateDisplay();
        }
        
        public void SetSlot(DraftSlotType type, int player)
        {
            slot_type = type;
            player_id = player;
            UpdateDisplay();
        }
        
        public void SetCard(DraftCardData card)
        {
            current_card = card;
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            // Update label
            if (slot_label != null)
                slot_label.text = slot_type.ToString().ToUpper();
            
            // Update slot color
            if (slot_image != null)
            {
                if (current_card != null)
                    slot_image.color = filled_color;
                else if (is_highlighted)
                    slot_image.color = highlight_color;
                else
                    slot_image.color = normal_color;
            }
            
            // Show/hide card display
            if (card_display != null)
                card_display.SetActive(current_card != null);
            
            // If there's a DraftCardUI component on the card display, update it
            if (card_display != null && current_card != null)
            {
                DraftCardUI card_ui = card_display.GetComponent<DraftCardUI>();
                if (card_ui != null)
                    card_ui.SetCard(current_card);
            }
        }
        
        public bool CanAcceptCard(DraftCardData card)
        {
            // Check if slot is empty
            if (current_card != null)
                return false;
            
            // Check if it's the player's turn
            Game game_data = GameClient.Get()?.GetGameData();
            if (game_data == null)
                return false;
            
            // Check if it's this player's slot
            if (player_id != GameClient.Get().GetPlayerID())
                return false;
            
            // Check if we're in draft phase
            if (game_data.phase != GamePhase.Draft)
                return false;
            
            // Additional validation can be added here
            return true;
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // This is handled by the DraftCardUI's OnEndDrag
            // But we can add visual feedback here if needed
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (current_card == null)
            {
                is_highlighted = true;
                if (highlight != null)
                    highlight.enabled = true;
                UpdateDisplay();
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            is_highlighted = false;
            if (highlight != null)
                highlight.enabled = false;
            UpdateDisplay();
        }
        
        public bool IsEmpty()
        {
            return current_card == null;
        }
        
        public DraftCardData GetCard()
        {
            return current_card;
        }
    }
}
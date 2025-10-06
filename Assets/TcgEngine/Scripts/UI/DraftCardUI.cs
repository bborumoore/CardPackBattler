using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Visual representation of a draft card in the UI
    /// Follows same structure as CardUI but for draft cards
    /// </summary>
    public class DraftCardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image card_image;
        public Image frame_image;
        public Image team_icon;
        public Image rarity_icon;
        
        public Text card_title;
        public Text card_text;
        
        [Header("Category Counts")]
        public Text general_count;
        public Text head_count;
        public Text body_count;
        public Text limb_count;
        public Text power_count;
        public Text knowledge_count;
        
        [Header("Drag Settings")]
        public bool can_drag = true;
        
        public UnityAction<DraftCardUI> onClick;
        public UnityAction<DraftCardUI> onClickRight;
        public UnityAction<DraftCardUI> onDragStart;
        public UnityAction<DraftCardUI> onDragEnd;

        private DraftCardData draft_card;
        private bool is_dragging = false;
        private Vector3 original_position;
        private Transform original_parent;
        private int original_sibling_index;
        private CanvasGroup canvas_group;

        void Awake()
        {
            canvas_group = GetComponent<CanvasGroup>();
            if (canvas_group == null)
                canvas_group = gameObject.AddComponent<CanvasGroup>();
        }

        public void SetCard(DraftCardData card)
        {
            if (card == null)
                return;

            this.draft_card = card;

            if (card_image != null)
                card_image.sprite = card.card_art;
            if (frame_image != null)
            {
                frame_image.sprite = card.card_frame;
                frame_image.color = card.card_color;
            }
            if (card_title != null)
                card_title.text = card.title.ToUpper();
            if (card_text != null)
                card_text.text = card.description;

            // Update category counts - show total cards that would be spawned
            if (general_count != null)
                general_count.text = GetPoolCardCount(card.general_pool).ToString();
            if (head_count != null)
                head_count.text = GetPoolCardCount(card.head_pool).ToString();
            if (body_count != null)
                body_count.text = GetPoolCardCount(card.body_pool).ToString();
            if (limb_count != null)
                limb_count.text = GetPoolCardCount(card.limb_pool).ToString();
            if (power_count != null)
                power_count.text = GetPoolCardCount(card.power_pool).ToString();
            if (knowledge_count != null)
                knowledge_count.text = GetPoolCardCount(card.knowledge_pool).ToString();

            // Hide team/rarity for draft cards if needed
            if (team_icon != null)
                team_icon.enabled = false;
            if (rarity_icon != null)
                rarity_icon.enabled = false;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        private int GetPoolCardCount(CardPoolCategory pool)
        {
            if (pool == null)
                return 0;
            
            int count = 0;
            if (pool.possible_deck_cards != null)
                count += pool.possible_deck_cards.Length;
            if (pool.possible_side_cards != null)
                count += pool.possible_side_cards.Length;
            return count;
        }

        public void SetMaterial(Material mat)
        {
            if (card_image != null)
                card_image.material = mat;
            if (frame_image != null)
                frame_image.material = mat;
            if (team_icon != null)
                team_icon.material = mat;
            if (rarity_icon != null)
                rarity_icon.material = mat;
        }

        public void SetOpacity(float opacity)
        {
            if (card_image != null)
                card_image.color = new Color(card_image.color.r, card_image.color.g, card_image.color.b, opacity);
            if (frame_image != null)
                frame_image.color = new Color(frame_image.color.r, frame_image.color.g, frame_image.color.b, opacity);
            if (team_icon != null)
                team_icon.color = new Color(team_icon.color.r, team_icon.color.g, team_icon.color.b, opacity);
            if (rarity_icon != null)
                rarity_icon.color = new Color(rarity_icon.color.r, rarity_icon.color.g, rarity_icon.color.b, opacity);
            if (card_title != null)
                card_title.color = new Color(card_title.color.r, card_title.color.g, card_title.color.b, opacity);
            if (card_text != null)
                card_text.color = new Color(card_text.color.r, card_text.color.g, card_text.color.b, opacity);
                
            // Category counts
            if (general_count != null)
                general_count.color = new Color(general_count.color.r, general_count.color.g, general_count.color.b, opacity);
            if (head_count != null)
                head_count.color = new Color(head_count.color.r, head_count.color.g, head_count.color.b, opacity);
            if (body_count != null)
                body_count.color = new Color(body_count.color.r, body_count.color.g, body_count.color.b, opacity);
            if (limb_count != null)
                limb_count.color = new Color(limb_count.color.r, limb_count.color.g, limb_count.color.b, opacity);
            if (power_count != null)
                power_count.color = new Color(power_count.color.r, power_count.color.g, power_count.color.b, opacity);
            if (knowledge_count != null)
                knowledge_count.color = new Color(knowledge_count.color.r, knowledge_count.color.g, knowledge_count.color.b, opacity);
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (is_dragging)
                return;
                
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (onClick != null)
                    onClick.Invoke(this);
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (onClickRight != null)
                    onClickRight.Invoke(this);
            }
        }

        // ----- Drag and Drop (Additional functionality for draft cards) -----

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!can_drag)
                return;

            is_dragging = true;
            original_position = transform.position;
            original_parent = transform.parent;
            original_sibling_index = transform.GetSiblingIndex();

            // Move to front
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                transform.SetParent(canvas.transform);
                transform.SetAsLastSibling();
            }

            // Make semi-transparent
            canvas_group.alpha = 0.75f;
            canvas_group.blocksRaycasts = false;

            if (onDragStart != null)
                onDragStart.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!can_drag || !is_dragging)
                return;

            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!can_drag || !is_dragging)
                return;

            is_dragging = false;

            // Check if dropped on valid slot
            DraftSlotUI slot = GetSlotUnderMouse(eventData);
            bool valid_drop = false;

            if (slot != null && slot.CanAcceptCard(draft_card))
            {
                // For now, just accept the drop visually
                // TODO: Integrate with GameClient when draft methods are added
                slot.SetCard(draft_card);
                valid_drop = true;
                Hide(); // Hide after successful play
            }

            if (!valid_drop)
            {
                // Return to original position
                transform.SetParent(original_parent);
                transform.SetSiblingIndex(original_sibling_index);
                transform.position = original_position;
            }

            // Reset visual state
            canvas_group.alpha = 1f;
            canvas_group.blocksRaycasts = true;

            if (onDragEnd != null)
                onDragEnd.Invoke(this);
        }

        private DraftSlotUI GetSlotUnderMouse(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                DraftSlotUI slot = result.gameObject.GetComponent<DraftSlotUI>();
                if (slot != null)
                    return slot;
            }

            return null;
        }

        public DraftCardData GetCard()
        {
            return draft_card;
        }

        public bool IsDragging()
        {
            return is_dragging;
        }
    }
}
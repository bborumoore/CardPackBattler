using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI player using the MinMax AI algorithm
    /// </summary>

    public class AIPlayerMM : AIPlayer
    {
        private AILogic ai_logic;

        private bool is_playing = false;

        public AIPlayerMM(GameLogic gameplay, int id, int level)
        {
            this.gameplay = gameplay;
            player_id = id;
            ai_level = Mathf.Clamp(level, 1, 10);
            ai_logic = AILogic.Create(id, ai_level);
        }

        public override void Update()
        {
            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);

             // NEW: Side deck selection
            if (!is_playing && game_data.phase == GamePhase.SideDeckSelection)
            {
                if (!player.side_deck_selected && player.HasSideCards())
                {
                    is_playing = true;
                    TimeTool.StartCoroutine(AiSelectSideDeck());
                }
            }

            if (!is_playing && game_data.IsPlayerTurn(player))
            {
                is_playing = true;
                TimeTool.StartCoroutine(AiTurn());
            }

            if (!is_playing && game_data.IsPlayerMulliganTurn(player))
            {
                SkipMulligan();
            }

            if (!game_data.IsPlayerTurn(player) && ai_logic.IsRunning())
                Stop();
        }

        // Add new coroutine for side deck AI decision:
        private IEnumerator AiSelectSideDeck()
        {
            yield return new WaitForSeconds(1f);

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);

            if (player.HasSideCards() && !player.side_deck_selected)
            {
                // Use AI logic to evaluate best side deck card
                Card best_card = EvaluateBestSideDeckCard(game_data, player);
                
                if (best_card != null)
                {
                    gameplay.SelectSideDeckCard(player, best_card);
                    Debug.Log("AI selected side deck card: " + best_card.CardData.title);
                }
            }

            is_playing = false;
        }

        // Evaluate which side deck card is best to select
        private Card EvaluateBestSideDeckCard(Game game_data, Player player)
        {
            if (player.cards_side.Count == 0)
                return null;

            Card best_card = null;
            float best_score = float.MinValue;

            // Evaluate each card in side deck
            foreach (Card card in player.cards_side)
            {
                float score = EvaluateSideDeckCard(game_data, player, card);
                
                if (score > best_score)
                {
                    best_score = score;
                    best_card = card;
                }
            }

            return best_card;
        }

        // Score a side deck card based on current game state
        private float EvaluateSideDeckCard(Game game_data, Player player, Card card)
        {
            float score = 0f;

            CardData data = card.CardData;
            Player opponent = game_data.GetOpponentPlayer(player.player_id);

            // Base value on card stats
            if (data.type == CardType.Character)
            {
                score += data.attack * 10f;
                score += data.hp * 8f;
            }

            // Value spells based on immediate impact
            if (data.type == CardType.Spell)
            {
                score += 50f; // Spells are generally good for flexibility
            }

            // Consider abilities
            if (data.abilities != null && data.abilities.Length > 0)
            {
                score += data.abilities.Length * 15f;
            }

            // Consider current hand size - if low, prefer drawing more cards
            if (player.cards_hand.Count < 3)
            {
                // Prefer cards that might help with board presence
                if (data.type == CardType.Character)
                    score += 20f;
            }

            // Consider board state
            if (opponent.cards_board.Count > player.cards_board.Count)
            {
                // Behind on board - prefer removal or strong characters
                if (data.type == CardType.Spell)
                    score += 30f;
                if (data.type == CardType.Character && data.attack >= 3)
                    score += 25f;
            }

            // Consider HP - if low, prefer defensive cards
            if (player.hp < player.hp_max / 3)
            {
                if (data.hp > 0)
                    score += data.hp * 5f;
            }

            // Add some randomness for variety
            score += Random.Range(-10f, 10f);

            return score;
        }

        private IEnumerator AiTurn()
        {
            yield return new WaitForSeconds(1f);

            Game game_data = gameplay.GetGameData();
            ai_logic.RunAI(game_data);

            while (ai_logic.IsRunning())
            {
                yield return new WaitForSeconds(0.1f);
            }

            AIAction best = ai_logic.GetBestAction();

            if (best != null)
            {
                Debug.Log("Execute AI Action: " + best.GetText(game_data) + "\n" + ai_logic.GetNodePath());
                //foreach (NodeState node in ai_logic.GetFirst().childs)
                //   Debug.Log(ai_logic.GetNodePath(node));

                ExecuteAction(best);
            }

            ai_logic.ClearMemory();

            yield return new WaitForSeconds(0.5f);
            is_playing = false;
        }

        private void Stop()
        {
            ai_logic.Stop();
            is_playing = false;
        }

        //----------

        private void ExecuteAction(AIAction action)
        {
            if (!CanPlay())
                return;

            if (action.type == GameAction.PlayCard)
            {
                PlayCard(action.card_uid, action.slot);
            }

            if (action.type == GameAction.Attack)
            {
                AttackCard(action.card_uid, action.target_uid);
            }

            if (action.type == GameAction.AttackPlayer)
            {
                AttackPlayer(action.card_uid, action.target_player_id);
            }

            if (action.type == GameAction.Move)
            {
                MoveCard(action.card_uid, action.slot);
            }

            if (action.type == GameAction.CastAbility)
            {
                CastAbility(action.card_uid, action.ability_id);
            }

            if (action.type == GameAction.SelectCard)
            {
                SelectCard(action.target_uid);
            }

            if (action.type == GameAction.SelectPlayer)
            {
                SelectPlayer(action.target_player_id);
            }

            if (action.type == GameAction.SelectSlot)
            {
                SelectSlot(action.slot);
            }

            if (action.type == GameAction.SelectChoice)
            {
                SelectChoice(action.value);
            }

            if (action.type == GameAction.SelectCost)
            {
                SelectCost(action.value);
            }

            if (action.type == GameAction.SelectMulligan)
            {
                SkipMulligan();
            }

            if (action.type == GameAction.CancelSelect)
            {
                CancelSelect();
            }

            if (action.type == GameAction.EndTurn)
            {
                EndTurn();
            }

            if (action.type == GameAction.Resign)
            {
                Resign();
            }
        }

        private void PlayCard(string card_uid, Slot slot)
        {
            Game game_data = gameplay.GetGameData();
            Card card = game_data.GetCard(card_uid);
            if (card != null)
            {
                gameplay.PlayCard(card, slot);
            }
        }

        private void MoveCard(string card_uid, Slot slot)
        {
            Game game_data = gameplay.GetGameData();
            Card card = game_data.GetCard(card_uid);
            if (card != null)
            {
                gameplay.MoveCard(card, slot); 
            }
        }

        private void AttackCard(string attacker_uid, string target_uid)
        {
            Game game_data = gameplay.GetGameData();
            Card card = game_data.GetCard(attacker_uid);
            Card target = game_data.GetCard(target_uid);
            if (card != null && target != null)
            {
                gameplay.AttackTarget(card, target);
            }
        }

        private void AttackPlayer(string attacker_uid, int target_player_id)
        {
            Game game_data = gameplay.GetGameData();
            Card card = game_data.GetCard(attacker_uid);
            if (card != null)
            {
                Player oplayer = game_data.GetPlayer(target_player_id);
                gameplay.AttackPlayer(card, oplayer);
            }
        }

        private void CastAbility(string caster_uid, string ability_id)
        {
            Game game_data = gameplay.GetGameData();
            Card caster = game_data.GetCard(caster_uid);
            AbilityData iability = AbilityData.Get(ability_id);
            if (caster != null && iability != null)
            {
                gameplay.CastAbility(caster, iability);
            }
        }

        private void SelectCard(string target_uid)
        {
            Game game_data = gameplay.GetGameData();
            Card target = game_data.GetCard(target_uid);
            if (target != null)
            {
                gameplay.SelectCard(target);
            }
        }

        private void SelectPlayer(int tplayer_id)
        {
            Game game_data = gameplay.GetGameData();
            Player target = game_data.GetPlayer(tplayer_id);
            if (target != null)
            {
                gameplay.SelectPlayer(target);
            }
        }

        private void SelectSlot(Slot slot)
        {
            if (slot != Slot.None)
            {
                gameplay.SelectSlot(slot);
            }
        }

        private void SelectChoice(int choice)
        {
            gameplay.SelectChoice(choice);
        }

        private void SelectCost(int cost)
        {
            gameplay.SelectCost(cost);
        }

        private void CancelSelect()
        {
            if (CanPlay())
            {
                gameplay.CancelSelection();
            }
        }

        private void SkipMulligan()
        {
            string[] cards = new string[0]; //Don't mulligan
            SelectMulligan(cards);
        }

        private void SelectMulligan(string[] cards)
        {
            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            gameplay.Mulligan(player, cards);
        }

        private void EndTurn()
        {
            if (CanPlay())
            {
                gameplay.EndTurn();
            }
        }

        private void Resign()
        {
            int other = player_id == 0 ? 1 : 0;
            gameplay.EndGame(other);
        }

    }

}
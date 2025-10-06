using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Extensions to the Game class for draft functionality
    /// These methods will be properly integrated into the game logic later
    /// </summary>
    public static class DraftGameExtensions
    {
        /// <summary>
        /// Check if a draft card can be played in a specific slot
        /// </summary>
        public static bool CanPlayDraftCard(this Game game, DraftCardData card, DraftSlotType slot)
        {
            // TODO: Implement full validation logic
            // For now, just check basic conditions
            
            if (game == null || card == null)
                return false;
                
            if (game.phase != GamePhase.Draft)
                return false;
            
            // Add more validation as needed:
            // - Check if it's the player's turn
            // - Check if player has already played max cards this turn
            // - Check if slot is empty
            // - etc.
            
            return true;
        }
        
        /// <summary>
        /// Get the current player's draft hand
        /// </summary>
        public static List<DraftCardData> GetDraftHand(this Game game, int player_id)
        {
            // TODO: This will need to be stored in Player or Game data
            // For now, return empty list
            return new List<DraftCardData>();
        }
        
        /// <summary>
        /// Check if we're in draft phase
        /// </summary>
        public static bool IsInDraftPhase(this Game game)
        {
            return game != null && game.phase == GamePhase.Draft;
        }
        
        /// <summary>
        /// Get draft slots for a player
        /// </summary>
        public static Dictionary<DraftSlotType, DraftCardData> GetPlayerDraftSlots(this Game game, int player_id)
        {
            // TODO: This will need to be stored in Player or Game data
            // For now, return empty dictionary
            return new Dictionary<DraftSlotType, DraftCardData>();
        }
        
        /// <summary>
        /// Check how many draft cards a player has played
        /// </summary>
        public static int GetDraftCardsPlayed(this Game game, int player_id)
        {
            // TODO: Track this in game state
            return 0;
        }
        
        /// <summary>
        /// Check how many draft cards the current player should play this turn
        /// Based on the 1-2-2-2-2-1 pattern
        /// </summary>
        public static int GetDraftCardsToPlayThisTurn(this Game game)
        {
            // TODO: Implement the 1-2-2-2-2-1 pattern logic
            // This will depend on tracking draft turn count
            return 1;
        }
    }
}
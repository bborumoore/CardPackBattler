﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// Contain UserData retrieved from the web api database
    /// </summary>

    [System.Serializable]
    public class UserData
    {
        public string id;
        public string username;

        public string email;
        public string avatar;
        public string cardback;
        public int permission_level = 1;
        public int validation_level = 1;

        public int coins;
        public int xp;
        public int elo;

        public int matches;
        public int victories;
        public int defeats;

        public UserCardData[] cards;
        public UserCardData[] packs;
        public UserDeckData[] decks;
        public string[] rewards;
        public string[] avatars;
        public string[] cardbacks;
        public string[] friends;

        public UserData()
        {
            cards = new UserCardData[0];
            packs = new UserCardData[0];
            decks = new UserDeckData[0];
            rewards = new string[0];
            avatars = new string[0];
            cardbacks = new string[0];
            friends = new string[0];
            permission_level = 1;
            coins = 10000;
            elo = 1000;
        }

        public int GetLevel()
        {
            return Mathf.FloorToInt(xp / 1000) + 1;
        }

        public string GetAvatar()
        {
            if (avatar != null)
                return avatar;
            return "";
        }

        public string GetCardback()
        {
            if (cardback != null)
                return cardback;
            return "";
        }

        public void SetDeck(UserDeckData deck)
        {
            for(int i=0; i<decks.Length; i++)
            {
                if (decks[i].tid == deck.tid)
                {
                    decks[i] = deck;
                    return;
                }
            }

            //Not found
            List<UserDeckData> ldecks = new List<UserDeckData>(decks);
            ldecks.Add(deck);
            this.decks = ldecks.ToArray();
        }

        public UserDeckData GetDeck(string tid)
        {
            foreach (UserDeckData deck in decks)
            {
                if (deck.tid == tid)
                    return deck;
            }
            return null;
        }

        public UserCardData GetCard(string tid, string variant)
        {
            foreach (UserCardData card in cards)
            {
                if (card.tid == tid && card.variant == variant)
                    return card;
            }
            return null;
        }

        public int GetCardQuantity(CardData card, VariantData variant)
        {
            return GetCardQuantity(card.id, variant.id, variant.is_default);
        }

        public int GetCardQuantity(string tid, string variant, bool default_variant = false)
        {
            if (cards == null)
                return 0;

            foreach (UserCardData card in cards)
            {
                if (card.tid == tid && card.variant == variant)
                    return card.quantity;
                if (card.tid == tid && card.variant == "" && default_variant)
                    return card.quantity;
            }
            return 0;
        }

        public UserCardData GetPack(string tid)
        {
            foreach (UserCardData pack in packs)
            {
                if (pack.tid == tid)
                    return pack;
            }
            return null;
        }

        public int GetPackQuantity(string tid)
        {
            if (packs == null)
                return 0;

            foreach (UserCardData pack in packs)
            {
                if (pack.tid == tid)
                    return pack.quantity;
            }
            return 0;
        }

        public int CountUniqueCards()
        {
            if (cards == null)
                return 0;

            HashSet<string> unique_cards = new HashSet<string>();
            foreach (UserCardData card in cards)
            {
                if (!unique_cards.Contains(card.tid))
                    unique_cards.Add(card.tid);
            }
            return unique_cards.Count;
        }

        public int CountCardType(VariantData variant)
        {
            int value = 0;
            foreach (UserCardData card in cards)
            {
                if (card.variant == variant.id)
                    value += 1;
            }
            return value;
        }

        public void AddCard(string tid, string variant, int quantity = 1)
        {
            UserCardData ucard = GetCard(tid, variant);
            if (ucard != null)
            {
                ucard.quantity += quantity;
            }
            else
            {
                UserCardData ncard = new UserCardData(tid, variant);
                ncard.quantity = quantity;
                List<UserCardData> acards = new List<UserCardData>(cards);
                acards.Add(ncard);
                cards = acards.ToArray();
            }
        }

        public void RemoveCard(string tid, string variant, int quantity = 1)
        {
            UserCardData ucard = GetCard(tid, variant);
            if (ucard != null)
            {
                ucard.quantity -= quantity;
                if (ucard.quantity <= 0)
                {
                    List<UserCardData> acards = new List<UserCardData>(cards);
                    acards.Remove(ucard);
                    cards = acards.ToArray();
                }
            }
        }

        public void AddPack(string tid, int quantity = 1)
        {
            UserCardData ucard = GetPack(tid);
            if (ucard != null)
            {
                ucard.quantity += quantity;
            }
            else
            {
                UserCardData ncard = new UserCardData();
                ncard.tid = tid;
                ncard.quantity = quantity;
                List<UserCardData> acards = new List<UserCardData>(packs);
                acards.Add(ncard);
                packs = acards.ToArray();
            }
        }

        public void RemovePack(string tid, int quantity = 1)
        {
            UserCardData ucard = GetPack(tid);
            if (ucard != null)
            {
                ucard.quantity -= quantity;
                if (ucard.quantity <= 0)
                {
                    List<UserCardData> acards = new List<UserCardData>(packs);
                    acards.Remove(ucard);
                    packs = acards.ToArray();
                }
            }
        }

        public void AddCoins(int value)
        {
            coins += value;
        }

        public void RemoveCoins(int value)
        {
            coins -= value;
        }

        public void AddXP(int value)
        {
            xp += value;
        }

        public void AddAvatar(string avatar_tid)
        {
            if (!HasAvatar(avatar_tid))
            {
                List<string> aavat = new List<string>(avatars);
                aavat.Add(avatar_tid);
                avatars = aavat.ToArray();
            }
        }

        public void AddCardback(string cardback_tid)
        {
            if (!HasCardback(cardback_tid))
            {
                List<string> acards = new List<string>(cardbacks);
                acards.Add(cardback_tid);
                cardbacks = acards.ToArray();
            }
        }

        public void AddCardFromPack(PackData pack)
        {
            if (pack != null)
            {
                // Get all cards available in this pack
                List<CardData> all_cards = CardData.GetAll(pack);
                
                if (all_cards.Count > 0)
                {
                    // Select a random card from the pack
                    int rand = Random.Range(0, all_cards.Count);
                    CardData selectedCard = all_cards[rand];
                    
                    // Create UserCardData from the selected CardData
                    UserCardData ncard = new UserCardData();
                    ncard.tid = selectedCard.id;
                    ncard.variant = VariantData.GetDefault().id;
                    ncard.quantity = 1;
                    
                    List<UserCardData> acards = new List<UserCardData>(cards);
                    acards.Add(ncard);
                    cards = acards.ToArray();
                }
            }
        }

        public void AddReward(string tid)
        {
            if (!HasReward(tid))
            {
                List<string> arewards = new List<string>(rewards);
                arewards.Add(tid);
                rewards = arewards.ToArray();
            }
        }

        public bool HasCard(string card_tid, string variant, int quantity = 1)
        {
            foreach (UserCardData card in cards)
            {
                if (card.tid == card_tid && card.variant == variant && card.quantity >= quantity)
                    return true;
            }
            return false;
        }

        public bool HasPack(string pack_tid, int quantity=1)
        {
            foreach (UserCardData pack in packs)
            {
                if (pack.tid == pack_tid && pack.quantity >= quantity)
                    return true;
            }
            return false;
        }
		
		public bool HasAvatar(string avatar_tid)
		{
			return avatars.Contains(avatar_tid);
		}

		public bool HasCardback(string cardback_tid)
		{
			return cardbacks.Contains(cardback_tid);
		}

        public bool HasReward(string reward_id)
        {
            foreach (string reward in rewards)
            {
                if (reward == reward_id)
                    return true;
            }
            return false;
        }

        public string GetCoinsString()
        {
            return coins.ToString();
        }

        public bool HasFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            return flist.Contains(username);
        }

        public void AddFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            if (!flist.Contains(username))
                flist.Add(username);
            friends = flist.ToArray();
        }

        public void RemoveFriend(string username)
        {
            List<string> flist = new List<string>(friends);
            if (flist.Contains(username))
                flist.Remove(username);
            friends = flist.ToArray();
        }

        public bool HasDeckCards(UserDeckData deck)
        {
            foreach (UserCardData card in deck.cards)
            {
                bool default_variant = true; //Count "" variant as valid for compatibility with older vers
                if (GetCardQuantity(card.tid, card.variant, default_variant) < card.quantity)
                    return false;
            }

            return true;
        }

        public bool IsDeckValid(UserDeckData deck)
        {
            if (Authenticator.Get().IsApi())
                return HasDeckCards(deck) && deck.IsValid();
            return deck.IsValid();
        }

        public void AddDeck(UserDeckData deck)
        {
            List<UserDeckData> udecks = new List<UserDeckData>(decks);
            udecks.Add(deck);
            decks = udecks.ToArray();

            foreach (UserCardData card in deck.cards)
            {
                AddCard(card.tid, card.variant, card.quantity);
            }
            
            // Also add side deck cards to collection
            if (deck.side_cards != null)
            {
                foreach (UserCardData card in deck.side_cards)
                {
                    AddCard(card.tid, card.variant, card.quantity);
                }
            }
        }
    }

    [System.Serializable]
    public class UserDeckData : INetworkSerializable
    {
        public string tid;
        public string title;
        public UserCardData hero;
        public UserCardData[] cards;
        public UserCardData[] side_cards;  // NEW: Side deck cards

        public UserDeckData() 
        {
            side_cards = new UserCardData[0];  // Initialize
        }

        public UserDeckData(string tid, string title)
        {
            this.tid = tid;
            this.title = title;
            hero = new UserCardData();
            cards = new UserCardData[0];
            side_cards = new UserCardData[0];  // Initialize
        }

        public UserDeckData(DeckData deck)
        {
            tid = deck.id;
            title = deck.title;
            hero = new UserCardData(deck.hero, VariantData.GetDefault());
            cards = new UserCardData[deck.cards.Length];
            for (int i = 0; i < deck.cards.Length; i++)
            {
                cards[i] = new UserCardData(deck.cards[i], VariantData.GetDefault());
            }
            
            // Initialize side deck - will be empty array if DeckData doesn't have side_cards field yet
            side_cards = new UserCardData[0];
        }

        public int GetQuantity()
        {
            int count = 0;
            foreach (UserCardData card in cards)
                count += card.quantity;
            return count;
        }

        public int GetSideQuantity()
        {
            int count = 0;
            if (side_cards != null)
            {
                foreach (UserCardData card in side_cards)
                    count += card.quantity;
            }
            return count;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(tid) && !string.IsNullOrWhiteSpace(title) && GetQuantity() >= GameplayData.Get().deck_size;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tid);
            serializer.SerializeValue(ref title);
            serializer.SerializeValue(ref hero);
            NetworkTool.NetSerializeArray(serializer, ref cards);
            NetworkTool.NetSerializeArray(serializer, ref side_cards);  // NEW: Serialize side deck
        }

        public static UserDeckData Default
        {
            get
            {
                UserDeckData deck = new UserDeckData();
                deck.tid = "";
                deck.title = "";
                deck.hero = new UserCardData();
                deck.cards = new UserCardData[0];
                deck.side_cards = new UserCardData[0];  // NEW: Initialize
                return deck;
            }
        }
    }

    [System.Serializable]
    public class UserCardData : INetworkSerializable
    {
        public string tid;
        public string variant;
        public int quantity;

        public UserCardData() { tid = ""; variant = ""; quantity = 1; }
        public UserCardData(string id, string v) { tid = id; variant = v; quantity = 1; }
        public UserCardData(CardData card, VariantData variant) 
        {
            this.tid = card != null ? card.id : "";
            this.variant = variant != null ? variant.id : "";
            this.quantity = 1;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tid);
            serializer.SerializeValue(ref variant);
            serializer.SerializeValue(ref quantity);
        }
    }
}
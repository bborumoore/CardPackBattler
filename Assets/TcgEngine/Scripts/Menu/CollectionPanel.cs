using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// CollectionPanel is the panel where players can see all the cards they own
    /// Also the panel where they can use the deckbuilder (including side deck)
    /// </summary>

    public class CollectionPanel : UIPanel
    {
        [Header("Cards")]
        public ScrollRect scroll_rect;
        public RectTransform scroll_content;
        public CardGrid grid_content;
        public GameObject card_prefab;

        [Header("Left Side")]
        public IconButton[] team_filters;
        public Toggle toggle_owned;
        public Toggle toggle_not_owned;

        public Toggle toggle_character;
        public Toggle toggle_spell;
        public Toggle toggle_artifact;
        public Toggle toggle_equipment;
        public Toggle toggle_secret;

        public Toggle toggle_common;
        public Toggle toggle_uncommon;
        public Toggle toggle_rare;
        public Toggle toggle_mythic;

        public Toggle toggle_foil;

        public Dropdown sort_dropdown;
        public InputField search;

        [Header("Right Side")]
        public UIPanel deck_list_panel;
        public UIPanel card_list_panel;
        public DeckLine[] deck_lines;

        [Header("Deckbuilding - Main Deck")]
        public InputField deck_title;
        public Text deck_quantity;
        public GameObject deck_cards_prefab;
        public RectTransform deck_content;
        public GridLayoutGroup deck_grid;
        public IconButton[] hero_powers;

        [Header("Deckbuilding - Side Deck")]
        public Text side_deck_title;
        public Text side_deck_quantity;
        public GameObject side_deck_cards_prefab;
        public RectTransform side_deck_content;
        public GridLayoutGroup side_deck_grid;

        private TeamData filter_team = null;
        private int filter_dropdown = 0;
        private string filter_search = "";

        private List<CollectionCard> card_list = new List<CollectionCard>();
        private List<CollectionCard> all_list = new List<CollectionCard>();
        private List<DeckLine> deck_card_lines = new List<DeckLine>();
        private List<DeckLine> side_deck_card_lines = new List<DeckLine>();

        private string current_deck_tid;
        private bool editing_deck = false;
        private bool saving = false;
        private bool spawned = false;
        private bool update_grid = false;
        private float update_grid_timer = 0f;

        private List<UserCardData> deck_cards = new List<UserCardData>();
        private List<UserCardData> side_deck_cards = new List<UserCardData>();

        private static CollectionPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            //Delete grid content
            for (int i = 0; i < grid_content.transform.childCount; i++)
                Destroy(grid_content.transform.GetChild(i).gameObject);
        }

        protected override void Start()
        {
            base.Start();

            toggle_owned.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            toggle_not_owned.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });

            toggle_character.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            toggle_spell.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            toggle_artifact.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            toggle_equipment.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            toggle_secret.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });

            toggle_common.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            toggle_uncommon.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            toggle_rare.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            toggle_mythic.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });

            toggle_foil.onValueChanged.AddListener((bool val) => { OnChangeToggle(); });
            sort_dropdown.onValueChanged.AddListener((int val) => { OnChangeDropdown(); });
            search.onValueChanged.AddListener((string val) => { OnChangeSearch(); });

            foreach (IconButton btn in team_filters)
                btn.onClick += OnClickTeam;

            foreach (IconButton btn in hero_powers)
                btn.onClick += (IconButton b) => { RefreshDeckCards(); };

            RefreshAll();
        }

        protected override void Update()
        {
            base.Update();

            update_grid_timer += Time.deltaTime;
            if (update_grid && update_grid_timer > 0.2f)
            {
                grid_content.GetColumnAndRow(out int rows, out int cols);
                if (cols > 0)
                {
                    float row_height = grid_content.GetGrid().cellSize.y + grid_content.GetGrid().spacing.y;
                    float height = rows * row_height;
                    scroll_content.sizeDelta = new Vector2(scroll_content.sizeDelta.x, height + 100);
                    update_grid = false;
                }
            }
        }

        private void SpawnCards()
        {
            spawned = true;
            foreach (CollectionCard card in all_list)
                Destroy(card.gameObject);
            all_list.Clear();

            foreach (VariantData variant in VariantData.GetAll())
            {
                foreach (CardData card in CardData.GetAll())
                {
                    GameObject nCard = Instantiate(card_prefab, grid_content.transform);
                    CollectionCard dCard = nCard.GetComponent<CollectionCard>();
                    dCard.SetCard(card, variant, 0);
                    dCard.onClick += OnClickCard;
                    dCard.onClickRight += OnClickCardRight;
                    all_list.Add(dCard);
                    nCard.SetActive(false);
                }
            }
        }

        //----- Reload User Data ---------------

        public async void ReloadUser()
        {
            await Authenticator.Get().LoadUserData();
            MainMenu.Get().RefreshDeckList();
            RefreshCardsQuantities();

            if (!editing_deck)
                RefreshDeckList();
        }

        public async void ReloadUserCards()
        {
            await Authenticator.Get().LoadUserData();
            RefreshCardsQuantities();
        }

        public async void ReloadUserDecks()
        {
            await Authenticator.Get().LoadUserData();
            MainMenu.Get().RefreshDeckList();
            RefreshDeckList();
        }

        //----- Refresh UI --------

        private void RefreshAll()
        {
            RefreshFilters();
            RefreshCards();
            RefreshDeckList();
            RefreshStarterDeck();
        }

        private void RefreshFilters()
        {
            search.text = "";
            sort_dropdown.value = 0;
            foreach (IconButton button in team_filters)
                button.Deactivate();

            filter_team = null;
            filter_dropdown = 0;
            filter_search = "";
        }

        private void ShowDeckList()
        {
            deck_list_panel.Show();
            card_list_panel.Hide();
            editing_deck = false;
        }

        private void ShowDeckCards()
        {
            deck_list_panel.Hide();
            card_list_panel.Show();
        }
        
        public void RefreshCards()
        {
            if (!spawned)
                SpawnCards();

            card_list.Clear();

            UserData udata = Authenticator.Get().UserData;
            if (udata == null)
                return;

            VariantData variant = VariantData.GetDefault();
            VariantData special = VariantData.GetSpecial();
            if (toggle_foil.isOn && special != null)
                variant = special;

            List<CardDataQ> all_cards = new List<CardDataQ>();
            List<CardDataQ> shown_cards = new List<CardDataQ>();

            foreach (CardData icard in CardData.GetAll())
            {
                CardDataQ card = new CardDataQ();
                card.card = icard;
                card.variant = variant;
                card.quantity = udata.GetCardQuantity(icard, variant);
                all_cards.Add(card);
            }

            if (filter_dropdown == 0) //Name
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return a.card.title.CompareTo(b.card.title); });
            if (filter_dropdown == 1) //Attack
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return b.card.attack == a.card.attack ? b.card.hp.CompareTo(a.card.hp) : b.card.attack.CompareTo(a.card.attack); });
            if (filter_dropdown == 2) //hp
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return b.card.hp == a.card.hp ? b.card.attack.CompareTo(a.card.attack) : b.card.hp.CompareTo(a.card.hp); });
            if (filter_dropdown == 3) //Cost (note: mana removed but keeping this for compatibility)
                all_cards.Sort((CardDataQ a, CardDataQ b) => { return a.card.title.CompareTo(b.card.title); });

            foreach (CardDataQ card in all_cards)
            {
                if (card.card.IsDeckbuilding())
                {
                    CardData icard = card.card;
                    if (filter_team == null || filter_team == icard.team)
                    {
                        bool owned = IsCardOwned(udata, icard, card.variant, 1);
                        RarityData rarity = icard.rarity;
                        CardType type = icard.type;

                        bool owned_check = (owned && toggle_owned.isOn)
                            || (!owned && toggle_not_owned.isOn)
                            || toggle_owned.isOn == toggle_not_owned.isOn;

                        bool type_check = (type == CardType.Character && toggle_character.isOn)
                            || (type == CardType.Spell && toggle_spell.isOn)
                            || (type == CardType.Artifact && toggle_artifact.isOn)
                            || (type == CardType.Equipment && toggle_equipment.isOn)
                            || (type == CardType.Secret && toggle_secret.isOn)
                            || (!toggle_character.isOn && !toggle_spell.isOn && !toggle_artifact.isOn && !toggle_equipment.isOn && !toggle_secret.isOn);

                        bool rarity_check = (rarity.rank == 1 && toggle_common.isOn)
                            || (rarity.rank == 2 && toggle_uncommon.isOn)
                            || (rarity.rank == 3 && toggle_rare.isOn)
                            || (rarity.rank == 4 && toggle_mythic.isOn)
                            || (!toggle_common.isOn && !toggle_uncommon.isOn && !toggle_rare.isOn && !toggle_mythic.isOn);

                        string search = filter_search.ToLower();
                        bool search_check = string.IsNullOrEmpty(search) || icard.title.ToLower().Contains(search);

                        if (owned_check && type_check && rarity_check && search_check)
                        {
                            shown_cards.Add(card);
                        }
                    }
                }
            }

            //Activate cards in list
            int index = 0;
            foreach (CollectionCard card in all_list)
            {
                bool found = false;
                if (index < shown_cards.Count && card.GetCard() == shown_cards[index].card && card.GetVariant() == shown_cards[index].variant)
                {
                    card.SetQuantity(shown_cards[index].quantity);
                    card.gameObject.SetActive(true);
                    card_list.Add(card);
                    found = true;
                    index++;
                }
                if (!found)
                    card.gameObject.SetActive(false);
            }

            update_grid = true;
            update_grid_timer = 0f;
        }

        private void RefreshCardsQuantities()
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata == null)
                return;

            foreach (CollectionCard card in card_list)
            {
                int quantity = udata.GetCardQuantity(card.GetCard(), card.GetVariant());
                card.SetQuantity(quantity);
            }
        }

        private void RefreshDeckList()
        {
            foreach (DeckLine line in deck_lines)
                line.Hide();
            deck_cards.Clear();
            side_deck_cards.Clear();
            editing_deck = false;
            saving = false;

            UserData udata = Authenticator.Get().UserData;
            if (udata == null)
                return;

            int index = 0;
            foreach (UserDeckData deck in udata.decks)
            {
                if (index < deck_lines.Length)
                {
                    DeckLine line = deck_lines[index];
                    line.SetLine(udata, deck);
                }
                index++;
            }

            if (index < deck_lines.Length)
            {
                DeckLine line = deck_lines[index];
                line.SetLine("+");
            }
            RefreshCardsQuantities();
        }

        private void RefreshDeck(UserDeckData deck)
        {
            deck_title.text = "Deck Name";
            current_deck_tid = GameTool.GenerateRandomID(7);
            deck_cards.Clear();
            side_deck_cards.Clear();
            saving = false;
            editing_deck = true;

            foreach (IconButton btn in hero_powers)
                btn.Deactivate();

            if (deck != null)
            {
                deck_title.text = deck.title;
                current_deck_tid = deck.tid;

                foreach (IconButton btn in hero_powers)
                {
                    if (deck.hero != null && btn.value == deck.hero.tid)
                        btn.Activate();
                }
                
                for (int i = 0; i < deck.cards.Length; i++)
                {
                    CardData card = CardData.Get(deck.cards[i].tid);
                    VariantData variant = VariantData.Get(deck.cards[i].variant);
                    if (card != null && variant != null)
                    {
                        AddDeckCard(card, variant, deck.cards[i].quantity);
                    }
                }

                // Load side deck
                if (deck.side_cards != null)
                {
                    for (int i = 0; i < deck.side_cards.Length; i++)
                    {
                        CardData card = CardData.Get(deck.side_cards[i].tid);
                        VariantData variant = VariantData.Get(deck.side_cards[i].variant);
                        if (card != null && variant != null)
                        {
                            AddSideDeckCard(card, variant, deck.side_cards[i].quantity);
                        }
                    }
                }
            }

            RefreshDeckCards();
        }

        private void RefreshDeckCards()
        {
            // Main Deck
            foreach (DeckLine line in deck_card_lines)
                line.Hide();

            List<CardDataQ> list = new List<CardDataQ>();
            foreach (UserCardData card in deck_cards)
            {
                CardDataQ acard = new CardDataQ();
                acard.card = CardData.Get(card.tid);
                acard.variant = VariantData.Get(card.variant);
                acard.quantity = card.quantity;
                list.Add(acard);
            }
            list.Sort((CardDataQ a, CardDataQ b) => { return a.card.title.CompareTo(b.card.title); });

            UserData udata = Authenticator.Get().UserData;
            int index = 0;
            int count = 0;
            foreach (CardDataQ card in list)
            {
                if (index >= deck_card_lines.Count)
                    CreateDeckCard();

                if (index < deck_card_lines.Count)
                {
                    DeckLine line = deck_card_lines[index];
                    if (line != null)
                    {
                        line.SetLine(card.card, card.variant, card.quantity, !IsCardOwned(udata, card.card, card.variant, card.quantity));
                        count += card.quantity;
                    }
                }
                index++;
            }

            deck_quantity.text = count + "/" + GameplayData.Get().deck_size;
            deck_quantity.color = count >= GameplayData.Get().deck_size ? Color.white : Color.red;

            // Side Deck
            foreach (DeckLine line in side_deck_card_lines)
                line.Hide();

            List<CardDataQ> side_list = new List<CardDataQ>();
            foreach (UserCardData card in side_deck_cards)
            {
                CardDataQ acard = new CardDataQ();
                acard.card = CardData.Get(card.tid);
                acard.variant = VariantData.Get(card.variant);
                acard.quantity = card.quantity;
                side_list.Add(acard);
            }
            side_list.Sort((CardDataQ a, CardDataQ b) => { return a.card.title.CompareTo(b.card.title); });

            int side_index = 0;
            int side_count = 0;
            foreach (CardDataQ card in side_list)
            {
                if (side_index >= side_deck_card_lines.Count)
                    CreateSideDeckCard();

                if (side_index < side_deck_card_lines.Count)
                {
                    DeckLine line = side_deck_card_lines[side_index];
                    if (line != null)
                    {
                        line.SetLine(card.card, card.variant, card.quantity, !IsCardOwned(udata, card.card, card.variant, card.quantity));
                        side_count += card.quantity;
                    }
                }
                side_index++;
            }

            if (side_deck_quantity != null)
            {
                side_deck_quantity.text = side_count + "/" + GameplayData.Get().side_deck_size;
                side_deck_quantity.color = side_count == GameplayData.Get().side_deck_size ? Color.white : Color.yellow;
            }

            RefreshCardsQuantities();
        }

        private void RefreshStarterDeck()
        {
            UserData udata = Authenticator.Get().UserData;
            if (udata != null && (udata.cards.Length == 0 || udata.rewards.Length == 0))
            {
                if (GameplayData.Get().starter_decks.Length > 0)
                {
                    StarterDeckPanel.Get().Show();
                }
            }
        }

        //-------- Deck editing actions

        private void CreateDeckCard()
        {
            GameObject deck_line = Instantiate(deck_cards_prefab, deck_grid.transform);
            DeckLine line = deck_line.GetComponent<DeckLine>();
            deck_card_lines.Add(line);
            float height = deck_card_lines.Count * 70f + 20f;
            deck_content.sizeDelta = new Vector2(deck_content.sizeDelta.x, height);
            line.onClick += OnClickCardLine;
            line.onClickRight += OnRightClickCardLine;
        }

        private void CreateSideDeckCard()
        {
            if (side_deck_cards_prefab == null || side_deck_grid == null)
                return;

            GameObject deck_line = Instantiate(side_deck_cards_prefab, side_deck_grid.transform);
            DeckLine line = deck_line.GetComponent<DeckLine>();
            side_deck_card_lines.Add(line);
            
            if (side_deck_content != null)
            {
                float height = side_deck_card_lines.Count * 70f + 20f;
                side_deck_content.sizeDelta = new Vector2(side_deck_content.sizeDelta.x, height);
            }

            line.onClick += OnClickSideCardLine;
            line.onClickRight += OnRightClickSideCardLine;
        }

        private void AddDeckCard(CardData card, VariantData variant, int quantity = 1)
        {
            AddDeckCard(card.id, variant.id, quantity);
        }

        private void RemoveDeckCard(CardData card, VariantData variant)
        {
            RemoveDeckCard(card.id, variant.id);
        }

        private void AddDeckCard(string tid, string variant, int quantity = 1)
        {
            UserCardData ucard = GetDeckCard(tid, variant);
            if (ucard != null)
            {
                ucard.quantity += quantity;
            }
            else
            {
                ucard = new UserCardData(tid, variant);
                ucard.quantity = quantity;
                deck_cards.Add(ucard);
            }
        }

        private void RemoveDeckCard(string tid, string variant)
        {
            for (int i = deck_cards.Count - 1; i >= 0; i--)
            {
                UserCardData ucard = deck_cards[i];
                if (ucard.tid == tid && ucard.variant == variant)
                {
                    ucard.quantity--;

                    if(ucard.quantity <= 0)
                        deck_cards.RemoveAt(i);
                }
            }
        }

        private UserCardData GetDeckCard(string tid, string variant)
        {
            foreach (UserCardData ucard in deck_cards)
            {
                if (ucard.tid == tid && ucard.variant == variant)
                    return ucard;
            }
            return null;
        }

        // Side Deck Management
        private void AddSideDeckCard(CardData card, VariantData variant, int quantity = 1)
        {
            AddSideDeckCard(card.id, variant.id, quantity);
        }

        private void RemoveSideDeckCard(CardData card, VariantData variant)
        {
            RemoveSideDeckCard(card.id, variant.id);
        }

        private void AddSideDeckCard(string tid, string variant, int quantity = 1)
        {
            // Check if side deck is full
            int current_count = 0;
            foreach (UserCardData ucard in side_deck_cards)
                current_count += ucard.quantity;

            if (current_count >= GameplayData.Get().side_deck_size)
                return;

            UserCardData ucard_existing = GetSideDeckCard(tid, variant);
            if (ucard_existing != null)
            {
                ucard_existing.quantity += quantity;
            }
            else
            {
                UserCardData ucard = new UserCardData(tid, variant);
                ucard.quantity = quantity;
                side_deck_cards.Add(ucard);
            }
        }

        private void RemoveSideDeckCard(string tid, string variant)
        {
            for (int i = side_deck_cards.Count - 1; i >= 0; i--)
            {
                UserCardData ucard = side_deck_cards[i];
                if (ucard.tid == tid && ucard.variant == variant)
                {
                    ucard.quantity--;
                    if (ucard.quantity <= 0)
                        side_deck_cards.RemoveAt(i);
                }
            }
        }

        private UserCardData GetSideDeckCard(string tid, string variant)
        {
            foreach (UserCardData card in side_deck_cards)
            {
                if (card.tid == tid && card.variant == variant)
                    return card;
            }
            return null;
        }

        private void SaveDeck()
        {
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = new UserDeckData();
            udeck.tid = current_deck_tid;
            udeck.title = deck_title.text;
            udeck.hero = new UserCardData();
            udeck.hero.tid = GetSelectedHeroId();
            udeck.hero.variant = VariantData.GetDefault().id;
            udeck.cards = deck_cards.ToArray();
            udeck.side_cards = side_deck_cards.ToArray();
            saving = true;

            if (Authenticator.Get().IsTest())
                SaveDeckTest(udata, udeck);

            if (Authenticator.Get().IsApi())
                SaveDeckAPI(udata, udeck);

            ShowDeckList();
        }

        private async void SaveDeckTest(UserData udata, UserDeckData udeck)
        {
            udata.SetDeck(udeck);
            await Authenticator.Get().SaveUserData();
            ReloadUserDecks();
        }

        private async void SaveDeckAPI(UserData udata, UserDeckData udeck)
        {
            string url = ApiClient.ServerURL + "/users/deck/" + udeck.tid;
            string jdata = ApiTool.ToJson(udeck);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, jdata);
            UserDeckData[] decks = ApiTool.JsonToArray<UserDeckData>(res.data);
            saving = res.success;

            if (res.success && decks != null)
            {
                udata.decks = decks;
                await Authenticator.Get().SaveUserData();
                ReloadUserDecks();
            }
        }

        private async void DeleteDeck(string deck_tid)
        {
            UserData udata = Authenticator.Get().UserData;
            UserDeckData udeck = udata.GetDeck(deck_tid);
            List<UserDeckData> decks = new List<UserDeckData>(udata.decks);
            decks.Remove(udeck);
            udata.decks = decks.ToArray();

            if (Authenticator.Get().IsApi())
            {
                string url = ApiClient.ServerURL + "/users/deck/" + deck_tid;
                await ApiClient.Get().SendRequest(url, "DELETE", "");
            }

            await Authenticator.Get().SaveUserData();
            ReloadUserDecks();
        }

        //---- Left Panel Filters Clicks -----------

        public void OnClickTeam(IconButton button)
        {
            filter_team = null;
            if (button.IsActive())
            {
                foreach (TeamData team in TeamData.GetAll())
                {
                    if (button.value == team.id)
                        filter_team = team;
                }
            }
            RefreshCards();
        }

        public void OnChangeToggle()
        {
            RefreshCards();
        }

        public void OnChangeDropdown()
        {
            filter_dropdown = sort_dropdown.value;
            RefreshCards();
        }

        public void OnChangeSearch()
        {
            filter_search = search.text;
            RefreshCards();
        }

        //---- Card grid clicks ----------

        public void OnClickCard(CardUI card)
        {
            if (!editing_deck)
            {
                CardZoomPanel.Get().ShowCard(card.GetCard(), card.GetVariant());
                return;
            }

            CardData icard = card.GetCard();
            VariantData variant = card.GetVariant();
            if (icard != null)
            {
                // Check if shift is held to add to side deck
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    int in_side_deck = 0;
                    foreach (UserCardData ucard in side_deck_cards)
                    {
                        if (ucard.tid == icard.id)
                            in_side_deck += ucard.quantity;
                    }

                    UserData udata = Authenticator.Get().UserData;
                    bool owner = IsCardOwned(udata, card.GetCard(), card.GetVariant(), in_side_deck + 1);

                    if (owner)
                    {
                        AddSideDeckCard(icard, variant);
                        RefreshDeckCards();
                    }
                }
                else
                {
                    // Normal main deck add
                    int in_deck = CountDeckCards(icard, variant);
                    int in_deck_same = CountDeckCards(icard);
                    UserData udata = Authenticator.Get().UserData;

                    bool owner = IsCardOwned(udata, card.GetCard(), card.GetVariant(), in_deck + 1);
                    // bool deck_limit = in_deck_same < GameplayData.Get().deck_duplicate_max;

                    //if (owner && deck_limit)
                    if (owner)
                    {
                        AddDeckCard(icard, variant);
                        RefreshDeckCards();
                    }
                }
            }
        }

        public void OnClickCardRight(CardUI card)
        {
            CardZoomPanel.Get().ShowCard(card.GetCard(), card.GetVariant());
        }

        //---- Right Panel Click -------

        public void OnClickDeckLine(DeckLine line)
        {
            if (line.IsHidden() || saving)
                return;
            UserDeckData deck = line.GetUserDeck();
            if (deck != null)
            {
                RefreshDeck(deck);
                ShowDeckCards();
            }
            else
            {
                // User clicked the "+" button to create new deck
                RefreshDeck(null);
                ShowDeckCards();
            }
        }

        private void OnClickCardLine(DeckLine line)
        {
            CardData card = line.GetCard();
            VariantData variant = line.GetVariant();
            if (card != null)
            {
                RemoveDeckCard(card, variant);
            }

            RefreshDeckCards();
        }

        private void OnRightClickCardLine(DeckLine line)
        {
            CardData icard = line.GetCard();
            if (icard != null)
                CardZoomPanel.Get().ShowCard(icard, line.GetVariant());
        }

        private void OnClickSideCardLine(DeckLine line)
        {
            CardData card = line.GetCard();
            VariantData variant = line.GetVariant();
            if (card != null)
            {
                RemoveSideDeckCard(card, variant);
            }

            RefreshDeckCards();
        }

        private void OnRightClickSideCardLine(DeckLine line)
        {
            CardData icard = line.GetCard();
            if (icard != null)
                CardZoomPanel.Get().ShowCard(icard, line.GetVariant());
        }

        // ---- Deck editing Click -----

        public void OnClickSaveDeck()
        {
            if (!saving)
            {
                SaveDeck();
            }
        }

        public void OnClickDeckBack()
        {
            ShowDeckList();
        }

        public void OnClickDeleteDeck()
        {
            if (editing_deck && !string.IsNullOrEmpty(current_deck_tid))
            {
                DeleteDeck(current_deck_tid);
            }
        }

        public void OnClickDeckDelete(DeckLine line)
        {
            if (line.IsHidden())
                return;
            UserDeckData deck = line.GetUserDeck();
            if (deck != null)
            {
                DeleteDeck(deck.tid);
            }
        }
        
        // ---- Getters -----

        public int CountDeckCards(CardData card, VariantData cvariant)
        {
            int count = 0;
            foreach (UserCardData ucard in deck_cards)
            {
                if (ucard.tid == card.id && ucard.variant == cvariant.id)
                    count += ucard.quantity;
            }
            return count;
        }

        public int CountDeckCards(CardData card)
        {
            int count = 0;
            foreach (UserCardData ucard in deck_cards)
            {
                if (ucard.tid == card.id)
                    count += ucard.quantity;
            }
            return count;
        }

        private bool IsCardOwned(UserData udata, CardData card, VariantData variant, int quantity)
        {
            if (card.availability == CardAvailability.AlwaysAvailable)
                return true;
            return udata.GetCardQuantity(card, variant) >= quantity;
        }

        private string GetSelectedHeroId()
        {
            foreach (IconButton btn in hero_powers)
            {
                if (btn.IsActive())
                    return btn.value;
            }
            return "";
        }

        //-----

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshAll();
            ShowDeckList();
        }

        public static CollectionPanel Get()
        {
            return instance;
        }
    }

    public struct CardDataQ
    {
        public CardData card;
        public VariantData variant;
        public int quantity;
    }
}
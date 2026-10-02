using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// The Habilidades tab, ConceptArt (1) filled with the game's real data (no invented levels or
    /// points): the character — name, day, the total of all skills against its cap in the ring, the
    /// guardian power, attributes and the PvP switch —; the skills grouped (combat, magic, work,
    /// movement, others) with level, gear bonus and progress, then the active effects; and the texts
    /// the game keeps (compendium, log, active effects, stats), read from vanilla's own texts dialog.
    /// </summary>
    [GameContract("assembly_valheim", "Player", "GetSkills")]
    [GameContract("assembly_valheim", "Skills", "GetSkillList")]
    [GameContract("assembly_valheim", "Skills", "GetSkillLevel")]
    [GameContract("assembly_valheim", "Skills", "GetTotalSkill")]
    [GameContract("assembly_valheim", "Skills", "GetTotalSkillCap")]
    [GameContract("assembly_valheim", "Skills+Skill", "m_info", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Skills\u002BSkillDef")]
    [GameContract("assembly_valheim", "Skills+Skill", "m_level", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GameContract("assembly_valheim", "Skills+Skill", "GetLevelPercentage")]
    [GameContract("assembly_valheim", "Skills+SkillDef", "m_skill", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Skills\u002BSkillType")]
    [GameContract("assembly_valheim", "Skills+SkillDef", "m_icon", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GameContract("assembly_valheim", "Skills+SkillDef", "m_description", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GameContract("assembly_valheim", "Character", "GetMaxHealth")]
    [GameContract("assembly_valheim", "Character", "GetSEMan")]
    [GameContract("assembly_valheim", "Player", "GetMaxStamina")]
    [GameContract("assembly_valheim", "Player", "GetMaxEitr")]
    [GameContract("assembly_valheim", "Player", "GetBodyArmor")]
    [GameContract("assembly_valheim", "Player", "GetMaxCarryWeight")]
    [GameContract("assembly_valheim", "Player", "GetComfortLevel")]
    [GameContract("assembly_valheim", "Player", "GetGuardianPowerHUD")]
    [GameContract("assembly_valheim", "Player", "CanSwitchPVP")]
    [GameContract("assembly_valheim", "SEMan", "GetHUDStatusEffects")]
    [GameContract("assembly_valheim", "StatusEffect", "m_name", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GameContract("assembly_valheim", "StatusEffect", "m_icon", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Sprite")]
    [GameContract("assembly_valheim", "StatusEffect", "GetIconText")]
    [GameContract("assembly_valheim", "EnvMan", "instance")]
    [GameContract("assembly_valheim", "EnvMan", "GetDay", Parameters = new string[0])]
    [GameContract("assembly_valheim", "Game", "GetPlayerProfile")]
    [GameContract("assembly_valheim", "PlayerProfile", "GetName")]
    [GameContract("assembly_valheim", "InventoryGui", "m_pvp", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.UI.Toggle")]
    [GameContract("assembly_valheim", "InventoryGui", "m_textsDialog", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "TextsDialog")]
    [GameContract("assembly_valheim", "TextsDialog", "UpdateTextsList")]
    [GameContract("assembly_valheim", "TextsDialog", "m_texts", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.List\u00601[[TextsDialog\u002BTextInfo, assembly_valheim, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")]
    [GameContract("assembly_valheim", "TextsDialog+TextInfo", "m_topic", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GameContract("assembly_valheim", "TextsDialog+TextInfo", "m_text", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.String")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Game", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Game")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "GetSkills", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "Skills")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "GetMaxStamina", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "GetMaxEitr", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Character", "GetBodyArmor", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Single")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "InventoryGui", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "InventoryGui")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_valheim", "Player", "m_localPlayer", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Player")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Widgets.ScrollArea), typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.WindowParts), typeof(GenesisUI.Theme.ThemeTokens), typeof(GenesisUI.Widgets.WindowCanvas), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Widgets.ColorExtensions), typeof(GenesisUI.Widgets.Frame), typeof(GenesisUI.Foundation.Guard))]
    internal sealed class SkillsWindowModule : WindowModuleBase
    {
        private const float CharacterW = 400f, SkillsX = 404f, SkillsW = 760f, TextsX = 1168f, TextsW = 412f;
        private const float CardW = 228f, CardH = 58f, CardGap = 8f;
        private const float UpdateSeconds = 0.5f;

        private static readonly string[][] Groups =
        {
            new[] { "Swords", "Axes", "Clubs", "Knives", "Polearms", "Spears", "Bows", "Crossbows", "Unarmed", "Blocking" },
            new[] { "ElementalMagic", "BloodMagic" },
            new[] { "WoodCutting", "Pickaxes", "Fishing", "Farming", "Cooking", "Crafting" },
            new[] { "Run", "Jump", "Sneak", "Swim", "Dodge", "Ride" },
        };

        private static readonly string[] GroupTokens =
        {
            "$genesisui_skills_combat", "$genesisui_skills_magic", "$genesisui_skills_work", "$genesisui_skills_movement", "$genesisui_skills_other",
        };

        private sealed class SkillCard
        {
            public RectTransform Root;
            public Image Icon, Bar;
            public TextMeshProUGUI Name, Level;
            public Skills.Skill Skill;
            public float ShownLevel = -1f, ShownTotal = -1f, ShownPct = -1f;
        }

        private sealed class EffectCard
        {
            public RectTransform Root;
            public Image Icon;
            public TextMeshProUGUI Name, Time;
        }

        private readonly List<SkillCard> _cards = new List<SkillCard>(32);
        private readonly List<GameObject> _groupObjects = new List<GameObject>(8);
        private readonly List<EffectCard> _effects = new List<EffectCard>(12);
        private readonly List<StatusEffect> _effectList = new List<StatusEffect>(12);
        private readonly TextMeshProUGUI[] _attrValues = new TextMeshProUGUI[6];
        private readonly List<(string Topic, string Text)> _texts = new List<(string, string)>(64);
        private readonly List<RectTransform> _textRows = new List<RectTransform>(64);

        private TextMeshProUGUI _name, _day, _total, _totalCap, _powerName, _powerState, _pvpLabel, _hint, _textTopic, _textBody, _effectsTitle;
        private Image _ring, _powerIcon;
        private Button _pvp;
        private ScrollArea _skillsScroll, _textList, _textRead;
        private float _updateIn;
        private float _textsReadAt = -1000f;
        private int _skillCount = -1, _selectedText = -1;
        private string _hoverText;

        private MethodInfo _updateTexts;
        private FieldInfo _textsField;
        private FieldInfo _topicField, _textField;

        protected override WindowShellModule.Tab Tab => WindowShellModule.Tab.Skills;
        public override string Id => "win.skills";
        public override string NameToken => "$genesisui_module_skills_window";

        protected override void Resolve()
        {
            _updateTexts = AccessTools.Method(typeof(TextsDialog), "UpdateTextsList");
            _textsField = AccessTools.Field(typeof(TextsDialog), "m_texts");
            _topicField = AccessTools.Field(typeof(TextsDialog.TextInfo), "m_topic");
            _textField = AccessTools.Field(typeof(TextsDialog.TextInfo), "m_text");
        }

        protected override void Forget()
        {
            _cards.Clear();
            _groupObjects.Clear();
            _effects.Clear();
            _textRows.Clear();
            _texts.Clear();
            _skillCount = -1;
            _selectedText = -1;
            _textsReadAt = -1000f;
            _updateIn = 0f;
            _hoverText = null;
        }

        // ------------------------------------------------------------------ data

        protected override void Opened(InventoryGui gui, Player player)
        {
            _updateIn = 0f;
            var profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            _name.text = (profile != null ? profile.GetName() : "").ToUpperInvariant();
            if (player.GetSkills().GetSkillList().Count != _skillCount) LayoutSkills(player);
            // Vanilla's texts list also writes a long stats block to the game log each time it is built:
            // read it at most once a minute (R-058 log).
            if (Time.unscaledTime - _textsReadAt > 60f || _texts.Count == 0)
            {
                _textsReadAt = Time.unscaledTime;
                ReadTexts(gui);
            }
            _skillsScroll.ToTop();
        }

        protected override void Tick(InventoryGui gui, Player player, float deltaSeconds)
        {
            // The PvP switch is vanilla's toggle: vanilla applies it to the player every frame.
            bool canSwitch = player.CanSwitchPVP();
            if (_pvp.interactable != canSwitch) _pvp.interactable = canSwitch;
            bool pvp = gui.m_pvp != null && gui.m_pvp.isOn;
            string pvpText = Localize(pvp ? "$genesisui_pvp_on" : "$genesisui_pvp_off");
            if (_pvpLabel.text != pvpText) _pvpLabel.text = pvpText;

            _updateIn -= deltaSeconds;
            if (_updateIn > 0f) return;
            _updateIn = UpdateSeconds;
            var skills = player.GetSkills();
            if (skills.GetSkillList().Count != _skillCount) LayoutSkills(player);
            foreach (var card in _cards) UpdateCard(card, skills);

            float total = skills.GetTotalSkill(), cap = Mathf.Max(1f, skills.GetTotalSkillCap());
            _total.SetText("{0}", Mathf.FloorToInt(total));
            _totalCap.SetText("{0} / {1}", Mathf.FloorToInt(total), Mathf.FloorToInt(cap));
            _ring.fillAmount = Mathf.Clamp01(total / cap);
            _day.text = EnvMan.instance != null ? Localize("$genesisui_day") + " " + EnvMan.instance.GetDay() : "";

            _attrValues[0].SetText("{0}", Mathf.CeilToInt(player.GetMaxHealth()));
            _attrValues[1].SetText("{0}", Mathf.CeilToInt(player.GetMaxStamina()));
            _attrValues[2].SetText("{0}", Mathf.CeilToInt(player.GetMaxEitr()));
            _attrValues[3].SetText("{0}", Mathf.RoundToInt(player.GetBodyArmor()));
            _attrValues[4].SetText("{0}", Mathf.CeilToInt(player.GetMaxCarryWeight()));
            _attrValues[5].SetText("{0}", player.GetComfortLevel());

            player.GetGuardianPowerHUD(out var power, out var cooldown);
            _powerIcon.enabled = power != null;
            if (power != null)
            {
                _powerIcon.sprite = power.m_icon;
                _powerName.text = Localize(power.m_name);
                _powerState.text = cooldown > 0f
                    ? Localize("$genesisui_power_cooldown") + " " + FormatTime(cooldown)
                    : Localize("$genesisui_power_ready");
            }
            else
            {
                _powerName.text = Localize("$genesisui_power_none");
                _powerState.text = "";
            }
            UpdateEffects(player);
        }

        private void UpdateCard(SkillCard card, Skills skills)
        {
            var s = card.Skill;
            float level = s.m_level, total = skills.GetSkillLevel(s.m_info.m_skill), pct = s.GetLevelPercentage();
            if (Mathf.Approximately(level, card.ShownLevel) && Mathf.Approximately(total, card.ShownTotal) && Mathf.Approximately(pct, card.ShownPct)) return;
            card.ShownLevel = level;
            card.ShownTotal = total;
            card.ShownPct = pct;
            int bonus = Mathf.RoundToInt(total - Mathf.Floor(level));
            card.Level.text = bonus != 0
                ? (int)level + " <color=#8FC77A>" + (bonus > 0 ? "+" : "") + bonus + "</color>"
                : ((int)level).ToString();
            card.Bar.fillAmount = level >= 100f ? 1f : Mathf.Clamp01(pct);
        }

        private void UpdateEffects(Player player)
        {
            _effectList.Clear();
            player.GetSEMan().GetHUDStatusEffects(_effectList);
            for (int i = 0; i < _effects.Count; i++)
            {
                var card = _effects[i];
                bool show = i < _effectList.Count;
                if (card.Root.gameObject.activeSelf != show) card.Root.gameObject.SetActive(show);
                if (!show) continue;
                var se = _effectList[i];
                card.Icon.sprite = se.m_icon;
                card.Name.text = Localize(se.m_name);
                card.Time.text = se.GetIconText() ?? "";
            }
            if (_effectsTitle != null) _effectsTitle.gameObject.SetActive(true);
        }

        private static string FormatTime(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            return s >= 60 ? (s / 60) + "m " + (s % 60).ToString("00") + "s" : s + "s";
        }

        /// <summary>
        /// Vanilla's texts dialog builds its list (known texts, the log, active effects, stats) without
        /// opening; we read that list so content added by the game or other mods shows here too.
        /// </summary>
        private void ReadTexts(InventoryGui gui)
        {
            _texts.Clear();
            var dialog = gui.m_textsDialog;
            if (dialog != null && _updateTexts != null)
            {
                _updateTexts.Invoke(dialog, null);
                if (_textsField.GetValue(dialog) is IList list)
                    foreach (var info in list)
                        _texts.Add(((string)_topicField.GetValue(info) ?? "", (string)_textField.GetValue(info) ?? ""));
            }
            for (int i = 0; i < _textRows.Count; i++)
            {
                bool show = i < _texts.Count;
                _textRows[i].gameObject.SetActive(show);
                if (show) _textRows[i].Find("Label").GetComponent<TextMeshProUGUI>().text = _texts[i].Topic;
            }
            _textList.ContentHeight = Mathf.Min(_texts.Count, _textRows.Count) * 42f;
            ShowText(_texts.Count > 0 ? Mathf.Clamp(_selectedText, 0, _texts.Count - 1) : -1);
        }

        private void ShowText(int index)
        {
            _selectedText = index;
            for (int i = 0; i < _textRows.Count; i++)
            {
                var sel = _textRows[i].Find("Selected");
                if (sel != null) sel.GetComponent<Image>().enabled = i == index;
            }
            if (index < 0) { _textTopic.text = ""; _textBody.text = Localize("$genesisui_texts_none"); return; }
            _textTopic.text = _texts[index].Topic.ToUpperInvariant();
            _textBody.text = _texts[index].Text;
            _textRead.ContentHeight = _textBody.GetPreferredValues(_textBody.text, _textRead.Content.sizeDelta.x, 0f).y + 12f;
            _textRead.ToTop();
        }

        // ------------------------------------------------------------------ building

        protected override void Draw()
        {
            var t = Theme.Tokens;
            var character = Parts.Panel(Panels, "Character", 0f, 0f, CharacterW, PanelsHeight, "$genesisui_panel_character", 54f, 23f, TextAlignmentOptions.Left);
            var skills = Parts.Panel(Panels, "Skills", SkillsX, 0f, SkillsW, PanelsHeight, "$genesisui_panel_skills", 54f, 23f, TextAlignmentOptions.Left);
            var texts = Parts.Panel(Panels, "Texts", TextsX, 0f, TextsW, PanelsHeight, "$genesisui_panel_texts", 54f, 23f, TextAlignmentOptions.Left);
            DrawCharacter(character, t);
            DrawSkills(skills, t);
            DrawTexts(texts, t);
        }

        private void DrawCharacter(RectTransform p, ThemeTokens t)
        {
            _name = Parts.Label(p, "Name", FontRole.Display, 25f, t.AccentGoldBright, 20f, 66f, CharacterW - 40f, 32f, TextAlignmentOptions.Center);
            _name.characterSpacing = 4f;
            _day = Parts.Label(p, "Day", FontRole.Body, 16f, t.TextFlavor, 20f, 98f, CharacterW - 40f, 22f, TextAlignmentOptions.Center);

            // The ring: the total of all skills against its cap (vanilla's own "total skill").
            const float ring = 150f;
            var rrt = WindowCanvas.At(p, "Ring", (CharacterW - ring) / 2f, 128f, ring, ring);
            _ring = Ui.Image(Ui.Fill(Ui.Child(rrt, "Fill")), Theme.Sprite("ring_fill"), ThemeRuntime.ToUnity(t.AccentGold).WithA(0.9f));
            _ring.type = Image.Type.Filled;
            _ring.fillMethod = Image.FillMethod.Radial360;
            _ring.fillOrigin = (int)Image.Origin360.Top;
            _ring.fillClockwise = true;
            Ui.Image(Ui.Fill(Ui.Child(rrt, "Frame")), Theme.Sprite("ring"), Color.white);
            var caption = Parts.Label(rrt, "Caption", FontRole.Label, 12f, t.TextFlavor, 0f, 44f, ring, 18f, TextAlignmentOptions.Center);
            caption.text = Localize("$genesisui_skills_total").ToUpperInvariant();
            caption.characterSpacing = 3f;
            _total = Parts.Label(rrt, "Total", FontRole.Display, 34f, t.AccentGoldBright, 0f, 62f, ring, 42f, TextAlignmentOptions.Center);
            _totalCap = Parts.Label(p, "TotalCap", FontRole.Body, 15f, t.TextFlavor, 20f, 284f, CharacterW - 40f, 20f, TextAlignmentOptions.Center);

            // Guardian power.
            var power = WindowCanvas.At(p, "Power", 30f, 318f, CharacterW - 60f, 52f);
            Frame.Dress(power, Theme, "keycap_wide", "Windows", 52f);
            _powerIcon = Ui.Image(WindowCanvas.At(power, "Icon", 8f, 6f, 40f, 40f), null, Color.white);
            _powerIcon.preserveAspect = true;
            _powerName = Parts.Label(power, "Name", FontRole.Body, 17f, t.TextTitle, 58f, 5f, CharacterW - 130f, 24f, TextAlignmentOptions.Left);
            _powerState = Parts.Label(power, "State", FontRole.Body, 14f, t.TextFlavor, 58f, 27f, CharacterW - 130f, 20f, TextAlignmentOptions.Left);

            var title = Parts.Label(p, "AttributesTitle", FontRole.Label, 14f, t.AccentGoldBright, 30f, 388f, 300f, 20f, TextAlignmentOptions.Left);
            title.text = Localize("$genesisui_attributes").ToUpperInvariant();
            title.characterSpacing = 3f;
            string[] names = { "$genesisui_attr_health", "$genesisui_attr_stamina", "$genesisui_attr_eitr", "$genesisui_armor_total", "$genesisui_attr_carry", "$genesisui_attr_comfort" };
            for (int i = 0; i < names.Length; i++)
            {
                var row = WindowCanvas.At(p, "Attr " + i, 30f, 412f + i * 28f, CharacterW - 60f, 27f);
                Parts.Label(row, "Label", FontRole.Body, 17f, t.TextBody, 0f, 0f, 220f, 26f, TextAlignmentOptions.Left).text = Localize(names[i]);
                _attrValues[i] = Parts.Label(row, "Value", FontRole.Body, 17f, t.TextTitle, CharacterW - 180f, 0f, 120f, 26f, TextAlignmentOptions.Right);
                Ui.Image(WindowCanvas.At(row, "Line", 0f, 26f, CharacterW - 60f, 1f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.12f));
            }

            // PvP: vanilla's own toggle, flipped by our button.
            _pvp = Parts.Button(p, "PvP", 30f, 604f, CharacterW - 60f, 44f, null, 17f, "pvp switch", TogglePvp, out _pvpLabel);
        }

        private void TogglePvp()
        {
            var gui = InventoryGui.instance;
            var player = Player.m_localPlayer;
            if (gui == null || gui.m_pvp == null || player == null || !player.CanSwitchPVP()) return;
            gui.m_pvp.isOn = !gui.m_pvp.isOn;
        }

        private void DrawSkills(RectTransform p, ThemeTokens t)
        {
            _skillsScroll = new ScrollArea(p, "SkillList", 22f, 64f, SkillsW - 44f, 516f, t);
            var rule = Parts.Rule(p, 22f, 596f, SkillsW - 44f);
            _hint = Parts.Label(p, "Hint", FontRole.Body, 16f, t.TextBody, 26f, 606f, SkillsW - 52f, 50f, TextAlignmentOptions.TopLeft);
            _hint.textWrappingMode = TextWrappingModes.Normal;
            _hint.enableAutoSizing = false;
            _hint.text = Localize("$genesisui_skills_hint");
        }

        /// <summary>Cards for the player's skills, in groups; the skill list only changes when a mod adds skills.</summary>
        private void LayoutSkills(Player player)
        {
            var t = Theme.Tokens;
            foreach (var go in _groupObjects) UnityEngine.Object.Destroy(go);
            _groupObjects.Clear();
            _cards.Clear();
            _effects.Clear();
            var list = player.GetSkills().GetSkillList();
            _skillCount = list.Count;
            var byName = new Dictionary<string, Skills.Skill>();
            var others = new List<Skills.Skill>();
            foreach (var s in list) byName[s.m_info.m_skill.ToString()] = s;
            var used = new HashSet<Skills.Skill>();

            var content = _skillsScroll.Content;
            float y = 0f;
            for (int g = 0; g <= Groups.Length; g++)
            {
                var members = new List<Skills.Skill>();
                if (g < Groups.Length)
                {
                    foreach (var key in Groups[g]) if (byName.TryGetValue(key, out var s)) { members.Add(s); used.Add(s); }
                }
                else
                {
                    foreach (var s in list) if (!used.Contains(s)) members.Add(s);
                }
                if (members.Count == 0) continue;
                var header = Parts.Label(content, "Group " + g, FontRole.Label, 14f, t.AccentGoldBright, 4f, y + 4f, 400f, 20f, TextAlignmentOptions.Left);
                header.text = Localize(GroupTokens[g]).ToUpperInvariant();
                header.characterSpacing = 3f;
                _groupObjects.Add(header.gameObject);
                y += 30f;
                for (int i = 0; i < members.Count; i++)
                {
                    int col = i % 3, row = i / 3;
                    var card = MakeCard(content, members[i], col * (CardW + CardGap), y + row * (CardH + CardGap));
                    _cards.Add(card);
                    _groupObjects.Add(card.Root.gameObject);
                }
                y += ((members.Count + 2) / 3) * (CardH + CardGap) + 8f;
            }

            // Active effects after the skills (vanilla's HUD effects: rested, sheltered, foods, powers).
            _effectsTitle = Parts.Label(content, "EffectsTitle", FontRole.Label, 14f, t.AccentGoldBright, 4f, y + 4f, 400f, 20f, TextAlignmentOptions.Left);
            _effectsTitle.text = Localize("$genesisui_active_effects").ToUpperInvariant();
            _effectsTitle.characterSpacing = 3f;
            _groupObjects.Add(_effectsTitle.gameObject);
            y += 30f;
            for (int i = 0; i < 12; i++)
            {
                int col = i % 3, row = i / 3;
                var root = WindowCanvas.At(content, "Effect " + i, col * (CardW + CardGap), y + row * (CardH + CardGap), CardW, CardH);
                Frame.Dress(root, Theme, "keycap_wide", "Windows", CardH);
                var e = new EffectCard { Root = root };
                e.Icon = Ui.Image(WindowCanvas.At(root, "Icon", 8f, 9f, 40f, 40f), null, Color.white);
                e.Icon.preserveAspect = true;
                e.Name = Parts.Label(root, "Name", FontRole.Body, 16f, t.TextTitle, 56f, 6f, CardW - 64f, 24f, TextAlignmentOptions.Left);
                e.Time = Parts.Label(root, "Time", FontRole.Body, 14f, t.TextFlavor, 56f, 30f, CardW - 64f, 20f, TextAlignmentOptions.Left);
                root.gameObject.SetActive(false);
                _effects.Add(e);
                _groupObjects.Add(root.gameObject);
            }
            y += 4 * (CardH + CardGap);
            _skillsScroll.ContentHeight = y;
            foreach (var card in _cards) UpdateCard(card, player.GetSkills());
        }

        private SkillCard MakeCard(RectTransform parent, Skills.Skill skill, float x, float y)
        {
            var t = Theme.Tokens;
            var card = new SkillCard { Skill = skill, Root = WindowCanvas.At(parent, "Skill " + skill.m_info.m_skill, x, y, CardW, CardH) };
            Frame.Dress(card.Root, Theme, "keycap_wide", "Windows", CardH);
            card.Icon = Ui.Image(WindowCanvas.At(card.Root, "Icon", 9f, 9f, 40f, 40f), skill.m_info.m_icon, Color.white);
            card.Icon.preserveAspect = true;
            card.Name = Parts.Label(card.Root, "Name", FontRole.Body, 16f, t.TextTitle, 56f, 6f, CardW - 110f, 24f, TextAlignmentOptions.Left);
            card.Name.text = Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower());
            card.Level = Parts.Label(card.Root, "Level", FontRole.Display, 17f, t.AccentGoldBright, CardW - 70f, 6f, 60f, 24f, TextAlignmentOptions.Right);
            var track = WindowCanvas.At(card.Root, "Track", 56f, 38f, CardW - 68f, 4f);
            Ui.Image(track, null, new Color(0f, 0f, 0f, 0.6f));
            card.Bar = Ui.Image(Ui.Fill(Ui.Child(track, "Fill")), Theme.Sprite("bar_fill"), ThemeRuntime.ToUnity(t.AccentGold));
            card.Bar.type = Image.Type.Filled;
            card.Bar.fillMethod = Image.FillMethod.Horizontal;
            string description = Localize(skill.m_info.m_description);
            string name = card.Name.text;
            Ui.Image(card.Root, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            card.Root.gameObject.AddComponent<Hover>().Init(() => SetHint(name, description));
            return card;
        }

        private void SetHint(string name, string description)
        {
            string text = "<color=#D8B76C>" + name + "</color>  " + description;
            if (text == _hoverText) return;
            _hoverText = text;
            _hint.text = text;
        }

        private void DrawTexts(RectTransform p, ThemeTokens t)
        {
            _textList = new ScrollArea(p, "TextList", 20f, 64f, TextsW - 40f, 230f, t, 42f);
            for (int i = 0; i < 64; i++)
            {
                int index = i;
                var row = WindowCanvas.At(_textList.Content, "Text " + i, 0f, i * 42f, TextsW - 50f, 40f);
                var sel = Ui.Image(Ui.Fill(Ui.Child(row, "Selected")), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.14f));
                sel.enabled = false;
                var label = Parts.Label(row, "Label", FontRole.Body, 16f, t.TextTitle, 10f, 0f, TextsW - 70f, 40f, TextAlignmentOptions.MidlineLeft);
                Ui.Image(WindowCanvas.At(row, "Line", 0f, 40f, TextsW - 50f, 1f), null, ThemeRuntime.ToUnity(t.AccentGold).WithA(0.12f));
                Parts.Clickable(row, "text topic", () => ShowText(index));
                row.gameObject.SetActive(false);
                _textRows.Add(row);
            }
            Parts.Rule(p, 20f, 306f, TextsW - 40f);
            _textTopic = Parts.Label(p, "Topic", FontRole.Display, 18f, t.AccentGoldBright, 22f, 318f, TextsW - 44f, 26f, TextAlignmentOptions.Left);
            _textTopic.characterSpacing = 3f;
            _textRead = new ScrollArea(p, "TextRead", 20f, 352f, TextsW - 40f, 300f, t, 48f);
            _textBody = Parts.Label(_textRead.Content, "Body", FontRole.Body, 16f, t.TextBody, 2f, 0f, TextsW - 56f, 300f, TextAlignmentOptions.TopLeft);
            _textBody.textWrappingMode = TextWrappingModes.Normal;
            _textBody.enableAutoSizing = false;
            _textBody.overflowMode = TextOverflowModes.Overflow;
            ((RectTransform)_textBody.transform).anchorMin = new Vector2(0f, 0f);
            ((RectTransform)_textBody.transform).anchorMax = new Vector2(1f, 1f);
            ((RectTransform)_textBody.transform).offsetMin = new Vector2(2f, 0f);
            ((RectTransform)_textBody.transform).offsetMax = new Vector2(-2f, 0f);
        }

        /// <summary>Pointer-enter callback for a card.</summary>
        private sealed class Hover : MonoBehaviour, IPointerEnterHandler
        {
            private Action _onEnter;
            internal void Init(Action onEnter) => _onEnter = onEnter;
            public void OnPointerEnter(PointerEventData e) => Guard.Run("module:win.skills", _onEnter);
        }
    }
}

#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Cooldown.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Data;
using SFXUtility.Library;
using SFXUtility.Library.Extensions.NET;
using SFXUtility.Library.Extensions.SharpDX;
using SFXUtility.Library.Logger;
using SFXUtility.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;
using Rectangle = SharpDX.Rectangle;

#endregion

#pragma warning disable 618

namespace SFXUtility.Features.Timers
{
    internal class Cooldown : Child<Timers>
    {
        private const float TeleportCd = 300f;
        private readonly Dictionary<int, List<SpellDataInst>> _spellDatas = new Dictionary<int, List<SpellDataInst>>();
        private readonly SpellSlot[] _spellSlots = { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };

        private readonly Dictionary<int, List<SpellDataInst>> _summonerDatas =
            new Dictionary<int, List<SpellDataInst>>();

        private readonly SpellSlot[] _summonerSlots = { SpellSlot.Summoner1, SpellSlot.Summoner2 };
        private readonly Dictionary<string, Texture> _summonerTextures = new Dictionary<string, Texture>();
        private readonly Dictionary<int, float> _teleports = new Dictionary<int, float>();
        private List<Obj_AI_Hero> _heroes = new List<Obj_AI_Hero>();
        private Texture _hudSelfTexture;
        private Texture _hudTexture;
        private Line _line;
        private Sprite _sprite;
        private Font _text;

        public Cooldown(Timers parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Cooldown"; }
        }

        protected override void OnEnable()
        {
            Drawing.OnEndScene += OnDrawingEndScene;
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            Obj_AI_Base.OnTeleport += OnObjAiBaseTeleport;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnEndScene -= OnDrawingEndScene;
            Obj_AI_Base.OnProcessSpellCast -= OnObjAiBaseProcessSpellCast;
            Obj_AI_Base.OnTeleport -= OnObjAiBaseTeleport;

            base.OnDisable();
        }

        private void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (hero != null)
                {
                    var data = hero.IsAlly
                        ? _manualAllySpells.FirstOrDefault(
                            m => m.Spell.Equals(args.SData.Name, StringComparison.OrdinalIgnoreCase))
                        : _manualEnemySpells.FirstOrDefault(
                            m => m.Spell.Equals(args.SData.Name, StringComparison.OrdinalIgnoreCase));
                    if (data != null && data.CooldownExpires - Game.Time < 0.5)
                    {
                        var spell = hero.GetSpell(data.Slot);
                        if (spell != null)
                        {
                            var cooldown = data.Cooldowns[spell.Level - 1];
                            var cdr = hero.PercentCooldownMod * -1 * 100;
                            data.Cooldown = cooldown - cooldown / 100 * (cdr > 40 ? 40 : cdr) + data.Additional;
                            data.CooldownExpires = Game.Time + data.Cooldown;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                var drawingMenu = new Menu("Drawing", Name + "Drawing");
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "TimeFormat", "Time Format").SetValue(
                        new StringList(new[] { "mm:ss", "ss" })));
                drawingMenu.AddItem(
                    new MenuItem(drawingMenu.Name + "FontSize", "Font Size").SetValue(new Slider(13, 3, 30)));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Enemy", "Enemy").SetValue(false));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Ally", "Ally").SetValue(false));
                drawingMenu.AddItem(new MenuItem(drawingMenu.Name + "Self", "Self").SetValue(false));

                Menu.AddSubMenu(drawingMenu);

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Menu.Item(Name + "DrawingEnemy").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    if (_heroes == null)
                    {
                        return;
                    }
                    var ally = Menu.Item(Name + "DrawingAlly").GetValue<bool>();
                    var enemy = args.GetNewValue<bool>();
                    _heroes = ally && enemy
                        ? GameObjects.Heroes.ToList()
                        : (ally ? GameObjects.AllyHeroes : (enemy ? GameObjects.EnemyHeroes : new List<Obj_AI_Hero>()))
                            .ToList();
                    if (Menu.Item(Name + "DrawingSelf").GetValue<bool>())
                    {
                        if (_heroes.All(h => h.NetworkId != ObjectManager.Player.NetworkId))
                        {
                            _heroes.Add(ObjectManager.Player);
                        }
                    }
                    else
                    {
                        _heroes.RemoveAll(h => h.NetworkId == ObjectManager.Player.NetworkId);
                    }
                };

                Menu.Item(Name + "DrawingAlly").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    if (_heroes == null)
                    {
                        return;
                    }
                    var ally = args.GetNewValue<bool>();
                    var enemy = Menu.Item(Name + "DrawingEnemy").GetValue<bool>();
                    _heroes = ally && enemy
                        ? GameObjects.Heroes.ToList()
                        : (ally ? GameObjects.AllyHeroes : (enemy ? GameObjects.EnemyHeroes : new List<Obj_AI_Hero>()))
                            .ToList();
                    if (Menu.Item(Name + "DrawingSelf").GetValue<bool>() &&
                        _heroes.All(h => h.NetworkId != ObjectManager.Player.NetworkId))
                    {
                        _heroes.Add(ObjectManager.Player);
                    }
                    if (Menu.Item(Name + "DrawingSelf").GetValue<bool>())
                    {
                        if (_heroes.All(h => h.NetworkId != ObjectManager.Player.NetworkId))
                        {
                            _heroes.Add(ObjectManager.Player);
                        }
                    }
                    else
                    {
                        _heroes.RemoveAll(h => h.NetworkId == ObjectManager.Player.NetworkId);
                    }
                };

                Menu.Item(Name + "DrawingSelf").ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                {
                    if (_heroes == null)
                    {
                        return;
                    }
                    var ally = Menu.Item(Name + "DrawingAlly").GetValue<bool>();
                    var enemy = Menu.Item(Name + "DrawingEnemy").GetValue<bool>();
                    _heroes = ally && enemy
                        ? GameObjects.Heroes.ToList()
                        : (ally ? GameObjects.AllyHeroes : (enemy ? GameObjects.EnemyHeroes : new List<Obj_AI_Hero>()))
                            .ToList();
                    if (args.GetNewValue<bool>())
                    {
                        if (_heroes.All(h => h.NetworkId != ObjectManager.Player.NetworkId))
                        {
                            _heroes.Add(ObjectManager.Player);
                        }
                    }
                    else
                    {
                        _heroes.RemoveAll(h => h.NetworkId == ObjectManager.Player.NetworkId);
                    }
                };

                Parent.Menu.AddSubMenu(Menu);

                _sprite = MDrawing.GetSprite();
                _line = MDrawing.GetLine(4);
                _text = MDrawing.GetFont(Menu.Item(Name + "DrawingFontSize").GetValue<Slider>().Value);
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            try
            {
                _hudTexture = Resources.CD_Hud.ToTexture();
                _hudSelfTexture = Resources.CD_HudSelf.ToTexture();

                foreach (var enemy in GameObjects.Heroes)
                {
                    var lEnemy = enemy;
                    _spellDatas.Add(enemy.NetworkId, _spellSlots.Select(slot => lEnemy.GetSpell(slot)).ToList());
                    _summonerDatas.Add(enemy.NetworkId, _summonerSlots.Select(slot => lEnemy.GetSpell(slot)).ToList());
                }

                foreach (var sName in
                    GameObjects.Heroes.SelectMany(
                        h =>
                            _summonerSlots.Select(summoner => h.Spellbook.GetSpell(summoner).Name.ToLower())
                                .Where(sName => !_summonerTextures.ContainsKey(Summoners.FixName(sName)))))
                {
                    _summonerTextures[Summoners.FixName(sName)] =
                        ((Bitmap) Resources.ResourceManager.GetObject(string.Format("CD_{0}", Summoners.FixName(sName))) ??
                         Resources.CD_summonerbarrier).ToTexture();
                }

                _heroes = Menu.Item(Name + "DrawingAlly").GetValue<bool>() &&
                          Menu.Item(Name + "DrawingEnemy").GetValue<bool>()
                    ? GameObjects.Heroes.ToList()
                    : (Menu.Item(Name + "DrawingAlly").GetValue<bool>()
                        ? GameObjects.AllyHeroes
                        : (Menu.Item(Name + "DrawingEnemy").GetValue<bool>()
                            ? GameObjects.EnemyHeroes
                            : new List<Obj_AI_Hero>())).ToList();

                if (!Menu.Item(Name + "DrawingSelf").GetValue<bool>())
                {
                    _heroes.RemoveAll(h => h.NetworkId == ObjectManager.Player.NetworkId);
                }

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnObjAiBaseTeleport(Obj_AI_Base sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                var packet = Packet.S2C.Teleport.Decoded(sender, args);
                if (packet.Type == Packet.S2C.Teleport.Type.Teleport &&
                    (packet.Status == Packet.S2C.Teleport.Status.Finish ||
                     packet.Status == Packet.S2C.Teleport.Status.Abort))
                {
                    var time = Game.Time;
                    Utility.DelayAction.Add(
                        250, delegate
                        {
                            var cd = packet.Status == Packet.S2C.Teleport.Status.Finish ? 300 : 200;
                            _teleports[packet.UnitNetworkId] = time + cd;
                        });
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                {
                    return;
                }

                var totalSeconds = Menu.Item(Name + "DrawingTimeFormat").GetValue<StringList>().SelectedIndex == 1;
                foreach (var hero in
                    _heroes.Where(
                        hero => hero != null && hero.IsValid && hero.IsHPBarRendered && hero.Position.IsOnScreen()))
                {
                    try
                    {
                        var lHero = hero;
                        if (!hero.Position.IsValid() || !hero.HPBarPosition.IsValid())
                        {
                            return;
                        }

                        var x = (int) hero.HPBarPosition.X - (hero.IsMe ? -10 : 8);
                        var y = (int) hero.HPBarPosition.Y + (hero.IsEnemy ? 17 : (hero.IsMe ? 6 : 14));

                        _sprite.Begin(SpriteFlags.AlphaBlend);
                        var summonerData = _summonerDatas[hero.NetworkId];
                        for (var i = 0; i < summonerData.Count; i++)
                        {
                            var spell = summonerData[i];
                            if (spell != null)
                            {
                                var teleportCd = 0f;
                                if (spell.Name.Contains("Teleport", StringComparison.OrdinalIgnoreCase) &&
                                    _teleports.ContainsKey(hero.NetworkId))
                                {
                                    _teleports.TryGetValue(hero.NetworkId, out teleportCd);
                                }
                                var t = teleportCd > 0.1f
                                    ? teleportCd - Game.Time
                                    : (spell.IsReady() ? 0 : spell.CooldownExpires - Game.Time);
                                var sCd = teleportCd > 0.1f ? TeleportCd : spell.Cooldown;
                                var percent = Math.Abs(sCd) > float.Epsilon ? t / sCd : 1f;
                                var n = t > 0 ? (int) (19 * (1f - percent)) : 19;
                                if (t > 0)
                                {
                                    _text.DrawTextCentered(
                                        t.FormatTime(totalSeconds), x - (hero.IsMe ? -160 : 13), y + 7 + 13 * i,
                                        new ColorBGRA(255, 255, 255, 255));
                                }
                                if (_summonerTextures.ContainsKey(Summoners.FixName(spell.Name)))
                                {
                                    _sprite.Draw(
                                        _summonerTextures[Summoners.FixName(spell.Name)],
                                        new ColorBGRA(255, 255, 255, 255), new Rectangle(0, 12 * n, 12, 12),
                                        new Vector3(-x - (hero.IsMe ? 132 : 3), -y - 1 - 13 * i, 0));
                                }
                            }
                        }

                        _sprite.Draw(
                            hero.IsMe ? _hudSelfTexture : _hudTexture, new ColorBGRA(255, 255, 255, 255), null,
                            new Vector3(-x, -y, 0));

                        _sprite.End();

                        var x2 = x + (hero.IsMe ? 24 : 19);
                        var y2 = y + 21;

                        _line.Begin();
                        var spellData = _spellDatas[hero.NetworkId];
                        foreach (var spell in spellData)
                        {
                            var lSpell = spell;
                            if (spell != null)
                            {
                                var spell1 = spell;
                                var manual = hero.IsAlly
                                    ? _manualAllySpells.FirstOrDefault(
                                        m =>
                                            m.Slot.Equals(lSpell.Slot) &&
                                            m.Champ.Equals(lHero.ChampionName, StringComparison.OrdinalIgnoreCase))
                                    : _manualEnemySpells.FirstOrDefault(
                                        m =>
                                            m.Slot.Equals(spell1.Slot) &&
                                            m.Champ.Equals(lHero.ChampionName, StringComparison.OrdinalIgnoreCase));
                                var t = (manual != null ? manual.CooldownExpires : spell.CooldownExpires) - Game.Time;
                                var spellCooldown = manual != null ? manual.Cooldown : spell.Cooldown;
                                var percent = t > 0 && Math.Abs(spellCooldown) > float.Epsilon
                                    ? 1f - t / spellCooldown
                                    : 1f;
                                if (t > 0 && t < 100)
                                {
                                    _text.DrawTextCentered(
                                        t.FormatTime(totalSeconds), x2 + 27 / 2, y2 + 13,
                                        new ColorBGRA(255, 255, 255, 255));
                                }

                                if (spell.Level > 0)
                                {
                                    _line.Draw(
                                        new[] { new Vector2(x2, y2), new Vector2(x2 + percent * 23, y2) },
                                        t > 0 ? new ColorBGRA(235, 137, 0, 255) : new ColorBGRA(0, 168, 25, 255));
                                }
                                x2 = x2 + 27;
                            }
                        }
                        _line.End();
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        // ReSharper disable StringLiteralTypo
        private readonly List<ManualSpell> _manualAllySpells = new List<ManualSpell>
        {
            new ManualSpell("Lux", "LuxLightStrikeKugel", SpellSlot.E, new[] { 10f, 10f, 10f, 10f, 10f }),
            new ManualSpell("Gragas", "GragasQ", SpellSlot.Q, new[] { 11f, 10f, 9f, 8f, 7f }),
            new ManualSpell("Riven", "RivenFengShuiEngine", SpellSlot.R, new[] { 110f, 80f, 50f }, 15),
            new ManualSpell("TwistedFate", "PickACard", SpellSlot.W, new[] { 6f, 6f, 6f, 6f, 6f }),
            new ManualSpell("Velkoz", "VelkozQ", SpellSlot.Q, new[] { 7f, 7f, 7f, 7f, 7f }, 0.75f),
            new ManualSpell("Xerath", "xeratharcanopulse2", SpellSlot.Q, new[] { 9f, 8f, 7f, 6f, 5f }),
            new ManualSpell("Ziggs", "ZiggsW", SpellSlot.W, new[] { 26f, 24f, 22f, 20f, 18f }),
            new ManualSpell("Rumble", "RumbleGrenade", SpellSlot.E, new[] { 10f, 10f, 10f, 10f, 10f }),
            new ManualSpell("Riven", "RivenTriCleave", SpellSlot.Q, new[] { 13f, 13f, 13f, 13f, 13f }),
            new ManualSpell("Fizz", "FizzJump", SpellSlot.E, new[] { 16f, 14f, 12f, 10f, 8f }, 0.75f)
        };

        private readonly List<ManualSpell> _manualEnemySpells = new List<ManualSpell>
        {
            new ManualSpell("Lux", "LuxLightStrikeKugel", SpellSlot.E, new[] { 10f, 10f, 10f, 10f, 10f }),
            new ManualSpell("Gragas", "GragasQ", SpellSlot.Q, new[] { 11f, 10f, 9f, 8f, 7f }),
            new ManualSpell("Riven", "RivenFengShuiEngine", SpellSlot.R, new[] { 110f, 80f, 50f }, 15),
            new ManualSpell("TwistedFate", "PickACard", SpellSlot.W, new[] { 6f, 6f, 6f, 6f, 6f }),
            new ManualSpell("Velkoz", "VelkozQ", SpellSlot.Q, new[] { 7f, 7f, 7f, 7f, 7f }, 0.75f),
            new ManualSpell("Xerath", "xeratharcanopulse2", SpellSlot.Q, new[] { 9f, 8f, 7f, 6f, 5f }),
            new ManualSpell("Ziggs", "ZiggsW", SpellSlot.W, new[] { 26f, 24f, 22f, 20f, 18f }),
            new ManualSpell("Rumble", "RumbleGrenade", SpellSlot.E, new[] { 10f, 10f, 10f, 10f, 10f }),
            new ManualSpell("Riven", "RivenTriCleave", SpellSlot.Q, new[] { 13f, 13f, 13f, 13f, 13f }),
            new ManualSpell("Fizz", "FizzJump", SpellSlot.E, new[] { 16f, 14f, 12f, 10f, 8f }, 0.75f)
        };

        // ReSharper restore StringLiteralTypo
    }

    internal class ManualSpell
    {
        public ManualSpell(string champ, string spell, SpellSlot slot, float[] cooldowns, float additional = 0)
        {
            Champ = champ;
            Spell = spell;
            Slot = slot;
            Cooldowns = cooldowns;
            Additional = additional;
        }

        public string Champ { get; private set; }
        public string Spell { get; private set; }
        public SpellSlot Slot { get; private set; }
        public float[] Cooldowns { get; set; }
        public float Additional { get; set; }
        public float Cooldown { get; set; }
        public float CooldownExpires { get; set; }
    }
}
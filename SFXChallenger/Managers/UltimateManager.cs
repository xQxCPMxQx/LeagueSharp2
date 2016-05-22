#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 UltimateManager.cs is part of SFXChallenger.

 SFXChallenger is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXChallenger is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXChallenger. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Args;
using SFXChallenger.Enumerations;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using Spell = SFXChallenger.Wrappers.Spell;
using Utils = SFXChallenger.Helpers.Utils;

#endregion

namespace SFXChallenger.Managers
{
    public class UltimateManager
    {
        private bool _autoDamageCheck = true;
        private int _damagePercent = 100;
        private bool _forceDamageCheck = true;
        private Menu _menu;
        private int _singleDamagePercent = 150;
        public bool Combo { get; set; }
        public bool Assisted { get; set; }
        public bool Auto { get; set; }
        public bool Flash { get; set; }
        public bool Required { get; set; }
        public bool Force { get; set; }
        public bool Gapcloser { get; set; }
        public bool GapcloserDelay { get; set; }
        public bool Interrupt { get; set; }
        public bool InterruptDelay { get; set; }
        public List<Spell> Spells { get; set; }
        public Func<Obj_AI_Hero, float, bool, float> DamageCalculation { get; set; }

        public int DamagePercent
        {
            get { return _damagePercent; }
            set { _damagePercent = Math.Min(200, Math.Max(1, value)); }
        }

        public int SingleDamagePercent
        {
            get { return _singleDamagePercent; }
            set { _singleDamagePercent = Math.Min(200, Math.Max(1, value)); }
        }

        public bool ForceDamageCheck
        {
            get { return _forceDamageCheck; }
            set { _forceDamageCheck = value; }
        }

        public bool ComboDamageCheck { get; set; }

        public bool AutoDamageCheck
        {
            get { return _autoDamageCheck; }
            set { _autoDamageCheck = value; }
        }

        public bool AssistedDamageCheck { get; set; }

        public Menu AddToMenu(Menu menu)
        {
            try
            {
                _menu = menu;

                var ultimateMenu = menu.AddSubMenu(new Menu("Ultimate", menu.Name + ".ultimate"));

                if (Required)
                {
                    var requiredMenu =
                        ultimateMenu.AddSubMenu(new Menu("Required Targets", ultimateMenu.Name + ".required"));

                    var modes = new List<UltimateModeType>();
                    if (Combo)
                    {
                        modes.Add(UltimateModeType.Combo);
                    }
                    if (Auto)
                    {
                        modes.Add(UltimateModeType.Auto);
                    }
                    if (Assisted)
                    {
                        modes.Add(UltimateModeType.Assisted);
                    }

                    requiredMenu.AddItem(
                        new MenuItem(requiredMenu.Name + ".mode", "Mode").SetValue(
                            new StringList(modes.Select(m => m.ToString()).ToArray()))).ValueChanged +=
                        delegate(object sender, OnValueChangeEventArgs eventArgs)
                        {
                            Utils.UpdateVisibleTags(requiredMenu, eventArgs.GetNewValue<StringList>().SelectedIndex + 1);
                        };

                    for (var i = 0; i < modes.Count; i++)
                    {
                        requiredMenu.AddItem(
                            new MenuItem(
                                requiredMenu.Name + "." + GetModeString(modes[i], true) + ".min", "Min. Required")
                                .SetValue(new Slider(1, 1, 5))).SetTag(i + 1);
                        HeroListManager.AddToMenu(
                            requiredMenu,
                            new HeroListManagerArgs("ultimate-required-" + GetModeString(modes[i], true))
                            {
                                IsWhitelist = true,
                                Allies = false,
                                Enemies = true,
                                DefaultValue = false,
                                DontSave = true,
                                Enabled = true,
                                MenuTag = i + 1,
                                EnabledButton = false
                            });
                    }

                    Utils.UpdateVisibleTags(
                        requiredMenu, _menu.Item(requiredMenu.Name + ".mode").GetValue<StringList>().SelectedIndex + 1);
                }

                if (Force)
                {
                    var uForceMenu = ultimateMenu.AddSubMenu(new Menu("Forced Targets", ultimateMenu.Name + ".force"));
                    if (DamageCalculation != null)
                    {
                        uForceMenu.AddItem(
                            new MenuItem(uForceMenu.Name + ".damage-check", "Damage Check").SetValue(ForceDamageCheck));
                    }
                    uForceMenu.AddItem(
                        new MenuItem(uForceMenu.Name + ".additional", "Additional Targets").SetValue(
                            new Slider(0, 0, 4)));
                    HeroListManager.AddToMenu(
                        uForceMenu,
                        new HeroListManagerArgs("ultimate-force")
                        {
                            IsWhitelist = true,
                            Allies = false,
                            Enemies = true,
                            DefaultValue = false,
                            DontSave = true,
                            Enabled = true,
                            EnabledButton = false
                        });
                }

                if (Combo)
                {
                    var uComboMenu = ultimateMenu.AddSubMenu(new Menu("Combo", ultimateMenu.Name + ".combo"));
                    uComboMenu.AddItem(
                        new MenuItem(uComboMenu.Name + ".min", "Min. Hits").SetValue(new Slider(2, 1, 5)));
                    if (DamageCalculation != null)
                    {
                        uComboMenu.AddItem(
                            new MenuItem(uComboMenu.Name + ".damage-check", "Damage Check").SetValue(ComboDamageCheck));
                    }
                    uComboMenu.AddItem(new MenuItem(uComboMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                if (Auto)
                {
                    var uAutoMenu = ultimateMenu.AddSubMenu(new Menu("Auto", ultimateMenu.Name + ".auto"));
                    if (Interrupt)
                    {
                        var autoInterruptMenu =
                            uAutoMenu.AddSubMenu(new Menu("Interrupt", uAutoMenu.Name + ".interrupt"));
                        if (InterruptDelay)
                        {
                            DelayManager.AddToMenu(
                                autoInterruptMenu, "ultimate-interrupt-delay", string.Empty, 0, 0, 500);
                        }
                        HeroListManager.AddToMenu(
                            autoInterruptMenu,
                            new HeroListManagerArgs("ultimate-interrupt")
                            {
                                IsWhitelist = false,
                                Allies = false,
                                Enemies = true,
                                DefaultValue = false,
                                DontSave = false,
                                Enabled = true
                            });
                    }
                    if (Gapcloser)
                    {
                        var autoGapcloserMenu =
                            uAutoMenu.AddSubMenu(new Menu("Gapcloser", uAutoMenu.Name + ".gapcloser"));
                        if (GapcloserDelay)
                        {
                            DelayManager.AddToMenu(
                                autoGapcloserMenu, "ultimate-gapcloser-delay", string.Empty, 0, 0, 500);
                        }
                        HeroListManager.AddToMenu(
                            autoGapcloserMenu,
                            new HeroListManagerArgs("ultimate-gapcloser")
                            {
                                IsWhitelist = false,
                                Allies = false,
                                Enemies = true,
                                DefaultValue = false,
                                DontSave = false,
                                Enabled = false
                            });
                        BestTargetOnlyManager.AddToMenu(autoGapcloserMenu, "r-gapcloser", true);
                    }
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".min", "Min. Hits").SetValue(new Slider(3, 1, 5)));
                    if (DamageCalculation != null)
                    {
                        uAutoMenu.AddItem(
                            new MenuItem(uAutoMenu.Name + ".damage-check", "Damage Check").SetValue(AutoDamageCheck));
                    }
                    uAutoMenu.AddItem(new MenuItem(uAutoMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                if (Assisted)
                {
                    var uAssistedMenu = ultimateMenu.AddSubMenu(new Menu("Assisted", ultimateMenu.Name + ".assisted"));
                    if (Flash)
                    {
                        uAssistedMenu.AddItem(
                            new MenuItem(ultimateMenu.Name + ".flash.min", "Flash Min. Hits").SetValue(
                                new Slider(3, 1, 5)));
                    }
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".min", "Min. Hits").SetValue(new Slider(1, 1, 5)));
                    if (Flash)
                    {
                        uAssistedMenu.AddItem(
                            new MenuItem(ultimateMenu.Name + ".flash.hotkey", "Flash").SetValue(
                                new KeyBind('Y', KeyBindType.Press)));
                    }
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".hotkey", "Hotkey").SetValue(
                            new KeyBind('T', KeyBindType.Press)));
                    uAssistedMenu.AddItem(
                        new MenuItem(uAssistedMenu.Name + ".move-cursor", "Move to Cursor").SetValue(true));
                    if (DamageCalculation != null)
                    {
                        uAssistedMenu.AddItem(
                            new MenuItem(uAssistedMenu.Name + ".damage-check", "Damage Check").SetValue(
                                AssistedDamageCheck));
                    }
                    uAssistedMenu.AddItem(new MenuItem(uAssistedMenu.Name + ".enabled", "Enabled").SetValue(true));
                }

                var uSingleMenu = ultimateMenu.AddSubMenu(new Menu("Single Target", ultimateMenu.Name + ".single"));
                uSingleMenu.AddItem(
                    new MenuItem(uSingleMenu.Name + ".min-health", "Min. Target Health %").SetValue(new Slider(15)));
                uSingleMenu.AddItem(
                    new MenuItem(uSingleMenu.Name + ".max-add-allies", "Max. Additional Allies").SetValue(
                        new Slider(3, 0, 4)));
                uSingleMenu.AddItem(
                    new MenuItem(uSingleMenu.Name + ".max-add-enemies", "Max. Additional Enemies").SetValue(
                        new Slider(0, 0, 4)));

                uSingleMenu.AddItem(
                    new MenuItem(uSingleMenu.Name + ".range-allies", "Allies Range Check").SetValue(
                        new Slider(1750, 500, 3000)));
                uSingleMenu.AddItem(
                    new MenuItem(uSingleMenu.Name + ".range-enemies", "Enemies Range Check").SetValue(
                        new Slider(1750, 500, 3000)));

                if (Combo)
                {
                    uSingleMenu.AddItem(new MenuItem(uSingleMenu.Name + ".combo", "Combo").SetValue(true));
                }
                if (Auto)
                {
                    uSingleMenu.AddItem(new MenuItem(uSingleMenu.Name + ".auto", "Auto").SetValue(false));
                }
                if (Assisted)
                {
                    if (Flash)
                    {
                        uSingleMenu.AddItem(new MenuItem(uSingleMenu.Name + ".flash", "Flash").SetValue(false));
                    }
                    uSingleMenu.AddItem(new MenuItem(uSingleMenu.Name + ".assisted", "Assisted").SetValue(false));
                }

                if (DamageCalculation != null)
                {
                    ultimateMenu.AddItem(
                        new MenuItem(ultimateMenu.Name + ".damage-percent-single", "Single Damage Check %").SetValue(
                            new Slider(SingleDamagePercent, 1, 200)));
                    ultimateMenu.AddItem(
                        new MenuItem(ultimateMenu.Name + ".damage-percent", "Damage Check %").SetValue(
                            new Slider(DamagePercent, 1, 200)));
                }

                return ultimateMenu;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return null;
        }

        public bool IsActive(UltimateModeType mode, Obj_AI_Hero hero = null)
        {
            if (_menu != null)
            {
                if (mode == UltimateModeType.Combo)
                {
                    return Combo && _menu.Item(_menu.Name + ".ultimate.combo.enabled").GetValue<bool>();
                }
                if (mode == UltimateModeType.Auto)
                {
                    return Auto && _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>();
                }
                if (mode == UltimateModeType.Flash)
                {
                    return Flash && Assisted && _menu.Item(_menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                           _menu.Item(_menu.Name + ".ultimate.flash.hotkey").GetValue<KeyBind>().Active;
                }
                if (mode == UltimateModeType.Assisted)
                {
                    return Assisted && _menu.Item(_menu.Name + ".ultimate.assisted.enabled").GetValue<bool>() &&
                           _menu.Item(_menu.Name + ".ultimate.assisted.hotkey").GetValue<KeyBind>().Active;
                }
                if (mode == UltimateModeType.Interrupt)
                {
                    return Auto && Interrupt && hero != null &&
                           _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                           HeroListManager.Check("ultimate-interrupt", hero);
                }
                if (mode == UltimateModeType.Gapcloser)
                {
                    return Auto && Gapcloser && hero != null &&
                           _menu.Item(_menu.Name + ".ultimate.auto.enabled").GetValue<bool>() &&
                           HeroListManager.Check("ultimate-gapcloser", hero);
                }
            }
            return false;
        }

        private string GetModeString(UltimateModeType mode, bool overrideFlash)
        {
            if (overrideFlash && mode == UltimateModeType.Flash)
            {
                mode = UltimateModeType.Assisted;
            }
            return mode.ToString().ToLower();
        }

        public bool ShouldMove(UltimateModeType mode)
        {
            if (_menu != null)
            {
                var enabled = _menu.Item(_menu.Name + ".ultimate." + GetModeString(mode, true) + ".move-cursor");
                return enabled != null && enabled.GetValue<bool>();
            }
            return false;
        }

        public bool ShouldSingle(UltimateModeType mode)
        {
            if (_menu != null)
            {
                var enabled = _menu.Item(_menu.Name + ".ultimate.single." + GetModeString(mode, false));
                return enabled != null && enabled.GetValue<bool>();
            }
            return false;
        }

        public float GetDamage(Obj_AI_Hero hero, UltimateModeType mode, int hits = 5)
        {
            if (DamageCalculation != null)
            {
                try
                {
                    var dmgMultiplicator =
                        _menu.Item(_menu.Name + ".ultimate.damage-percent" + (hits <= 1 ? "-single" : string.Empty))
                            .GetValue<Slider>()
                            .Value / 100;
                    return DamageCalculation(hero, dmgMultiplicator, mode != UltimateModeType.Flash) * dmgMultiplicator;
                }
                catch (Exception ex)
                {
                    Global.Logger.AddItem(new LogItem(ex));
                }
            }
            return 0f;
        }

        public int GetMinHits(UltimateModeType mode)
        {
            return _menu.Item(_menu.Name + ".ultimate." + GetModeString(mode, false) + ".min").GetValue<Slider>().Value;
        }

        public bool CheckSingle(UltimateModeType mode, Obj_AI_Hero target)
        {
            try
            {
                if (_menu == null || target == null || !target.IsValidTarget())
                {
                    return false;
                }

                if (ShouldSingle(mode))
                {
                    var minHealth = _menu.Item(_menu.Name + ".ultimate.single.min-health").GetValue<Slider>().Value;
                    if (Spells != null &&
                        !Spells.Any(
                            s =>
                                s.Slot != SpellSlot.R && s.IsReady() && s.IsInRange(target) &&
                                s.GetDamage(target, 1) > 10 && Math.Abs(s.Speed - float.MaxValue) < 1 ||
                                s.From.Distance(target.ServerPosition) / s.Speed + s.Delay <= 1.0f))
                    {
                        minHealth = 0;
                    }
                    if (target.HealthPercent < minHealth)
                    {
                        return false;
                    }

                    var alliesRange = _menu.Item(_menu.Name + ".ultimate.single.range-allies").GetValue<Slider>().Value;
                    var alliesMax = _menu.Item(_menu.Name + ".ultimate.single.max-add-allies").GetValue<Slider>().Value;

                    var enemiesRange = _menu.Item(_menu.Name + ".ultimate.single.range-allies").GetValue<Slider>().Value;
                    var enemiesMax =
                        _menu.Item(_menu.Name + ".ultimate.single.max-add-enemies").GetValue<Slider>().Value;

                    var pos = ObjectManager.Player.Position.Extend(
                        target.Position, ObjectManager.Player.Distance(target) / 2f);
                    var aCount =
                        GameObjects.AllyHeroes.Count(
                            h => h.IsValid && !h.IsDead && !h.IsMe && h.Distance(pos) <= alliesRange);
                    var eCount =
                        GameObjects.EnemyHeroes.Count(
                            h =>
                                h.IsValid && !h.IsDead && h.IsVisible && h.NetworkId != target.NetworkId &&
                                h.Distance(pos) <= enemiesRange);

                    if (aCount > alliesMax || eCount > enemiesMax)
                    {
                        return false;
                    }

                    if (DamageCalculation != null)
                    {
                        if (GetDamage(target, mode, 1) < target.Health)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        public bool Check(UltimateModeType mode, List<Obj_AI_Hero> hits)
        {
            try
            {
                var modeString = GetModeString(mode, true);
                if (_menu == null || hits == null || !hits.Any(h => h.IsValidTarget()))
                {
                    return false;
                }
                if (IsActive(mode))
                {
                    if (mode != UltimateModeType.Gapcloser && mode != UltimateModeType.Interrupt)
                    {
                        if (Force && HeroListManager.Enabled("ultimate-force"))
                        {
                            var dmgCheck = DamageCalculation != null &&
                                           _menu.Item(_menu.Name + ".ultimate.force.damage-check").GetValue<bool>();
                            var additional =
                                _menu.Item(_menu.Name + ".ultimate.force.additional").GetValue<Slider>().Value + 1;
                            if (
                                hits.Any(
                                    hit =>
                                        HeroListManager.Check("ultimate-force", hit) && hits.Count >= additional &&
                                        (!dmgCheck || GetDamage(hit, mode, additional) >= hit.Health)))
                            {
                                return true;
                            }
                        }
                        if (Required && HeroListManager.Enabled("ultimate-required-" + modeString))
                        {
                            var minReq =
                                _menu.Item(_menu.Name + ".ultimate.required." + modeString + ".min")
                                    .GetValue<Slider>()
                                    .Value;
                            var enabledHeroes = HeroListManager.GetEnabledHeroes("ultimate-required-" + modeString);
                            if (minReq > 0 && enabledHeroes.Count > 0)
                            {
                                var count =
                                    enabledHeroes.Where(
                                        e => !e.IsDead && e.IsVisible && e.Distance(ObjectManager.Player) <= 2000)
                                        .Count(e => hits.Any(h => h.NetworkId.Equals(e.NetworkId)));
                                if (count < minReq)
                                {
                                    return false;
                                }
                            }
                        }
                        if (DamageCalculation != null &&
                            _menu.Item(_menu.Name + ".ultimate." + modeString + ".damage-check").GetValue<bool>())
                        {
                            if (hits.All(h => GetDamage(h, mode, hits.Count) < h.Health))
                            {
                                return false;
                            }
                        }
                        return hits.Count >= GetMinHits(mode);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }
    }
}
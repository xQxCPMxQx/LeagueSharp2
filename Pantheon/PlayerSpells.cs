using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Pantheon
{
    internal class PlayerSpells
    {
        private static Menu menu;
        public static SpellSlot SmiteSlot = SpellSlot.Unknown;
        public static SpellSlot IgniteSlot = SpellSlot.Unknown;
        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static string Smitetype
        {
            get
            {
                if (SmiteBlue.Any(i => LeagueSharp.Common.Items.HasItem(i)))
                    return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => LeagueSharp.Common.Items.HasItem(i)))
                    return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => LeagueSharp.Common.Items.HasItem(i)))
                    return "s5_summonersmitequick";

                if (SmitePurple.Any(i => LeagueSharp.Common.Items.HasItem(i)))
                    return "itemsmiteaoe";

                return "summonersmite";
            }
        }

        public static void Initialize()
        {
            SetSmiteSlot();
            menu = new Menu("Smite", "Smite");
            if (SmiteSlot != SpellSlot.Unknown)
            {
                menu.AddItem(new MenuItem("Spells.Smite", "Use Smite Combo for enemy!").SetValue(true));
            }

            SetIgniteSlot();
            if (IgniteSlot != SpellSlot.Unknown)
            {
                menu.AddItem(new MenuItem("Spells.Ignite", "Use Ignite!").SetValue(true));
            }

            if (menu.Items.Count > 0)
                Program.Config.AddSubMenu(menu);

            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                UseSpells();
            }
        }

        private static void UseSpells()
        {
            var t = Program.AssassinManager.GetTarget(Program.Q.Range, TargetSelector.DamageType.Magical);

            if (!t.IsValidTarget())
            {
                return;
            }

            if (SmiteSlot != SpellSlot.Unknown &&
                Program.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
            {
                SmiteOnTarget(t);
            }

            if (IgniteSlot != SpellSlot.Unknown &&
                Program.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                IgniteOnTarget(t);
            }
        }

        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    Program.Player.Spellbook.Spells.Where(
                        spell => string.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
            }
        }

        private static void SetIgniteSlot()
        {
            IgniteSlot = Program.Player.GetSpellSlot("SummonerDot");
        }

        private static void SmiteOnTarget(Obj_AI_Hero t)
        {
            var range = 700f;
            var use = menu.Item("Spells.Smite").GetValue<bool>();
            var itemCheck = SmiteBlue.Any(i => LeagueSharp.Common.Items.HasItem(i)) || SmiteRed.Any(i => LeagueSharp.Common.Items.HasItem(i));
            if (itemCheck && use &&
                Program.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready &&
                t.Distance(Program.Player.Position) < range)
            {
                Program.Player.Spellbook.CastSpell(SmiteSlot, t);
            }
        }

        private static void IgniteOnTarget(Obj_AI_Hero t)
        {
            var range = 550f;
            var use = menu.Item("Spells.Ignite").GetValue<bool>();
            if (use && Program.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                t.Distance(Program.Player.Position) < range &&
                Program.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health)
            {
                Program.Player.Spellbook.CastSpell(IgniteSlot, t);
            }
        }
    }
}
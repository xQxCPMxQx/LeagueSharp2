using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Olafisback
{
    internal class Summoners
    {
        private static Menu menu;
        public static SpellSlot SmiteSlot = SpellSlot.Unknown;
        public static SpellSlot IgniteSlot = SpellSlot.Unknown;
        public static SpellSlot FlashSlot = SpellSlot.Unknown;
        public static SpellSlot TeleportSlot = ObjectManager.Player.GetSpellSlot("SummonerTeleport");
        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static string Smitetype
        {
            get
            {
                if (SmiteBlue.Any(i => Items.HasItem(i)))
                    return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => Items.HasItem(i)))
                    return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => Items.HasItem(i)))
                    return "s5_summonersmitequick";

                if (SmitePurple.Any(i => Items.HasItem(i)))
                    return "itemsmiteaoe";

                return "summonersmite";
            }
        }

        public static void Initialize()
        {
            SetSmiteSlot();
            menu = new Menu("Spells", "Spells");
            if (SmiteSlot != SpellSlot.Unknown)
            {
                Program.MenuCombo.AddItem(new MenuItem("Spells.Smite", "Use Smite to Enemy!").SetValue(true));
            }

            SetIgniteSlot();
            if (IgniteSlot != SpellSlot.Unknown)
            {
                Program.MenuCombo.AddItem(new MenuItem("Spells.Ignite", "Use Ignite!").SetValue(true));
            }

            SetFlatSlot();
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                UseSpells();
        }

        private static void UseSpells()
        {
            var t = TargetSelector.GetTarget(Program.Q.Range, TargetSelector.DamageType.Magical);

            if (!t.IsValidTarget())
                return;

            if (SmiteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
            {
                SmiteOnTarget(t);
            }

            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                IgniteOnTarget(t);
            }
        }

        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => string.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
            }
        }

        private static void SetIgniteSlot()
        {
            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
        }

        private static void SetFlatSlot()
        {
            FlashSlot = ObjectManager.Player.GetSpellSlot("SummonerFlash");
        }


        private static void SmiteOnTarget(Obj_AI_Hero t)
        {
            var range = 700f;
            var use = menu.Item("Spells.Smite").GetValue<bool>();
            var itemCheck = SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i));
            if (itemCheck && use &&
                ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready &&
                t.Distance(ObjectManager.Player.Position) < range)
            {
                ObjectManager.Player.Spellbook.CastSpell(SmiteSlot, t);
            }
        }

        private static void IgniteOnTarget(Obj_AI_Hero t)
        {
            var range = 550f;
            var use = menu.Item("Spells.Ignite").GetValue<bool>();
            if (use && ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                t.Distance(ObjectManager.Player.Position) < range &&
                ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health)
            {
                ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, t);
            }
        }
    }
}
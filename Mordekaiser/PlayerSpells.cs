using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Mordekaiser
{
    internal class PlayerSpells
    {
        private static LeagueSharp.Common.Menu menu;

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
                if (SmiteBlue.Any(i => LeagueSharp.Common.Items.HasItem(i))) return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => LeagueSharp.Common.Items.HasItem(i))) return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => LeagueSharp.Common.Items.HasItem(i))) return "s5_summonersmitequick";

                if (SmitePurple.Any(i => LeagueSharp.Common.Items.HasItem(i))) return "itemsmiteaoe";

                return "summonersmite";
            }
        }

        public static void Initialize()
        {
            SetSmiteSlot();
            menu = new LeagueSharp.Common.Menu("Smite", "Smite");
            if (SmiteSlot != SpellSlot.Unknown)
            {
                menu.AddItem(new MenuItem("Spells.Smite.Enemy", "Use Smite for Enemy!").SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Toggle)));
                menu.AddItem(new MenuItem("Spells.Smite.Monster", "Use Smite for Monsters!").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle)));
            }

            SetIgniteSlot();
            if (IgniteSlot != SpellSlot.Unknown)
            {
                menu.AddItem(new MenuItem("Spells.Ignite", "Use Ignite!").SetValue(true));
            }

            if (menu.Items.Count > 0) Program.Config.AddSubMenu(menu);

            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) UseSpells();

            if (SmiteSlot != SpellSlot.Unknown && Program.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
            {
                SmiteOnTarget();
                SmiteOnMonters();
            }
        }

        private static void UseSpells()
        {

            var t = TargetSelector.GetTarget(Spells.E.Range, TargetSelector.DamageType.Magical);

            if (!t.IsValidTarget()) return;

            if (IgniteSlot != SpellSlot.Unknown && Program.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                IgniteOnTarget(t);
            }
        }

        private static void SetSmiteSlot()
        {
            foreach (var spell in
                Program.Player.Spellbook.Spells.Where(
                    spell => string.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
            }
        }

        private static int GetSmiteDmg()
        {
            int level = Program.Player.Level;
            int index = Program.Player.Level / 5;
            float[] dmgs = { 370 + 20 * level, 330 + 30 * level, 240 + 40 * level, 100 + 50 * level };
            return (int)dmgs[index];
        }

        private static void SetIgniteSlot()
        {
            IgniteSlot = Program.Player.GetSpellSlot("SummonerDot");
        }

        private static void SmiteOnTarget()
        {
            if (!menu.Item("Spells.Smite.Enemy").GetValue<KeyBind>().Active) return;

            var t = TargetSelector.GetTarget(Spells.E.Range, TargetSelector.DamageType.Magical);

            if (!t.IsValidTarget()) return;

            var range = 700f;

            var itemCheck = SmiteBlue.Any(i => LeagueSharp.Common.Items.HasItem(i))
                            || SmiteRed.Any(i => LeagueSharp.Common.Items.HasItem(i));
            if (itemCheck && Program.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready
                && t.Distance(Program.Player.Position) < range)
            {
                Program.Player.Spellbook.CastSpell(SmiteSlot, t);
            }
        }

        private static void SmiteOnMonters()
        {
            if (!menu.Item("Spells.Smite.Monster").GetValue<KeyBind>().Active) return;

            if (Program.Player.Spellbook.CanUseSpell(SmiteSlot) != SpellState.Ready) return;

            string[] jungleMinions;
            if (Utility.Map.GetMap().Type.Equals(Utility.Map.MapType.TwistedTreeline))
            {
                jungleMinions = new string[] { "TT_Spiderboss", "TT_NWraith", "TT_NGolem", "TT_NWolf" };
            }
            else
            {
                jungleMinions = new string[]
                                    {
                                        "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red", "SRU_Krug",
                                        "SRU_Dragon", "SRU_Baron", "Sru_Crab"
                                    };
            }
            var minions = MinionManager.GetMinions(Program.Player.Position, 1000, MinionTypes.All, MinionTeam.Neutral);
            if (minions.Any())
            {
                int smiteDmg = GetSmiteDmg();

                foreach (Obj_AI_Base minion in minions)
                {
                    if (Utility.Map.GetMap().Type.Equals(Utility.Map.MapType.TwistedTreeline)
                        && minion.Health <= smiteDmg
                        && jungleMinions.Any(name => minion.Name.Substring(0, minion.Name.Length - 5).Equals(name)))
                    {
                        Program.Player.Spellbook.CastSpell(SmiteSlot, minion);
                    }
                    if (minion.Health <= smiteDmg && jungleMinions.Any(name => minion.Name.StartsWith(name))
                        && !jungleMinions.Any(name => minion.Name.Contains("Mini")))
                    {
                        Program.Player.Spellbook.CastSpell(SmiteSlot, minion);
                    }
                }
            }
        }

        private static void IgniteOnTarget(Obj_AI_Hero t)
        {
            var range = 550f;
            var use = menu.Item("Spells.Ignite").GetValue<bool>();
            if (use && Program.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready
                && t.Distance(Program.Player.Position) < range
                && Program.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health)
            {
                Program.Player.Spellbook.CastSpell(IgniteSlot, t);
            }
        }
    }
}
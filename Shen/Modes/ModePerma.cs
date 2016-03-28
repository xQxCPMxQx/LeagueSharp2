using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Shen.Champion;

namespace Shen.Modes
{
    internal static class ModePerma
    {
        public static LeagueSharp.Common.Menu LocalMenu { get; private set; }
        public static bool FlashEActive => LocalMenu.Item("Perma.FlashE").GetValue<KeyBind>().Active;
        public static bool FleeActive => LocalMenu.Item("Perma.Flee").GetValue<KeyBind>().Active;
        public static Spell Q => Shen.Champion.PlayerSpells.Q;
        public static Spell W => Shen.Champion.PlayerSpells.W;
        public static Spell E => Shen.Champion.PlayerSpells.E;
        public static Spell R => Shen.Champion.PlayerSpells.R;

        public static void Initialize(LeagueSharp.Common.Menu MenuConfig)
        {
            LocalMenu = MenuConfig;
            LocalMenu.AddItem(new MenuItem("Perma.FlashE", "Flash + E:").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            LocalMenu.AddItem(new MenuItem("Perma.Flee", "Flee").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)).SetFontStyle(System.Drawing.FontStyle.Regular, SharpDX.Color.Coral));

            Game.OnUpdate += GameOnOnUpdate;
        }

        private static void GameOnOnUpdate(EventArgs args)
        {
            if (FlashEActive)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

                var t = Modes.ModeSelector.GetTarget(E.Range + 500, LeagueSharp.Common.TargetSelector.DamageType.Physical);
                
                if (!t.IsValidTarget() || !E.IsReady() || ObjectManager.Player.Spellbook.CanUseSpell(Common.SummonerManager.FlashSlot) != SpellState.Ready)
                {
                    return;
                }

                var hithere = t.Position + Vector3.Normalize(t.ServerPosition - ObjectManager.Player.Position) * 60;
                if (ObjectManager.Player.Distance(t) > E.Range && E.IsReady() &&
                    ObjectManager.Player.Position.Distance(hithere) <= 430 + E.Range &&
                    Common.SummonerManager.FlashSlot != SpellSlot.Unknown &&
                    ObjectManager.Player.Spellbook.CanUseSpell(Common.SummonerManager.FlashSlot) == SpellState.Ready)
                {
                        ObjectManager.Player.Spellbook.CastSpell(Common.SummonerManager.FlashSlot, t.ServerPosition);
                        Utility.DelayAction.Add(100, () => Shen.Champion.PlayerSpells.E.Cast(hithere));
                }
            }

            if (FleeActive)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

                if (E.IsReady())
                {
                    E.Cast(Game.CursorPos);
                }
            }
        }
    }
}
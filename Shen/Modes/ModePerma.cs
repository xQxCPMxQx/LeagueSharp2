using System;
using LeagueSharp;
using LeagueSharp.Common;

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

                var fqTarget = Modes.ModeSelector.GetTarget(E.Range + 500,
                    LeagueSharp.Common.TargetSelector.DamageType.Physical);

                if (ObjectManager.Player.Distance(fqTarget) > E.Range && E.IsReady() && fqTarget != null &&
                    Common.SummonerManager.FlashSlot != SpellSlot.Unknown &&
                    ObjectManager.Player.Spellbook.CanUseSpell(Common.SummonerManager.FlashSlot) == SpellState.Ready)
                {
                    ObjectManager.Player.Spellbook.CastSpell(Common.SummonerManager.FlashSlot, fqTarget.ServerPosition);
                    Utility.DelayAction.Add(100, () => E.Cast(fqTarget.Position));
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
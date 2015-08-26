using System.Drawing;
using System.Linq;
using LeagueSharp.Common;

namespace Mordekaiser
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Net;

    using LeagueSharp;

    using SharpDX;

    using Color = System.Drawing.Color;

    internal class Menu
    {
        public static LeagueSharp.Common.Menu MenuQ;
        public static LeagueSharp.Common.Menu MenuW;
        public static LeagueSharp.Common.Menu MenuE;
        public static LeagueSharp.Common.Menu MenuR;
        public static LeagueSharp.Common.Menu MenuGhost;
        public static LeagueSharp.Common.Menu MenuKeys;
        public static LeagueSharp.Common.Menu MenuItems;
        public static LeagueSharp.Common.Menu MenuDrawings;

        public Menu()
        {
            // Q
            MenuQ = new LeagueSharp.Common.Menu("Q", "Q");
            {
                MenuQ.AddItem(new MenuItem("UseQ.Active", "Use Q").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
                MenuQ.AddItem(new MenuItem("UseQ.Combo", Utils.Tab + "Combo").SetValue(true));
                MenuQ.AddItem(new MenuItem("UseQ.Lane", Utils.Tab + "Lane Clear").SetValue(new StringList(new[] {"Off", "On", "Only Siege/Super Minion"}, 1)));
                MenuQ.AddItem(new MenuItem("UseQ.Jungle", Utils.Tab + "Jungle Clear").SetValue(new StringList(new[] {"Off", "On", "Only Big Mobs"}, 1)));

                MenuQ.AddItem(new MenuItem("UseQ.Mana.Title", "Min. Heal Settings:").SetFontStyle(FontStyle.Regular,SharpDX.Color.BlueViolet));
                {
                    MenuQ.AddItem(new MenuItem("UseQ.Lane.MinHeal", Utils.Tab + "Lane Clear:").SetValue(new Slider(30, 0, 100)));
                    MenuQ.AddItem(new MenuItem("UseQ.Jungle.MinHeal", Utils.Tab + "Jungle Clear:").SetValue(new Slider(30, 0, 100)));
                }
                Program.Config.AddSubMenu(MenuQ);
            }
            // W
            MenuW = new LeagueSharp.Common.Menu("W", "W");
            {
                MenuW.AddItem(new MenuItem("Allies.Active", "Combo").SetValue(true)).SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
                {
                    MenuW.AddItem(new MenuItem("Selected" + Utils.Player.Self.ChampionName, Utils.Tab + Utils.Player.Self.ChampionName + " (Yourself)").SetValue(new StringList(new[] { "Don't", "Combo", "Everytime" }, Utils.TargetSelector.Ally.GetPriority(Utils.Player.Self.ChampionName))));
                    MenuW.AddItem(new MenuItem("SelectedGhost", Utils.Tab + "Dragon / Ghost Enemy").SetValue(new StringList(new[] { "Don't", "Combo", "Everytime" }, Utils.TargetSelector.Ally.GetPriority("Dragon"))));
                    foreach (var ally in HeroManager.Allies.Where(a => !a.IsMe))
                    {
                        MenuW.AddItem(new MenuItem("Selected" + ally.ChampionName, Utils.Tab + ally.CharData.BaseSkinName).SetValue(new StringList(new[] {"Don't", "Combo", "Everytime"}, Utils.TargetSelector.Ally.GetPriority(ally.ChampionName))));
                    }
                    MenuW.AddItem(new MenuItem("Allies.AutoPriority", Utils.Tab + "Auto Arrange Priorities").SetShared().SetValue(false)).ValueChanged += Utils.TargetSelector.Ally.AutoPriorityItemValueChanged;
                }
                MenuW.AddItem(new MenuItem("UseW.DamageRadius", "W Damage Radius Range (Default = 350):").SetValue(new Slider(350, 250, 400)).SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
                MenuW.AddItem(new MenuItem("UseW.Clear.Title", "Lane / Jungle Settings:").SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow));
                {
                    string[] StrLaneMinCount = new string[8];
                    
                    for (var i = 1; i < 7; i++) { StrLaneMinCount[i] = "Minion Count >= " + i; }
                    StrLaneMinCount[0] = "Off"; 
                    StrLaneMinCount[7] = "Auto (Recommend!)";

                    MenuW.AddItem(new MenuItem("UseW.Lane", Utils.Tab + "Lane Clear:").SetValue(new StringList(StrLaneMinCount, 7)));
                    MenuW.AddItem(new MenuItem("UseW.Jungle", Utils.Tab + "JungleClear").SetValue(true));
                }

                MenuW.AddItem(new MenuItem("DrawW.Title", "Drawings").SetFontStyle(FontStyle.Regular,SharpDX.Color.Aqua));
                {
                    MenuW.AddItem(new MenuItem("DrawW.Search", Utils.Tab + "W Range").SetValue(new Circle(true,Color.Aqua)));
                    MenuW.AddItem(new MenuItem("DrawW.DamageRadius", Utils.Tab + "W Damage Radius").SetValue(new Circle(true, Color.Coral)));
                }
                Program.Config.AddSubMenu(MenuW);
            }
            // E
            MenuE = new LeagueSharp.Common.Menu("E", "E");
            {
                MenuE.AddItem(new MenuItem("UseE.Active", "Use E").SetFontStyle(FontStyle.Regular,SharpDX.Color.Aqua));
                MenuE.AddItem(new MenuItem("UseE.Combo", Utils.Tab + "Combo").SetValue(true));
                MenuE.AddItem(new MenuItem("UseE.Harass", Utils.Tab + "Harass").SetValue(true));
                MenuE.AddItem(new MenuItem("UseE.Lane.Title", Utils.Tab + "Lane Clear").SetValue(true));
                MenuE.AddItem(new MenuItem("UseE.Lane", Utils.Tab + "Lane Clear").SetValue(true));
                MenuE.AddItem(new MenuItem("UseE.Jungle", Utils.Tab + "Jungle Clear").SetValue(true));

                MenuE.AddItem(new MenuItem("UseE.Toggle.Title", "Toggle Settings:").SetFontStyle(FontStyle.Regular,SharpDX.Color.GreenYellow));
                {
                    MenuE.AddItem(new MenuItem("UseE.Toggle", Utils.Tab + "E Toggle:").SetValue(new KeyBind("T".ToCharArray()[0],KeyBindType.Toggle)));
                }

                MenuE.AddItem(new MenuItem("UseE.Mana.Title", "Min. Heal Settings:").SetFontStyle(FontStyle.Regular,SharpDX.Color.BlueViolet));
                {
                    MenuE.AddItem(new MenuItem("UseE.Harass.MinHeal", Utils.Tab + "Harass:").SetValue(new Slider(30, 0, 100)));
                    MenuE.AddItem(new MenuItem("UseE.Lane.MinHeal", Utils.Tab + "Lane Clear:").SetValue(new Slider(30, 0, 100)));
                    MenuE.AddItem(new MenuItem("UseE.Jungle.MinHeal", Utils.Tab + "Jungle Clear:").SetValue(new Slider(30, 0, 100)));
                }

                MenuE.AddItem(new MenuItem("DrawE.Title", "Drawings").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
                {
                    MenuE.AddItem(new MenuItem("DrawE.Search", Utils.Tab + "E Range").SetValue(new Circle(true, Color.Aqua)));
                }
                Program.Config.AddSubMenu(MenuE);
            }

            // R
            MenuR = new LeagueSharp.Common.Menu("R", "R");
            {
                MenuR.AddItem(new MenuItem("UseR.Active", "Use R").SetValue(true))
                    .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
                {
                    foreach (var enemy in HeroManager.Enemies)
                    {
                        MenuR.AddItem(new MenuItem("Selected" + enemy.ChampionName, Utils.Tab + enemy.ChampionName).SetValue(new StringList(new[] {"Don't Use", "Low", "Medium", "High"},Utils.TargetSelector.Enemy.GetPriority(enemy.ChampionName))));
                    }
                    MenuR.AddItem(new MenuItem("Enemies.AutoPriority Focus", Utils.Tab + "Auto arrange priorities").SetShared().SetValue(false)).ValueChanged += Utils.TargetSelector.Enemy.AutoPriorityItemValueChanged;
                }


                MenuR.AddItem(new MenuItem("DrawR.Title", "Drawings").SetFontStyle(FontStyle.Regular,SharpDX.Color.Aqua));
                {
                    MenuR.AddItem(new MenuItem("DrawR.Search", Utils.Tab + "R Skill Range").SetValue(new Circle(true,Color.GreenYellow)));
                    MenuR.AddItem(new MenuItem("DrawR.Status.Show", Utils.Tab + "Targeting Notification:").SetValue(new StringList(new[] {"Off", "On", "Only High Target"})));
                }
                Program.Config.AddSubMenu(MenuR);
            }

            //ghost
            MenuGhost = new LeagueSharp.Common.Menu("Ghost", "Ghost").SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
            {
                MenuGhost.AddItem(new MenuItem("Ghost.Title", "What do you want with the Ghost?").SetFontStyle(FontStyle.Regular,SharpDX.Color.Aqua));
                MenuGhost.AddItem(new MenuItem("Ghost.Use", Utils.Tab + "Do this:").SetValue(new StringList(new[]{"Nothing / Manual Control", "Fight With Me!", "Attack/Harass to High Priority Target(s)"}, 1)));

                MenuGhost.AddItem(new MenuItem("Ghost.Draw.Ghost.Title", "Drawings").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
                {
                    MenuGhost.AddItem(new MenuItem("Ghost.Draw.Position", Utils.Tab + "Ghost Position").SetValue(new Circle(true, Color.DarkRed)));
                    MenuGhost.AddItem(new MenuItem("Ghost.Draw.AARange", Utils.Tab + "Ghost AA Range").SetValue(new Circle(true, Color.DarkRed)));
                    MenuGhost.AddItem(new MenuItem("Ghost.Draw.ControlRange", Utils.Tab + "Ghost Control Range").SetValue(new Circle(true, Color.WhiteSmoke)));
                }

                Program.Config.AddSubMenu(MenuGhost);
            }

            MenuKeys = new LeagueSharp.Common.Menu("Keys", "Keys").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua);
            {
                MenuKeys.AddItem(
                    new MenuItem("Keys.Combo", "Combo").SetValue(
                        new KeyBind(Program.Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)))
                    .SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
                MenuKeys.AddItem(
                    new MenuItem("Keys.Harass", "Harass").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                MenuKeys.AddItem(
                    new MenuItem("Keys.Lane", "Lane Clear").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                MenuKeys.AddItem(
                    new MenuItem("Keys.Jungle", "Jungle Clear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
                Program.Config.AddSubMenu(MenuKeys);
            }

            MenuItems = new LeagueSharp.Common.Menu("Items", "Items").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua); ;
            {
                MenuItems.AddItem(new MenuItem("Items.Title", "Use Items on This Mode:").SetFontStyle(
                    FontStyle.Regular, SharpDX.Color.Aqua));
                MenuItems.AddItem(new MenuItem("Items.Combo", Utils.Tab + "Combo").SetValue(true));
                MenuItems.AddItem(new MenuItem("Items.Lane", Utils.Tab + "Lane Clear").SetValue(true));
                MenuItems.AddItem(new MenuItem("Items.Jungle", Utils.Tab + "Jungle Clear").SetValue(true));
                Program.Config.AddSubMenu(MenuItems);
            }

            MenuDrawings = new LeagueSharp.Common.Menu("Drawings", "Drawings");
            {
                /* [ Damage After Combo ] */
                var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
                MenuDrawings.AddItem(dmgAfterComboItem);
                MenuDrawings.AddItem(new MenuItem("Draw.Calc.Q", Utils.Tab + "Q Damage").SetValue(true));
                MenuDrawings.AddItem(new MenuItem("Draw.Calc.W", Utils.Tab + "W Damage").SetValue(true));
                MenuDrawings.AddItem(new MenuItem("Draw.Calc.E", Utils.Tab + "E Damage").SetValue(true));
                MenuDrawings.AddItem(new MenuItem("Draw.Calc.R", Utils.Tab + "R Damage").SetValue(true));
                MenuDrawings.AddItem(new MenuItem("Draw.Calc.I", Utils.Tab + "Ignite Damage").SetValue(true).SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
                MenuDrawings.AddItem(new MenuItem("Draw.Calc.T", Utils.Tab + "Item Damage").SetValue(true).SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
                if (PlayerSpells.SmiteSlot != SpellSlot.Unknown)
                {
                    MenuDrawings.AddItem(
                        new MenuItem("Calc.S", Utils.Tab + "Smite Damage").SetValue(true)
                            .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua));
                }

                Utility.HpBarDamageIndicator.DamageToUnit = Program.DamageCalc.GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
                dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };
                Program.Config.AddSubMenu(MenuDrawings);
            }
        }
    }
}
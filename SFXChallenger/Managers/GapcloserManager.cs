#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 GapcloserManager.cs is part of SFXChallenger.

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
using SFXChallenger.Library;
using SFXChallenger.Library.Extensions.NET;
using SFXChallenger.Library.Logger;
using SharpDX;

#endregion

namespace SFXChallenger.Managers
{
    public static class GapcloserManager
    {
        private static readonly Dictionary<string, Menu> Menues = new Dictionary<string, Menu>();
        private static readonly Random Random = new Random();

        private static readonly List<GapcloserSpell> GapcloserSpells = new List<GapcloserSpell>
        {
            #region Spells
            new GapcloserSpell { Champion = "Aatrox", Slot = SpellSlot.Q, Name = "aatroxq" },
            new GapcloserSpell { Champion = "Akali", Slot = SpellSlot.R, Name = "akalishadowdance", IsTargeted = true },
            new GapcloserSpell { Champion = "Alistar", Slot = SpellSlot.W, Name = "headbutt", IsTargeted = true },
            new GapcloserSpell { Champion = "Corki", Slot = SpellSlot.W, Name = "carpetbomb" },
            new GapcloserSpell { Champion = "Diana", Slot = SpellSlot.R, Name = "dianateleport", IsTargeted = true },
            new GapcloserSpell { Champion = "Elise", Slot = SpellSlot.Q, Name = "elisespiderqcast" },
            new GapcloserSpell
            {
                Champion = "Elise",
                Slot = SpellSlot.E,
                Name = "elisespideredescent",
                IsTargeted = true
            },
            new GapcloserSpell { Champion = "Fiora", Slot = SpellSlot.Q, Name = "fioraq", IsTargeted = true },
            new GapcloserSpell
            {
                Champion = "Fizz",
                Slot = SpellSlot.Q,
                Name = "fizzpiercingstrike",
                IsTargeted = true,
                IsUnitDash = true,
                DashDistance = 550f
            },
            new GapcloserSpell { Champion = "Gnar", Slot = SpellSlot.E, Name = "gnarbige" },
            new GapcloserSpell { Champion = "Gnar", Slot = SpellSlot.E, Name = "gnare" },
            new GapcloserSpell { Champion = "Gragas", Slot = SpellSlot.E, Name = "gragase", Collision = true },
            new GapcloserSpell { Champion = "Graves", Slot = SpellSlot.E, Name = "gravesmove" },
            new GapcloserSpell { Champion = "Hecarim", Slot = SpellSlot.R, Name = "hecarimult" },
            new GapcloserSpell { Champion = "Irelia", Slot = SpellSlot.Q, Name = "ireliagatotsu", IsTargeted = true },
            new GapcloserSpell { Champion = "JarvanIV", Slot = SpellSlot.Q, Name = "jarvanivdragonstrike" },
            new GapcloserSpell { Champion = "Jax", Slot = SpellSlot.Q, Name = "jaxleapstrike", IsTargeted = true },
            new GapcloserSpell { Champion = "Jayce", Slot = SpellSlot.Q, Name = "jaycetotheskies", IsTargeted = true },
            new GapcloserSpell { Champion = "Kassadin", Slot = SpellSlot.R, Name = "riftwalk" },
            new GapcloserSpell { Champion = "Khazix", Slot = SpellSlot.E, Name = "khazixe" },
            new GapcloserSpell { Champion = "Khazix", Slot = SpellSlot.E, Name = "khazixelong" },
            new GapcloserSpell { Champion = "LeBlanc", Slot = SpellSlot.W, Name = "leblancslide" },
            new GapcloserSpell { Champion = "LeBlanc", Slot = SpellSlot.R, Name = "leblancslidem" },
            new GapcloserSpell { Champion = "LeeSin", Slot = SpellSlot.Q, Name = "blindmonkqtwo", IsTargeted = true },
            new GapcloserSpell { Champion = "Leona", Slot = SpellSlot.E, Name = "leonazenithblade" },
            new GapcloserSpell { Champion = "Lucian", Slot = SpellSlot.E, Name = "luciane" },
            new GapcloserSpell { Champion = "Malphite", Slot = SpellSlot.R, Name = "ufslash" },
            new GapcloserSpell { Champion = "MasterYi", Slot = SpellSlot.Q, Name = "alphastrike", IsTargeted = true },
            new GapcloserSpell
            {
                Champion = "MonkeyKing",
                Slot = SpellSlot.E,
                Name = "monkeykingnimbus",
                IsTargeted = true
            },
            new GapcloserSpell
            {
                Champion = "Pantheon",
                Slot = SpellSlot.W,
                Name = "pantheon_leapbash",
                IsTargeted = true
            },
            new GapcloserSpell { Champion = "Pantheon", Slot = SpellSlot.R, Name = "pantheonrjump" },
            new GapcloserSpell { Champion = "Pantheon", Slot = SpellSlot.R, Name = "pantheonrfall" },
            new GapcloserSpell
            {
                Champion = "Poppy",
                Slot = SpellSlot.E,
                Name = "poppyheroiccharge",
                IsTargeted = true,
                IsUnitDash = true,
                DashDistance = 475f
            },
            new GapcloserSpell { Champion = "Renekton", Slot = SpellSlot.E, Name = "renektonsliceanddice" },
            new GapcloserSpell { Champion = "Riven", Slot = SpellSlot.Q, Name = "riventricleave" },
            new GapcloserSpell { Champion = "Riven", Slot = SpellSlot.E, Name = "rivenfeint" },
            new GapcloserSpell
            {
                Champion = "Sejuani",
                Slot = SpellSlot.Q,
                Name = "sejuaniarcticassault",
                Collision = true
            },
            new GapcloserSpell { Champion = "Shen", Slot = SpellSlot.E, Name = "shenshadowdash" },
            new GapcloserSpell { Champion = "Shyvana", Slot = SpellSlot.R, Name = "shyvanatransformcast" },
            new GapcloserSpell { Champion = "Talon", Slot = SpellSlot.E, Name = "taloncutthroat", IsTargeted = true },
            new GapcloserSpell { Champion = "Tristana", Slot = SpellSlot.W, Name = "rocketjump" },
            new GapcloserSpell { Champion = "Tryndamere", Slot = SpellSlot.E, Name = "slashcast" },
            new GapcloserSpell { Champion = "Vi", Slot = SpellSlot.Q, Name = "viq" },
            new GapcloserSpell { Champion = "XinZhao", Slot = SpellSlot.E, Name = "xenzhaosweep", IsTargeted = true },
            new GapcloserSpell
            {
                Champion = "Yasuo",
                Slot = SpellSlot.E,
                Name = "yasuodashwrapper",
                IsTargeted = true,
                IsUnitDash = true,
                DashDistance = 475f
            },
            new GapcloserSpell { Champion = "Zac", Slot = SpellSlot.E, Name = "zace" },
            new GapcloserSpell { Champion = "Ziggs", Slot = SpellSlot.W, Name = "ziggswtoggle" }
            #endregion Spells
        };

        static GapcloserManager()
        {
            Obj_AI_Base.OnProcessSpellCast += OnObjAiBaseProcessSpellCast;
            CustomEvents.Unit.OnDash += OnUnitDash;
        }

        public static void AddToMenu(Menu menu, HeroListManagerArgs args, bool dangerous = false)
        {
            try
            {
                if (Menues.ContainsKey(args.UniqueId))
                {
                    throw new ArgumentException(
                        string.Format("GapcloserManager: UniqueID \"{0}\" already exist.", args.UniqueId));
                }

                args.Enemies = true;
                args.Allies = false;

                menu.AddItem(
                    new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".delay", "Delay").SetValue(
                        new Slider(100, 0, 500)));
                menu.AddItem(
                    new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".randomize", "Randomize Position").SetValue(
                        new Slider(10)));
                menu.AddItem(
                    new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".distance", "Min. Distance").SetValue(
                        new Slider(150, 0, 500)));
                menu.AddItem(
                    new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".dangerous", "Only Dangerous").SetValue(
                        dangerous));

                menu.AddItem(new MenuItem(menu.Name + ".gap-" + args.UniqueId + ".separator", string.Empty));

                HeroListManager.AddToMenu(menu, args);

                Menues[args.UniqueId] = menu;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        public static event EventHandler<GapcloserManagerArgs> OnGapcloser;

        private static void OnObjAiBaseProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                var enemy = sender as Obj_AI_Hero;
                if (enemy == null || !enemy.IsEnemy)
                {
                    return;
                }
                var gSpell =
                    GapcloserSpells.FirstOrDefault(
                        spell => spell.Name.Equals(args.SData.Name, StringComparison.OrdinalIgnoreCase));
                if (!gSpell.Equals(default(GapcloserSpell)))
                {
                    var startPosition = args.Start;
                    var endPosition = args.End;
                    var cSpell = enemy.GetSpell(gSpell.Slot);
                    if (gSpell.IsTargeted && args.Target != null)
                    {
                        endPosition = args.Target.Position;
                    }
                    if (gSpell.IsUnitDash)
                    {
                        endPosition = startPosition.Extend(endPosition, gSpell.DashDistance);
                    }
                    if (gSpell.Collision)
                    {
                        if (cSpell != null)
                        {
                            var colObjects =
                                GameObjects.AllyHeroes.Select(a => a as Obj_AI_Base)
                                    .Concat(GameObjects.AllyMinions.Where(m => m.Distance(enemy) <= 2000))
                                    .OrderBy(c => c.Distance(enemy))
                                    .ToList();
                            var rect = new Geometry.Polygon.Rectangle(
                                startPosition, endPosition, cSpell.SData.LineWidth + enemy.BoundingRadius);
                            var collision =
                                colObjects.FirstOrDefault(
                                    col =>
                                        new Geometry.Polygon.Circle(col.ServerPosition, col.BoundingRadius).Points.Any(
                                            p => rect.IsInside(p)));
                            if (collision != null)
                            {
                                endPosition = collision.ServerPosition.Extend(
                                    startPosition, collision.BoundingRadius + enemy.BoundingRadius);
                                if (collision is Obj_AI_Minion && endPosition.Distance(startPosition) <= 100 &&
                                    !GameObjects.AllyHeroes.Any(a => a.Distance(endPosition) <= 150))
                                {
                                    return;
                                }
                            }
                        }
                    }
                    var endTime = Game.Time;
                    if (cSpell != null)
                    {
                        var time = startPosition.Distance(endPosition) /
                                   Math.Max(cSpell.SData.MissileSpeed, enemy.MoveSpeed * 1.25f);
                        if (time <= 3)
                        {
                            endTime += time;
                        }
                    }
                    Check(false, enemy, startPosition, endPosition, endTime, gSpell.IsTargeted);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void OnUnitDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            try
            {
                var hero = sender as Obj_AI_Hero;
                if (hero != null && hero.IsEnemy)
                {
                    Utility.DelayAction.Add(
                        100,
                        delegate
                        {
                            Check(
                                true, hero, args.StartPos.To3D(), args.EndPos.To3D(), args.EndTick / 1000f - 0.1f, false);
                        });
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static void Check(bool dash,
            Obj_AI_Hero sender,
            Vector3 startPosition,
            Vector3 endPosition,
            float endTime,
            bool targeted)
        {
            try
            {
                if (!sender.IsValid || !sender.IsEnemy || sender.IsDead)
                {
                    return;
                }
                if (Game.Time - endTime >= 5)
                {
                    return;
                }
                if (endPosition.Distance(ObjectManager.Player.ServerPosition) >= 2000)
                {
                    return;
                }

                foreach (var entry in Menues)
                {
                    var uniqueId = entry.Key;
                    var menu = entry.Value;
                    if (HeroListManager.Check(uniqueId, sender))
                    {
                        var distance = menu.Item(menu.Name + ".gap-" + uniqueId + ".distance").GetValue<Slider>().Value;
                        var dangerous = menu.Item(menu.Name + ".gap-" + uniqueId + ".dangerous").GetValue<bool>();
                        if (startPosition.Distance(ObjectManager.Player.Position) >= distance &&
                            (!dangerous || IsDangerous(sender, startPosition, endPosition, targeted)))
                        {
                            var delay = menu.Item(menu.Name + ".gap-" + uniqueId + ".delay").GetValue<Slider>().Value;
                            var randomize =
                                menu.Item(menu.Name + ".gap-" + uniqueId + ".randomize").GetValue<Slider>().Value;
                            if (delay > 1)
                            {
                                delay = Random.Next((int) (delay * 0.9f), (int) (delay * 1.1f));
                            }
                            if (randomize > 0)
                            {
                                if (!startPosition.Equals(Vector3.Zero))
                                {
                                    startPosition.X += Random.Next(0, randomize * 2 + 1) - randomize;
                                    startPosition.Y += Random.Next(0, randomize * 2 + 1) - randomize;
                                }
                                if (!endPosition.Equals(Vector3.Zero))
                                {
                                    endPosition.X += Random.Next(0, randomize * 2 + 1) - randomize;
                                    endPosition.Y += Random.Next(0, randomize * 2 + 1) - randomize;
                                }
                            }
                            Utility.DelayAction.Add(
                                Math.Max(1, dash ? delay - 100 : delay),
                                () =>
                                    OnGapcloser.RaiseEvent(
                                        null,
                                        new GapcloserManagerArgs(uniqueId, sender, startPosition, endPosition, endTime)));
                        }
                    }
                }
                OnGapcloser.RaiseEvent(
                    null, new GapcloserManagerArgs(string.Empty, sender, startPosition, endPosition, endTime));
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private static bool IsDangerous(Obj_AI_Hero sender, Vector3 startPosition, Vector3 endPosition, bool targeted)
        {
            try
            {
                var endDistance = endPosition.Distance(ObjectManager.Player.Position);
                var startDistance = startPosition.Distance(ObjectManager.Player.Position);
                if (targeted)
                {
                    return true;
                }
                if (endDistance <= 150f)
                {
                    return true;
                }
                if (startDistance - 100f > endDistance)
                {
                    var spell = sender.GetSpell(SpellSlot.R);
                    if (spell != null && endDistance <= 600)
                    {
                        return spell.Cooldown >= 20 && spell.IsReady(2500);
                    }
                    if (endDistance <= 500 && ObjectManager.Player.HealthPercent < 50)
                    {
                        return true;
                    }
                }
                if (endDistance > startDistance)
                {
                    return false;
                }
                if (endDistance >= 450)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return true;
        }

        private struct GapcloserSpell
        {
#pragma warning disable 414
            public string Champion;
#pragma warning restore 414
            public bool Collision;
            public float DashDistance;
            public bool IsTargeted;
            public bool IsUnitDash;
            public string Name;
            public SpellSlot Slot;
        }
    }
}
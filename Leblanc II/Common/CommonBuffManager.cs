using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Leblanc.Common
{

    public class CommonBuffManager
    {
        public static List<JungleBuffs> JungleBuffs = new List<JungleBuffs>();
        public static List<PassiveBuffs> PassiveBuffs = new List<PassiveBuffs>();

        void InitJungleBuffs()
        {
            #region Blue

            JungleBuffs.Add(new JungleBuffs
            {
                Number = 1,
                BuffName = "CrestoftheAncientGolem",
                Color = System.Drawing.Color.Blue
            });

            #endregion Blue

            #region Red

            JungleBuffs.Add(new JungleBuffs
            {
                Number = 0,
                BuffName = "BlessingoftheLizardElder",
                Color = System.Drawing.Color.Red
            });

            #endregion Red

            #region RiftHerald

            JungleBuffs.Add(new JungleBuffs
            {
                Number = 2,
                BuffName = "RiftHeraldBuffCounter",
                Color = System.Drawing.Color.Indigo
            });
            #endregion RiftHerald
        }
    
        void InitPassiveBuffs()
        {
            #region Teleport
            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Teleport",
                    BuffName = "Teleport",
                    Color = Color.Bisque
                });
            #endregion Zhonya

            #region Zhonya
            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Zhonya",
                    BuffName = "Zhonyas Ring",
                    Color = Color.Bisque
                });
            #endregion Zhonya

            #region Aatrox
            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Aatrox",
                    BuffName = "AatroxWONHLifeBuff",
                    Color = Color.FromArgb(85, 4, 144)
                });

            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Aatrox",
                    BuffName = "AatroxPassiveActivate",
                    Color = Color.FromArgb(85, 4, 144)
                });
            #endregion Aatrox

            #region Anivia
            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Anivia Passive",
                    BuffName = "Rebirth",
                    Color = Color.FromArgb(85, 4, 144)
                });

            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Anivia",
                    BuffName = "RebirthCooldown",
                    Color = Color.FromArgb(85, 4, 144)
                });
            #endregion Anivia

            #region Volibear

            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Volibear",
                    BuffName = "VolibearPassiveCD",
                    Color = Color.Red
                });
            #endregion Volibear

            #region Zac
            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Zac",
                    BuffName = "ZacRebirthCooldown",
                    Color = Color.FromArgb(85, 4, 144)
                });

            PassiveBuffs.Add(
                new PassiveBuffs
                {
                    ChampionName = "Zac",
                    BuffName = "zacrebirthstart",
                    Color = Color.FromArgb(85, 4, 144)
                });
            #endregion Anivia            
        }

        public CommonBuffManager()
        {
            InitPassiveBuffs();
            InitJungleBuffs();

            Drawing.OnDraw += DrawingOnOnDraw;

        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            if (!Modes.ModeDraw.MenuLocal.Item("Draw.Enable").GetValue<bool>())
            {
                return;
            }

            foreach (var hero in HeroManager.AllHeroes)
            {
                var passiveBuffs = (from b in hero.Buffs join b1 in PassiveBuffs on b.DisplayName equals b1.BuffName select new {b, b1}).Distinct();
                foreach (var buffName in passiveBuffs)
                {
                    for (int i = 0; i < passiveBuffs.Count(); i++)
                    {
                        if (buffName.b.EndTime >= Game.Time)
                        {
                            CommonGeometry.DrawBox(new Vector2(hero.HPBarPosition.X + 10, (i*8) + hero.HPBarPosition.Y + 32), 130, 6,Color.FromArgb(100, 255, 200, 37), 1, Color.Black);

                            var buffTime = buffName.b.EndTime - buffName.b.StartTime;
                            CommonGeometry.DrawBox(new Vector2(hero.HPBarPosition.X + 11, (i*8) + hero.HPBarPosition.Y + 33),(130/buffTime)*(buffName.b.EndTime - Game.Time), 4, buffName.b1.Color, 1,buffName.b1.Color);

                            TimeSpan timeSpan = TimeSpan.FromSeconds(buffName.b.EndTime - Game.Time);
                            var timer = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                            CommonGeometry.DrawText(CommonGeometry.TextPassive, timer, hero.HPBarPosition.X + 142,(i*8) + hero.HPBarPosition.Y + 29, SharpDX.Color.Wheat);
                        }
                    }
                }

                var jungleBuffs = (from b in hero.Buffs join b1 in JungleBuffs on b.DisplayName equals b1.BuffName select new { b, b1 }).Distinct();
                foreach (var buffName in jungleBuffs.ToList())
                {
                    var circle1 = new CommonGeometry.Circle2(new Vector2(hero.Position.X + 3, hero.Position.Y - 3), 140 + (buffName.b1.Number * 20), Game.Time - buffName.b.StartTime, buffName.b.EndTime - buffName.b.StartTime).ToPolygon();
                    circle1.Draw(Color.Black, 3);

                    var circle = new CommonGeometry.Circle2(hero.Position.To2D(), 140 + (buffName.b1.Number * 20), Game.Time - buffName.b.StartTime, buffName.b.EndTime - buffName.b.StartTime).ToPolygon();
                    circle.Draw(buffName.b1.Color, 3);
                }
            }
        }
    }

    public class JungleBuffs
    {
        public int Number;
        public string BuffName;
        public System.Drawing.Color Color;

        public JungleBuffs() { }

        public JungleBuffs(int number, string buffName, System.Drawing.Color color)
        {
            Number = number;
            BuffName = buffName;
            Color = color;
        }
    }

    public class PassiveBuffs
    {
        public string ChampionName;
        public string BuffName;
        public System.Drawing.Color Color;

        public PassiveBuffs() { }

        public PassiveBuffs(string championName, string buffName, System.Drawing.Color color)
        {
            ChampionName = championName;
            BuffName = buffName;
            Color = color;
        }
    }


}

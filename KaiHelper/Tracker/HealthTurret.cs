using System;
using System.Globalization;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace KaiHelper.Tracker
{
    internal class HealthTurret
    {
        public Menu MenuHealthTurret;
        public Font Text;

        public HealthTurret(Menu config)
        {
            MenuHealthTurret = config.AddSubMenu(new Menu("Health", "Health"));
            MenuHealthTurret.AddItem(
                new MenuItem("TIHealth", "Turret & Inhibitor Health").SetValue(
                    new StringList(new[] { "Percent", "Health " })));
            MenuHealthTurret.AddItem(new MenuItem("HealthActive", "Active").SetValue(true));
            Text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Calibri",
                    Height = 13,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                });
            Drawing.OnEndScene += DrawTurrentHealth;
        }

        private bool IsActive()
        {
            return MenuHealthTurret.Item("HealthActive").GetValue<bool>();
        }

        private void DrawTurrentHealth(EventArgs args)
        {
            if (!IsActive())
            {
                return;
            }
            foreach (Obj_AI_Turret turret in ObjectManager.Get<Obj_AI_Turret>())
            {
                if ((turret.HealthPercentage() == 100))
                {
                    continue;
                }
                int health = 0;
                switch (MenuHealthTurret.Item("TIHealth").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        health = (int) turret.HealthPercentage();
                        break;

                    case 1:
                        health = (int) turret.Health;
                        break;
                }
                Vector2 pos = Drawing.WorldToMinimap(turret.Position);
                var perHealth = (int) turret.HealthPercentage();
                if (perHealth >= 75)
                {
                    Helper.DrawText(
                        Text, health.ToString(CultureInfo.InvariantCulture), (int) pos[0], (int) pos[1], Color.LimeGreen);
                }
                else if (perHealth < 75 && perHealth >= 50)
                {
                    Helper.DrawText(
                        Text, health.ToString(CultureInfo.InvariantCulture), (int) pos[0], (int) pos[1],
                        Color.YellowGreen);
                }
                else if (perHealth < 50 && perHealth >= 25)
                {
                    Helper.DrawText(
                        Text, health.ToString(CultureInfo.InvariantCulture), (int) pos[0], (int) pos[1], Color.Orange);
                }
                else if (perHealth < 25)
                {
                    Helper.DrawText(
                        Text, health.ToString(CultureInfo.InvariantCulture), (int) pos[0], (int) pos[1], Color.Red);
                }
            }
            foreach (Obj_BarracksDampener inhibitor in ObjectManager.Get<Obj_BarracksDampener>())
            {
                if (inhibitor.Health != 0 && (inhibitor.Health / inhibitor.MaxHealth) * 100 != 100)
                {
                    Vector2 pos = Drawing.WorldToMinimap(inhibitor.Position);
                    var health = (int) ((inhibitor.Health / inhibitor.MaxHealth) * 100);
                    if (health >= 75)
                    {
                        Helper.DrawText(
                            Text, health.ToString(CultureInfo.InvariantCulture), (int) pos[0], (int) pos[1],
                            Color.LimeGreen);
                    }
                    else if (health < 75 && health >= 50)
                    {
                        Helper.DrawText(
                            Text, health.ToString(CultureInfo.InvariantCulture), (int) pos[0], (int) pos[1],
                            Color.YellowGreen);
                    }
                    else if (health < 50 && health >= 25)
                    {
                        Helper.DrawText(
                            Text, health.ToString(CultureInfo.InvariantCulture), (int) pos[0], (int) pos[1],
                            Color.Orange);
                    }
                    else if (health < 25)
                    {
                        Helper.DrawText(
                            Text, health.ToString(CultureInfo.InvariantCulture), (int) pos[0], (int) pos[1], Color.Red);
                    }
                }
            }
        }
    }
}
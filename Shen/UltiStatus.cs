using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


namespace Shen
{
    internal class UltiStatus
    {
        public static Menu LocalMenu;
        private static Render.Sprite _sprite;

        private static string MenuTab
        {
            get { return "    "; }
        }

        public UltiStatus()
        {
            Load();
        }

        private static void Load()
        {
            LocalMenu = new Menu("Ulti Status", "UltiStatus");
            Drawing.OnDraw += Drawing_OnDraw;
        }

        public static void DrawLineInWorld(Vector3 start, Vector3 end, int width, Color color)
        {
            Drawing.DrawLine(start.X, start.Y, end.X, end.Y, width, color);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {

            var allies = HeroManager.Allies.Where(a => !a.IsMe);
            var objAiHeroes = allies as Obj_AI_Hero[] ?? allies.ToArray(); 

            for (var i = 0; i < objAiHeroes.Count(); i++)
            {
                Drawing.DrawLine(Drawing.Width*0.892f + 0, Drawing.Height*0.479f + (float) (i + 1)*20, Drawing.Width*0.895f + 150, Drawing.Height*0.479f + (float) (i + 1)*20, 16, Color.Black);
                Drawing.DrawLine(Drawing.Width*0.892f + 1, Drawing.Height*0.480f + (float) (i + 1)*20, Drawing.Width*0.895f + 149, Drawing.Height*0.480f + (float) (i + 1)*20, 14, Color.BurlyWood);

                int hPercent = (int) Math.Ceiling((objAiHeroes[i].Health*150/objAiHeroes[i].MaxHealth));
                if (hPercent > 0)
                {
                    Drawing.DrawLine(Drawing.Width*0.892f + 1, Drawing.Height*0.480f + (float) (i + 1)*20,
                        Drawing.Width*0.895f + hPercent - 1, Drawing.Height*0.480f + (float) (i + 1)*20,
                        14,
                        hPercent < 50 && hPercent > 30
                            ? Color.Yellow
                            : hPercent <= 30 ? Color.Red : Color.DarkOliveGreen);
                }


                Utils.DrawText(Utils.Text, objAiHeroes[i].ChampionName, Drawing.Width * 0.895f, Drawing.Height * 0.48f + (float)(i + 1) * 20, SharpDX.Color.Black);
                //Utils.DrawText(Utils.Text, objAiHeroes[i].ChampionName + ": " + hPercent + "%", Drawing.Width * 0.895f, Drawing.Height * 0.48f + (float)(i + 1) * 20, SharpDX.Color.Black);

            }

            var t = Utils.ChampAlly;
            if (t != null && !ObjectManager.Player.IsDead && Program.R.IsReady() && Program.Config.Item("Draw.Notification").GetValue<bool>())
            {
                var xKey = char.ConvertFromUtf32((int)Program.Config.Item("ComboUseRK").GetValue<KeyBind>().Key);
                var xText = "Press "+ xKey + " for Ulti: " + t.ChampionName;
                Utils.DrawText(Utils.TextWarning, xText, Drawing.Width * 0.322f, Drawing.Height * 0.442f, SharpDX.Color.Black);
                Utils.DrawText(Utils.TextWarning, xText, Drawing.Width * 0.32f, Drawing.Height * 0.44f, SharpDX.Color.White);

                Utils.DrawText(Utils.Text, "You can Turn Off this message! Please Check 'Protector Settings -> Show Notification Text'", Drawing.Width*0.325f, Drawing.Height*0.52f, SharpDX.Color.White);
            }
        }
    }
}
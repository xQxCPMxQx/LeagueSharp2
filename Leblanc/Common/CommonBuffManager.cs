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


        public CommonBuffManager()
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

   
}

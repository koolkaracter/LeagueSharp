using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlsSaveMe
{
    class Program
    {
        public static Menu config;
        public static Items.Item QSS = new Items.Item(3140, 0);
        public static Items.Item Dervish = new Items.Item(3137, 0);
        public static Items.Item Zhonya = new Items.Item(3157, 0);
        public static Items.Item Wooglet = new Items.Item(3090, 0);
        public static bool trigger =false;
        private static List<string> dotsHighDmg = new List<string>(new string[] {"zedulttargetmark", "VladimirHemoplague"});
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- PlsSaveMe</font>");
            Game.OnUpdate += Game_OnGameUpdate;
            //Drawing.OnDraw += Game_OnDraw;
            LoadMenu();
        }

        private static void LoadMenu()
        {
            config = new Menu("PlsSaveMe", "PlsSaveMe", true);
            config.AddItem(new MenuItem("Enable", "Enable")).SetValue(false);
            config.AddToMainMenu();
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (config.Item("Enable").GetValue<bool>())
            {
                var delay = -1;
                foreach (BuffInstance buff in player.Buffs)
                {
                    switch (buff.Name)
                    {
                        case "zedulttargetmark":
                            delay = 2900;
                            break;
                        case "VladimirHemoplague":
                            delay = 4900;
                            break;
                    }
                }
                if (delay > 0 && !trigger)
                {
                    trigger = true;
                    Utility.DelayAction.Add(2900, () =>
                    {
                        SaveMe();
                        trigger = false;
                    });
                }
            }
        }
    

        private static void SaveMe()
        {
            if (Items.HasItem(QSS.Id) && Items.CanUseItem(QSS.Id))
            {
                QSS.Cast();
                return;
            }
            if (Items.HasItem(Dervish.Id) && Items.CanUseItem(Dervish.Id))
            {
                Dervish.Cast();
                return;
            }
            if (Items.HasItem(Zhonya.Id) && Items.CanUseItem(Zhonya.Id))
            {
                Zhonya.Cast();
                return;
            }
            if (Items.HasItem(Wooglet.Id) && Items.CanUseItem(Wooglet.Id))
            {
                Wooglet.Cast();
            }
        }
        private static void Game_OnDraw(EventArgs args)
        {

        }
        
    }
}

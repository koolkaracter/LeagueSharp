using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using UnderratedAIO.Helpers;

namespace UnderratedAIO
{
    internal class Program
    {
        public static Obj_AI_Hero player = ObjectManager.Player;
        public static string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static IncomingDamage IncDamages;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            try
            {
                var type = Type.GetType("UnderratedAIO.Champions." + player.ChampionName);
                if (type != null)
                {
                    Helpers.DynamicInitializer.NewInstance(type);
                }
                else
                {
                    var common = Type.GetType("UnderratedAIO.Champions." + "Other");
                    if (common != null)
                    {
                        Helpers.DynamicInitializer.NewInstance(common);
                    }
                }
                IncDamages = new IncomingDamage();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
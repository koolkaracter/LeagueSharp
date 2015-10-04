using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace ARAMShooter
{
    internal class Program
    {
        public static Spell Throw;
        public static Menu Menu;

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            var spell = ObjectManager.Player.GetSpellSlot("summonersnowball");

            if (spell == SpellSlot.Unknown)
            {
                return;
            }


            Menu = new Menu("ARAMShooter", "ARAMShooter", true);
            Menu.AddItem(new MenuItem("DecreaseRange", "Decrease Range by").SetValue(new Slider(10)));
            Menu.AddItem(
                new MenuItem("HitChance", "MinHitChance").SetValue(
                    new StringList(
                        new[] { HitChance.Low.ToString(), HitChance.Medium.ToString(), HitChance.High.ToString() }, 1)));
            Menu.AddItem(new MenuItem("Auto", "AutoDash").SetValue(true));
            Menu.AddItem(new MenuItem("AutoThrow", "AutoThrow").SetValue(false));
            Menu.AddItem(new MenuItem("Throw", "Throw").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            var dontThrow = new Menu("Don't throw at", "DontThrow");
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                dontThrow.AddItem(new MenuItem("DontThrow" + hero.SkinName, hero.SkinName)).SetValue(false);
            }
            Menu.AddSubMenu(dontThrow);
            Menu.Item("HitChance").ValueChanged += Program_ValueChanged;
            Menu.Item("DecreaseRange").ValueChanged += Program_ValueChanged1;
            Menu.AddToMainMenu();

            Throw = new Spell(spell, 2500f);
            Throw.SetSkillshot(.33f, 50f, 1600, true, SkillshotType.SkillshotLine);
            Throw.MinHitChance = GetHitChance();

            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Menu.Item("Throw").GetValue<KeyBind>().Active)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            if (!Throw.IsReady())
            {
                return;
            }

            if (Throw.Instance.Name.Equals("snowballfollowupcast"))
            {
                if (!Menu.Item("Auto").IsActive())
                {
                    return;
                }

                Throw.Cast();
                return;
            }
            if (Menu.Item("Throw").GetValue<KeyBind>().Active || Menu.Item("AutoThrow").GetValue<bool>())
            {
                foreach (var champ in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            h => h.IsValidTarget(Throw.Range) && !Menu.Item("DontThrow" + h.SkinName).GetValue<bool>()))
                {
                    Throw.Cast(champ);
                }
            }
        }

        private static void Program_ValueChanged1(object sender, OnValueChangeEventArgs e)
        {
            Throw.Range = 2500f - e.GetNewValue<Slider>().Value;
        }

        private static void Program_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            Throw.MinHitChance = GetHitChance();
        }

        private static HitChance GetHitChance()
        {
            var hc = Menu.Item("HitChance").GetValue<StringList>();
            switch (hc.SList[hc.SelectedIndex])
            {
                case "Low":
                    return HitChance.Low;
                case "Medium":
                    return HitChance.Medium;
                case "High":
                    return HitChance.High;
            }
            return HitChance.Medium;
        }
    }
}
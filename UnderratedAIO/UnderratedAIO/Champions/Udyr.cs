using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using Color = System.Drawing.Color;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Multimedia;
using UnderratedAIO.Helpers;
using Environment = UnderratedAIO.Helpers.Environment;
using Orbwalking = UnderratedAIO.Helpers.Orbwalking;

namespace UnderratedAIO.Champions
{
    internal class Udyr
    {
        public static Menu config;
        public static Orbwalking.Orbwalker orbwalker;
        public static AutoLeveler autoLeveler;
        public static Spell Q, W, E, R, R2;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static bool justR2, IncSpell;
        public static float DamageTaken;
        public static float DamageTakenTime;
        public static Stance stance;

        internal enum Stance
        {
            Tiger,
            Turtle,
            Bear,
            Phoenix
        }

        public Udyr()
        {
            InitUdyr();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Udyr</font>");
            Drawing.OnDraw += Game_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            Helpers.Jungle.setSmiteSlot();
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
        }


        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe && stance == Stance.Phoenix && player.GetBuff("UdyrPhoenixStance").Count == 3)
            {
                justR2 = true;
                Utility.DelayAction.Add((int) (player.AttackDelay * 1000), () => justR2 = false);
            }
            if (!args.Unit.IsMe || !R.IsReady())
            {
                return;
            }
            var target =
                HeroManager.Enemies.FirstOrDefault(
                    h => h.Distance(player) < R2.Range && CombatHelper.IsFacing(player, h.Position, 45f));
            if (target != null && orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                Harass();
            }
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            var dist = sender.Distance(player) < 750;
            if (E.IsReady() && config.Item("Interrupt", true).GetValue<bool>() && dist && CanStun(sender))
            {
                E.Cast(config.Item("packets").GetValue<bool>());
            }
            if (stance == Stance.Bear && dist && CanStun(sender))
            {
                orbwalker.ForceTarget(sender);
                player.IssueOrder(GameObjectOrder.AttackTo, sender);
            }
        }


        private void Game_ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.SData.Name)
                {
                    case "UdyrTigerStance":
                        stance = Stance.Tiger;
                        break;
                    case "UdyrTurtleStance":
                        stance = Stance.Turtle;
                        break;
                    case "UdyrBearStance":
                        stance = Stance.Bear;
                        break;
                    case "UdyrPhoenixStance":
                        stance = Stance.Phoenix;
                        break;
                }
            }
            if (!(sender is Obj_AI_Base))
            {
                return;
            }
            Obj_AI_Hero target = args.Target as Obj_AI_Hero;
            if (target != null)
            {
                if (sender.IsValid && !sender.IsDead && sender.IsEnemy && target.IsValid && target.IsMe)
                {
                    if (((config.Item("usewLC", true).GetValue<bool>() &&
                          orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) ||
                         (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                          config.Item("usew", true).GetValue<bool>())))
                    {
                        /*var ShieldBuff = new Int32[] { 60, 100, 140, 180, 220 }[W.Level - 1] +
                                            0.5 * player.FlatMagicDamageMod;*/
                        if (Orbwalking.IsAutoAttack(args.SData.Name))
                        {
                            var dmg = (float) sender.GetAutoAttackDamage(player, true);
                            DamageTaken += dmg;
                        }
                        else
                        {
                            if (W.IsReady())
                            {
                                IncSpell = true;
                                Utility.DelayAction.Add(300, () => IncSpell = false);
                            }
                        }
                    }
                }
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Clear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    break;
                default:
                    break;
            }
            Jungle.CastSmite(config.Item("useSmite").GetValue<KeyBind>().Active);
            if (System.Environment.TickCount - DamageTakenTime > 3000)
            {
                DamageTakenTime = System.Environment.TickCount;
                DamageTaken = 0f;
            }
        }

        private void Harass()
        {
            float perc = config.Item("minmanaH", true).GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            if (config.Item("usewH", true).GetValue<bool>())
            {
                castR();
            }
        }

        private static bool CanStun(Obj_AI_Base target)
        {
            return !target.HasBuff("UdyrBearStunCheck");
        }

        private void Clear()
        {
            float perc = config.Item("minmana", true).GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            var target =
                MinionManager.GetMinions(R.Range, MinionTypes.All, MinionTeam.NotAlly)
                    .FirstOrDefault(m => m.MaxHealth > 1000 && m.Health > 300);
            if (target != null && W.IsReady() && player.HealthPercent < 25 &&
                ComboDamage(target) + player.GetAutoAttackDamage(target) * 3 < target.Health)
            {
                castW();
                return;
            }
            if (R.IsReady() && (stance == Stance.Phoenix && player.GetBuff("UdyrPhoenixStance").Count == 3 || justR2) &&
                (target == null || (target != null && target.Position.Distance(player.Position) < 300)))
            {
                return;
            }
            if (target != null && (stance == Stance.Tiger || stance == Stance.Bear))
            {
                orbwalker.ForceTarget(target);
            }
            if (R.IsReady() && config.Item("userLC", true).GetValue<bool>() &&
                ((target != null && (player.ManaPercent > 20 || (player.ManaPercent < 20 && stance == Stance.Turtle))) ||
                 config.Item("rMinHit", true).GetValue<Slider>().Value <=
                 Environment.Minion.countMinionsInrange(player.Position, R.Range)))
            {
                castR();
                return;
            }
            bool CanUseW = config.Item("usewLC", true).GetValue<bool>() && W.IsReady();
            if (CanUseW &&
                (DangerLevel() >= 2.5 || (DangerLevel() <= 2.5 && player.HealthPercent < 60 && stance == Stance.Tiger)))
            {
                castW();
                return;
            }
            if (target != null && config.Item("useqLC", true).GetValue<bool>() && Q.IsReady() && target.Health > 550f &&
                (player.ManaPercent > 50 || player.ManaPercent < 50 && stance == Stance.Turtle))
            {
                castQ();
                return;
            }
            if (CanUseW && DangerLevel() >= 2)
            {
                castW();
                return;
            }
        }

        private double DangerLevel()
        {
            var cons = 30 + player.Level * 5;
            var cons2 = 30 + player.Level * 15;
            var health = player.HealthPercent;
            if (DamageTaken > cons && health > 50)
            {
                return 2;
            }
            if (DamageTaken > cons && health < 50)
            {
                return 2.5;
            }
            if (DamageTaken > cons2 && health > 50)
            {
                return 3;
            }
            if (DamageTaken > cons2 && health < 50)
            {
                return 3.5;
            }
            if (DamageTaken > cons && IncSpell)
            {
                return 4;
            }
            if (DamageTaken > cons2 && IncSpell)
            {
                return 5;
            }
            return 1;
        }

        private void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(600, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config);
            }
            if (DontChangeStance(target))
            {
                return;
            }
            if (W.IsReady() && CheckDmg(target) < target.Health && player.HealthPercent < 25)
            {
                castW();
            }
            if (config.Item("useeOthers", true).GetValue<bool>() && !CanStun(target))
            {
                var others =
                    HeroManager.Enemies.Where(
                        e => e.Distance(player) < 250f && CanStun(e) && e.NetworkId != target.NetworkId);
                if (others.Any())
                {
                    orbwalker.ForceTarget(others.OrderBy(o => player.Distance(o)).FirstOrDefault());
                }
            }
            var dist = player.Distance(target);
            var inSpellrange = dist < 300;

            if (config.Item("usew", true).GetValue<bool>() && W.IsReady() && inSpellrange && DangerLevel() > 2.5f)
            {
                castW();
            }

            if (Q.GetDamage(target) > GetRDmagage(target) && inSpellrange)
            {
                if (config.Item("useq", true).GetValue<bool>() && Q.IsReady())
                {
                    castQ();
                }
                if (config.Item("usew", true).GetValue<bool>() && W.IsReady() && DangerLevel() >= 2.5f)
                {
                    castW();
                }
                if (config.Item("user", true).GetValue<bool>() && R.IsReady())
                {
                    castR();
                }
            }
            else if (inSpellrange)
            {
                if (config.Item("user", true).GetValue<bool>() && R.IsReady())
                {
                    castR();
                }
                if (config.Item("usew", true).GetValue<bool>() && W.IsReady() && DangerLevel() >= 2.5f)
                {
                    castW();
                }
                if (config.Item("useq", true).GetValue<bool>() && Q.IsReady())
                {
                    castQ();
                }
            }
            if (config.Item("usee", true).GetValue<bool>() && E.IsReady() && CanStun(target) &&
                ((!inSpellrange && player.ManaPercent < 55) || player.ManaPercent > 55) &&
                CombatHelper.IsPossibleToReachHim2(
                    target, new float[5] { 0.15f, 0.2f, 0.25f, 0.3f, 0.35f }[Q.Level - 1],
                    new float[5] { 2f, 2.25f, 2.5f, 2.75f, 3f }[Q.Level - 1]) && !justR2)
            {
                E.Cast(config.Item("packets").GetValue<bool>());
            }
            var ignitedmg = (float) player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            if (config.Item("useIgnite", true).GetValue<bool>() && ignitedmg > target.Health && hasIgnite)
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
        }

        private static float CheckDmg(Obj_AI_Base target)
        {
            return (float) (ComboDamage(target) + player.GetAutoAttackDamage(target) * 3);
        }

        private void castQ()
        {
            if (!player.HasBuff("udyrtigerpunch"))
            {
                Q.Cast(config.Item("packets").GetValue<bool>());
            }
        }

        private void castW()
        {
            if (!player.HasBuff("udyrturtleactivation") && player.HealthPercent < 90)
            {
                W.Cast(config.Item("packets").GetValue<bool>());
            }
        }

        private void castR()
        {
            if (!player.HasBuff("udyrphoenixactivation"))
            {
                R.Cast(config.Item("packets").GetValue<bool>());
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawrr", true).GetValue<Circle>(), R.Range);
            Helpers.Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo", true).GetValue<bool>();
        }

        private static float ComboDamage(Obj_AI_Base hero)
        {
            double damage = 0;
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (R.IsReady())
            {
                damage += GetRDmagage(hero);
            }
            //damage += ItemHandler.GetItemsDamage(hero);
            var ignitedmg = player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }
            return (float) damage;
        }

        private static double GetRDmagage(Obj_AI_Base hero)
        {
            return Damage.GetSpellDamage(player, hero, SpellSlot.R) * 5 +
                   Damage.CalcDamage(
                       player, hero, Damage.DamageType.Magical,
                       new double[5] { 40, 80, 120, 160, 200 }[R.Level - 1] + 0.45 * (double) player.FlatMagicDamageMod);
        }

        private static bool DontChangeStance(Obj_AI_Base target)
        {
            var killable = CheckDmg(target) > target.Health;
            switch (stance)
            {
                case Stance.Tiger:
                    if (Q.IsReady() && target is Obj_AI_Hero && player.ManaPercent < 50 && target.HealthPercent > 40 &&
                        !killable)
                    {
                        return true;
                    }
                    break;
                case Stance.Turtle:
                    if (player.HasBuff("udyrtigerpunch"))
                    {
                        return true;
                    }
                    break;
                case Stance.Bear:
                    if (!E.IsReady() && target is Obj_AI_Hero && CanStun(target))
                    {
                        return true;
                    }
                    break;
                case Stance.Phoenix:
                    if (R.IsReady() && (player.GetBuff("UdyrPhoenixStance").Count == 3 || justR2) &&
                        (target == null || (target != null && target.Position.Distance(player.Position) < 300)))
                    {
                        return true;
                    }
                    if (R.IsReady() && player.ManaPercent < 50 && target.HealthPercent > 40 && !killable)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        private void InitUdyr()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 300);
            R2 = new Spell(SpellSlot.R, 600);
            R2.SetSkillshot(0.3f, 90f * 2 * (float) Math.PI / 180, 1800f, false, SkillshotType.SkillshotCone);
        }

        private void InitMenu()
        {
            config = new Menu("Udyr ", "Udyr", true);
            // Target Selector
            Menu menuTS = new Menu("Selector", "tselect");
            TargetSelector.AddToMenu(menuTS);
            config.AddSubMenu(menuTS);
            // Orbwalker
            Menu menuOrb = new Menu("Orbwalker", "orbwalker");
            orbwalker = new Orbwalking.Orbwalker(menuOrb);
            config.AddSubMenu(menuOrb);
            // Draw settings
            Menu menuD = new Menu("Drawings ", "dsettings");
            menuD.AddItem(new MenuItem("drawrr", "Draw R range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 100, 146, 166)));
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage", true)).SetValue(true);
            config.AddSubMenu(menuD);
            // Combo Settings
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usew", "Use W", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usee", "Use E", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useeOthers", "   Stun nearby enemies too", true)).SetValue(true);
            menuC.AddItem(new MenuItem("user", "Use R", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useIgnite", "Use Ignite", true)).SetValue(true);
            menuC = ItemHandler.addItemOptons(menuC);
            config.AddSubMenu(menuC);
            // Harass Settings

            Menu menuH = new Menu("Harass ", "Hsettings");
            menuH.AddItem(new MenuItem("usewH", "Use R", true)).SetValue(true);
            menuH.AddItem(new MenuItem("minmanaH", "Keep X% mana", true)).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuH);

            // LaneClear Settings
            Menu menuLC = new Menu("LaneClear ", "Lcsettings");
            menuLC.AddItem(new MenuItem("useqLC", "Use Q", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("usewLC", "Use W", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("userLC", "Use R", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("rMinHit", "   R min hit", true)).SetValue(new Slider(3, 1, 6));
            menuLC.AddItem(new MenuItem("minmana", "Keep X% mana", true)).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuLC);
            // Misc Settings
            Menu menuM = new Menu("Misc ", "Msettings");
            menuM.AddItem(new MenuItem("Interrupt", "Use E to interupt", true)).SetValue(true);
            menuM = Jungle.addJungleOptions(menuM);
            
            Menu autolvlM = new Menu("AutoLevel", "AutoLevel");
            autoLeveler = new AutoLeveler(autolvlM);
            menuM.AddSubMenu(autolvlM);
            config.AddSubMenu(menuM);
            config.AddItem(new MenuItem("packets", "Use Packets")).SetValue(false);
            config.AddItem(new MenuItem("UnderratedAIO", "by Soresu v" + Program.version.ToString().Replace(",", ".")));
            config.AddToMainMenu();
        }
    }
}
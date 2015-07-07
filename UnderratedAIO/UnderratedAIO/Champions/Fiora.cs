using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX.Multimedia;
using UnderratedAIO.Helpers;
using Environment = UnderratedAIO.Helpers.Environment;
using Orbwalking = UnderratedAIO.Helpers.Orbwalking;

namespace UnderratedAIO.Champions
{
    internal class Fiora
    {
        private static Menu config;
        private static Orbwalking.Orbwalker orbwalker;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static Spell Q, W, E, R;
        private static float lastQ;
        public static AutoLeveler autoLeveler;

        public Fiora()
        {
            InitFiora();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Fiora</font>");
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Game_OnDraw;
            Orbwalking.AfterAttack += AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            Jungle.setSmiteSlot();
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
        }


        private void Game_OnGameUpdate(EventArgs args)
        {
            Jungle.CastSmite(config.Item("useSmite").GetValue<KeyBind>().Active);
            if (config.Item("QSSEnabled").GetValue<bool>())
            {
                ItemHandler.UseCleanse(config);
            }
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
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            Obj_AI_Hero targ = (Obj_AI_Hero) target;
            bool rapid = player.GetAutoAttackDamage(targ) * 6 + ComboDamage(targ) > targ.Health ||
                         (player.Health < targ.Health && player.Health < player.MaxHealth / 2);
            if (unit.IsMe && E.IsReady() && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                (config.Item("usee", true).GetValue<bool>() ||
                 (unit.IsMe && config.Item("RapidAttack", true).GetValue<KeyBind>().Active || rapid)) &&
                !Orbwalking.CanAttack())
            {
                E.Cast(config.Item("packets").GetValue<bool>());
                Orbwalking.ResetAutoAttackTimer();
            }
            if (unit.IsMe && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                (config.Item("RapidAttack", true).GetValue<KeyBind>().Active || rapid) && !Orbwalking.CanAttack())
            {
                if (Q.CanCast(targ))
                {
                    Q.CastOnUnit(targ, config.Item("packets").GetValue<bool>());
                    Orbwalking.ResetAutoAttackTimer();
                }
            }
        }

        private void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range * 2, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config, ComboDamage(target));
            }
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            if (config.Item("useq", true).GetValue<bool>() && Q.IsReady() && lastQ.Equals(0) && Orbwalking.CanMove(100))
            {
                if (target.Distance(player) < Q.Range)
                {
                    Q.CastOnUnit(target, config.Item("packets").GetValue<bool>());
                    lastQ = System.Environment.TickCount;
                }
                else if (config.Item("useqMini", true).GetValue<bool>())
                {
                    var mini =
                        MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                            .Where(m => m.Distance(Prediction.GetPrediction(target, 0.2f).UnitPosition) < Q.Range)
                            .OrderBy(m => m.Distance(target))
                            .FirstOrDefault();
                    if (mini != null)
                    {
                        Q.CastOnUnit(mini, config.Item("packets").GetValue<bool>());
                        lastQ = System.Environment.TickCount;
                    }
                }
            }
            if (config.Item("useq", true).GetValue<bool>() && Q.CanCast(target) && !lastQ.Equals(0) && Orbwalking.CanMove(100))
            {
                var time = System.Environment.TickCount - lastQ;
                if (time > 3500f || player.Distance(target) > Orbwalking.GetRealAutoAttackRange(player) ||
                    Q.GetDamage(target) > target.Health)
                {
                    Q.CastOnUnit(target, config.Item("packets").GetValue<bool>());
                    lastQ = 0;
                }
            }
            if (config.Item("user", true).GetValue<bool>() && R.CanCast(target) && ComboDamage(target) > target.Health * 1.3 &&
                NeedToUlt(target))
            {
                R.CastOnUnit(target, config.Item("packets").GetValue<bool>());
            }
            if (config.Item("useIgnite").GetValue<bool>() && hasIgnite && ComboDamage(target) > target.Health)
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
        }

        private bool NeedToUlt(Obj_AI_Hero target)
        {
            if (player.Distance(target) > 300f && !Q.CanCast(target))
            {
                return true;
            }
            if (player.UnderTurret(true))
            {
                return true;
            }
            if (player.Health < target.Health && player.Health < player.MaxHealth / 2)
            {
                return true;
            }
            return false;
        }

        public static void Game_ProcessSpell(Obj_AI_Base hero, GameObjectProcessSpellCastEventArgs args)
        {
            if (args == null || hero == null)
            {
                return;
            }
            var spellName = args.SData.Name;
            Obj_AI_Hero target = args.Target as Obj_AI_Hero;
            if (target != null &&
                (W.IsReady() && target.IsMe &&
                 (Orbwalking.IsAutoAttack(spellName) || CombatHelper.IsAutoattack(spellName)) &&
                 ((config.Item("usew", true).GetValue<bool>() && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) ||
                  config.Item("autoW", true).GetValue<bool>()) &&
                 !(hero is Obj_AI_Turret || hero.Name == "OdinNeutralGuardian") && player.Distance(hero) < 700))
            {
                var perc = config.Item("minmanaP", true).GetValue<Slider>().Value / 100f;
                if (player.Mana > player.MaxMana * perc && hero.TotalAttackDamage > 50 &&
                    ((player.UnderTurret(true) && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) ||
                     !player.UnderTurret(true)))
                {
                    W.Cast(config.Item("packets").GetValue<bool>());
                }
            }
            if (!config.Item("dodgeWithR", true).GetValue<bool>())
            {
                return;
            }
            if (spellName == "CurseofTheSadMummy")
            {
                if (player.Distance(hero.Position) <= 600f)
                {
                    R.Cast(TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical));
                }
            }
            if (spellName == "EnchantedCrystalArrow")
            {
                if (player.Distance(hero.Position) <= 400f)
                {
                    R.Cast(TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical));
                }
            }
            if (spellName == "EnchantedCrystalArrow" || spellName == "EzrealTrueshotBarrage" || spellName == "JinxR" ||
                spellName == "sejuaniglacialprison")
            {
                if (player.Distance(hero.Position) <= 400f)
                {
                    R.Cast(TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical));
                }
            }
            if (spellName == "InfernalGuardian" || spellName == "UFSlash")
            {
                if (player.Distance(args.End) <= 270f)
                {
                    R.Cast(TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical));
                }
            }
            if (spellName == "BlindMonkRKick" || spellName == "SyndraR" || spellName == "VeigarPrimordialBurst" ||
                spellName == "AlZaharNetherGrasp" || spellName == "LissandraR")
            {
                if (args.Target.IsMe)
                {
                    R.Cast(TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical));
                }
            }
            if (spellName == "TristanaR" || spellName == "ViR")
            {
                if (args.Target.IsMe || player.Distance(args.Target.Position) <= 100f)
                {
                    R.Cast(TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical));
                }
            }
            if (spellName == "GalioIdolOfDurand")
            {
                if (player.Distance(hero.Position) <= 600f)
                {
                    R.Cast(TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical));
                }
            }
        }

        private void Clear()
        {
            float perc = (float)config.Item("minmana", true).GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }

            var target =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(i => i.Distance(player) < Q.Range && i.Health < Q.GetDamage(i))
                    .OrderByDescending(i => i.Distance(player))
                    .FirstOrDefault();
            if (config.Item("useqLC", true).GetValue<bool>() && Q.CanCast(target))
            {
                Q.CastOnUnit(target, config.Item("packets").GetValue<bool>());
            }
            if (config.Item("useeLC", true).GetValue<bool>() &&
                Environment.Minion.countMinionsInrange(player.Position, Q.Range) > 3)
            {
                E.Cast(config.Item("packets").GetValue<bool>());
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawaa", true).GetValue<Circle>(), Orbwalking.GetRealAutoAttackRange(player));
            DrawHelper.DrawCircle(config.Item("drawqq", true).GetValue<Circle>(), Q.Range);
            DrawHelper.DrawCircle(config.Item("drawrr", true).GetValue<Circle>(), R.Range);
            Helpers.Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo").GetValue<bool>();
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q) * 2;
            }
            if (W.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.W);
            }
            if (R.IsReady())
            {
                damage += (float) GetRDamage(hero);
            }
            damage += ItemHandler.GetItemsDamage(hero);
            var ignitedmg = player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }
            return (float) damage;
        }

        private static float fioraRSingle(Obj_AI_Hero target)
        {
            return
                (float)
                    Damage.CalcDamage(
                        player, target, Damage.DamageType.Physical,
                        new float[3] { 125f, 255f, 385f }[R.Level - 1] + 0.9f * player.FlatPhysicalDamageMod);
        }

        private static double GetRDamage(Obj_AI_Hero target)
        {
            float singleR = fioraRSingle(target);
            if (target.CountEnemiesInRange(400) == 1)
            {
                return Damage.GetSpellDamage(player, target, SpellSlot.R);
            }
            if (target.CountEnemiesInRange(400) == 2)
            {
                return singleR + (singleR * 0.4) * 2;
            }
            return singleR + singleR * 0.4;
        }

        private void InitFiora()
        {
            Q = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 375);
        }

        private void InitMenu()
        {
            config = new Menu("Fiora", "Fiora", true);
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
            menuD.AddItem(new MenuItem("drawaa", "Draw AA range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 58, 100, 150)));
            menuD.AddItem(new MenuItem("drawqq", "Draw Q range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 58, 100, 150)));
            menuD.AddItem(new MenuItem("drawrr", "Draw R range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 58, 100, 150)));
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage")).SetValue(true);
            config.AddSubMenu(menuD);
            // Combo Settings
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useqMini", "   Use to jump closer", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usew", "Use W", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usee", "Use E", true)).SetValue(true);
            menuC.AddItem(new MenuItem("user", "R if killable", true)).SetValue(true);
            menuC.AddItem(new MenuItem("RapidAttack", "Fast AA Combo", true))
                .SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle));
            menuC.AddItem(new MenuItem("dodgeWithR", "Dodge ults with R", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useIgnite", "Use Ignite")).SetValue(true);
            menuC = ItemHandler.addItemOptons(menuC);
            config.AddSubMenu(menuC);
            // LaneClear Settings
            Menu menuLC = new Menu("LaneClear ", "Lcsettings");
            menuLC.AddItem(new MenuItem("useqLC", "Use Q", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("useeLC", "Use E", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("minmana", "Keep X% mana", true)).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuLC);
            // Misc Settings
            Menu menuM = new Menu("Misc ", "Msettings");
            menuM.AddItem(new MenuItem("autoW", "Auto W AA", true)).SetValue(true);
            menuM.AddItem(new MenuItem("minmanaP", "Min mana percent", true)).SetValue(new Slider(1, 1, 100));
            menuM = Jungle.addJungleOptions(menuM);
            menuM = ItemHandler.addCleanseOptions(menuM);

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
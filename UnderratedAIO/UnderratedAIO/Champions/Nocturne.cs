using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;
using LeagueSharp;
using LeagueSharp.Common;
using UnderratedAIO.Helpers;
using Environment = UnderratedAIO.Helpers.Environment;
using Orbwalking = UnderratedAIO.Helpers.Orbwalking;

namespace UnderratedAIO.Champions
{
    internal class Nocturne
    {
        public static Menu config;
        public static Orbwalking.Orbwalker orbwalker;
        public static AutoLeveler autoLeveler;
        public static Spell P, Q, W, E, R;
        private static float lastR;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;

        public Nocturne()
        {
            if (player.BaseSkinName != "Nocturne")
            {
                return;
            }
            InitNocturne();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Nocturne</font>");
            Drawing.OnDraw += Game_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Helpers.Jungle.setSmiteSlot();
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            bool minionBlock = false;
            foreach (var minion in
                MinionManager.GetMinions(
                    player.Position, player.AttackRange, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.None))
            {
                if (HealthPrediction.GetHealthPrediction(minion, 3000) <=
                    Damage.GetAutoAttackDamage(player, minion, false))
                {
                    minionBlock = true;
                }
            }
            if (lastR > 4000f)
            {
                lastR = 0f;
            }
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo(target);
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    if (!minionBlock)
                    {
                        Harass(target);
                    }
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    if (!minionBlock)
                    {
                        Clear();
                    }
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    break;
                default:
                    break;
            }
            Jungle.CastSmite(config.Item("useSmite").GetValue<KeyBind>().Active);
            if (config.Item("QSSEnabled").GetValue<bool>())
            {
                ItemHandler.UseCleanse(config);
            }
        }

        private void Combo(Obj_AI_Hero target)
        {
            if (target == null)
            {
                return;
            }
            var cmbdmg = ComboDamage(target);
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config, cmbdmg);
            }
            var dist = player.Distance(target);
            if (config.Item("useq").GetValue<bool>() && Q.CanCast(target) &&
                dist < config.Item("useqMaxRange").GetValue<Slider>().Value)
            {
                var pos = Q.GetPrediction(target, true);
                if ((pos.Hitchance >= HitChance.Medium && dist < 650) || pos.Hitchance >= HitChance.High)
                {
                    Q.Cast(target, config.Item("packets").GetValue<bool>());
                }
            }
            if (config.Item("usee").GetValue<bool>() && E.CanCast(target) &&
                dist < config.Item("useeMaxRange").GetValue<Slider>().Value)
            {
                E.Cast(target, config.Item("packets").GetValue<bool>());
            }
            if (config.Item("user").GetValue<bool>() && lastR.Equals(0) && !target.UnderTurret(true) &&
                R.CanCast(target) &&
                ((qTrailOnMe && (eBuff(target) || target.HasBuffOfType(BuffType.Flee)) &&
                  target.MoveSpeed > player.MoveSpeed && dist > 340) ||
                 (dist < E.Range && dist > E.Range && target.CountAlliesInRange(1000) == 1 &&
                  cmbdmg + Environment.Hero.GetAdOverFive(target) > target.Health &&
                  (target.Health > Q.GetDamage(target) || !Q.IsReady())) ||
                 (player.HealthPercent < 40 && target.HealthPercent < 40 && target.CountAlliesInRange(1000) == 1 &&
                 target.CountEnemiesInRange(1000) == 1)))
            {
                R.Cast(target, config.Item("packets").GetValue<bool>());
                lastR = System.Environment.TickCount;
            }
            if (config.Item("user").GetValue<bool>() && !lastR.Equals(0) && R.CanCast(target) &&
                ((cmbdmg * 1.6 + Environment.Hero.GetAdOverFive(target) > target.Health ||
                  R.GetDamage(target) > target.Health)))
            {
                var time = System.Environment.TickCount - lastR;
                if (time > 3500f || player.Distance(target) > E.Range || cmbdmg > target.Health ||
                    (player.HealthPercent < 40 && target.HealthPercent < 40))
                {
                    R.Cast(target, config.Item("packets").GetValue<bool>());
                    lastR = 0f;
                }
            }
            var ignitedmg = (float) player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            if (config.Item("useIgnite").GetValue<bool>() && ignitedmg > target.Health && hasIgnite &&
                !E.CanCast(target))
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
        }

        private void Clear()
        {
            float perc = config.Item("minmana").GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            MinionManager.FarmLocation bestPositionQ =
                E.GetLineFarmLocation(MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly));
            if (config.Item("useqLC").GetValue<bool>() && Q.IsReady() &&
                bestPositionQ.MinionsHit >= config.Item("qhitLC").GetValue<Slider>().Value)
            {
                Q.Cast(bestPositionQ.Position, config.Item("packets").GetValue<bool>());
            }
        }

        private void Harass(Obj_AI_Hero target)
        {
            float perc = config.Item("minmanaH").GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            if (target == null)
            {
                return;
            }
            if (config.Item("useqH").GetValue<bool>() && Q.CanCast(target))
            {
                Q.Cast(target, config.Item("packets").GetValue<bool>());
            }
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;
            if (Q.IsReady() && Q.Instance.ManaCost < player.Mana)
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (E.IsReady() && E.Instance.ManaCost < player.Mana)
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.E);
            }
            if (R.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.R);
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

        private void Game_ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (config.Item("usew").GetValue<bool>() && orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                W.IsReady())
            {
                var spellName = args.SData.Name;
                if (args.Target.IsMe)
                {
                    if (spellName == "TristanaR" || spellName == "BlindMonkRKick" || spellName == "AlZaharNetherGrasp" ||
                        spellName == "VayneCondemn" || spellName == "JayceThunderingBlow" || spellName == "Headbutt" ||
                        spellName == "Drain" || spellName == "BlindingDart" || spellName == "RunePrison" ||
                        spellName == "IceBlast" || spellName == "Dazzle" || spellName == "Fling" ||
                        spellName == "MaokaiUnstableGrowth" || spellName == "MordekaiserChildrenOfTheGrave" ||
                        spellName == "ZedUlt" || spellName == "LuluW" || spellName == "PantheonW" || spellName == "ViR" ||
                        spellName == "JudicatorReckoning" || spellName == "IreliaEquilibriumStrike" ||
                        spellName == "InfiniteDuress" || spellName == "SkarnerImpale" || spellName == "SowTheWind" ||
                        spellName == "PuncturingTaunt" || spellName == "UrgotSwap2" || spellName == "NasusW" ||
                        spellName == "VolibearW" || spellName == "Feast")
                    {
                        W.Cast(config.Item("packets").GetValue<bool>());
                    }
                }
                if (sender.Distance(player) < 600 && spellName == "GalioIdolOfDurand")
                {
                    W.Cast(config.Item("packets").GetValue<bool>());
                }
                if (sender.Distance(player) < 450 && spellName == "LissandraW")
                {
                    W.Cast(config.Item("packets").GetValue<bool>());
                }
                if (sender.Distance(player) < 250 && spellName == "MaokaiTrunkLine")
                {
                    W.Cast(config.Item("packets").GetValue<bool>());
                }
                if (sender.Distance(player) < 600 && spellName == "SoulShackles")
                {
                    W.Cast(config.Item("packets").GetValue<bool>());
                }
                if (sender.Distance(player) < 260 && spellName == "RivenMartyr")
                {
                    W.Cast(config.Item("packets").GetValue<bool>());
                }
                if (sender.Distance(player) < 550 && spellName == "CurseoftheSadMummy")
                {
                    W.Cast(config.Item("packets").GetValue<bool>());
                }
                if (sender.Distance(player) < 600 && spellName == "CassiopeiaPetrifyingGaze" && player.IsFacing(sender) &&
                    sender.IsFacing(player))
                {
                    W.Cast(config.Item("packets").GetValue<bool>());
                }
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawqq", true).GetValue<Circle>(), Q.Range);
            DrawHelper.DrawCircle(config.Item("drawee", true).GetValue<Circle>(), E.Range);
            DrawHelper.DrawCircle(config.Item("drawrr", true).GetValue<Circle>(), R.Range);
            Helpers.Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo").GetValue<bool>();
            if (!config.Item("bestpospas").GetValue<bool>())
            {
                return;
            }
            MinionManager.FarmLocation bestPositionP =
                P.GetCircularFarmLocation(MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly));
            if (bestPositionP.Position.IsValid() && bestPositionP.MinionsHit > 2 && uBlades)
            {
                Drawing.DrawCircle(bestPositionP.Position.To3D(), 150f, Color.Crimson);
            }
        }

        private void InitNocturne()
        {
            P = new Spell(SpellSlot.Q, 1000);
            P.SetSkillshot(
                3000, Orbwalking.GetRealAutoAttackRange(player) + 50, 3000, false, SkillshotType.SkillshotCircle);
            Q = new Spell(SpellSlot.Q, 1150);
            Q.SetSkillshot(
                Q.Instance.SData.SpellCastTime, Q.Instance.SData.LineWidth, Q.Speed, false, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 2000);
        }

        private static bool qTrail(Obj_AI_Hero target)
        {
            return target.Buffs.Any(buff => buff.Name == "nocturneduskbringertrail");
        }

        private static bool qTrailOnMe
        {
            get { return player.Buffs.Any(buff => buff.Name == "nocturneduskbringerhaste"); }
        }

        private static bool eBuff(Obj_AI_Hero target)
        {
            return target.Buffs.Any(buff => buff.Name == "NocturneUnspeakableHorror");
        }

        private static bool uBlades
        {
            get { return player.Buffs.Any(buff => buff.Name == "nocturneumbrablades"); }
        }

        private void InitMenu()
        {
            config = new Menu("Nocturne", "Nocturne", true);
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
            menuD.AddItem(new MenuItem("drawqq", "Draw Q range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 100, 146, 166)));
            menuD.AddItem(new MenuItem("drawee", "Draw E range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 100, 146, 166)));
            menuD.AddItem(new MenuItem("drawrr", "Draw R range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 100, 146, 166)));
            menuD.AddItem(new MenuItem("bestpospas", "Best position for passive")).SetValue(false);
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage")).SetValue(true);
            config.AddSubMenu(menuD);
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q")).SetValue(true);
            menuC.AddItem(new MenuItem("useqMaxRange", "   Q max Range")).SetValue(new Slider(1000, 0, (int) Q.Range));
            menuC.AddItem(new MenuItem("usew", "Use W against targeted CC")).SetValue(true);
            menuC.AddItem(new MenuItem("usee", "Use E")).SetValue(true);
            menuC.AddItem(new MenuItem("useeMaxRange", "   E max Range")).SetValue(new Slider(300, 0, (int) E.Range));
            menuC.AddItem(new MenuItem("user", "Use R in close range")).SetValue(true);
            menuC.AddItem(new MenuItem("useIgnite", "Use Ignite")).SetValue(true);
            menuC = ItemHandler.addItemOptons(menuC);
            config.AddSubMenu(menuC);
            // Harass Settings
            Menu menuH = new Menu("Harass ", "Hsettings");
            menuH.AddItem(new MenuItem("useqH", "Use Q")).SetValue(true);
            menuH.AddItem(new MenuItem("minmanaH", "Keep X% mana")).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuH);
            // LaneClear Settings
            Menu menuLC = new Menu("LaneClear ", "Lcsettings");
            menuLC.AddItem(new MenuItem("useqLC", "Use Q")).SetValue(true);
            menuLC.AddItem(new MenuItem("qhitLC", "   Min hit").SetValue(new Slider(2, 1, 10)));
            menuLC.AddItem(new MenuItem("minmana", "Keep X% mana")).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuLC);
            Menu menuM = new Menu("Misc ", "Msettings");
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
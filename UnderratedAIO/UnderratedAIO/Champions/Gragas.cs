using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Color = System.Drawing.Color;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using UnderratedAIO.Helpers;
using Environment = UnderratedAIO.Helpers.Environment;
using Orbwalking = UnderratedAIO.Helpers.Orbwalking;

namespace UnderratedAIO.Champions
{
    internal class Gragas
    {
        public static Menu config;
        public static Orbwalking.Orbwalker orbwalker;
        public static AutoLeveler autoLeveler;
        public static Spell Q, W, E, R;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static bool justQ, useIgnite;
        public Vector3 qPos;
        public const int QExplosionRange = 300;
        public static GragasQ savedQ = null;
        public double[] Rwave = new double[] { 50, 70, 90 };

        public Gragas()
        {
            InitGragas();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Gragas</font>");
            Drawing.OnDraw += Game_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Helpers.Jungle.setSmiteSlot();
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            GameObject.OnCreate += GameObjectOnOnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
        }

        private void OnInterruptableTarget(Obj_AI_Hero target, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (E.CanCast(target) && config.Item("useEint", true).GetValue<bool>())
            {
                if (E.CastIfHitchanceEquals(target, HitChance.High, config.Item("packets").GetValue<bool>()))
                {
                    return;
                }
            }
            if (R.CanCast(target) && config.Item("useRint", true).GetValue<bool>())
            {
                if (savedQ != null && !SimpleQ && !target.IsMoving && target.Distance(qPos) > QExplosionRange &&
                    target.Distance(player) < R.Range - 100 &&
                    target.Position.Distance(savedQ.position) < 550 + QExplosionRange / 2 &&
                    !target.HasBuffOfType(BuffType.Knockback))
                {
                    var cast = Prediction.GetPrediction(target, 1000f).UnitPosition.Extend(savedQ.position, -100);
                    R.Cast(cast);
                }
                else if (target.Distance(player) < R.Range - 100)
                {
                    if (player.CountEnemiesInRange(2000) <= player.CountAlliesInRange(2000))
                    {
                        var cast = target.Position.Extend(player.Position, -100);
                        R.Cast(cast);
                    }
                    else
                    {
                        var cast = target.Position.Extend(player.Position, 100);
                        R.Cast(cast);
                    }
                }
            }
        }

        private void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Gragas_Base_Q_Ally.troy")
            {
                savedQ = null;
                qPos = Vector3.Zero;
            }
        }

        private void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Gragas_Base_Q_Ally.troy")
            {
                savedQ = new GragasQ(sender.Position, System.Environment.TickCount);
            }
        }

        private void InitGragas()
        {
            Q = new Spell(SpellSlot.Q, 800);
            Q.SetSkillshot(0.3f, 110f, 1000f, false, SkillshotType.SkillshotCircle);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 600);
            E.SetSkillshot(0.3f, 50, 1000, true, SkillshotType.SkillshotLine);
            R = new Spell(SpellSlot.R, 1050);
            R.SetSkillshot(0.3f, 300, 1000, false, SkillshotType.SkillshotCircle);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            orbwalker.SetAttack(true);
            Jungle.CastSmite(config.Item("useSmite").GetValue<KeyBind>().Active);

            Obj_AI_Hero target = TargetSelector.GetTarget(1300, TargetSelector.DamageType.Magical, true);
            var combodmg = 0f;
            if (target != null)
            {
                combodmg = ComboDamage(target);
            }
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo(combodmg);
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Clear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    break;
                default:
                    break;
            }
            if (config.Item("insec", true).GetValue<KeyBind>().Active)
            {
                if (target == null)
                {
                    player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    return;
                }
                else if (savedQ != null)
                {
                    if (E.CanCast(target))
                    {
                        E.CastIfHitchanceEquals(target, HitChance.High, config.Item("packets").GetValue<bool>());
                    }
                    HandeR(target);
                    DetonateQ();
                }
                Orbwalking.Orbwalk(target, Game.CursorPos, 90, 90);
            }
            if (config.Item("autoQ", true).GetValue<bool>())
            {
                if (Q.IsReady() && config.Item("useqH", true).GetValue<bool>() && savedQ != null)
                {
                    DetonateQ();
                }
            }
            if (savedQ != null && !SimpleQ)
            {
                var mob = Jungle.GetNearest(player.Position);
                if (mob != null && getQdamage(mob) > mob.Health)
                {
                    Q.Cast();
                }
            }
        }


        private void Harass()
        {
            float perc = config.Item("minmanaH", true).GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            Obj_AI_Hero target = TargetSelector.GetTarget(1300, TargetSelector.DamageType.Magical, true);
            if (target == null || target.IsInvulnerable)
            {
                return;
            }
            if (Q.CanCast(target) && config.Item("useqH", true).GetValue<bool>() && savedQ == null && SimpleQ)
            {
                Q.CastIfHitchanceEquals(target, HitChance.VeryHigh, config.Item("packets").GetValue<bool>());
            }
            if (Q.IsReady() && config.Item("useqH", true).GetValue<bool>() && savedQ != null &&
                target.Distance(savedQ.position) < QExplosionRange)
            {
                DetonateQ();
            }
            if (E.CanCast(target) && config.Item("useeH", true).GetValue<bool>())
            {
                CastE(target);
            }
        }

        private void DetonateQ()
        {
            var targethero =
                HeroManager.Enemies.Where(e => e.Distance(savedQ.position) < QExplosionRange && e.IsValidTarget())
                    .OrderByDescending(e => e.Distance(savedQ.position))
                    .FirstOrDefault();
            if (targethero == null)
            {
                return;
            }
            if (savedQ.deltaT() < 2000 &&
                Prediction.GetPrediction(targethero, 0.1f).UnitPosition.Distance(savedQ.position) < QExplosionRange &&
                HeroManager.Enemies.Count(
                    h => h.Distance(savedQ.position) < QExplosionRange && h.IsValidTarget() && h.Health < getQdamage(h)) ==
                0)
            {
                //waiting
            }
            else
            {
                Q.Cast();
            }
        }

        private static bool SimpleQ
        {
            get { return player.Spellbook.GetSpell(SpellSlot.Q).Name == "GragasQ"; }
        }

        private void Clear()
        {
            if (Q.IsReady() && savedQ != null &&
                ((Environment.Minion.countMinionsInrange(savedQ.position, QExplosionRange) >
                  config.Item("eMinHit", true).GetValue<Slider>().Value && savedQ.deltaT() > 2000) ||
                 MinionManager.GetMinions(
                     ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                     .Count(m => HealthPrediction.GetHealthPrediction(m, 600) < 0 || m.Health < 35) > 0))
            {
                Q.Cast(config.Item("packets").GetValue<bool>());
            }

            float perc = config.Item("minmana", true).GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            if (Q.IsReady() && savedQ == null && SimpleQ && config.Item("useqLC", true).GetValue<bool>())
            {
                MinionManager.FarmLocation bestPositionQ =
                    Q.GetCircularFarmLocation(
                        MinionManager.GetMinions(
                            ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly),
                        QExplosionRange);
                if (bestPositionQ.MinionsHit > config.Item("qMinHit", true).GetValue<Slider>().Value)
                {
                    Q.Cast(bestPositionQ.Position, config.Item("packets").GetValue<bool>());
                    return;
                }
            }

            if (config.Item("useeLC", true).GetValue<bool>() && E.IsReady())
            {
                MinionManager.FarmLocation bestPositionE =
                    E.GetLineFarmLocation(
                        MinionManager.GetMinions(
                            ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly));

                if (bestPositionE.MinionsHit >= config.Item("eMinHit", true).GetValue<Slider>().Value)
                {
                    E.Cast(bestPositionE.Position, config.Item("packets").GetValue<bool>());
                }
            }
            if (W.IsReady() && config.Item("usewLC", true).GetValue<bool>() &&
                MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                    .Count(m => m.Health > 600) > 0)
            {
                W.Cast(config.Item("packets").GetValue<bool>());
            }
        }

        private void Combo(float combodmg)
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(1100, TargetSelector.DamageType.Magical, true);
            if (target == null || target.IsInvulnerable || target.MagicImmune)
            {
                return;
            }
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config, ComboDamage(target));
            }
            var ignitedmg = (float) player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            if (config.Item("useIgnite", true).GetValue<bool>() &&
                ignitedmg > HealthPrediction.GetHealthPrediction(target, 700) && hasIgnite &&
                !CombatHelper.CheckCriticalBuffs(target) && !Q.IsReady() &&
                ((savedQ == null ||
                  (savedQ != null && target.Distance(savedQ.position) < QExplosionRange &&
                   getQdamage(target) > target.Health)) || useIgnite))
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
            if (Q.CanCast(target) && config.Item("useq", true).GetValue<bool>() && savedQ == null && SimpleQ)
            {
                if (Q.CastIfHitchanceEquals(target, HitChance.VeryHigh, config.Item("packets").GetValue<bool>()))
                {
                    return;
                }
            }
            if (Q.IsReady() && config.Item("useq", true).GetValue<bool>() && savedQ != null &&
                target.Distance(savedQ.position) < QExplosionRange)
            {
                DetonateQ();
            }
            if (config.Item("usee", true).GetValue<bool>())
            {
                if (E.CanCast(target))
                {
                    CastE(target, combodmg);
                }
            }
            if (W.IsReady() && !Q.CanCast(target) && config.Item("usew", true).GetValue<bool>() &&
                player.Distance(target) < 300 && Orbwalking.CanMove(100) &&
                target.Health > combodmg - getWdamage(target))
            {
                W.Cast(config.Item("packets").GetValue<bool>());
            }
            if (R.IsReady())
            {
                if (R.CastIfWillHit(
                    target, config.Item("Rmin", true).GetValue<Slider>().Value, config.Item("packets").GetValue<bool>()))
                {
                    return;
                }
                var logic = config.Item("user", true).GetValue<bool>() && !target.IsMoving &&
                            (target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Stun) ||
                             target.HasBuffOfType(BuffType.Suppression)) && !target.HasBuffOfType(BuffType.Knockback);
                if (config.Item("rtoq", true).GetValue<bool>() && savedQ != null && !SimpleQ &&
                    target.Distance(qPos) > QExplosionRange && target.Distance(player) < R.Range - 100 &&
                    (target.Health < combodmg || CheckRPushForAlly(target, combodmg)) &&
                    target.Position.Distance(savedQ.position) < 550 + QExplosionRange / 2)
                {
                    var cast = Prediction.GetPrediction(target, 1000f).UnitPosition.Extend(savedQ.position, -100);
                    if (cast.Distance(player.Position) < R.Range)
                    {
                        //Console.WriteLine("R to Q");
                        useIgnite = true;
                        Utility.DelayAction.Add(200, () => useIgnite = false);
                        R.Cast(cast);
                        return;
                    }
                }
                if (config.Item("rtoally", true).GetValue<bool>() && logic &&
                    target.Health - combodmg < target.MaxHealth * 0.5f)
                {
                    var allies =
                        HeroManager.Allies.Where(
                            a => !a.IsDead && !a.IsMe && a.HealthPercent > 40 && a.Distance(target) < 700)
                            .OrderByDescending(a => TargetSelector.GetPriority(a));
                    if (allies.Any())
                    {
                        foreach (var ally in allies)
                        {
                            var cast = Prediction.GetPrediction(target, 1000f).UnitPosition.Extend(ally.Position, -100);
                            if (cast.CountEnemiesInRange(1000) <= cast.CountAlliesInRange(1000) &&
                                cast.Distance(player.Position) < R.Range &&
                                cast.Extend(target.Position, 500).Distance(ally.Position) <
                                target.Distance(ally.Position))
                            {
                                //Console.WriteLine("R to Ally");
                                R.Cast(cast);
                                return;
                            }
                        }
                    }
                    var turret =
                        ObjectManager.Get<Obj_AI_Turret>()
                            .OrderBy(t => t.Distance(target))
                            .FirstOrDefault(t => t.Distance(target) < 2000 && t.IsAlly && !t.IsDead);

                    if (config.Item("rtoturret", true).GetValue<bool>() && turret != null)
                    {
                        var pos = target.Position.Extend(turret.Position, -100);
                        if (target.Distance(turret) > pos.Extend(target.Position, 500).Distance(turret.Position))
                        {
                            //nothing
                        }
                        if ((pos.CountEnemiesInRange(1000) < pos.CountAlliesInRange(1000) &&
                             target.Health - combodmg < target.MaxHealth * 0.4f) ||
                            (ObjectManager.Get<Obj_AI_Turret>()
                                .Count(t => t.Distance(pos) < 950 && t.IsAlly && t.IsValid && !t.IsDead) > 0 &&
                             target.Health - combodmg < target.MaxHealth * 0.5f))
                        {
                            //Console.WriteLine("R to Turret");
                            R.Cast(pos);
                            return;
                        }
                    }
                }
                if (config.Item("rtokill", true).GetValue<bool>() && config.Item("user", true).GetValue<bool>() &&
                    R.GetDamage(target) > target.Health &&
                    (savedQ == null ||
                     (savedQ != null && !qPos.IsValid() && target.Distance(savedQ.position) > QExplosionRange)) &&
                    (target.CountAlliesInRange(700) <= 1 || player.HealthPercent < 35))
                {
                    //Console.WriteLine("R to Kill");
                    R.CastIfHitchanceEquals(target, HitChance.VeryHigh, config.Item("packets").GetValue<bool>());
                    return;
                }
            }
        }

        private bool checkMana()
        {
            var manareq = 0f;
            if (Q.IsReady())
            {
                manareq += Q.ManaCost;
            }
            if (W.IsReady())
            {
                manareq += W.ManaCost;
            }
            if (E.IsReady())
            {
                manareq += E.ManaCost;
            }
            if (R.IsReady())
            {
                manareq += R.ManaCost;
            }
            return player.Mana > manareq;
        }

        private void HandeR(Obj_AI_Hero target)
        {
            if (savedQ != null && !SimpleQ && !target.IsMoving && target.Distance(qPos) > QExplosionRange &&
                target.Distance(player) < R.Range - 100 &&
                (target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Stun) ||
                 target.HasBuffOfType(BuffType.Suppression)) &&
                target.Position.Distance(savedQ.position) < 550 + QExplosionRange / 2 &&
                !target.HasBuffOfType(BuffType.Knockback))
            {
                var cast = Prediction.GetPrediction(target, 1000f).UnitPosition.Extend(savedQ.position, -100);
                R.Cast(cast);
            }
        }

        private bool CheckRPushForAlly(Obj_AI_Hero target, float combodmg)
        {
            var pos = target.Position.Extend(savedQ.position, 550);
            var turret =
                ObjectManager.Get<Obj_AI_Turret>()
                    .OrderBy(t => t.Distance(target))
                    .FirstOrDefault(t => t.Distance(target) < 2000 && t.IsEnemy && t.IsValidTarget());
            if (turret != null && target.Distance(turret) > pos.Extend(target.Position, 500).Distance(turret.Position))
            {
                return false;
            }
            if ((pos.CountEnemiesInRange(1000) < pos.CountAlliesInRange(1000) &&
                 target.Health - combodmg < target.MaxHealth * 0.4f) ||
                (ObjectManager.Get<Obj_AI_Turret>()
                    .Count(t => t.Distance(pos) < 950 && t.IsAlly && t.IsValid && !t.IsDead) > 0 &&
                 target.Health - combodmg < target.MaxHealth * 0.5f))
            {
                return true;
            }
            return false;
        }

        private void CastE(Obj_AI_Hero target, float cmbdmg = -1f)
        {
            if (cmbdmg < 0f)
            {
                E.CastIfHitchanceEquals(target, HitChance.High, config.Item("packets").GetValue<bool>());
                return;
            }
            if (R.IsReady() && target.Health > cmbdmg - R.GetDamage(target) &&
                target.Health < cmbdmg + ItemHandler.GetItemsDamage(target))
            {
                //wait
            }
            else
            {
                E.CastIfHitchanceEquals(target, HitChance.High, config.Item("packets").GetValue<bool>());
            }
        }


        private void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawqq", true).GetValue<Circle>(), Q.Range);
            DrawHelper.DrawCircle(config.Item("drawee", true).GetValue<Circle>(), E.Range);
            Helpers.Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo", true).GetValue<bool>();
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;
            damage += getQdamage(hero);
            if (E.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.E);
            }
            if (W.IsReady() || player.HasBuff("gragaswattackbuff"))
            {
                damage += getWdamage(hero);
            }
            if (R.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.R);
            }
            //damage += ItemHandler.GetItemsDamage(target);
            var ignitedmg = player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }
            return (float) damage;
        }

        private static double getWdamage(Obj_AI_Hero target)
        {
            var dmg = new double[] { 20, 50, 80, 110, 140 }[W.Level - 1] + 8f / 100f * target.MaxHealth +
                      0.3f * player.FlatMagicDamageMod;
            return Damage.CalcDamage(player, target, Damage.DamageType.Magical, dmg);
        }

        public static float getQdamage(Obj_AI_Base target)
        {
            var damage = 0d;
            if (Q.IsReady())
            {
                if (savedQ == null)
                {
                    damage += Damage.GetSpellDamage(player, target, SpellSlot.Q);
                }
                else
                {
                    if (savedQ.deltaT() > 2000)
                    {
                        damage += Damage.GetSpellDamage(player, target, SpellSlot.Q) * 1.5f;
                    }
                    else
                    {
                        damage += Damage.GetSpellDamage(player, target, SpellSlot.Q);
                    }
                }
            }
            if (target.Name.Contains("SRU_Dragon"))
            {
                var dsBuff = player.GetBuff("s5test_dragonslayerbuff");
                if (dsBuff != null)
                {
                    damage = damage * (1f - 0.07f * dsBuff.Count);
                }
            }
            if (target.Name.Contains("SRU_Baron"))
            {
                var bBuff = player.GetBuff("barontarget");
                if (bBuff != null)
                {
                    damage = damage * 0.5f;
                }
            }
            return (float) damage;
        }

        private void Game_ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "GragasQ")
                {
                    if (!justQ)
                    {
                        justQ = true;
                        qPos = args.End;
                        Utility.DelayAction.Add(500, () => justQ = false);
                    }
                }
            }
        }

        private void InitMenu()
        {
            config = new Menu("Gragas ", "Gragas", true);
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
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage", true)).SetValue(true);
            config.AddSubMenu(menuD);
            // Combo Settings 
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usew", "Use W", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usee", "Use E", true)).SetValue(true);
            menuC.AddItem(new MenuItem("user", "Use R 1v1", true)).SetValue(true);
            menuC.AddItem(new MenuItem("rtoally", "   To ally", true)).SetValue(true);
            menuC.AddItem(new MenuItem("rtoq", "   To Q", true)).SetValue(true);
            menuC.AddItem(new MenuItem("rtoturret", "   To turret", true)).SetValue(true);
            menuC.AddItem(new MenuItem("rtokill", "   To kill", true)).SetValue(true);
            menuC.AddItem(new MenuItem("Rmin", "Use R teamfigh", true)).SetValue(new Slider(2, 1, 5));
            menuC.AddItem(new MenuItem("insec", "E-R combo to Q", true))
                .SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press));
            menuC.AddItem(new MenuItem("useIgnite", "Use Ignite", true)).SetValue(true);
            menuC = ItemHandler.addItemOptons(menuC);
            config.AddSubMenu(menuC);
            // Harass Settings
            Menu menuH = new Menu("Harass ", "Hsettings");
            menuH.AddItem(new MenuItem("useqH", "Use Q harass", true)).SetValue(true);
            menuH.AddItem(new MenuItem("useeH", "Use E", true)).SetValue(true);
            menuH.AddItem(new MenuItem("minmanaH", "Keep X% mana", true)).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuH);
            // LaneClear Settings
            Menu menuLC = new Menu("LaneClear ", "Lcsettings");
            menuLC.AddItem(new MenuItem("useqLC", "Use Q", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("qMinHit", "   Min hit", true)).SetValue(new Slider(3, 1, 6));
            menuLC.AddItem(new MenuItem("usewLC", "Use W", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("useeLC", "Use E", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("eMinHit", "   Min hit", true)).SetValue(new Slider(3, 1, 6));
            menuLC.AddItem(new MenuItem("minmana", "Keep X% mana", true)).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuLC);

            Menu menuM = new Menu("Misc ", "Msettings");
            menuM.AddItem(new MenuItem("useEint", "Use E interrupt", true)).SetValue(true);
            menuM.AddItem(new MenuItem("useRint", "Use R interrupt", true)).SetValue(true);
            menuM.AddItem(new MenuItem("autoQ", "Auto Q", true)).SetValue(true);
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

    internal class GragasQ
    {
        public Vector3 position;
        public int time;

        public GragasQ(Vector3 _position, int _tickCount)
        {
            position = _position;
            time = _tickCount;
        }

        public float deltaT()
        {
            return System.Environment.TickCount - time;
        }
    }
}
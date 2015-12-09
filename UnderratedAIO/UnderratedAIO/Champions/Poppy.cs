using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using UnderratedAIO.Helpers;
using Environment = UnderratedAIO.Helpers.Environment;
using Orbwalking = UnderratedAIO.Helpers.Orbwalking;

namespace UnderratedAIO.Champions
{
    internal class Poppy
    {
        public static Menu config;
        public static Orbwalking.Orbwalker orbwalker;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static Spell Q, W, E, R;
        public static double[] eSecond = new double[5] { 75, 125, 175, 225, 275 };
        public static AutoLeveler autoLeveler;

        public Poppy()
        {
            Initpoppy();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Poppy</font>");
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Game_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnInterruptableTarget;
            Jungle.setSmiteSlot();
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
        }

        private void OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (config.Item("useEint", true).GetValue<bool>() && E.IsReady() && E.CanCast(sender))
            {
                E.CastOnUnit(sender, config.Item("packets").GetValue<bool>());
            }
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            Obj_AI_Hero targetf = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);
            if (config.Item("useeflashforced", true).GetValue<KeyBind>().Active)
            {
                if (targetf == null)
                {
                    player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
                else
                {
                    var bestpos = CombatHelper.bestVectorToPoppyFlash2(targetf);
                    bool hasFlash = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerFlash")) ==
                                    SpellState.Ready;
                    if (E.IsReady() && hasFlash && !CheckWalls(player, targetf) && bestpos.IsValid())
                    {
                        player.Spellbook.CastSpell(player.GetSpellSlot("SummonerFlash"), bestpos);
                        player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    }
                    else if (!hasFlash)
                    {
                        Combo();
                        Orbwalking.Orbwalk(targetf, Game.CursorPos, 90, 90);
                    }
                }
            }
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
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
            Jungle.CastSmite(config.Item("useSmite").GetValue<KeyBind>().Active);
            if (!player.IsDead)
            {
                foreach (var dashingEnemy in
                    HeroManager.Enemies.Where(
                        e =>
                            e.IsValidTarget() && (e.IsDashing() || e.HasBuffOfType(BuffType.Knockback)) &&
                            !e.HasBuffOfType(BuffType.SpellShield) &&
                            config.Item("useAutoW" + e.SkinName, true).GetValue<bool>() && !e.HasBuff("poppyepushenemy"))
                    )
                {
                    var nextpos = Prediction.GetPrediction(dashingEnemy, 0.1f).UnitPosition;
                    if (dashingEnemy.Distance(player) <= W.Range &&
                        (nextpos.Distance(player.Position) > W.Range || (player.Distance(dashingEnemy) < W.Range - 100)))
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);
            if (target == null)
            {
                return;
            }
            var cmbdmg = ComboDamage(target);
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config, ComboDamage(target));
            }
            bool hasFlash = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerFlash")) == SpellState.Ready;
            if (config.Item("usee", true).GetValue<bool>() && E.IsReady())
            {
                if (config.Item("useewall", true).GetValue<bool>())
                {
                    var bestpos = CombatHelper.bestVectorToPoppyFlash2(target);
                    float damage =
                        (float)
                            (ComboDamage(target) +
                             Damage.CalcDamage(
                                 player, target, Damage.DamageType.Magical,
                                 (eSecond[E.Level - 1] + 0.8f * player.FlatMagicDamageMod)) +
                             (player.GetAutoAttackDamage(target) * 4));
                    float damageno = (float) (ComboDamage(target) + (player.GetAutoAttackDamage(target) * 4));
                    if (config.Item("useeflash", true).GetValue<bool>() && hasFlash && !CheckWalls(player, target) &&
                        damage > target.Health && target.Health > damageno &&
                        CombatHelper.bestVectorToPoppyFlash(target).IsValid())
                    {
                        player.Spellbook.CastSpell(player.GetSpellSlot("SummonerFlash"), bestpos);
                        Utility.DelayAction.Add(
                            100, () => E.CastOnUnit(target, config.Item("packets").GetValue<bool>()));
                    }
                    if (E.CanCast(target) &&
                        (CheckWalls(player, target) ||
                         target.Health < E.GetDamage(target) + player.GetAutoAttackDamage(target, true)))
                    {
                        E.CastOnUnit(target, config.Item("packets").GetValue<bool>());
                    }
                    if (E.CanCast(target) && Q.IsReady() && Q.Instance.ManaCost + E.Instance.ManaCost > player.Mana &&
                        target.Health <
                        E.GetDamage(target) + Q.GetDamage(target) + player.GetAutoAttackDamage(target, true))
                    {
                        E.CastOnUnit(target, config.Item("packets").GetValue<bool>());
                    }
                }
                else
                {
                    if (E.CanCast(target))
                    {
                        E.CastOnUnit(target, config.Item("packets").GetValue<bool>());
                    }
                }
            }
            if (config.Item("useq", true).GetValue<bool>() && Q.IsReady() && Q.CanCast(target) &&
                Orbwalking.CanMove(100) && target.Distance(player) < Q.Range &&
                (player.Distance(target) > Orbwalking.GetRealAutoAttackRange(target) || !Orbwalking.CanAttack()))
            {
                Q.CastIfHitchanceEquals(target, HitChance.High, config.Item("packets").GetValue<bool>());
            }

            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            var ignitedmg = (float) player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            if (config.Item("useIgnite").GetValue<bool>() && ignitedmg > target.Health && hasIgnite &&
                !E.CanCast(target) && !Q.CanCast(target))
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
            var ShouldRCauseW = player.HasBuff("poppywzone") &&
                                CombatHelper.GetBuffTime(player.GetBuff("poppywzone")) < 1f;
            if (config.Item("userindanger", true).GetValue<bool>() && R.IsReady() && player.CountEnemiesInRange(800) > 2 &&
                player.CountEnemiesInRange(800) >= player.CountAlliesInRange(1500))
            {
                var targ =
                    HeroManager.Enemies.Where(e => e.IsValidTarget() && R.CanCast(e))
                        .OrderBy(e => TargetSelector.GetPriority(target))
                        .ThenByDescending(e => e.MaxHealth)
                        .FirstOrDefault();
                if (!R.IsCharging && targ != null)
                {
                    R.StartCharging();
                }
                if (R.IsCharging && targ != null && R.CanCast(targ) && R.Range < 500)
                {
                    R.CastIfHitchanceEquals(targ, HitChance.VeryHigh, config.Item("packets").GetValue<bool>());
                }
                if (R.IsCharging && ShouldRCauseW)
                {
                    if (targ != null && W.IsInRange(targ))
                    {
                        R.CastIfHitchanceEquals(targ, HitChance.VeryHigh, config.Item("packets").GetValue<bool>());
                        return;
                    }
                    if (target != null && W.IsInRange(target))
                    {
                        R.CastIfHitchanceEquals(target, HitChance.VeryHigh, config.Item("packets").GetValue<bool>());
                    }
                }
                if (targ != null && R.Range < 700)
                {
                    return;
                }
            }
            if (config.Item("user", true).GetValue<bool>() && R.IsReady() && player.Distance(target) < 1000 &&
                target.UnderTurret(true))
            {
                if (!R.IsCharging &&
                    ((cmbdmg < target.Health && cmbdmg + R.GetDamage(target) > target.Health) ||
                     (target.Distance(player) > E.Range && R.GetDamage(target) > target.Health &&
                      target.Distance(player) < R.ChargedMaxRange - 300)) && !Q.IsReady())
                {
                    R.StartCharging();
                }
                if (!R.IsCharging && target.HealthPercent < 40 && target.Distance(player) < W.Range && !Q.IsReady())
                {
                    if (W.IsReady())
                    {
                        W.Cast();
                        return;
                    }
                    if (player.HasBuff("poppywzone"))
                    {
                        R.StartCharging();
                    }
                }
                if (R.IsCharging && ShouldRCauseW)
                {
                    if (target != null && W.IsInRange(target))
                    {
                        R.CastIfHitchanceEquals(target, HitChance.VeryHigh, config.Item("packets").GetValue<bool>());
                    }
                }
                if (R.IsCharging && R.CanCast(target))
                {
                    if (hasIgnite && cmbdmg > target.Health && cmbdmg - R.GetDamage(target) < target.Health)
                    {
                        player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
                    }
                    R.CastIfHitchanceEquals(target, HitChance.VeryHigh, config.Item("packets").GetValue<bool>());
                }
            }
        }

        private static void Clear()
        {
            var mob = Jungle.GetNearest(player.Position);
            float perc = config.Item("minmana", true).GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            if (config.Item("useeLC", true).GetValue<bool>() && E.CanCast(mob) && CheckWalls(player, mob))
            {
                E.CastOnUnit(mob, config.Item("packets").GetValue<bool>());
            }
            MinionManager.FarmLocation bestPositionQ =
                Q.GetLineFarmLocation(MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly));
            if (bestPositionQ.MinionsHit >= config.Item("qMinHit", true).GetValue<Slider>().Value &&
                config.Item("useqLC", true).GetValue<bool>())
            {
                Q.Cast(bestPositionQ.Position, config.Item("packets").GetValue<bool>());
            }
        }

        private static void Harass()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            float perc = config.Item("minmanaH", true).GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc || target == null)
            {
                return;
            }
            if (config.Item("useqH", true).GetValue<bool>() && Q.IsReady() && Q.CanCast(target) &&
                Orbwalking.CanMove(100) && target.Distance(player) < Q.Range &&
                (player.Distance(target) > Orbwalking.GetRealAutoAttackRange(target) || !Orbwalking.CanAttack()))
            {
                Q.CastIfHitchanceEquals(target, HitChance.High, config.Item("packets").GetValue<bool>());
            }
        }

        private static void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawqq", true).GetValue<Circle>(), Q.Range);
            DrawHelper.DrawCircle(config.Item("drawww", true).GetValue<Circle>(), W.Range);
            DrawHelper.DrawCircle(config.Item("drawee", true).GetValue<Circle>(), E.Range);
            DrawHelper.DrawCircle(config.Item("drawrr", true).GetValue<Circle>(), R.Range);
            Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo").GetValue<bool>();
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (config.Item("useEgap", true).GetValue<bool>() && E.IsReady() && E.CanCast(gapcloser.Sender) &&
                CheckWalls(player, gapcloser.Sender))
            {
                E.CastOnUnit(gapcloser.Sender, config.Item("packets").GetValue<bool>());
            }
            if (W.IsReady() && config.Item("useAutoW" + gapcloser.Sender.SkinName, true).GetValue<bool>() &&
                gapcloser.End.Distance(player.Position) <= W.Range &&
                gapcloser.Sender.Distance(player.Position) <= W.Range)
            {
                W.Cast();
            }
        }

        public static bool CheckWalls(Obj_AI_Base player, Obj_AI_Base enemy)
        {
            var distance = player.Position.Distance(enemy.Position);
            for (int i = 1; i < 6; i++)
            {
                if (player.Position.Extend(enemy.Position, distance + 60 * i).IsWall())
                {
                    return true;
                }
            }
            return false;
        }

        public static float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (E.IsReady())
            {
                damage += (float) Damage.GetSpellDamage(player, hero, SpellSlot.E);
            }
            if ((Items.HasItem(ItemHandler.Bft.Id) && Items.CanUseItem(ItemHandler.Bft.Id)) ||
                (Items.HasItem(ItemHandler.Dfg.Id) && Items.CanUseItem(ItemHandler.Dfg.Id)))
            {
                damage = (float) (damage * 1.2);
            }
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready)
            {
                damage += (float) player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            damage += ItemHandler.GetItemsDamage(hero);
            return (float) damage;
        }

        private static void Initpoppy()
        {
            Q = new Spell(SpellSlot.Q, 400);
            Q.SetSkillshot(0.55f, 90f, float.MaxValue, false, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 500);
            R = new Spell(SpellSlot.R);
            R.SetSkillshot(0, 90f, 1400, true, SkillshotType.SkillshotLine);
            R.SetCharged("PoppyR", "PoppyR", 425, 1400, 1.1f);
        }

        private static void InitMenu()
        {
            config = new Menu("Poppy", "Poppy", true);
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
            menuD.AddItem(new MenuItem("drawqq", "Draw Q range", true)).SetValue(new Circle(false, Color.DarkCyan));
            menuD.AddItem(new MenuItem("drawww", "Draw W range", true)).SetValue(new Circle(false, Color.DarkCyan));
            menuD.AddItem(new MenuItem("drawee", "Draw E range", true)).SetValue(new Circle(false, Color.DarkCyan));
            menuD.AddItem(new MenuItem("drawrr", "Draw R range", true)).SetValue(new Circle(false, Color.DarkCyan));
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage")).SetValue(true);
            config.AddSubMenu(menuD);
            // Combo Settings
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usew", "Use W", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usee", "Use E", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useewall", "Use E only near walls", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useeflash", "Use flash to positioning", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useeflashforced", "Forced flash+E if possible", true))
                .SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press))
                .SetFontStyle(System.Drawing.FontStyle.Bold, SharpDX.Color.Orange);
            menuC.AddItem(new MenuItem("user", "Use R to maximize dmg", true)).SetValue(true);
            menuC.AddItem(new MenuItem("userindanger", "Use R in teamfight", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useIgnite", "Use Ignite")).SetValue(true);
            menuC = ItemHandler.addItemOptons(menuC);
            config.AddSubMenu(menuC);
            // Harass Settings
            Menu menuH = new Menu("Harass ", "Hsettings");
            menuH.AddItem(new MenuItem("useqH", "Use Q", true)).SetValue(true);
            menuH.AddItem(new MenuItem("minmanaH", "Keep X% mana", true)).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuH);
            // LaneClear Settings
            Menu menuLC = new Menu("Clear ", "Lcsettings");
            menuLC.AddItem(new MenuItem("useqLC", "Use Q", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("qMinHit", "   Q min hit", true)).SetValue(new Slider(3, 1, 6));
            menuLC.AddItem(new MenuItem("useeLC", "Use E", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("minmana", "Keep X% mana", true)).SetValue(new Slider(50, 1, 100));
            config.AddSubMenu(menuLC);
            // Misc Settings
            Menu menuM = new Menu("Misc", "Msettings");
            Menu menuMW = new Menu("Auto W", "MWsettings");
            Menu menuME = new Menu("Auto E", "MEsettings");
            menuME.AddItem(new MenuItem("useEint", "Use E interrupt", true)).SetValue(true);
            menuME.AddItem(new MenuItem("useEgap", "Use E on gapcloser near walls", true)).SetValue(true);
            menuM = Jungle.addJungleOptions(menuM);

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                menuMW.AddItem(new MenuItem("useAutoW" + hero.SkinName, hero.SkinName, true)).SetValue(true);
            }

            menuM.AddSubMenu(menuMW);
            menuM.AddSubMenu(menuME);
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
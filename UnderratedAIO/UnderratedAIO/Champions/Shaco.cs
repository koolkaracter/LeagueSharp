using System;
using System.Collections.Generic;
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
    internal class Shaco
    {
        public static Menu config;
        private static Orbwalking.Orbwalker orbwalker;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static Spell Q, W, E, R;
        public static bool hasGhost = false;
        public static bool GhostDelay = false;
        public static int GhostRange = 2200;
        public static AutoLeveler autoLeveler;
        public static int LastAATick;
        public static float cloneTime;

        public Shaco()
        {
            InitShaco();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Shaco</font>");
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Game_OnDraw;
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Helpers.Jungle.setSmiteSlot();
        }

        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base hero, GameObjectProcessSpellCastEventArgs args)
        {
            if (ShacoClone)
            {
                var clone = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(m => m.Name == player.Name && !m.IsMe);

                if (args == null || clone == null)
                {
                    return;
                }
                if (hero.NetworkId != clone.NetworkId)
                {
                    return;
                }
                LastAATick = Utils.GameTimeTickCount;
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(
                Q.Range + player.MoveSpeed * 3, TargetSelector.DamageType.Physical);
            if (ShacoStealth && target != null && target.Health > ComboDamage(target) &&
                CombatHelper.IsFacing(target, player.Position))
            {
                orbwalker.SetAttack(false);
            }
            else
            {
                orbwalker.SetAttack(true);
            }
            if (!ShacoClone)
            {
                cloneTime = System.Environment.TickCount;
            }
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo(target);
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
            if (config.Item("ks", true).GetValue<bool>() && E.IsReady())
            {
                var ksTarget =
                    HeroManager.Enemies.FirstOrDefault(
                        h =>
                            h.IsValidTarget() && !h.Buffs.Any(b => CombatHelper.invulnerable.Contains(b.Name)) &&
                            h.Health < E.GetDamage(h));
                if (ksTarget != null)
                {
                    if (E.CanCast(ksTarget) && player.Mana > E.Instance.ManaCost)
                    {
                        E.Cast(ksTarget);
                    }
                    else if (Q.IsReady() && ksTarget.Distance(player) < Q.Range + E.Range &&
                             ksTarget.Distance(player) > E.Range &&
                             !player.Position.Extend(ksTarget.Position, Q.Range).IsWall() &&
                             player.Mana > Q.Instance.ManaCost + E.Instance.ManaCost)
                    {
                        Q.Cast(player.Position.Extend(ksTarget.Position, Q.Range));
                    }
                }
            }
            Jungle.CastSmite(config.Item("useSmite").GetValue<KeyBind>().Active);
        }

        private void Combo(Obj_AI_Hero target)
        {
            if (target == null)
            {
                return;
            }
            if (ShacoClone && !GhostDelay && config.Item("useClone", true).GetValue<bool>())
            {
                var Gtarget = TargetSelector.GetTarget(GhostRange, TargetSelector.DamageType.Physical);
                switch (config.Item("ghostTarget", true).GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        Gtarget = TargetSelector.GetTarget(GhostRange, TargetSelector.DamageType.Physical);
                        break;
                    case 1:
                        Gtarget =
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(i => i.IsEnemy && !i.IsDead && player.Distance(i) <= GhostRange)
                                .OrderBy(i => i.Health)
                                .FirstOrDefault();
                        break;
                    case 2:
                        Gtarget =
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(i => i.IsEnemy && !i.IsDead && player.Distance(i) <= GhostRange)
                                .OrderBy(i => player.Distance(i))
                                .FirstOrDefault();
                        break;
                    default:
                        break;
                }
                var clone = ObjectManager.Get<Obj_AI_Minion>().FirstOrDefault(m => m.Name == player.Name && !m.IsMe);
                if (clone != null && (System.Environment.TickCount - cloneTime > 16500f || clone.HealthPercent < 20) &&
                    !clone.IsWindingUp)
                {
                    var pos =
                        CombatHelper.PointsAroundTheTarget(clone.Position, 600)
                            .OrderByDescending(p => p.CountEnemiesInRange(250))
                            .ThenBy(p => Environment.Minion.countMinionsInrange(p, 250))
                            .FirstOrDefault();
                    if (pos.IsValid())
                    {
                        R.Cast(pos, config.Item("packets").GetValue<bool>());
                    }
                }
                if (clone != null && Gtarget.IsValid && !clone.IsWindingUp)
                {
                    if (CanCloneAttack() || player.HealthPercent < 25)
                    {
                        R.CastOnUnit(Gtarget, config.Item("packets").GetValue<bool>());
                    }
                    else
                    {
                        var prediction = Prediction.GetPrediction(Gtarget, 2);
                        R.Cast(
                            target.Position.Extend(prediction.UnitPosition, Orbwalking.GetRealAutoAttackRange(Gtarget)),
                            config.Item("packets").GetValue<bool>());
                    }

                    GhostDelay = true;
                    Utility.DelayAction.Add(200, () => GhostDelay = false);
                }
            }
            if ((config.Item("WaitForStealth", true).GetValue<bool>() && ShacoStealth &&
                 ComboDamage(target) < target.Health) || !Orbwalking.CanMove(100))
            {
                return;
            }
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config, ComboDamage(target));
            }
            float dist = (float) (Q.Range + player.MoveSpeed * 2.5);
            if (config.Item("useq", true).GetValue<bool>() && Q.IsReady() && target.Distance(player) < dist)
            {
                if (target.Distance(player) < Q.Range)
                {
                    Q.Cast(target.Position, config.Item("packets").GetValue<bool>());
                }
                else
                {
                    if (!CheckWalls(target) &&
                        (ComboDamage(target) > target.Health ||
                         target.CountAlliesInRange(dist) > target.CountEnemiesInRange(dist) ||
                         Game.CursorPos.Distance(target.Position) < 250) ||
                        Environment.Map.GetPath(player, target.Position) < dist)
                    {
                        Q.Cast(
                            player.Position.Extend(target.Position, Q.Range), config.Item("packets").GetValue<bool>());
                    }
                }
            }
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            if (config.Item("usew", true).GetValue<bool>() && W.IsReady() && !target.UnderTurret(true))
            {
                HandleW(target);
            }
            if (config.Item("usee", true).GetValue<bool>() && E.CanCast(target))
            {
                E.CastOnUnit(target, config.Item("packets").GetValue<bool>());
            }
            if (config.Item("user", true).GetValue<bool>() && R.IsReady() && !ShacoClone && target.HealthPercent < 75 &&
                ComboDamage(target) < target.Health && target.HealthPercent > ComboDamage(target) &&
                target.HealthPercent > 25)
            {
                R.Cast(config.Item("packets").GetValue<bool>());
            }
            if (config.Item("useIgnite").GetValue<bool>() &&
                player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health && hasIgnite)
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
        }

        private bool CheckWalls(Obj_AI_Hero target)
        {
            var step = player.Distance(target) / 15;
            for (int i = 1; i < 16; i++)
            {
                if (player.Position.Extend(target.Position, step * i).IsWall())
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleW(Obj_AI_Hero target)
        {
            var turret =
                ObjectManager.Get<Obj_AI_Turret>().OrderByDescending(t=>t.Distance(target))
                    .FirstOrDefault(t => t.IsEnemy && t.Distance(target) < 3000 && !t.IsDead);
            if (turret != null)
            {
                CastW(target, target.Position, turret.Position);
            }
            else
            {
                if (target.IsMoving)
                {
                    var pred = Prediction.GetPrediction(target, 2);
                    if (pred.Hitchance >= HitChance.VeryHigh)
                    {
                        CastW(target, target.Position, pred.UnitPosition);
                    }
                }
                else
                {
                    W.Cast(player.Position.Extend(target.Position, W.Range - player.Distance(target)));
                }
            }
        }

        public static bool CanCloneAttack()
        {
            var ghost = ObjectManager.Get<Obj_AI_Base>().FirstOrDefault();
            if (ghost != null)
            {
                return Utils.GameTimeTickCount >= LastAATick + (ghost.AttackDelay - ghost.AttackCastDelay) * 1000;
            }
            return false;
        }

        private void CastW(Obj_AI_Hero target, Vector3 from, Vector3 to)
        {
            var positions = new List<Vector3>();

            for (int i = 1; i < 11; i++)
            {
                positions.Add(from.Extend(to, 42 * i));
            }
            var best =
                positions.OrderByDescending(p => p.Distance(target.Position))
                    .FirstOrDefault(
                        p => !p.IsWall() && p.Distance(player.Position) < W.Range && p.Distance(target.Position) > 350);
            if (best != null && best.IsValid())
            {
                W.Cast(best, config.Item("packets").GetValue<bool>());
            }
        }

        private static bool ShacoClone
        {
            get { return player.Spellbook.GetSpell(SpellSlot.R).Name == "hallucinateguide"; }
        }

        private static bool ShacoStealth
        {
            get { return player.HasBuff("Deceive"); }
        }

        private void Harass()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            if (config.Item("useeH", true).GetValue<bool>() && E.CanCast(target))
            {
                E.Cast(target, config.Item("packets").GetValue<bool>());
            }
        }

        private void Clear()
        {
            MinionManager.FarmLocation bestPosition =
                W.GetCircularFarmLocation(MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.NotAlly), 300);
            if (config.Item("usewLC", true).GetValue<bool>() && W.IsReady() &&
                bestPosition.MinionsHit > config.Item("whitLC", true).GetValue<Slider>().Value)
            {
                W.Cast(bestPosition.Position, config.Item("packets").GetValue<bool>());
            }
            var mob = Jungle.GetNearest(player.Position);

            if (config.Item("useeLC", true).GetValue<bool>() && E.IsReady() && mob != null &&
                mob.Health < E.GetDamage(mob))
            {
                E.Cast(mob, config.Item("packets").GetValue<bool>());
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawqq", true).GetValue<Circle>(), Q.Range);
            DrawHelper.DrawCircle(config.Item("drawww", true).GetValue<Circle>(), W.Range);
            DrawHelper.DrawCircle(config.Item("drawee", true).GetValue<Circle>(), E.Range);
            Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo").GetValue<bool>();
        }

        private float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;

            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (E.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.E);
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

        private void InitShaco()
        {
            Q = new Spell(SpellSlot.Q, 400);
            W = new Spell(SpellSlot.W, 425);
            E = new Spell(SpellSlot.E, 625);
            R = new Spell(SpellSlot.R);
        }

        private void InitMenu()
        {
            config = new Menu("Shaco", "Shaco", true);
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
                .SetValue(new Circle(false, Color.FromArgb(180, 109, 111, 126)));
            menuD.AddItem(new MenuItem("drawww", "Draw W range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 109, 111, 126)));
            menuD.AddItem(new MenuItem("drawee", "Draw E range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 109, 111, 126)));
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage")).SetValue(true);
            config.AddSubMenu(menuD);
            // Combo Settings
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usew", "Use W", true)).SetValue(true);
            menuC.AddItem(new MenuItem("usee", "Use E", true)).SetValue(true);
            menuC.AddItem(new MenuItem("user", "Use R", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useClone", "   Move clone", true)).SetValue(true);
            menuC.AddItem(new MenuItem("WaitForStealth", "Block spells in stealth", true)).SetValue(true);
            menuC.AddItem(new MenuItem("useIgnite", "Use Ignite")).SetValue(true);
            menuC = ItemHandler.addItemOptons(menuC);
            config.AddSubMenu(menuC);
            // Harass Settings
            Menu menuH = new Menu("Harass ", "Hsettings");
            menuH.AddItem(new MenuItem("useeH", "Use E", true)).SetValue(true);
            config.AddSubMenu(menuH);
            // LaneClear Settings
            Menu menuLC = new Menu("LaneClear ", "Lcsettings");
            menuLC.AddItem(new MenuItem("usewLC", "Use W", true)).SetValue(true);
            menuLC.AddItem(new MenuItem("whitLC", "   Min mob", true).SetValue(new Slider(2, 1, 5)));
            menuLC.AddItem(new MenuItem("useeLC", "Use E", true)).SetValue(true);
            config.AddSubMenu(menuLC);
            // Misc Settings
            Menu menuM = new Menu("Misc ", "Msettings");
            menuM.AddItem(new MenuItem("ghostTarget", "Ghost target priority", true))
                .SetValue(new StringList(new[] { "Targetselector", "Lowest health", "Closest to you" }, 0));
            menuM.AddItem(new MenuItem("ks", "KS Q+E", true)).SetValue(true);
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
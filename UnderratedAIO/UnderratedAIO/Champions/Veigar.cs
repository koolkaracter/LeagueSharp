using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using Color = System.Drawing.Color;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using UnderratedAIO.Helpers;
using Environment = UnderratedAIO.Helpers.Environment;
using Orbwalking = UnderratedAIO.Helpers.Orbwalking;

namespace UnderratedAIO.Champions
{
    internal class Veigar
    {
        public static Menu config;
        public static Orbwalking.Orbwalker orbwalker;
        public static AutoLeveler autoLeveler;
        public static Spell Q, W, E, R;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static bool justQ, justW, justR, justE;
        public static Vector3 wPos, ePos;
        public static Obj_AI_Minion LastAttackedminiMinion;
        public static float LastAttackedminiMinionTime;

        public Veigar()
        {
            if (player.BaseSkinName != "Veigar")
            {
                return;
            }
            InitVeigar();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Veigar</font>");
            Drawing.OnDraw += Game_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            Helpers.Jungle.setSmiteSlot();
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
        }

        private void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (E.IsReady() && config.Item("Interrupt").GetValue<bool>())
            {
                CastE(sender);
            }
        }


        private void Game_ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "VeigarBalefulStrike")
                {
                    if (!justQ)
                    {
                        justQ = true;
                        Utility.DelayAction.Add(300, () => justQ = false);
                    }
                }
                if (args.SData.Name == "VeigarDarkMatter")
                {
                    if (!justW)
                    {
                        wPos = args.End;
                        justW = true;
                        Utility.DelayAction.Add(
                            1250, () =>
                            {
                                justW = false;
                                wPos = Vector3.Zero;
                            });
                    }
                }
                if (args.SData.Name == "VeigarEventHorizon")
                {
                    if (!justE)
                    {
                        ePos = args.End;
                        justE = true;
                        Utility.DelayAction.Add(3500, () =>
                        {
                            justE = false;
                            ePos = Vector3.Zero;
                        });
                    }
                }
                if (args.SData.Name == "VeigarPrimordialBurst")
                {
                    if (!justR)
                    {
                        justR = true;
                        Utility.DelayAction.Add(500, () => justR = false);
                    }
                }
            }
        }

        private void InitVeigar()
        {
            Q = new Spell(SpellSlot.Q, 950);
            Q.SetSkillshot(0.25f, 70f, 2000f, false, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 900);
            W.SetSkillshot(1.25f, 225f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E = new Spell(SpellSlot.E, 1050);
            E.SetSkillshot(.8f, 25f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R = new Spell(SpellSlot.R, 650);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
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
                    Lasthit();
                    break;
                default:
                    break;
            }
            Jungle.CastSmite(config.Item("useSmite").GetValue<KeyBind>().Active);
            if (config.Item("QSSEnabled").GetValue<bool>())
            {
                ItemHandler.UseCleanse(config);
            }
            if (config.Item("autoQ").GetValue<bool>() && Q.IsReady() && !player.IsRecalling() && orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                LastHitQ(true);
            }
            if (config.Item("autoW").GetValue<bool>() && !player.IsRecalling())
            {
                var targ =
                    HeroManager.Enemies.Where(
                        hero =>
                            W.CanCast(hero) &&
                            (hero.HasBuffOfType(BuffType.Snare) || hero.HasBuffOfType(BuffType.Stun) ||
                             hero.HasBuffOfType(BuffType.Taunt) || hero.HasBuffOfType(BuffType.Suppression)))
                        .OrderBy(h => h.Health)
                        .FirstOrDefault();
                if (targ != null)
                {
                    W.Cast(targ, config.Item("packets").GetValue<bool>());
                }
            }
        }

        private void Harass()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            float perc = config.Item("minmanaH").GetValue<Slider>().Value / 100f;
            if (config.Item("useqLHinHarass").GetValue<bool>())
            {
                Lasthit();
            }
            if (player.Mana < player.MaxMana * perc || target == null)
            {
                return;
            }
            if (config.Item("useqH").GetValue<bool>() && Q.IsReady())
            {
                var targQ = Q.GetPrediction(target, true);
                if (Q.Range - 100 > targQ.CastPosition.Distance(player.Position) && targQ.CollisionObjects.Count <= 2 &&
                    targQ.Hitchance >= HitChance.High)
                {
                    Q.Cast(targQ.CastPosition, config.Item("packets").GetValue<bool>());
                }
            }
            if (config.Item("usewH").GetValue<bool>() && W.IsReady())
            {
                var tarPered = W.GetPrediction(target);
                if (W.Range - 80 > tarPered.CastPosition.Distance(player.Position) &&
                    tarPered.Hitchance >= HitChance.VeryHigh)
                {
                    W.Cast(tarPered.CastPosition, config.Item("packets").GetValue<bool>());
                }
            }
        }

        private void Clear()
        {
            float perc = config.Item("minmana").GetValue<Slider>().Value / 100f;
            Lasthit();
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            if (config.Item("usewLC").GetValue<bool>() && W.IsReady())
            {
                MinionManager.FarmLocation bestPositionW =
                    W.GetCircularFarmLocation(MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.NotAlly));
                if (bestPositionW.MinionsHit >= config.Item("wMinHit").GetValue<Slider>().Value)
                {
                    W.Cast(bestPositionW.Position, config.Item("packets").GetValue<bool>());
                }
            }
        }

        private void Lasthit()
        {
            float perc = config.Item("minmanaLH").GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            LastHitQ();
        }

        private void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Magical);
            if (target == null || target.Buffs.Any(b => CombatHelper.invulnerable.Contains(b.Name)))
            {
                return;
            }
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config);
            }
            var cmbDmg = ComboDamage(target);
            if (config.Item("useq").GetValue<bool>() && Q.IsReady() && target.IsValidTarget())
            {
                var targQ = Q.GetPrediction(target, true);
                if (Q.Range - 100 > targQ.CastPosition.Distance(player.Position) && targQ.CollisionObjects.Count <= 1 &&
                    targQ.Hitchance >= HitChance.High)
                {
                    Q.Cast(targQ.CastPosition, config.Item("packets").GetValue<bool>());
                }
            }
            if (config.Item("usew").GetValue<bool>() && W.IsReady())
            {
                var tarPered = W.GetPrediction(target);
                if (justE && ePos.IsValid() && target.Distance(ePos)<375)
                {
                    if (W.Range - 80 > tarPered.CastPosition.Distance(player.Position) &&
                        tarPered.Hitchance >= HitChance.Medium)
                    {
                        W.Cast(target.Position, config.Item("packets").GetValue<bool>());
                    }
                }
                else
                {
                    if (W.Range - 80 > tarPered.CastPosition.Distance(player.Position) &&
                        tarPered.Hitchance >= HitChance.VeryHigh)
                    {
                        W.Cast(tarPered.CastPosition, config.Item("packets").GetValue<bool>());
                    }
                }
            }
            if (R.IsReady() && R.CanCast(target))
            {
                if (config.Item("user").GetValue<bool>() && R.CanCast(target) && CheckW(target) && !Q.CanCast(target) &&
                    !CombatHelper.CheckCriticalBuffs(target) && R.GetDamage(target) > target.Health)
                {
                    R.CastOnUnit(target, config.Item("packets").GetValue<bool>());
                }
            }
            bool canKill = cmbDmg > target.Health;
            if (config.Item("usee").GetValue<bool>() && E.IsReady() &&
                ((canKill && config.Item("useekill").GetValue<bool>()) ||
                 (!config.Item("useekill").GetValue<bool>() && CheckMana())))
            {
                CastE(target);
            }
            var ignitedmg = (float) player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            if (config.Item("useIgnite").GetValue<bool>() && ignitedmg > target.Health && hasIgnite &&
                !player.IsChannelingImportantSpell() && !justQ && !Q.CanCast(target) && !justR && !R.CanCast(target) &&
                CheckW(target))
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
        }

        private bool CheckMana()
        {
            float mana = 0;
            if (Q.IsReady())
            {
                mana += Q.Instance.ManaCost;
            }
            if (W.IsReady())
            {
                mana += W.Instance.ManaCost;
            }
            if (E.IsReady())
            {
                mana += E.Instance.ManaCost;
            }
            if (R.IsReady())
            {
                mana += R.Instance.ManaCost;
            }
            return mana < player.Mana;
        }

        private void CastE(Obj_AI_Hero target)
        {
            var targE = Prediction.GetPrediction(target, 0.5f);
            if (targE.CastPosition.Distance(player.Position) < 700f)
            {
                E.Cast(targE.CastPosition.Extend(player.Position, 375), config.Item("packets").GetValue<bool>());
            }
        }

        private bool CheckW(Obj_AI_Hero target)
        {
            if (justW && W.GetDamage(target) > target.Health && wPos.Distance(target.Position) < W.Width)
            {
                return false;
            }
            return true;
        }

        private void LastHitQ(bool auto = false)
        {
            if (!Q.IsReady())
            {
                return;
            }
            if (auto && player.ManaPercent < config.Item("autoQmana").GetValue<Slider>().Value)
            {
                return;
            }
            if (config.Item("useqLC").GetValue<bool>() || config.Item("useqLH").GetValue<bool>() || auto)
            {
                var minions =
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(
                            m =>
                                m.Distance(player) < Q.Range &&
                                m.Health < Q.GetDamage(m) * config.Item("qLHDamage").GetValue<Slider>().Value / 100);
                if (minions != null)
                {
                    Obj_AI_Base target = null;
                    foreach (var minion in minions)
                    {
                        var qPred = Q.GetPrediction(minion);
                        if (qPred.CollisionObjects.Count <= 2)
                        {
                            Q.Cast(minion, config.Item("packets").GetValue<bool>());
                        }
                    }
                }
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawqq", true).GetValue<Circle>(), Q.Range);
            DrawHelper.DrawCircle(config.Item("drawww", true).GetValue<Circle>(), W.Range);
            DrawHelper.DrawCircle(config.Item("drawee", true).GetValue<Circle>(), 700f);
            Helpers.Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo").GetValue<bool>();
            if (wPos.IsValid() && config.Item("drawW").GetValue<bool>())
            {
                Render.Circle.DrawCircle(wPos, W.Width, Color.Blue, 8);
            }
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (W.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.W);
            }
            if (R.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.R);
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

        private void InitMenu()
        {
            config = new Menu("Veigar ", "Veigar", true);
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
            menuD.AddItem(new MenuItem("drawww", "Draw W range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 100, 146, 166)));
            menuD.AddItem(new MenuItem("drawee", "Draw E range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 100, 146, 166)));
            menuD.AddItem(new MenuItem("drawrr", "Draw R range", true))
                .SetValue(new Circle(false, Color.FromArgb(180, 100, 146, 166)));
            menuD.AddItem(new MenuItem("drawW", "Draw W Area")).SetValue(true);
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage")).SetValue(true);
            config.AddSubMenu(menuD);
            // Combo Settings
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q")).SetValue(true);
            menuC.AddItem(new MenuItem("usew", "Use W")).SetValue(false);
            menuC.AddItem(new MenuItem("usee", "Use E")).SetValue(true);
            menuC.AddItem(new MenuItem("useekill", "   Only for kill")).SetValue(true);
            menuC.AddItem(new MenuItem("user", "Use R")).SetValue(true);
            menuC.AddItem(new MenuItem("useIgnite", "Use Ignite")).SetValue(true);
            menuC = ItemHandler.addItemOptons(menuC);
            config.AddSubMenu(menuC);
            // Harass Settings
            Menu menuH = new Menu("Harass ", "Hsettings");
            menuH.AddItem(new MenuItem("useqH", "Use Q")).SetValue(true);
            menuH.AddItem(new MenuItem("usewH", "Use W")).SetValue(true);
            menuH.AddItem(new MenuItem("minmanaH", "Keep X% mana")).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuH);
            // LaneClear Settings
            Menu menuLC = new Menu("LaneClear ", "Lcsettings");
            menuLC.AddItem(new MenuItem("useqLC", "Use Q")).SetValue(true);
            menuLC.AddItem(new MenuItem("usewLC", "Use W")).SetValue(true);
            menuLC.AddItem(new MenuItem("wMinHit", "   W min hit")).SetValue(new Slider(3, 1, 6));
            menuLC.AddItem(new MenuItem("minmana", "Keep X% mana")).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuLC);
            // Lasthit Settings
            Menu menuLH = new Menu("Lasthit ", "Lasthcsettings");
            menuLH.AddItem(new MenuItem("useqLH", "Use Q")).SetValue(true);
            menuLH.AddItem(new MenuItem("qLHDamage", "   Q lasthit damage percent")).SetValue(new Slider(1, 1, 100));
            menuLH.AddItem(new MenuItem("useqLHinHarass", "LastHit in harass")).SetValue(true);
            menuLH.AddItem(new MenuItem("minmanaLH", "Keep X% mana")).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuLH);
            Menu menuM = new Menu("Misc ", "Msettings");
            menuM.AddItem(new MenuItem("autoQ", "Auto Q lasthit")).SetValue(true);
            menuM.AddItem(new MenuItem("autoQmana", "   Keep X% mana")).SetValue(new Slider(1, 1, 100));
            menuM.AddItem(new MenuItem("autoW", "Auto W on stun")).SetValue(true);
            menuM.AddItem(new MenuItem("Interrupt", "Cast E to interrupt spells")).SetValue(true);
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
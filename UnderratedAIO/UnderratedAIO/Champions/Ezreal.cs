using System;
using System.Collections.Generic;
using System.Drawing.Text;
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
    internal class Ezreal
    {
        public static Menu config;
        public static Orbwalking.Orbwalker orbwalker;
        public static AutoLeveler autoLeveler;
        public static Spell Q, W, E, R;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static bool justJumped;

        public Ezreal()
        {
            if (player.BaseSkinName != "Ezreal")
            {
                return;
            }
            InitEzreal();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Ezreal</font>");
            Drawing.OnDraw += Game_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            Helpers.Jungle.setSmiteSlot();
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
        }

        private void Game_ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "EzrealArcaneShift")
            {
                if (!justJumped)
                {
                    justJumped = true;
                    Utility.DelayAction.Add(200, () => justJumped = false);
                }
            }
        }

        private void InitEzreal()
        {
            Q = new Spell(SpellSlot.Q, 1150);
            Q.SetSkillshot(250f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 1000);
            W.SetSkillshot(250f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 450);
            R = new Spell(SpellSlot.R, 2000);
            R.SetSkillshot(1000f, 160f, 2000f, false, SkillshotType.SkillshotLine);
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
        }

        private void Harass()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            float perc = config.Item("minmanaH").GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            if (target == null)
            {
                return;
            }
            if (config.Item("useqH").GetValue<bool>() && Q.IsReady())
            {
                var targQ = Q.GetPrediction(target);
                if (Q.Range - 100 > targQ.CastPosition.Distance(player.Position) &&
                    targQ.Hitchance >= HitChance.VeryHigh)
                {
                    Q.Cast(targQ.CastPosition, config.Item("packets").GetValue<bool>());
                }
            }
            if (config.Item("usewH").GetValue<bool>() && W.IsReady())
            {
                var time = player.Distance(target) / W.Speed;
                var tarPered = Prediction.GetPrediction(target, time);
                if (W.Range > tarPered.CastPosition.Distance(player.Position))
                {
                    W.Cast(tarPered.CastPosition, config.Item("packets").GetValue<bool>());
                }
            }
        }

        private void Clear()
        {
            float perc = config.Item("minmana").GetValue<Slider>().Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            LastHitQ();
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
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target == null)
            {
                return;
            }
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config);
            }
            var cmbDmg = ComboDamage(target);
            if (config.Item("useq").GetValue<bool>() && Q.IsReady() && target.IsValidTarget() && !justJumped)
            {
                var targQ = Q.GetPrediction(target);
                if (Q.Range - 100 > targQ.CastPosition.Distance(player.Position) &&
                    targQ.Hitchance >= HitChance.VeryHigh)
                {
                    Q.Cast(targQ.CastPosition, config.Item("packets").GetValue<bool>());
                }
            }
            if (config.Item("usew").GetValue<bool>() && W.IsReady() && !justJumped)
            {
                var time = player.Distance(target) / W.Speed;
                var tarPered = Prediction.GetPrediction(target, time);
                if (W.Range - 80 > tarPered.CastPosition.Distance(player.Position))
                {
                    W.Cast(tarPered.CastPosition, config.Item("packets").GetValue<bool>());
                }
            }
            if (R.IsReady() && !justJumped)
            {
                var dist = player.Distance(target);
                if (config.Item("user").GetValue<bool>() && !Q.CanCast(target) && !W.CanCast(target) &&
                    !CombatHelper.CheckCriticalBuffs(target) && config.Item("usermin").GetValue<Slider>().Value < dist &&
                    2000 > dist && target.Health < R.GetDamage(target) * 0.7)
                {
                    var time = player.Distance(target) / R.Speed + 1000;
                    var tarPered = Prediction.GetPrediction(target, time);
                    R.Cast(tarPered.CastPosition, config.Item("packets").GetValue<bool>());
                }
                if (target.CountAlliesInRange(700) > 0)
                {
                    R.CastIfWillHit(
                        target, config.Item("usertf").GetValue<Slider>().Value, config.Item("packets").GetValue<bool>());
                }
            }
            if (config.Item("usee").GetValue<bool>() && E.IsReady())
            {
                if (R.IsReady() && config.Item("Calcr").GetValue<bool>())
                {
                    cmbDmg -= (float) Damage.GetSpellDamage(player, target, SpellSlot.R);
                }
                var bestPositons =
                    (from pos in
                        CombatHelper.PointsAroundTheTarget(target.Position, 750)
                            .Where(
                                p =>
                                    !p.IsWall() && p.IsValid() && p.Distance(player.Position) < E.Range &&
                                    p.Distance(target.Position) < 680 && !p.UnderTurret(true))
                        let mob =
                            ObjectManager.Get<Obj_AI_Base>()
                                .Where(
                                    m =>
                                        m.IsEnemy && m.IsValidTarget() && m.Distance(target.Position) < 750 &&
                                        m.SkinName != target.SkinName)
                                .OrderBy(m => m.Distance(pos))
                                .FirstOrDefault()
                        where (mob != null && mob.Distance(pos) > pos.Distance(target.Position) + 80) || (mob == null)
                        select pos).ToList();
                bool canKill = cmbDmg > target.Health;
                if (config.Item("useekill").GetValue<bool>() && canKill)
                {
                    CastE(bestPositons, target);
                }
                else if ((!config.Item("useekill").GetValue<bool>() &&
                          target.CountEnemiesInRange(1200) < target.CountAlliesInRange(1200)) || canKill)
                {
                    CastE(bestPositons, target);
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

        private void CastE(IEnumerable<Vector3> bestPositons, Obj_AI_Hero target)
        {
            var pos = bestPositons.OrderBy(p => target.Distance(p)).FirstOrDefault();
            if (pos != null && pos.IsValid())
            {
                E.Cast(pos, config.Item("packets").GetValue<bool>());
            }
        }

        private void LastHitQ()
        {
            if (!Q.IsReady())
            {
                return;
            }
            if (config.Item("useqLC").GetValue<bool>() || config.Item("useqLH").GetValue<bool>())
            {
                var minion =
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .FirstOrDefault(m => m.Health < Q.GetDamage(m) && Q.CanCast(m));
                if (minion != null)
                {
                    Q.Cast(minion, config.Item("packets").GetValue<bool>());
                }
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawqq", true).GetValue<Circle>(), Q.Range);
            DrawHelper.DrawCircle(config.Item("drawww", true).GetValue<Circle>(), W.Range);
            DrawHelper.DrawCircle(config.Item("drawee", true).GetValue<Circle>(), E.Range);
            Helpers.Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo").GetValue<bool>();
            return;
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var bestPositons =
                (from pos in
                    CombatHelper.PointsAroundTheTarget(target.Position, 750)
                        .Where(
                            p =>
                                !p.IsWall() && p.IsValid() && p.Distance(player.Position) < E.Range &&
                                p.Distance(target.Position) < 680)
                    let mob =
                        ObjectManager.Get<Obj_AI_Base>()
                            .Where(
                                m =>
                                    m.IsEnemy && m.IsValidTarget() && m.Distance(target.Position) < 750 &&
                                    m.SkinName != target.SkinName)
                            .OrderBy(m => m.Distance(pos))
                            .FirstOrDefault()
                    where (mob != null && mob.Distance(pos) > pos.Distance(target.Position) + 35) || (mob == null)
                    select pos).ToList();
            foreach (var V in bestPositons)
            {
                Drawing.DrawCircle(V, 80, Color.Blue);
            }
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;
            if (Q.IsReady() && config.Item("Calcq").GetValue<bool>())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (W.IsReady() && config.Item("Calcw").GetValue<bool>())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.W);
            }
            if (E.IsReady() && config.Item("Calce").GetValue<bool>())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.E);
            }
            if (R.IsReady() && config.Item("Calcr").GetValue<bool>())
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
            config = new Menu("Ezreal ", "Ezreal", true);
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
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage")).SetValue(true);
            menuD.AddItem(new MenuItem("Calcq", "   Calc Q")).SetValue(true);
            menuD.AddItem(new MenuItem("Calcw", "   Calc W")).SetValue(true);
            menuD.AddItem(new MenuItem("Calce", "   Calc E")).SetValue(true);
            menuD.AddItem(new MenuItem("Calcr", "   Calc R")).SetValue(true);
            config.AddSubMenu(menuD);
            // Combo Settings
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q")).SetValue(true);
            menuC.AddItem(new MenuItem("usew", "Use W")).SetValue(true);
            menuC.AddItem(new MenuItem("usee", "Use E")).SetValue(true);
            menuC.AddItem(new MenuItem("useekill", "   Only for kill")).SetValue(true);
            menuC.AddItem(new MenuItem("user", "Use R in 1v1")).SetValue(true);
            menuC.AddItem(new MenuItem("usermin", "   Min range")).SetValue(new Slider(500, 0, 1500));
            menuC.AddItem(new MenuItem("usertf", "R min enemy in teamfight")).SetValue(new Slider(3, 1, 5));
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
            menuLC.AddItem(new MenuItem("minmana", "Keep X% mana")).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuLC);
            // Lasthit Settings
            Menu menuLH = new Menu("Lasthit ", "Lasthcsettings");
            menuLH.AddItem(new MenuItem("useqLH", "Use Q")).SetValue(true);
            menuLH.AddItem(new MenuItem("minmanaLH", "Keep X% mana")).SetValue(new Slider(1, 1, 100));
            config.AddSubMenu(menuLH);
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
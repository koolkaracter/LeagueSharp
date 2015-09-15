using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using UnderratedAIO.Helpers;
using Color = System.Drawing.Color;
using Orbwalking = UnderratedAIO.Helpers.Orbwalking;

namespace UnderratedAIO.Champions
{
    internal class Shen
    {
        public static Menu config;
        private static Orbwalking.Orbwalker orbwalker;
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static Spell Q, W, E, EFlash, R;
        public static float currEnergy;
        public static bool haspassive = true;
        public static bool PingCasted = false;
        public static int[] eEnergy = new Int32[] { 100, 95, 90, 85, 80 };
        private const int XOffset = 36;
        private const int YOffset = 9;
        private const int Width = 103;
        private const int Height = 8;
        public static int[] ShieldBuff = new Int32[] { 60, 100, 140, 180, 200 };
        public static float DamageTaken, DamageTakenTime;
        public static bool IncSpell;

        private static readonly Render.Text Text = new Render.Text(
            0, 0, "", 11, new ColorBGRA(255, 0, 0, 255), "monospace");

        public static AutoLeveler autoLeveler;

        public Shen()
        {
            InitShen();
            InitMenu();
            Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Shen</font>");
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Game_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnPossibleToInterrupt;
            Obj_AI_Base.OnDamage += Obj_AI_Base_OnDamage;
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            Jungle.setSmiteSlot();
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
        }

        private void Obj_AI_Base_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            var t = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.NetworkId == args.SourceNetworkId);
            var s = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.NetworkId == args.TargetNetworkId);
            if (t != null && s != null &&
                (t.IsMe &&
                 ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(tw => tw.Distance(t) < 750 && tw.Distance(s) < 750) !=
                 null))
            {
                if (config.Item("autotauntattower").GetValue<bool>() && E.CanCast(s))
                {
                    E.Cast(s, config.Item("packets").GetValue<bool>());
                }
            }
        }


        private void OnPossibleToInterrupt(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!config.Item("useeint").GetValue<bool>())
            {
                return;
            }
            if (unit.IsValidTarget(E.Range) && E.IsReady())
            {
                E.Cast(unit, config.Item("packets").GetValue<bool>());
            }
        }

        private static void Game_OnDraw(EventArgs args)
        {
            DrawHelper.DrawCircle(config.Item("drawaa").GetValue<Circle>(), player.AttackRange);
            DrawHelper.DrawCircle(config.Item("drawqq").GetValue<Circle>(), Q.Range);
            DrawHelper.DrawCircle(config.Item("drawee").GetValue<Circle>(), E.Range);
            DrawHelper.DrawCircle(config.Item("draweeflash").GetValue<Circle>(), EFlash.Range);
            Helpers.Jungle.ShowSmiteStatus(
                config.Item("useSmite").GetValue<KeyBind>().Active, config.Item("smiteStatus").GetValue<bool>());
            if (config.Item("drawallyhp").GetValue<bool>())
            {
                DrawHealths();
            }
            if (config.Item("drawincdmg").GetValue<bool>())
            {
                getIncDmg();
            }
            Utility.HpBarDamageIndicator.Enabled = config.Item("drawcombo").GetValue<bool>();
        }

        private static void DrawHealths()
        {
            float i = 0;
            foreach (
                var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && !hero.IsMe && !hero.IsDead))
            {
                var playername = hero.Name;
                if (playername.Length > 13)
                {
                    playername = playername.Remove(9) + "...";
                }
                var champion = hero.SkinName;
                if (champion.Length > 12)
                {
                    champion = champion.Remove(7) + "...";
                }
                var percent = (int) (hero.Health / hero.MaxHealth * 100);
                var color = Color.Red;
                if (percent > 25)
                {
                    color = Color.Orange;
                }
                if (percent > 50)
                {
                    color = Color.Yellow;
                }
                if (percent > 75)
                {
                    color = Color.LimeGreen;
                }
                Drawing.DrawText(
                    Drawing.Width * 0.8f, Drawing.Height * 0.1f + i, color, playername + "(" + champion + ")");
                Drawing.DrawText(
                    Drawing.Width * 0.9f, Drawing.Height * 0.1f + i, color,
                    ((int) hero.Health).ToString() + " (" + percent.ToString() + "%)");
                i += 20f;
            }
        }

        private static void getIncDmg()
        {
            var color = Color.Red;
            float result = CombatHelper.getIncDmg();
            var barPos = player.HPBarPosition;
            var damage = (float) result;
            if (damage == 0)
            {
                return;
            }
            var percentHealthAfterDamage = Math.Max(0, player.Health - damage) / player.MaxHealth;
            var xPos = barPos.X + XOffset + Width * percentHealthAfterDamage;

            if (damage > player.Health)
            {
                Text.X = (int) barPos.X + XOffset;
                Text.Y = (int) barPos.Y + YOffset - 13;
                Text.text = ((int) (player.Health - damage)).ToString();
                Text.OnEndScene();
            }

            Drawing.DrawLine(xPos, barPos.Y + YOffset, xPos, barPos.Y + YOffset + Height, 3, color);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            GetPassive();
            Ulti();
            currEnergy = player.Mana;
            if (config.Item("useeflash").GetValue<KeyBind>().Active &&
                player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerFlash")) == SpellState.Ready)
            {
                FlashCombo();
            }
            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    LasthitQ();
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LasthitQ();
                    Clear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    LasthitQ();
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
            if (!W.IsReady())
            {
                return;
            }
            var shield = (ShieldBuff[W.Level - 1] + 0.6f * player.FlatMagicDamageMod) *
                         config.Item("wabove").GetValue<Slider>().Value / 100f;
            if (shield <= DamageTaken || IncSpell)
            {
                if ((config.Item("autow").GetValue<bool>() &&
                     (config.Item("autowwithe").GetValue<bool>() &&
                      currEnergy - player.Spellbook.GetSpell(SpellSlot.W).ManaCost > getEenergy()) ||
                     !config.Item("autowwithe").GetValue<bool>()) ||
                    (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && config.Item("usew").GetValue<bool>()))
                {
                    W.Cast();
                }
            }
        }

        private static void GetPassive()
        {
            var has = false;
            foreach (BuffInstance buff in player.Buffs)
            {
                if (buff.Name == "shenwayoftheninjaaura")
                {
                    has = true;
                }
            }
            haspassive = has;
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!config.Item("useeagc").GetValue<bool>())
            {
                return;
            }
            if (gapcloser.Sender.IsValidTarget(E.Range) && E.IsReady() &&
                player.Distance(gapcloser.Sender.Position) < 400)
            {
                E.Cast(gapcloser.End, config.Item("packets").GetValue<bool>());
            }
        }

        private static void Clear()
        {
            var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(400)).ToList();
            if (minions.Count() > 2)
            {
                if (Items.HasItem(3077) && Items.CanUseItem(3077))
                {
                    Items.UseItem(3077);
                }
                if (Items.HasItem(3074) && Items.CanUseItem(3074))
                {
                    Items.UseItem(3074);
                }
            }
        }

        private static void Ulti()
        {
            if (!R.IsReady() || PingCasted || player.IsDead)
            {
                return;
            }

            foreach (var allyObj in
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        i =>
                            i.IsAlly && !i.IsMe && !i.IsDead &&
                            ((Checkinrange(i) &&
                              ((i.Health * 100 / i.MaxHealth) <= config.Item("atpercent").GetValue<Slider>().Value)) ||
                             (CombatHelper.CheckCriticalBuffs(i) && i.CountEnemiesInRange(600) < 1))))
            {
                if (config.Item("user").GetValue<bool>() && orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo &&
                    R.IsReady() && player.CountEnemiesInRange((int) E.Range) < 1 &&
                    !config.Item("ult" + allyObj.SkinName).GetValue<bool>())
                {
                    R.Cast(allyObj);
                    return;
                }
                else
                {
                    DrawHelper.popUp("Use R to help " + allyObj.ChampionName, 3000, Color.Red, Color.White, Color.Red);
                }
                PingCasted = true;
                Utility.DelayAction.Add(5000, () => PingCasted = false);
            }
        }

        private static bool Checkinrange(Obj_AI_Hero i)
        {
            if (i.CountEnemiesInRange(750) >= 1 && i.CountEnemiesInRange(750) < 3)
            {
                return true;
            }
            return false;
        }

        private static void LasthitQ()
        {
            if (config.Item("autoqwithe").GetValue<bool>() &&
                !(currEnergy - player.Spellbook.GetSpell(SpellSlot.Q).ManaCost > getEenergy()))
            {
                return;
            }
            var allMinions = MinionManager.GetMinions(player.ServerPosition, Q.Range);
            if (config.Item("autoqls").GetValue<bool>() && Q.IsReady() && Orbwalking.CanMove(100))
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        HealthPrediction.GetHealthPrediction(
                            minion, (int) (player.Distance(minion.Position) * 1000 / 1400)) <
                        player.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Q.CastOnUnit(minion);
                        currEnergy -= player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
                        return;
                    }
                }
            }
        }

        private static double getEenergy()
        {
            if (E.Level - 1 >= 0)
            {
                return eEnergy[E.Level - 1];
            }
            else
            {
                return 0;
            }
        }

        private static void Harass()
        {
            if (config.Item("harassqwithe").GetValue<bool>() &&
                !(currEnergy - player.Spellbook.GetSpell(SpellSlot.Q).ManaCost > getEenergy()))
            {
                return;
            }
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target != null && Q.IsReady() && config.Item("harassq").GetValue<bool>() && Orbwalking.CanMove(100))
            {
                Q.CastOnUnit(target, config.Item("packets").GetValue<bool>());
                currEnergy -= player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
            }
        }

        private static void Combo()
        {
            var minHit = config.Item("useemin").GetValue<Slider>().Value;
            Obj_AI_Hero target = TargetSelector.GetTarget(E.Range + 400, TargetSelector.DamageType.Magical);
            Obj_AI_Hero targetQ = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null)
            {
                return;
            }
            if (config.Item("useItems").GetValue<bool>())
            {
                ItemHandler.UseItems(target, config, ComboDamage(target));
            }
            var useE = config.Item("usee").GetValue<bool>() && E.IsReady() && player.Distance(target.Position) < E.Range;
            if (useE)
            {
                if (minHit > 1)
                {
                    CastEmin(target, minHit);
                }
                else if (player.Distance(target.Position) > player.AttackRange &&
                         E.GetPrediction(target).Hitchance >= HitChance.High)
                {
                    E.Cast(target, config.Item("packets").GetValue<bool>());
                }
            }
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            var ignitedmg = (float) player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            if (config.Item("useIgnite").GetValue<bool>() && ignitedmg > target.Health && hasIgnite &&
                !E.CanCast(target) && !Q.CanCast(target))
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
            if (useE && target.Health > Q.GetDamage(target))
            {
                return;
            }
            if (Q.IsReady() && config.Item("useq").GetValue<bool>() && Q.CanCast(targetQ) && Orbwalking.CanMove(100))
            {
                Q.CastOnUnit(targetQ, config.Item("packets").GetValue<bool>());
                currEnergy -= player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
            }
        }

        public static void CastEmin(Obj_AI_Base target, int min)
        {
            var MaxEnemy = player.CountEnemiesInRange(1580);
            if (MaxEnemy==1)
            {
                E.Cast(target);
            }
            else
            {
                var MinEnemy = Math.Min(min, MaxEnemy);
                foreach (var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(i => i.Distance(player) < E.Range && i.IsEnemy && !i.IsDead && i.IsValidTarget()))
                {
                    for (int i = MaxEnemy; i > MinEnemy - 1; i--)
                    {
                        if (E.CastIfWillHit(enemy, i))
                        {
                            return;
                        }
                    }
                }
            }

        }

        private static void FlashCombo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(EFlash.Range, TargetSelector.DamageType.Magical);
            if (config.Item("usee").GetValue<bool>() && E.IsReady() && player.Distance(target.Position) < EFlash.Range &&
                player.Distance(target.Position) > 480 && !((getPosToEflash(target.Position)).IsWall()))
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerFlash"), getPosToEflash(target.Position));

                E.Cast(target.Position, config.Item("packets").GetValue<bool>());
            }
            if (Q.IsReady() && config.Item("useq").GetValue<bool>() &&
                currEnergy - player.Spellbook.GetSpell(SpellSlot.E).ManaCost >= getEenergy())
            {
                Q.CastOnUnit(target, config.Item("packets").GetValue<bool>());
                currEnergy -= player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
            }
            ItemHandler.UseItems(target, config);
        }

        public static Vector3 getPosToEflash(Vector3 target)
        {
            return target + (player.Position - target) / 2;
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            float damage = 0;
            if (Q.IsReady() && player.Spellbook.GetSpell(SpellSlot.Q).ManaCost < player.Mana)
            {
                damage += (float) Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (E.IsReady() && player.Spellbook.GetSpell(SpellSlot.E).ManaCost < player.Mana)
            {
                damage += (float) Damage.GetSpellDamage(player, hero, SpellSlot.E);
            }
            if (haspassive)
            {
                var bonusHp = ((player.MaxHealth - (485.8f + (85 * (player.Level - 1)))) * 0.10);
                var passive = bonusHp + 4 + player.Level * 4;
                damage += (float) player.CalcDamage(hero, Damage.DamageType.Magical, passive);
            }
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
                hero.Health - damage < (float) player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite))
            {
                damage += (float) player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            return damage;
        }

        private void Game_ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!W.IsReady(2000))
            {
                return;
            }
            if (!(sender is Obj_AI_Base))
            {
                return;
            }
            Obj_AI_Hero target = args.Target as Obj_AI_Hero;
            if (target != null && target.IsMe)
            {
                if (config.Item("usee").GetValue<bool>() && E.IsReady() &&
                    orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                    player.Distance(target.Position) < E.Range)
                {
                    return;
                }
                if (sender.IsValid && !sender.IsDead && sender.IsEnemy && target.IsValid && target.IsMe)
                {
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

        private static void InitShen()
        {
            Q = new Spell(SpellSlot.Q, 475);
            Q.SetTargetted(0.5f, 1500f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 600);
            E.SetSkillshot(0.5f, 50f, 1600f, false, SkillshotType.SkillshotLine);
            EFlash = new Spell(SpellSlot.E, 990);
            EFlash.SetSkillshot(
                E.Instance.SData.SpellCastTime, E.Instance.SData.LineWidth, E.Speed, false, SkillshotType.SkillshotLine);
            R = new Spell(SpellSlot.R, float.MaxValue);
        }

        private static void InitMenu()
        {
            config = new Menu("Shen", "SRS_Shen", true);
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
            menuD.AddItem(new MenuItem("drawaa", "Draw AA range"))
                .SetValue(new Circle(false, Color.FromArgb(150, 150, 62, 172)));
            menuD.AddItem(new MenuItem("drawqq", "Draw Q range"))
                .SetValue(new Circle(false, Color.FromArgb(150, 150, 62, 172)));
            menuD.AddItem(new MenuItem("drawee", "Draw E range"))
                .SetValue(new Circle(false, Color.FromArgb(150, 150, 62, 172)));
            menuD.AddItem(new MenuItem("draweeflash", "Draw E+flash range"))
                .SetValue(new Circle(true, Color.FromArgb(50, 250, 248, 110)));
            menuD.AddItem(new MenuItem("drawallyhp", "Draw teammates' HP")).SetValue(true);
            menuD.AddItem(new MenuItem("drawincdmg", "Draw incoming damage")).SetValue(true);
            menuD.AddItem(new MenuItem("drawcombo", "Draw combo damage")).SetValue(true);
            config.AddSubMenu(menuD);

            // Combo Settings
            Menu menuC = new Menu("Combo ", "csettings");
            menuC.AddItem(new MenuItem("useq", "Use Q")).SetValue(true);
            menuC.AddItem(new MenuItem("usew", "Use W")).SetValue(true);
            menuC.AddItem(new MenuItem("usee", "Use E")).SetValue(true);
            menuC.AddItem(new MenuItem("useemin", "   Min target in teamfight")).SetValue(new Slider(1, 1, 5));
            menuC.AddItem(new MenuItem("useIgnite", "Use Ignite")).SetValue(true);
            menuC = ItemHandler.addItemOptons(menuC);
            config.AddSubMenu(menuC);

            // Harass Settings
            Menu menuH = new Menu("Harass ", "hsettings");
            menuH.AddItem(new MenuItem("harassq", "Harass with Q")).SetValue(true);
            menuH.AddItem(new MenuItem("harassqwithe", "Keep energy for E")).SetValue(true);
            config.AddSubMenu(menuH);

            // Lasthit Settings
            Menu menuLH = new Menu("Lasthit ", "lhsettings");
            menuLH.AddItem(new MenuItem("autoqls", "Lasthit with Q")).SetValue(true);
            menuLH.AddItem(new MenuItem("autoqwithe", "Keep energy for E")).SetValue(true);
            config.AddSubMenu(menuLH);

            // Misc Settings
            Menu menuU = new Menu("Misc ", "usettings");
            menuU.AddItem(new MenuItem("autow", "Try to block non-skillshot spells")).SetValue(true);
            menuU.AddItem(new MenuItem("wabove", "Min damage in shield %")).SetValue(new Slider(50, 0, 100));
            menuU.AddItem(new MenuItem("autowwithe", "Keep energy for E")).SetValue(true);
            menuU.AddItem(new MenuItem("autotauntattower", "Auto taunt in tower range")).SetValue(true);
            menuU.AddItem(new MenuItem("useeagc", "Use E to anti gap closer")).SetValue(false);
            menuU.AddItem(new MenuItem("useeint", "Use E to interrupt")).SetValue(true);
            menuU.AddItem(new MenuItem("useeflash", "Flash+E"))
                .SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press));
            menuU.AddItem(new MenuItem("user", "Use R")).SetValue(true);
            menuU.AddItem(new MenuItem("atpercent", "Friend under")).SetValue(new Slider(20, 0, 100));
            menuU = Jungle.addJungleOptions(menuU);


            Menu autolvlM = new Menu("AutoLevel", "AutoLevel");
            autoLeveler = new AutoLeveler(autolvlM);
            menuU.AddSubMenu(autolvlM);

            config.AddSubMenu(menuU);
            var sulti = new Menu("Don't ult on ", "dontult");
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly))
            {
                if (hero.SkinName != player.SkinName)
                {
                    sulti.AddItem(new MenuItem("ult" + hero.SkinName, hero.SkinName)).SetValue(false);
                }
            }
            config.AddSubMenu(sulti);
            config.AddItem(new MenuItem("packets", "Use Packets")).SetValue(false);
            config.AddItem(new MenuItem("UnderratedAIO", "by Soresu v" + Program.version.ToString().Replace(",", ".")));
            config.AddToMainMenu();
        }
    }
}
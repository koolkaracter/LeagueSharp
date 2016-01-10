using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalking = UnderratedAIO.Helpers.Orbwalking;

namespace UnderratedAIO.Helpers
{
    internal class IncomingDamage
    {
        public List<IncData> IncomingDamagesAlly = new List<IncData>();
        public List<IncData> IncomingDamagesEnemy = new List<IncData>();
        public bool enabled;

        public IncData GetAllyData(int networkId)
        {
            return IncomingDamagesAlly.FirstOrDefault(i => i.Hero.NetworkId == networkId);
        }

        public IncData GetEnemyData(int networkId)
        {
            return IncomingDamagesAlly.FirstOrDefault(i => i.Hero.NetworkId == networkId);
        }

        public void Debug()
        {
            var data = IncomingDamagesAlly.Concat(IncomingDamagesEnemy);
            foreach (var d in data)
            {
                Console.WriteLine(d.Hero.Name);
                Console.WriteLine("\t DamageCount" + d.DamageCount);
                Console.WriteLine("\t DamageTaken" + d.DamageTaken);
                Console.WriteLine("\t TargetedCC" + d.TargetedCC);
            }
        }

        public IncomingDamage()
        {
            Obj_AI_Base.OnProcessSpellCast += Game_ProcessSpell;
            Game.OnUpdate += Game_OnGameUpdate;
            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly))
            {
                IncomingDamagesAlly.Add(new IncData(ally));
            }
            foreach (var Enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy))
            {
                IncomingDamagesEnemy.Add(new IncData(Enemy));
            }
            enabled = true;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            resetData();
        }

        private void resetData()
        {
            foreach (var incDamage in
                IncomingDamagesAlly.Concat(IncomingDamagesEnemy))
            {
                for (int index = 0; index < incDamage.Damages.Count; index++)
                {
                    var d = incDamage.Damages[index];
                    if (Game.Time - d.Time > 0.8f)
                    {
                        incDamage.Damages.RemoveAt(index);
                        if (incDamage.DamageCount > 0)
                        {
                            incDamage.DamageCount--;
                        }
                    }
                }
                incDamage.TargetedCC = false;
            }
        }

        private void Game_ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (enabled)
            {
                Obj_AI_Hero target = args.Target as Obj_AI_Hero;
                if (target != null && target.Team != sender.Team)
                {
                    if (sender.IsValid && !sender.IsDead)
                    {
                        var data =
                            IncomingDamagesAlly.Concat(IncomingDamagesEnemy)
                                .FirstOrDefault(i => i.Hero.NetworkId == target.NetworkId);
                        if (data != null)
                        {
                            if (Orbwalking.IsAutoAttack(args.SData.Name))
                            {
                                var dmg = (float) sender.GetAutoAttackDamage(target, true);
                                data.Damages.Add(new Dmg(dmg));
                                data.DamageCount++;
                            }
                            else
                            {
                                var hero = sender as Obj_AI_Hero;
                                if (hero != null)
                                {
                                    data.Damages.Add(
                                        new Dmg(
                                            (float) Damage.GetSpellDamage(hero, (Obj_AI_Base) args.Target, args.Slot)));
                                    data.DamageCount++;
                                }
                            }
                            if (sender is Obj_AI_Hero && target != null && target.IsAlly && !target.IsMe &&
                                CombatHelper.isTargetedCC(args.SData.Name, true) && args.SData.Name != "NasusW")
                            {
                                data.TargetedCC = true;
                            }
                        }
                    }
                    //Debug();
                }
            }
        }
    }

    internal class IncData
    {
        public List<Dmg> Damages = new List<Dmg>();
        public int DamageCount;
        public Obj_AI_Hero Hero;
        public bool TargetedCC;

        public float DamageTaken
        {
            get { return Damages.Sum(d => d.DamageTaken); }
            set { DamageTaken = value; }
        }

        public IncData(Obj_AI_Hero _hero)
        {
            this.Hero = _hero;
        }
    }

    internal class Dmg
    {
        public float DamageTaken;
        public float Time;

        public Dmg(float dmg)
        {
            DamageTaken = dmg;
            Time = Game.Time;
        }
    }
}
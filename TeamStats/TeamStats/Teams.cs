using LeagueSharp;
using LeagueSharp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamStats
{
    internal class Teams
    {
        public List<Obj_AI_Hero> myTeam = new List<Obj_AI_Hero>();
        public List<Obj_AI_Hero> EnemyTeam = new List<Obj_AI_Hero>();
        public int myTeamNum;
        public int enemyTeamNum;
        public int myTeamDmg;
        public int enemyTeamDmg;
        public int myTeamHP { get; set; }
        public int enemyTeamHP { get; set; }

        public Teams()
        {
            SetNums();
        }
        public void SetNums()
        {
            foreach (var player in ObjectManager.Get<Obj_AI_Hero>().Where(i => !i.IsDead && !i.IsMinion && Program.player.Distance(i) < Program.range))
            {
                //list
                if (player.IsEnemy) EnemyTeam.Add(player);
                if (player.IsAlly || player.IsMe) myTeam.Add(player);
                //num
                if (player.IsEnemy) enemyTeamNum++;
                if (player.IsAlly || player.IsMe) myTeamNum++;
            }
            myTeam = myTeam.OrderByDescending(i => i.ChampionsKilled).ToList();
            EnemyTeam = EnemyTeam.OrderByDescending(i => i.ChampionsKilled).ToList();
            var e = 0;
            var eHP = EnemyTeam[0].Health;
            foreach (var enemy in EnemyTeam)
            {
                if (myTeam.Count - 1 < e)
                {
                    break;
                }
                if (eHP < 0 && myTeam.Count-1>=e+1)
                {
                    e++;
                    eHP = myTeam[e].Health + eHP;
                }
                enemyTeamDmg += (int)ComboDamage(enemy, myTeam[e]);
                eHP -= (int)ComboDamage(enemy, myTeam[e]);
                enemyTeamHP += (int)(enemy.Health);

            }
            var t = 0;
            var tHP = EnemyTeam[0].Health;
            foreach (var teammate in myTeam)
            {
                if (EnemyTeam.Count-1<t)
                {
                    break;
                }
                if (tHP < 0 && EnemyTeam.Count-1>=t+1)
                {                    
                    t++; 
                    tHP = EnemyTeam[t].Health + tHP;

                }
                myTeamDmg += (int)ComboDamage(teammate, EnemyTeam[t]);
                tHP -= (int)ComboDamage(teammate, EnemyTeam[t]);
                myTeamHP += (int)(teammate.Health);
            }
        }
        private static float ComboDamage(Obj_AI_Hero src, Obj_AI_Hero dsc)
        {
            if (!src.IsValid || !dsc.IsValid) return 0f;
            float basicDmg = 0;
            int attacks = (int)Math.Floor(src.AttackSpeedMod*5);
            for (int i = 0; i < attacks; i++)
            {
                
                if (src.Crit>0)
                {
                    
                    basicDmg += (float)src.GetAutoAttackDamage(dsc) * attacks;
                }
                else
                {

                    basicDmg += (float)src.GetAutoAttackDamage(dsc);
                }
  
            };
            float damage = basicDmg;
            var spells = src.Spellbook.Spells;
            
            foreach (var spell in spells)
            {
                var t = spell.CooldownExpires - Game.Time;
                if (spell.Level > 0 && t < 0.5 && Damage.GetSpellDamage(src, dsc, spell.Slot) > 0)
                {
                    switch (src.SkinName)
                    {
                                case "Ahri":
                                    if (spell.Slot == SpellSlot.Q)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot));
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot,1));
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Akali":
                                    if (spell.Slot == SpellSlot.R)
                                    {
                                        damage += (float) (Damage.GetSpellDamage(src, dsc, spell.Slot)*spell.Ammo);
                                    }
                                    else if (spell.Slot == SpellSlot.Q)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot));
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot, 1));
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Amumu":
                                    if (spell.Slot == SpellSlot.W)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot) * 5);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Cassiopeia":
                                    if (spell.Slot == SpellSlot.Q || spell.Slot == SpellSlot.E || spell.Slot == SpellSlot.W)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot) * 2);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Fiddlesticks":
                                    if (spell.Slot == SpellSlot.W || spell.Slot == SpellSlot.E)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot)*5); 
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Garen":
                                    if (spell.Slot == SpellSlot.E)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot) * 3);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Irelia":
                                    if (spell.Slot == SpellSlot.W)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot) * attacks);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Karthus":
                                    if (spell.Slot == SpellSlot.Q)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot) * 4);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "KogMaw":
                                    if (spell.Slot == SpellSlot.W)
                                    {
                                        damage += (float)(Damage.GetSpellDamage(src, dsc, spell.Slot) * attacks);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "LeeSin":
                                    if (spell.Slot == SpellSlot.Q)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot,1);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Lucian":
                                    if (spell.Slot == SpellSlot.R)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot)*4;
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Nunu":
                                    if (spell.Slot != SpellSlot.R && spell.Slot != SpellSlot.Q)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "MasterYi":
                                    if (spell.Slot != SpellSlot.E)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot)*attacks;
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "MonkeyKing":
                                    if (spell.Slot != SpellSlot.R)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot) * 4;
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Pantheon":
                                    if (spell.Slot == SpellSlot.E)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot)*3;
                                    }
                                    else if (spell.Slot == SpellSlot.R)
                                    {
                                        damage +=0;
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    
                                    break;
                                case "Rammus":
                                    if (spell.Slot == SpellSlot.R)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot)*6;
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Riven":
                                    if (spell.Slot == SpellSlot.Q)
                                    {
                                        damage += RivenDamageQ(spell, src, dsc);
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Viktor":
                                    if (spell.Slot == SpellSlot.R)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot,1) * 5;
                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                case "Vladimir":
                                    if (spell.Slot == SpellSlot.E)
                                    {
                                        damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot) * 2;

                                    }
                                    else damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                    break;
                                default:
                                damage += (float)Damage.GetSpellDamage(src, dsc, spell.Slot);
                                break;
                    }
                                     
                }
            }
            
            if (src.Spellbook.CanUseSpell(src.GetSpellSlot("summonerdot")) == SpellState.Ready)
            {
                damage += (float)src.GetSummonerSpellDamage(dsc, Damage.SummonerSpell.Ignite);
            }
            return damage;
        }

        private static float RivenDamageQ(SpellDataInst spell,Obj_AI_Hero src, Obj_AI_Hero dsc)
        {
            double dmg = 0;
            if (spell.IsReady())
            {
                dmg += src.CalcDamage(dsc, Damage.DamageType.Physical,
                (-10 + (spell.Level * 20) +
                (0.35 + (spell.Level * 0.05)) * (src.FlatPhysicalDamageMod + src.BaseAttackDamage))*3);
            }
            return (float)dmg;
        }
    }
 }

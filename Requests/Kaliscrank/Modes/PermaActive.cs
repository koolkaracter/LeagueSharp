using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using LeagueSharp;
using LeagueSharp.Common;

using Settings = KalistaResurrection.Config.Misc;

namespace KalistaResurrection.Modes
{
    
    public class PermaActive : ModeBase
    {

        internal enum Spells
        {
            Q,
            W,
            E,
            R
        }

        private static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>()
        {
            { Spells.Q, new Spell(SpellSlot.Q, 1180) },
            { Spells.W, new Spell(SpellSlot.W, 5200) },
            { Spells.E, new Spell(SpellSlot.E, 1000) },
            { Spells.R, new Spell(SpellSlot.R, 1400) }
        };

        public PermaActive()
        {
            Orbwalking.OnNonKillableMinion += OnNonKillableMinion;
        }

        public override bool ShouldBeExecuted()
        {
            return true;
        }

        public override void Execute()
        {
            
            
            // Clear the forced target
            Hero.Orbwalker.ForceTarget(null);

            if (E.IsReady())
            {
                #region Killsteal
                // elKalista code by jQuery thx
                var target =
                HeroManager.Enemies.FirstOrDefault(
                    x =>
                        !x.HasBuffOfType(BuffType.Invulnerability) && !x.HasBuffOfType(BuffType.SpellShield) && !x.HasBuff("Undying Rage") &&
                        spells[Spells.E].CanCast(x) && (x.Health + (x.HPRegenRate / 2))
                        <= spells[Spells.E].GetDamage(x));

                if (Settings.UseKillsteal && spells[Spells.E].IsReady() && spells[Spells.E].CanCast(target))
                {
                    E.Cast();
                }

                #endregion

                #region E on big mobs

                if (Settings.UseEBig &&
                    ObjectManager.Get<Obj_AI_Minion>().Any(m => m.IsValidTarget(E.Range) && (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.Contains("Dragon") || m.BaseSkinName.Contains("Baron")) && m.IsRendKillable()))
                {
                    E.Cast();
                }

                #endregion

                #region E combo (minion + champ)

                else if (Settings.UseHarassPlus)
                {
                    var enemy = HeroManager.Enemies.Where(o => o.HasRendBuff()).OrderBy(o => o.Distance(Player, true)).FirstOrDefault();
                    if (enemy != null)
                    {
                        if (enemy.Distance(Player, true) < Math.Pow(E.Range + 200, 2))
                        {
                            if (ObjectManager.Get<Obj_AI_Minion>().Any(o => o.IsRendKillable() && E.IsInRange(o)))
                            {
                                E.Cast();
                            }
                        }
                    }
                }

                #endregion
            }
        }

        private void OnNonKillableMinion(AttackableUnit minion)
        {
            if (Settings.SecureMinionKillsE && E.IsReady())
            {
                var target = minion as Obj_AI_Base;
                if (target != null && target.IsRendKillable())
                {
                    // Cast since it's killable with E
                    SpellManager.E.Cast();
                }
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CombatExtended.WorldObjects
{
    public class HealthComp : WorldObjectComp, IWorldCompCE
    {        
        public const float HEALTH_HEALRATE_DAY = 0.15f;

        private int lastTick = -1;

        private float health = 1.0f;
        public float Health
        {
            get => health;
            set => health = Mathf.Clamp01(value);
        }        

        public float HealingRatePerTick
        {
            get => parent.Faction != null ? ((int)parent.Faction.def.techLevel) / 4f * HEALTH_HEALRATE_DAY / 60000f : 0f;
        }

        public float ArmorDamageMultiplier
        {
            get =>  4f / Mathf.Max((int)parent.Faction.def.techLevel, 1f);
        }

        public WorldObjectCompProperties_Health Props
        {
            get => props as WorldObjectCompProperties_Health;
        }
    
        public HealthComp()
        {
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref health, "health", 1.0f);
            Scribe_Values.Look(ref lastTick, "lastTick", -1);
        }
        
        public void ThrottledCompTick()
        {
            Health += HealingRatePerTick * WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
        }

        public void ApplyDamage(float amount, Faction attackingFaction)
        {
            if (Props.destoyedInstantly)
            {
                parent.Destroy();
                return;
            }            
            Health -= ArmorDamageMultiplier * amount;
            Notify_DamageTaken(attackingFaction);
        }

        public void Notify_DamageTaken(Faction attackingFaction)
        {
            if(health <= 1e-4)
            {
                Destroy();
                return;
            }
        }

        public void Destroy(Faction attackingFaction = null)
        {
            int tile = parent.Tile;
            Faction faction = parent.Faction;
            if (parent is Settlement settlement)
            {                
                parent.Destroy();
                DestroyedSettlement destroyedSettlement = (DestroyedSettlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.DestroyedSettlement);
                destroyedSettlement.tileInt = tile;
                if (faction != null)
                {
                    destroyedSettlement.SetFaction(faction);
                }
                destroyedSettlement.SpawnSetup();
                Find.World.worldObjects.Add(destroyedSettlement);                               
            }            
            else
            {                
                parent.Destroy();
            }           
            if (faction != null && faction.def.humanlikeFaction &&  attackingFaction != null && attackingFaction != faction) // check the projectile faction
            {
                FactionRelation relation = faction.RelationWith(attackingFaction, true);
                if (relation == null)
                {
                    faction.TryMakeInitialRelationsWith(attackingFaction);
                    relation = faction.RelationWith(attackingFaction, true);
                }
                faction.TryAffectGoodwillWith(attackingFaction, -100, true, true, HistoryEventDefOf.DestroyedEnemyBase, null);
            }
        }
    }
}


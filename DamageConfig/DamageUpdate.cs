using System.Collections.Generic;
using System.Linq;
using System.Text;
using OptionalUI;
using Partiality.Modloader;
using UnityEngine;

namespace DamageConfig
{
    public class DamageConfig : PartialityMod
    {
        public static OptionInterface LoadOI()
        {
            return new Config();
        }

        public DamageConfig()
        {
            instance = this;
            this.ModID = "DamageConfig";
            this.Version = "2";
            this.author = "laura#2871";
        }
        public static DamageConfig instance;

        public override void OnEnable()
        {
            base.OnEnable();
            On.Creature.Violence += new On.Creature.hook_Violence(ViolencePatch);
            On.Lizard.Violence += new On.Lizard.hook_Violence(LizardViolencePatch);
        }

        static void ViolencePatch(On.Creature.orig_Violence orig, Creature instance,
                              BodyChunk source, Vector2? directionAndMomentum,
                              BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage,
                              Creature.DamageType type, float damage, float stunBonus)
        {
            float newdamage = GetNewDamage(damage, source, type);
            orig(instance, source, directionAndMomentum, hitChunk, hitAppendage, type, newdamage, stunBonus);
        }

        static void LizardViolencePatch(On.Lizard.orig_Violence orig, Lizard instance,
                              BodyChunk source, Vector2? directionAndMomentum,
                              BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos,
                              Creature.DamageType type, float damage, float stunBonus)
        {
            float newdamage = GetNewDamage(damage, source, type);
            orig(instance, source, directionAndMomentum, hitChunk, onAppendagePos, type, newdamage, stunBonus);
        }

        static float GetNewDamage(float damage, BodyChunk source, Creature.DamageType type)
        {
            float newdamage = damage;
            if (source != null)
            {
                if (source?.owner is Weapon wep)
                {
                    if (wep.thrownBy is Player || DamageConfig.AllDamage)
                    {
                        if (wep is Spear)
                        {
                            newdamage *= DamageConfig.SpearDamageMult;
                            Debug.Log($"Spear damage changed from {damage} to {newdamage}");
                        }
                        else if (wep is Rock)
                        {
                            newdamage *= DamageConfig.RockDamageMult;
                            Debug.Log($"Rock damage changed from {damage} to {newdamage}");
                        }
                    }
                }
            }
            if (type == Creature.DamageType.Explosion)
            {
                newdamage *= DamageConfig.BombDamageMult;
                Debug.Log($"Explosion damage changed from {damage} to {newdamage}");
            }
            if (newdamage == damage)
            {
                Debug.Log($"Damage {damage} maintained of type {type}");
            }
            return newdamage;
        }
        public static float SpearDamageMult;
        public static float RockDamageMult;
        public static float BombDamageMult;
        public static bool AllDamage;
    }
}
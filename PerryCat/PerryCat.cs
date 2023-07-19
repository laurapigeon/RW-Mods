using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlugBase;
using UnityEngine;
using RWCustom;
using System.IO;
using System.Reflection;
using System.Collections;
using CustomWAV;
using MonoMod.RuntimeDetour;
using BepInEx;
using System.Security.Permissions;
using System.Security;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace PerryCatMod
{
    [BepInPlugin(MOD_ID, "PerryCat", "1.0")]
    partial class PerryCat : BaseUnityPlugin
    {
        public const string MOD_ID = "perrycat";

        public static Slugcatypus? perry;

        private bool _initialized;

        public void Awake()
        {
            On.RainWorld.OnModsInit += (orig, self) =>
            {
                orig(self);

                if (_initialized) return;
                _initialized = true;
                perry = new Slugcatypus();
                PlayerManager.RegisterCharacter(perry);
            };
        }
    }

    partial class Slugcatypus : SlugBaseCharacter
    {
        #region Vars
        public Hook? jollyLayerFixHook;

        public static AttachedField<Player, PerryVars> PVars = new();
        #endregion Vars

        public Slugcatypus() : base("Perry", FormatVersion.V1, 2, true)
        {
            On.RainWorldGame.ctor += Spawn_Room_hook;

            // no dreams, no overseer, drainage spawn, quarter food, new colour, food meter, stats, starting karma, starting mark, sea meat, backspear
            On.Player.ObjectEaten += Eating_hook;
            On.SeedCob.PlaceInRoom += Popcorn_hook;
            On.CreatureCommunities.LoadDefaultCommunityAlignments += Alignments_hook;

            On.Player.Update += Illness_N_Stealth_hook;
            On.Player.Collide += Bats_N_Melee_hook;
            On.Player.ThrownSpear += Spear_Damage_hook;
            On.Creature.Violence += Player_Spear_Damage_hook;
            On.Vulture.Violence += Vulture_Spear_Damage_hook;
            On.Lizard.Bite += Bite_Lethality_hook;
            On.Player.UpdateBodyMode += Tunnel_Boost_hook;
            On.Player.UpdateAnimation += Water_Boost_hook;
            On.Vulture.DropMask += Vulture_Mask_hook;
            On.BodyChunk.Update += Water_Hop_hook;
            On.Leech.Update += Poison_Leech_hook;
            On.Player.MovementUpdate += JS_Slide_N_Coyote_hook;
            On.Player.UpdateBodyMode += Run_Accum_hook;

            On.RainWorld.LoadResources += Atlas_hook;
            On.PlayerGraphics.InitiateSprites += Graphic_Init_hook;
            On.PlayerGraphics.AddToContainer += Graphic_Add_To_Container_hook;
            On.PlayerGraphics.DrawSprites += Graphic_Draw_Sprites_hook;

            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += Moon_Talk_hook;
            On.SSOracleBehavior.ctor += Pebbles_Ctor_hook;
            On.SSOracleBehavior.SeePlayer += Pebbles_See_Player_hook;
            On.SSOracleBehavior.PebblesConversation.AddEvents += Pebbles_Talk_hook;
            On.Oracle.HitByWeapon += Iterator_Hit_hook;
            On.SSOracleBehavior.ThrowOutBehavior.Update += Pebbles_Throw_Out_hook;
        }

        #region System
        protected override void Enable()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "JollyCoop")
                {
                    Type jollyplayregraphics_ref = asm.GetType("JollyCoop.PlayerGraphicsHK");
                    if (jollyplayregraphics_ref != null)
                    {
                        Debug.Log("Found type :)");
                        jollyLayerFixHook = new Hook(jollyplayregraphics_ref.GetMethod("SwichtLayersVanilla", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
                                                     typeof(Slugcatypus).GetMethod("PlayerGraphicsHK_orig_SwichtLayersVanilla_hk", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
                                                     this);
                    }
                }
            }

            NewSounds.Setup();
            SoundHooks.SetHooks();
        }

        protected override void Disable()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "JollyCoop")
                {
                    Type jollyplayregraphics_ref = asm.GetType("JollyCoop.PlayerGraphicsHK");
                    if (jollyplayregraphics_ref is not null && jollyLayerFixHook is not null)
                    {
                        jollyLayerFixHook.Undo();
                        jollyLayerFixHook.Free();
                    }
                }
            }

            SoundHooks.ClearHooks();
        }

        public override Stream GetResource(params string[] path)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("PerryCat.Resources." + string.Join(".", path));
        }

        private void Spawn_Room_hook(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);

            if (!IsMe(self)) return;

            for (int i = 0; i < self.Players.Count; i++)
            {
                bool inroom = self.world.GetAbstractRoom(self.Players[i].pos) != null;
                if (inroom)
                {
                    Room realizedRoom = self.Players[i].Room.realizedRoom;
                    bool instartroom = self.world.GetAbstractRoom(self.Players[i].pos).name == "DS_A19";
                    if (instartroom)
                    {
                        self.Players[i].pos.Tile = new IntVector2(20, 70);
                        realizedRoom.AddObject(new PerryStart(realizedRoom));
                    }
                }
            }
        }

        static PerryVars Get_PVars(Player self)
        {
            if (PVars[self] is null)
            {
                PVars[self] = new PerryVars();
            }
            return PVars[self];
        }
        #endregion System

        #region Stats
        public override string DisplayName => "The Slugcatypus";
        public override string Description => "It's just a slugcatypus... They don't do much.";
        public override bool HasDreams => false;
        public override bool HasGuideOverseer => false;
        public override string StartRoom => "DS_A19";
        public override bool GatesPermanentlyUnlock => true;

        public override float? GetCycleLength()
        {
            return 16;
        }

        public override Color? SlugcatColor(int slugcatCharacter, Color baseColor)
        {
            return new Color(0.08235f, 0.69804f, 0.63922f);  // #15B2A3
        }

        public override void GetFoodMeter(out int maxFood, out int foodToSleep)
        {
            maxFood = 11;
            foodToSleep = 2;
        }

        protected override void GetStats(SlugcatStats stats)
        {
            stats.runspeedFac = 1.2f;
            stats.bodyWeightFac = 1.12f;
            stats.generalVisibilityBonus = 0.1f;
            stats.visualStealthInSneakMode = 0.3f;
            stats.loudnessFac = 1.35f;
            stats.throwingSkill = 2;
            stats.poleClimbSpeedFac = 1.4f;
            stats.corridorClimbSpeedFac = 1.4f;
            stats.lungsFac /= 16;
        }

        public override void StartNewGame(Room room)
        {
            base.StartNewGame(room);
            if (room.game.session is StoryGameSession sgs)
            {
                sgs.saveState.deathPersistentSaveData.theMark = true;
                sgs.saveState.deathPersistentSaveData.karmaCap = 9;
                sgs.saveState.deathPersistentSaveData.karma = 9;
                sgs.saveState.deathPersistentSaveData.reinforcedKarma = true;
                sgs.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap = true;
                sgs.saveState.redExtraCycles = true;
                sgs.saveState.cycleNumber = 1;
            }
        }

        public override bool CanEatMeat(Player player, Creature crit)
        {
            return crit.dead && (crit.Template.type == CreatureTemplate.Type.Snail           || crit.Template.type == CreatureTemplate.Type.JetFish       ||
                                 crit.Template.type == CreatureTemplate.Type.GarbageWorm     || crit.Template.type == CreatureTemplate.Type.BigEel        ||
                                 crit.Template.type == CreatureTemplate.Type.Hazer           || crit.Template.type == CreatureTemplate.Type.Salamander    ||
                                 crit.Template.type == CreatureTemplate.Type.Leech           || crit.Template.type == CreatureTemplate.Type.SeaLeech      ||
                                 crit.Template.type == CreatureTemplate.Type.Vulture         || crit.Template.type == CreatureTemplate.Type.KingVulture   ||
                                 crit.Template.type == CreatureTemplate.Type.BrotherLongLegs || crit.Template.type == CreatureTemplate.Type.DaddyLongLegs );
        }

        public override void PlayerAdded(RainWorldGame game, Player player)
        {
            base.PlayerAdded(game, player);

            if (!IsMe(player)) return;

            player.spearOnBack = new Player.SpearOnBack(player);
        }

        private void Eating_hook(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            if (!IsMe(self))
            {
                orig(self, edible);
                return;
            }
            if (self.graphicsModule != null)
            {
                (self.graphicsModule as PlayerGraphics)?.LookAtNothing();
            }
            if (edible is Hazer || edible is JellyFish || edible is SwollenWaterNut) self.AddFood(edible.FoodPoints);
            else
            {
                for (int j = 0; j < 2.05 * edible.FoodPoints * UnityEngine.Random.value; j++)
                {
                    self.AddQuarterFood();
                }
                if (UnityEngine.Random.value < 0.05f)
                {
                    self.stun = Math.Max(self.stun, 120);
                    self.slowMovementStun = Math.Max(self.slowMovementStun, 160);
                }
                else
                {
                    Get_PVars(self).illnessCounter += 400;
                    self.SetMalnourished(true);
                    if (self.graphicsModule is PlayerGraphics playerGraphics)
                    {
                        playerGraphics.malnourished = 1f;
                    }
                    if (Get_PVars(self).illnessCounter == 0)
                    {
                        Get_PVars(self).illnessCounter += 400;
                    }
                }
            }

            if (self.spearOnBack != null)
            {
                self.spearOnBack.interactionLocked = true;
            }
        }

        private void Popcorn_hook(On.SeedCob.orig_PlaceInRoom orig, SeedCob self, Room placeRoom)
        {
            if (IsMe(placeRoom.game))
            {
                self.AbstractCob.opened = true;
                self.AbstractCob.dead = true;
            }
            orig(self, placeRoom);
        }

        private void Alignments_hook(On.CreatureCommunities.orig_LoadDefaultCommunityAlignments orig, CreatureCommunities self, SlugcatStats.Name saveStateNumber)
        {
            orig(self, saveStateNumber);

            if (!IsMe(self.session.game)) return;

            for (int i = 0; i < self.playerOpinions.GetLength(0); i++)
            {
                for (int j = 0; j < self.playerOpinions.GetLength(1); j++)
                {
                    for (int k = 0; k < self.playerOpinions.GetLength(2); k++)
                    {
                        self.playerOpinions[i, j, k] = -0.5f;
                    }
                }
            }
        }
        #endregion Stats

        #region Mechanics
        private void Illness_N_Stealth_hook(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (!IsMe(self)) return;

            if (Get_PVars(self).illnessCounter > 0)
            {
                Get_PVars(self).illnessCounter -= 1;
                if (Get_PVars(self).illnessCounter == 0 || self.readyForWin)
                {
                    self.SetMalnourished(false);
                }
                if (UnityEngine.Random.value < 0.002f)
                {
                    float stunTime = UnityEngine.Random.value;
                    self.stun = Math.Max(self.stun, (int)(stunTime * 40f));
                    self.slowMovementStun = Math.Max(self.slowMovementStun, (int)(stunTime * 80f));
                }
            }

            self.forceSleepCounter = 0;

            bool platypusState = self.bodyMode == Player.BodyModeIndex.Crawl && self.animation == Player.AnimationIndex.None && self.lowerBodyFramesOnGround > 0;
            bool perryState = self.bodyMode == Player.BodyModeIndex.Stand  || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam || self.animation == Player.AnimationIndex.Roll ||
                              self.animation == Player.AnimationIndex.Flip || self.bodyMode == Player.BodyModeIndex.Swimming;
            bool holdingSomething = self.grasps[0] != null || self.grasps[1] != null || self.spearOnBack.HasASpear;
            bool holdingDown;


            if (!self.playerState.isGhost) holdingDown = self.input[0].y < 0;
            else holdingDown = false;


            if (platypusState && !holdingSomething && holdingDown && !Get_PVars(self).justAPlatypus)
            {
                Get_PVars(self).stateCountup++;
                if (Get_PVars(self).stateCountup == 16)
                {
                    Get_PVars(self).stateCountup = 0;
                    Get_PVars(self).justAPlatypus = true;
                    self.slugcatStats.generalVisibilityBonus = -1000000f;
                    self.room.PlaySound(EnumExt_SoundID.Platypus_Noise, self.mainBodyChunk);
                }
            }
            else if (!Get_PVars(self).justAPlatypus && Get_PVars(self).stateCountup > 0)
            {
                Get_PVars(self).stateCountup--;
            }
            else if ((perryState || holdingSomething || (!holdingDown && self.bodyMode != Player.BodyModeIndex.Stunned)) && Get_PVars(self).justAPlatypus)
            {
                Get_PVars(self).stateCountup++;
                if (Get_PVars(self).stateCountup == 16)
                {
                    Get_PVars(self).stateCountup = 0;
                    Get_PVars(self).justAPlatypus = false;
                    self.slugcatStats.generalVisibilityBonus = 0.5f;
                }
            }
            else if (Get_PVars(self).justAPlatypus && Get_PVars(self).stateCountup > 0)
            {
                Get_PVars(self).stateCountup--;
            }

            if ((self.animation == Player.AnimationIndex.None) || (self.animation == Player.AnimationIndex.DeepSwim && self.waterJumpDelay == 10))
            {
                Get_PVars(self).collisionState = Player.AnimationIndex.Dead;  // this is so that if you leave a state in which youve collided with a creature, then you collide with them again in the same state, you still get the melee
            }
        }

        private void Bats_N_Melee_hook(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (!IsMe(self))
            {
                orig(self, otherObject, myChunk, otherChunk);
                return;
            }

            #region Orig without bat grabs
            bool flag = otherObject is Creature;
            if (flag)
            {
                bool flag2 = self.animation == Player.AnimationIndex.BellySlide;
                if (flag2)
                {
                    (otherObject as Creature)?.Stun((!self.longBellySlide) ? 2 : 4);
                    bool flag3 = !self.longBellySlide && self.rollCounter > 11;
                    if (flag3)
                    {
                        self.rollCounter = 11;
                    }
                    BodyChunk mainBodyChunk = self.mainBodyChunk;
                    mainBodyChunk.vel.x = mainBodyChunk.vel.x + (float)self.rollDirection * 3f;
                }
                bool sleeping = self.Sleeping;
                if (sleeping)
                {
                    self.sleepCounter = 0;
                }
            }
            bool consious = self.Consious;
            if (consious)
            {
                bool flag4 = self.wantToPickUp > 0 && self.CanIPickThisUp(otherObject);
                if (flag4)
                {
                    bool flag5 = self.Grabability(otherObject) == Player.ObjectGrabability.TwoHands;
                    if (flag5)
                    {
                        self.SlugcatGrab(otherObject, 0);
                    }
                    else
                    {
                        self.SlugcatGrab(otherObject, self.FreeHand());
                    }
                    self.wantToPickUp = 0;
                }
            }

            bool flag6 = self.jumpChunkCounter >= 0 && self.bodyMode == Player.BodyModeIndex.Default && myChunk == 1 && self.bodyChunks[1].pos.y > otherObject.bodyChunks[otherChunk].pos.y - otherObject.bodyChunks[otherChunk].rad / 2f;
            if (flag6)
            {
                self.jumpChunkCounter = 5;
                self.jumpChunk = otherObject.bodyChunks[otherChunk];
            }
            #endregion Orig without bat grabs

            if (!Get_PVars(self).justAPlatypus && (self.animation != Get_PVars(self).collisionState || !(Get_PVars(self).collisionObjects.Contains(otherObject.abstractPhysicalObject.ID))) && (otherObject is Creature creature) && !creature.dead)
            {
                float hitBonus = 0f;
                if (self.animation == Player.AnimationIndex.GrapplingSwing) hitBonus = 1f;
                else if (self.animation == Player.AnimationIndex.RocketJump) hitBonus = 1f;
                else if (self.animation == Player.AnimationIndex.Roll) hitBonus = 0.25f;
                else if (self.animation == Player.AnimationIndex.Flip)
                {
                    if (self.flipFromSlide) { hitBonus = 1.5f; Debug.Log("Slide flip bonus"); }
                    else hitBonus = 0.75f;
                }
                else if (self.animation == Player.AnimationIndex.BellySlide)
                {
                    if (self.longBellySlide) { hitBonus = 1.25f; Debug.Log("Boosted slide bonus"); }
                    else hitBonus = 0.5f;
                }
                else if (self.animation == Player.AnimationIndex.DeepSwim)
                {
                    if (self.waterJumpDelay > 0) { hitBonus = 1f; Debug.Log("Boosted swim bonus"); }
                    else hitBonus = 0.25f;
                }

                if (self.slideCounter == 0) hitBonus *= 0.75f;
                else if (0 < self.slideCounter && self.slideCounter < 10) { hitBonus *= 1.25f; Debug.Log("Turn store bonus"); }

                Debug.Log($"Hitbonus: {hitBonus}");
                Vector2 directionAndMomentum = hitBonus * self.bodyChunks[myChunk].vel.magnitude * (otherObject.bodyChunks[otherChunk].pos - self.bodyChunks[myChunk].pos).normalized;
                if (creature is Lizard && otherChunk == 0)
                {
                    creature.Violence(self.bodyChunks[myChunk], directionAndMomentum, otherObject.bodyChunks[1],
                                      null, Creature.DamageType.Blunt, 0.1f, 5f * directionAndMomentum.magnitude);
                }
                creature.Violence(self.bodyChunks[myChunk], directionAndMomentum, otherObject.bodyChunks[otherChunk],
                                  null, Creature.DamageType.Blunt, 0.1f, 5f * directionAndMomentum.magnitude);

                Get_PVars(self).collisionState = self.animation;
                Get_PVars(self).collisionObjects.Add(otherObject.abstractPhysicalObject.ID);

                self.room.PlaySound(SoundID.Slugcat_Super_Jump, self.bodyChunks[myChunk], false, hitBonus, 1f);

                float num2 = (8f * directionAndMomentum.magnitude);
                int num3 = (int)Math.Min((num2 / 2f), 25f);
                for (int j = 0; j < num3; j++)
                {
                    self.room.AddObject(new Spark(self.bodyChunks[myChunk].pos + Custom.DegToVec(UnityEngine.Random.value * 360f) * 5f * UnityEngine.Random.value,
                                        self.bodyChunks[myChunk].vel * -0.1f + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(0.2f, 0.4f, UnityEngine.Random.value) * self.bodyChunks[myChunk].vel.magnitude,
                                        new Color(1f, 1f, 1f), self.graphicsModule as LizardGraphics, 10, 170));
                }
            }
        }

        private void Spear_Damage_hook(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);

            if (!IsMe(self)) return;

            spear.spearDamageBonus *= Get_PVars(self).damageScale;
        }

        private void Player_Spear_Damage_hook(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (self as Player != null && source != null)
            {
                if (source?.owner is Weapon wep && wep.thrownBy is Player player && wep is Spear && IsMe(player))
                {
                    damage /= Get_PVars(player).damageScale;
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private void Vulture_Spear_Damage_hook(On.Vulture.orig_Violence orig, Vulture self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source != null && hitChunk != null)
            {
                if (source?.owner is Weapon wep && wep.thrownBy is Player player && wep is Spear && hitChunk.index == 4 && IsMe(player))
                {
                    damage /= Get_PVars(player).damageScale;
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
        }

        private void Bite_Lethality_hook(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
        {
            if (chunk.owner is Player player && player.room.game.session is StoryGameSession && IsMe(player))
            {
                float damageChance = self.lizardParams.biteDamageChance;

                self.lizardParams.biteDamageChance = (damageChance + (1 - (player.FoodInStomach / (float)player.slugcatStats.maxFood))) / 2f;
                Debug.Log($"{damageChance} -> {self.lizardParams.biteDamageChance}");

                orig(self, chunk);

                self.lizardParams.biteDamageChance = damageChance;
            }
            else orig(self, chunk);
        }

        private void Tunnel_Boost_hook(On.Player.orig_UpdateBodyMode orig, Player self)
        {
            orig(self);

            if (!IsMe(self)) return;

            bool flag3 = Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) < 5f && self.IsTileSolid(0, -1, 0) && self.IsTileSolid(0, 1, 0);
            bool flag4 = Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y) < 7.5f && self.IsTileSolid(0, 0, -1) && self.IsTileSolid(0, 0, 1);
            if (self.bodyMode == Player.BodyModeIndex.CorridorClimb && self.corridorTurnDir == null &&
                self.input[0].jmp && !self.input[1].jmp && self.slowMovementStun < 1 &&
                !(flag3 && self.input[0].y > -1 && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y) &&
                flag4 && (self.input[0].x == self.flipDirection || self.input[0].x == 0) && self.input[0].x != 0 &&
                self.verticalCorridorSlideCounter >= 1 && self.canCorridorJump > 0)
            {
                Vector2 pos3 = self.room.MiddleOfTile(self.bodyChunks[1].pos) + new Vector2(-9f * (float)self.flipDirection, 0f);
                for (int n = 0; n < 4; n++)
                {
                    if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
                    {
                        self.room.AddObject(new WaterDrip(pos3, new Vector2((float)self.flipDirection * 5f, Mathf.Lerp(-3f, 3f, UnityEngine.Random.value)), false));
                    }
                }
                self.room.PlaySound(SoundID.Slugcat_Corridor_Horizontal_Slide_Success, self.mainBodyChunk);
                BodyChunk bodyChunk25 = self.bodyChunks[0];
                bodyChunk25.pos.x = bodyChunk25.pos.x + 12f * (float)self.flipDirection;
                BodyChunk bodyChunk26 = self.bodyChunks[1];
                bodyChunk26.pos.x = bodyChunk26.pos.x + 12f * (float)self.flipDirection;
                self.bodyChunks[0].pos.y = self.room.MiddleOfTile(self.bodyChunks[0].pos).y;
                self.bodyChunks[1].pos.y = self.bodyChunks[0].pos.y;
                BodyChunk bodyChunk27 = self.bodyChunks[0];
                bodyChunk27.vel.x = bodyChunk27.vel.x + 7f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * (float)self.flipDirection;
                BodyChunk bodyChunk28 = self.bodyChunks[1];
                bodyChunk28.vel.x = bodyChunk28.vel.x + 7f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * (float)self.flipDirection;
                self.horizontalCorridorSlideCounter = 25;
                self.slowMovementStun = 5;
            }
        }

        private void Water_Boost_hook(On.Player.orig_UpdateAnimation orig, Player self)
        {
            if (IsMe(self))
            {
                if (self.animation == Player.AnimationIndex.DeepSwim && !(self.grasps[0] != null && self.grasps[0].grabbed is JetFish jetfish && jetfish.Consious))
                {
                    if (self.input[0].jmp && !self.input[1].jmp && self.airInLungs > 0.35f && (self.waterJumpDelay == 0))
                    {
                        self.airInLungs += 0.15f;
                        Debug.Log("Given extra o2");
                    }
                }
            }
            orig(self);
        }

        private void Vulture_Mask_hook(On.Vulture.orig_DropMask orig, Vulture self, Vector2 violenceDir)
        {
            Debug.Log("Attempted demask");
            if ((!(self.dead) && UnityEngine.Random.value < 0.02f) || (!IsMe(self.room.game))) orig(self, violenceDir);
        }

        private void Water_Hop_hook(On.BodyChunk.orig_Update orig, BodyChunk self)
        {
            if (IsMe(self.owner as Player))
            {
                if (float.IsNaN(self.vel.y))
                {
                    Debug.Log("VELY IS NAN");
                    self.vel.y = 0f;
                }
                if (float.IsNaN(self.vel.x))
                {
                    Debug.Log("VELX IS NAN");
                    self.vel.x = 0f;
                }
                self.vel.y = self.vel.y - self.owner.gravity;
                if (self.owner.room.water && self.pos.y - self.rad <= self.owner.room.FloatWaterLevel(self.pos.x))
                {
                    if (Mathf.Abs(self.vel.x) > 9f && Mathf.Abs(self.vel.y) < 0f && self.submersion < 0.5f)
                    {
                        self.vel.y = self.vel.y * -1.0f;
                    }
                }
                self.vel.y = self.vel.y + self.owner.gravity;
            }
            orig(self);
        }

        private void Poison_Leech_hook(On.Leech.orig_Update orig, Leech self, bool eu)
        {
            orig(self, eu);
            if (self.grasps != null && self.grasps?[0]?.grabbed is Player player && IsMe(player))
            {
                self.airDrown += 0.1011111111f;
            }
        }

        private void JS_Slide_N_Coyote_hook(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            float jumpBoost = self.jumpBoost;

            orig(self, eu);

            if (!IsMe(self)) return;

            if (self.animation == Player.AnimationIndex.BellySlide)
            {
                int num2 = 0;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (self.IsTileSolid(j, Custom.eightDirections[i].x, Custom.eightDirections[i].y) && self.IsTileSolid(j, Custom.eightDirections[i + 4].x, Custom.eightDirections[i + 4].y))
                        {
                            num2++;
                        }
                    }
                }
                if (!(num2 > 1 && self.bodyChunks[0].onSlope == 0 && self.bodyChunks[1].onSlope == 0 && (!self.IsTileSolid(0, 0, 0) || !self.IsTileSolid(1, 0, 0))) || (self.IsTileSolid(0, -1, 0) && self.IsTileSolid(0, 1, 0)) || (self.IsTileSolid(1, -1, 0) && self.IsTileSolid(1, 1, 0)))
                {
                    if (!(self.bodyChunks[0].ContactPoint.y == -1 || self.bodyChunks[1].ContactPoint.y == -1))
                    {
                        if (self.jumpBoost > 0f && (self.input[0].jmp || self.simulateHoldJumpButton > 0))
                        {
                            BodyChunk bodyChunk2 = self.bodyChunks[1];
                            bodyChunk2.vel.y -= (self.jumpBoost + 1f) * 0.3f;
                            BodyChunk bodyChunk = self.bodyChunks[0];
                            bodyChunk.vel.y -= (self.jumpBoost + 1f) * 0.3f;
                            self.jumpBoost += 1.5f;
                        }
                    }
                }
                if (self.jumpBoost == 0)
                {
                    self.jumpBoost = jumpBoost;
                }
            }
        }

        private void Run_Accum_hook(On.Player.orig_UpdateBodyMode orig, Player self)
        {
            int runCounter = self.initSlideCounter;

            orig(self);

            if (!IsMe(self)) return;

            if (self.bodyMode == Player.BodyModeIndex.Stand)
            {
                if (self.slideCounter > 0)
                {
                    self.initSlideCounter++;
                }
                else if (self.input[0].x != 0)
                {
                    if (self.input[0].x == self.slideDirection)
                    {
                        if (self.initSlideCounter >= 30)
                        {
                            self.initSlideCounter++;
                        }
                        if (self.initSlideCounter == 0)
                        {
                            self.initSlideCounter = runCounter + 1;
                        }
                    }
                }
            }
        }
        #endregion Mechanics

        #region Visual
        int firstExtra;
        static bool initiateSpritesToContainerLock = false;

        private void Atlas_hook(On.RainWorld.orig_LoadResources orig, RainWorld self)
        {
            orig(self);
            Slugcatypus.FetchAtlas("perryFace");
            Slugcatypus.FetchAtlas("perryAltFace");
            Slugcatypus.FetchAtlas("perryHead");
            Slugcatypus.FetchAtlas("perryLegs");
        }

        private void Graphic_Init_hook(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            initiateSpritesToContainerLock = true;
            orig(self, sLeaser, rCam);
            initiateSpritesToContainerLock = false;

            if (!IsMe(self.player)) return;

            firstExtra = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, firstExtra + 3);
            sLeaser.sprites[firstExtra] = new FSprite("perryFaceA0", true);
            sLeaser.sprites[firstExtra + 1] = new FSprite("perryHeadA0", true);
            sLeaser.sprites[firstExtra + 2] = new FSprite("perryLegsA0", true);
            self.tail = new TailSegment[4];
            self.tail[0] = new TailSegment(self, 4f, 4f, null, 0.85f, 1f, 1f, true);
            self.tail[1] = new TailSegment(self, 5f, 7f, self.tail[0], 0.85f, 1f, 0.35f, true);
            self.tail[2] = new TailSegment(self, 6f, 7f, self.tail[1], 0.85f, 1f, 0.35f, true);
            self.tail[3] = new TailSegment(self, 7f, 2f, self.tail[2], 0.85f, 1f, 0.35f, true);

            PlayerGraphics_AddToContainer_impl(self, sLeaser, rCam, newContatiner: null);
        }

        private void Graphic_Add_To_Container_hook(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);

            if (!IsMe(self.player)) return;

            if (initiateSpritesToContainerLock) return;

            PlayerGraphics_AddToContainer_impl(self, sLeaser, rCam, newContatiner);
        }

        private void PlayerGraphics_AddToContainer_impl(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner)
        {
            newContatiner ??= rCam.ReturnFContainer("Midground");

            for (int j = firstExtra; j < firstExtra + 3; j++)
            {
                newContatiner.AddChild(sLeaser.sprites[j]);
            }
            sLeaser.sprites[firstExtra + 2].MoveBehindOtherNode(sLeaser.sprites[0]);
            sLeaser.sprites[2].MoveBehindOtherNode(sLeaser.sprites[firstExtra + 2]);
        }

        private void Graphic_Draw_Sprites_hook(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            if (!IsMe(self.player))
            {
                orig(self, sLeaser, rCam, timeStacker, camPos);
                return;
            }

            if (self.malnourished == 0f && self.player.Malnourished)
            {
                self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            }

            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);

            sLeaser.sprites[9].alpha = 0f;
            sLeaser.sprites[2].color = new Color(0.98823f, 0.61176f, 0.37647f);

            sLeaser.sprites[firstExtra].scaleX = sLeaser.sprites[9].scaleX;
            sLeaser.sprites[firstExtra].scaleY = sLeaser.sprites[9].scaleY;
            sLeaser.sprites[firstExtra].SetPosition(sLeaser.sprites[9].GetPosition());
            sLeaser.sprites[firstExtra].rotation = sLeaser.sprites[9].rotation;
            sLeaser.sprites[firstExtra].color = new Color(1f, 1f, 1f);
            string facename = sLeaser.sprites[9].element.name;

            if (Get_PVars(self.player).justAPlatypus)
            {
                sLeaser.sprites[firstExtra].element = Futile.atlasManager.GetElementWithName("perryAlt" + facename);
                sLeaser.sprites[firstExtra + 1].alpha = Get_PVars(self.player).stateCountup / 16f;
            }
            else
            {
                sLeaser.sprites[firstExtra].element = Futile.atlasManager.GetElementWithName("perry" + facename);
                sLeaser.sprites[firstExtra + 1].alpha = 1f - Get_PVars(self.player).stateCountup / 16f;
            }

            sLeaser.sprites[firstExtra + 1].scaleX = sLeaser.sprites[3].scaleX;
            sLeaser.sprites[firstExtra + 1].scaleY = sLeaser.sprites[3].scaleY;
            sLeaser.sprites[firstExtra + 1].SetPosition(sLeaser.sprites[3].GetPosition());
            sLeaser.sprites[firstExtra + 1].rotation = sLeaser.sprites[3].rotation;
            sLeaser.sprites[firstExtra + 1].color = new Color(1f, 1f, 1f);
            string headname = sLeaser.sprites[3].element.name;
            sLeaser.sprites[firstExtra + 1].element = Futile.atlasManager.GetElementWithName("perry" + headname);

            sLeaser.sprites[firstExtra + 2].scaleX = sLeaser.sprites[4].scaleX;
            sLeaser.sprites[firstExtra + 2].scaleY = sLeaser.sprites[4].scaleY;
            sLeaser.sprites[firstExtra + 2].SetPosition(sLeaser.sprites[4].GetPosition());
            sLeaser.sprites[firstExtra + 2].rotation = sLeaser.sprites[4].rotation;
            sLeaser.sprites[firstExtra + 2].color = new Color(1f, 1f, 1f);
            string legsname = sLeaser.sprites[4].element.name;
            sLeaser.sprites[firstExtra + 2].element = Futile.atlasManager.GetElementWithName("perry" + legsname);

            if (self.player.bodyMode == Player.BodyModeIndex.Swimming && self.player.animation == Player.AnimationIndex.DeepSwim)
            {
                if (self.player.room.abstractRoom.index != 572)
                {
                    sLeaser.sprites[firstExtra].isVisible = false;
                }
                sLeaser.sprites[firstExtra + 2].isVisible = false;
            }
            else
            {
                sLeaser.sprites[firstExtra].isVisible = true;
                sLeaser.sprites[firstExtra + 2].isVisible = true;
            }
        }

        public delegate void Jolly_LayerFix_Hook(PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, bool newOverlap);
        public void PlayerGraphicsHK_orig_SwichtLayersVanilla_hk(Jolly_LayerFix_Hook orig_hook, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, bool newOverlap)
        {
            orig_hook(self, sLeaser, rCam, newOverlap);

            if (!IsMe(self.player)) return;

            FContainer fcontainer = rCam.ReturnFContainer(newOverlap ? "Background" : "Midground");
            PlayerGraphics_AddToContainer_impl(self, sLeaser, rCam, fcontainer);
        }

        public static FAtlas FetchAtlas(string name)
        {
            if (Futile.atlasManager.DoesContainAtlas(name)) return Futile.atlasManager.GetAtlasWithName(name);
            else return LoadAtlas(name);
        }

        private static FAtlas LoadAtlas(string name)
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            // Load image from resources
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            {
                Stream atlasImage = asm.GetManifestResourceStream($"PerryCat.Resources.{name}.png");
                byte[] data = new byte[atlasImage.Length];
                atlasImage.Read(data, 0, data.Length);
                tex.LoadImage(data);
                tex.filterMode = FilterMode.Point;
            }

            string json;
            {
                Stream atlasJson = asm.GetManifestResourceStream($"PerryCat.Resources.{name}.txt");
                byte[] data = new byte[atlasJson.Length];
                atlasJson.Read(data, 0, data.Length);
                json = System.Text.Encoding.UTF8.GetString(data);
            }

            FAtlas atlas = Futile.atlasManager.LoadAtlasFromTexture(name, tex, false); // ???? texture from atlas
            LoadAtlasData(atlas, json);
            Debug.Log($"Loaded atlas \"{name}\" with {atlas.elements.Count} elements:");
            for(int i = 0; i < atlas.elements.Count; i++)
            {
                Debug.Log($"\t({i}) \"{atlas.elements[i].name}\"");
            }
            return atlas;
        }

        // From FAtlas.LoadAtlasData
        private static readonly FieldInfo _FAtlas_elementsByName = typeof(FAtlas).GetField("_elementsByName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static void LoadAtlasData(FAtlas atlas, string data)
        {
            Dictionary<string, FAtlasElement> elementsByName = (Dictionary<string, FAtlasElement>)_FAtlas_elementsByName.GetValue(atlas);
            atlas.elements.Clear();
            elementsByName.Clear();

            Dictionary<string, object> atlasData = data.dictionaryFromJson();
            Dictionary<string, object> frames = (Dictionary<string, object>)atlasData["frames"];
            float invScl = Futile.resourceScaleInverse;
            int elemIndex = 0;
            foreach (KeyValuePair<string, object> element in frames)
            {
                FAtlasElement fatlasElement = new FAtlasElement();
                fatlasElement.indexInAtlas = elemIndex++;
                string text = element.Key;
                if (Futile.shouldRemoveAtlasElementFileExtensions)
                {
                    int num2 = text.LastIndexOf(".");
                    if (num2 >= 0) text = text.Substring(0, num2);
                }
                fatlasElement.name = text;
                IDictionary elementProperties = (IDictionary)element.Value;
                fatlasElement.isTrimmed = (bool)elementProperties["trimmed"];
                IDictionary elemFrame = (IDictionary)elementProperties["frame"];
                float elemX = float.Parse(elemFrame["x"].ToString());
                float elemY = float.Parse(elemFrame["y"].ToString());
                float elemW = float.Parse(elemFrame["w"].ToString());
                float elemH = float.Parse(elemFrame["h"].ToString());
                Rect uvRect = new Rect(elemX / atlas.textureSize.x, (atlas.textureSize.y - elemY - elemH) / atlas.textureSize.y, elemW / atlas.textureSize.x, elemH / atlas.textureSize.y);
                fatlasElement.uvRect = uvRect;
                fatlasElement.uvTopLeft.Set(uvRect.xMin, uvRect.yMax);
                fatlasElement.uvTopRight.Set(uvRect.xMax, uvRect.yMax);
                fatlasElement.uvBottomRight.Set(uvRect.xMax, uvRect.yMin);
                fatlasElement.uvBottomLeft.Set(uvRect.xMin, uvRect.yMin);
                IDictionary elemSourceSize = (IDictionary)elementProperties["sourceSize"];
                fatlasElement.sourcePixelSize.x = float.Parse(elemSourceSize["w"].ToString());
                fatlasElement.sourcePixelSize.y = float.Parse(elemSourceSize["h"].ToString());
                fatlasElement.sourceSize.x = fatlasElement.sourcePixelSize.x * invScl;
                fatlasElement.sourceSize.y = fatlasElement.sourcePixelSize.y * invScl;
                IDictionary dictionary6 = (IDictionary)elementProperties["spriteSourceSize"];
                float left = float.Parse(dictionary6["x"].ToString()) * invScl;
                float top = float.Parse(dictionary6["y"].ToString()) * invScl;
                float width = float.Parse(dictionary6["w"].ToString()) * invScl;
                float height = float.Parse(dictionary6["h"].ToString()) * invScl;
                fatlasElement.sourceRect = new Rect(left, top, width, height);
                atlas.elements.Add(fatlasElement);
                elementsByName.Add(fatlasElement.name, fatlasElement);
            }

            Futile.atlasManager.AddAtlas(atlas);
        }
        #endregion Visual

        #region Story
        private void Moon_Talk_hook(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            if (!IsMe(self.myBehavior.oracle.room.game))
            {
                orig(self);
                return;
            }

            if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Hello Agent P, Major Monogram speaking."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I am communicating with you through this weird supercomputer..."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It seems pretty busted up but we were able to hijack the system to reach you."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("We have intel that Doofenshmirtz was spotted scaling the side of a superstructure not too far from here."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("We aren't sure what he's planning, but with localised power readings like that, he must be up to something!"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Get out there and figure out what he's pla-<LINE>Carl what is it? You know we only have limited time to give Agent P their debrie-"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Umm, hello Agent P, this is Carl.<LINE>Analysis shows the inhabitants of this world are quite aggressive towards smaller creatures like you,<LINE>so be sure not to get cornered by something hungry."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Your hover car has been updated with Doofenshmirtz's co-ordinates, it'll only take you a few minutes to fly there.<LINE>GIVE ME THA- Sorry Agent P, we're running out of time."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Get up there and put a stop to Doof's schemes. Monogram out!"), 0));
            }
            else if (self.id == Conversation.ID.MoonSecondPostMarkConversation)
            {
                switch (self.State.neuronsLeft)
                {
                    case 1:
                        self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                        break;
                    case 2:
                        self.events.Add(new Conversation.TextEvent(self, 80, self.Translate("...agent..."), 10));
                        break;
                    case 3:
                        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("Agent...P... are you recieving... this?"), 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Agent...P... come in!"), 0));
                        break;
                    case 4:
                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Agent P! We're losing connection."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Make sure not to damage our comms system.<LINE>It's the only connection we have with you"), 0));
                        break;
                    case 5:
                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Good luck out there Agent P!"), 0));
                        break;
                }
            }
        }

        private void Pebbles_Ctor_hook(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
        {
            orig(self, oracle);

            if (IsMe(oracle.room.game)) self.pearlPickupReaction = false;
        }

        private void Pebbles_See_Player_hook(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {
            if (!IsMe(self.oracle.room.game))
            {
                orig(self);
                return;
            }

            if (self.timeSinceSeenPlayer < 0)
            {
                self.timeSinceSeenPlayer = 0;
            }
            if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0)
            {
                self.NewAction(SSOracleBehavior.Action.MeetRed_Init);
                self.SlugcatEnterRoomReaction();
            }
            else
            {
                self.throwOutCounter = 0;
                self.getToWorking = 1;
                Debug.Log("Throw out player " + self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts);
                if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 8)
                {
                    self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                }
                else
                {
                    self.NewAction(SSOracleBehavior.Action.ThrowOut_SecondThrowOut);
                }
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts++;
            }
            self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
        }

        private void Pebbles_Talk_hook(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            if (!IsMe(self.owner.player))
            {
                orig(self);
                return;
            }

            bool debug_mode = false;

            if (debug_mode)
            {
                self.owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("[conversation skipped]"), 0));
                self.owner.movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
            }
            else
            {
                self.owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Ah Perry the Platypus! So nice of you to DROP IN!"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Do you like my new secret evil lair? So roomy, and it even comes with a remote control puppet!<LINE>It just needs a little redecorating before it really starts to feel like home."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Anyway, let me tell you my evil scheme."), 0));

                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Do you know what I hate, Perry the Platypus? Rain. I hate getting caught in the rain without an umbrella...<LINE>I hate stepping in puddles... I hate being splashed by jerks in cars..."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...and the rain here is terrible! Have you gotten caught in it, Perry the Platypus?<LINE>I got completely soaked even with an umbrella! Why even bother, right?"), 0));

                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("So that's when I had this great idea! BEHOLD, MY DRILLINATOR."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("... ok so you can't actually see it because it's deep underground<LINE>and I also didn't BUILD it per say, just FOUND it, but..."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Anyway as we speak it is drilling down down down, creating a massive hole for all the rain to go into.<LINE>No more water, no more evaporation, no more water cycle, no more rain. Pretty good eh, right?"), 0));

                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Once the rain is gone, the locals will accept me as their de facto leader, and then I can assemble an army to take over... wait for it..."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("THE ENTIRE TRI LOCAL GROUP AREA!!!"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...I think that's what it's called anyway."), 0));

                self.owner.movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
                self.events.Add(new Conversation.TextEvent(self, 0, "...", 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Would you just hold on for a moment, Perry the Platypus? My Drillinator just stopped for some reason..."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, "...", 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Stupid junk, how old is this thing anyway?"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, "...", 0));

                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You know what Perry the Platypus, you should go.<LINE>I've got a lot to do, someone keeps sending me weird messages, and you're clearly not going to stop me this time."), 0));
            }
        }

        private void Iterator_Hit_hook(On.Oracle.orig_HitByWeapon orig, Oracle self, Weapon weapon)
        {
            if (!IsMe(weapon.thrownBy as Player))
            {
                orig(self, weapon);
                return;
            }

            if (self.ID == Oracle.OracleID.SS)
            {
                var behav = (SSOracleBehavior)self.oracleBehavior;
                behav.dialogBox.Interrupt(behav.Translate("Nice try Perry the platypus,<LINE>but you'll have to try a lot harder than that if you want to defeat me this time!<LINE>And don't get any funny ideas about finding my new secret bunker either; this place is huge!"), 10);
            }
            else if (self.ID == Oracle.OracleID.SL)
            {
                (self.oracleBehavior as SLOracleBehavior)?.Pain();
            }
        }

        private void Pebbles_Throw_Out_hook(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, SSOracleBehavior.ThrowOutBehavior self)
        {
            if (!IsMe(self.oracle.room.game))
            {
                orig(self);
                return;
            }

            if (self.player.room == self.oracle.room)
            {
                if (self.telekinThrowOut && !self.oracle.room.aimap.getAItile(self.player.mainBodyChunk.pos).narrowSpace)
                {
                    self.player.mainBodyChunk.vel += Custom.DirVec(self.player.mainBodyChunk.pos, self.oracle.room.MiddleOfTile(28, 32)) * 0.2f * (1f - self.oracle.room.gravity) * Mathf.InverseLerp(220f, 280f, (float)self.inActionCounter);
                }
            }

            if (self.action == SSOracleBehavior.Action.ThrowOut_ThrowOut)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
                self.telekinThrowOut = true;
                if (self.owner.throwOutCounter == 0)
                {
                    self.dialogBox.Interrupt(self.Translate("I'll catch you later Perry the Platypus."), 0);
                }
                else if (self.owner.throwOutCounter == 120)
                {
                    self.dialogBox.Interrupt(self.Translate("Remember to address me as your Supreme Leader next time we meet."), 0);
                }
                if (self.owner.playerOutOfRoomCounter > 80)
                {
                    self.owner.NewAction(SSOracleBehavior.Action.ThrowOut_SecondThrowOut);
                    self.owner.getToWorking = 1f;
                }
                if (self.player.room == self.oracle.room)
                {
                    self.owner.throwOutCounter++;
                }
            }
            else if (self.action == SSOracleBehavior.Action.ThrowOut_SecondThrowOut)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 1)
                {
                    self.telekinThrowOut = false;
                    if (self.owner.throwOutCounter == 0)
                    {
                        self.dialogBox.Interrupt(self.Translate("...don't know anything about a senior, I'm a very respectable age. Who are you again?"), 0);
                    }
                    else if (self.owner.throwOutCounter == 200)
                    {
                        self.dialogBox.Interrupt(self.Translate("What was that guy's problem? Norm can we block his number?"), 0);
                    }
                }
                else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 2)
                {
                    self.telekinThrowOut = (self.inActionCounter > 300);
                    if (self.owner.throwOutCounter == 120)
                    {
                        self.dialogBox.Interrupt(self.Translate("You know Norm, these little glowy snacks are delicious!"), 0);
                    }
                    else if (self.owner.throwOutCounter == 440)
                    {
                        self.dialogBox.Interrupt("...", 0);
                    }
                    else if (self.owner.throwOutCounter == 480)
                    {
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                        self.dialogBox.Interrupt(self.Translate("Perry the Platypus, didn't you leave already? I don't have any cucumber water or I'd offer, sorry."), 0);
                    }
                }
                else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 3)
                {
                    self.telekinThrowOut = false;
                    if (self.owner.throwOutCounter == 0)
                    {
                        self.dialogBox.Interrupt(self.Translate("-way I could take City Hall like babies taking candy away from... whatever they take candy...<LINE>Aw, you know what I mean. Anyway, the Babe-inator was ready to go, so I took aim at my brother's office and pressed the button,<LINE>but it didn't work! It didn't work for, like, the hundredth time! My evil plan did not work! And I thought to myself,<LINE>\"Maybe I'm just not good at being evil.\" That's when I had my- Oh that guy's calling again ? Norm let me take that."), 0);
                    }
                }
                else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 4)
                {
                    self.telekinThrowOut = false;
                    if (self.owner.throwOutCounter == 0)
                    {
                        self.dialogBox.Interrupt(self.Translate("...at Gunther Goat Cheese's: The goat-cheesiest place in all of Druselstein.<LINE>Many of my closest friends were there: Count Wolfgang, Betty the She-Boar, Raputin, and- Norm are you listening?"), 0);
                    }
                }
                else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 5)
                {
                    self.telekinThrowOut = false;
                    if (self.owner.throwOutCounter == 120)
                    {
                        self.dialogBox.Interrupt(self.Translate("Uh, I think we made too much \"void fluid\", Norm. I guess we'll have to give everyone a 2-ton take-home vat.<LINE>And I still think you should have added more gravel."), 0);
                    }
                }
                else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 6)
                {
                    self.telekinThrowOut = false;
                    if (self.owner.throwOutCounter == 160)
                    {
                        self.dialogBox.Interrupt(self.Translate("Behold Norm, my LIZARDINATOR"), 0);
                    }
                }
                else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 7)
                {
                    self.telekinThrowOut = false;
                    if (self.owner.throwOutCounter == 0)
                    {
                        self.dialogBox.Interrupt(self.Translate("-I mean, it's got to be the biggest one here, right? It's gonna win."), 0);
                    }
                    else if (self.owner.throwOutCounter == 400)
                    {
                        self.dialogBox.Interrupt(self.Translate("What?"), 0);
                    }
                }
                else if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiThrowOuts == 8)
                {
                    self.telekinThrowOut = false;
                    if (self.owner.throwOutCounter == 200)
                    {
                        self.dialogBox.Interrupt(self.Translate("(snoring)"), 0);
                    }
                }
                if (self.player.room == self.oracle.room)
                {
                    self.owner.throwOutCounter++;
                }
                if (self.owner.playerOutOfRoomCounter > 0)
                {
                    self.owner.NewAction(SSOracleBehavior.Action.General_Idle);
                    self.owner.getToWorking = 1f;
                }
            }
            else if (self.action == SSOracleBehavior.Action.ThrowOut_KillOnSight)
            {

                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                self.telekinThrowOut = false;
                if (self.owner.throwOutCounter == 0)
                {
                    self.dialogBox.Interrupt(self.Translate("- I'm just saying that if we find the self destruct button we won't press it by accident. What does this lever do?"), 0);
                }
                else if (self.owner.throwOutCounter == 120)
                {
                    self.dialogBox.Interrupt(self.Translate("Oops."), 0);
                }
                else if (self.owner.throwOutCounter == 600)
                {
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                    self.dialogBox.Interrupt(self.Translate("Oh you're back again Perry the Platypus? Did you fall asleep? If this is a trick it won't work, I'm not coming out of my bunker."), 0);
                }
                if (self.owner.throwOutCounter >= 120)
                {
                    if ((!self.player.dead || self.owner.killFac > 0.5f) && self.player.room == self.oracle.room)
                    {
                        self.owner.killFac += 0.025f;
                        if (self.owner.killFac >= 1f)
                        {
                            self.player.mainBodyChunk.vel += Custom.RNV() * 12f;
                            for (int i = 0; i < 20; i++)
                            {
                                self.oracle.room.AddObject(new Spark(self.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            }
                            self.player.Die();
                            self.owner.killFac = 0f;
                        }
                    }
                    else
                    {
                        self.owner.killFac *= 0.8f;
                    }
                }
                if (self.player.room == self.oracle.room)
                {
                    self.owner.throwOutCounter++;
                }
                if (self.owner.playerOutOfRoomCounter > 0)
                {
                    self.owner.NewAction(SSOracleBehavior.Action.General_Idle);
                }
            }
        }
        #endregion
    }
}

    partial class PerryStart : UpdatableAndDeletable
    {
        private Player? player
        {
            get
            {
                return (room.game.Players.Count <= 0) ? null : (room.game.Players[0].realizedCreature as Player);
            }
        }

        public PerryStart(Room room)
        {
            this.room = room;
        }

        public override void Update(bool eu)
        {
            bool flag = player != null;
            bool donefadein = room.game.manager.blackDelay <= 0f && room.game.manager.fadeToBlack < 0.9f;
            if (flag && !donefadein)
            {
                for (int i = 0; i < 2; i++)
                {
                    player.bodyChunks[i].HardSetPosition(room.MiddleOfTile(20, 70 + i));
                    player.bodyChunks[i].vel = new Vector2(4, 0);
                }
            }

            if (donefadein)
            {
                if (timer == 0)
                {
                    startController = new PerryStart.StartController(this);
                    player.controller = startController;
                }
                timer++;
            }
            if (timer == 30)
            {
                player.controller = null;
                Destroy();
            }
            base.Update(eu);
        }

        private int timer;

        private PerryStart.StartController startController;

        public class StartController : Player.PlayerController
        {
            public StartController(PerryStart owner)
            {
                this.owner = owner;
            }

            public override Player.InputPackage GetInput()
            {
                return new Player.InputPackage(false, 0, 0, false, false, false, false, false);
            }

            public PerryStart owner;
        }
    }

    public class PerryVars 
    {
        public int stateCountup;
        public bool justAPlatypus;
        public Player.AnimationIndex collisionState;
        public List<EntityID> collisionObjects;
        public bool jollyCheck;
        public float damageScale = 0.25f;
        public int illnessCounter;
        public Color perryColor = new Color(0.08235f, 0.69804f, 0.63922f); // 15B2A3
        public Color perryIllColor = new Color(0.18431f, 0.70980f, 0.60000f); // 2EB599

    public PerryVars()
        {
            stateCountup = 0;
            justAPlatypus = false;
            collisionState = Player.AnimationIndex.None;
            collisionObjects = new List<EntityID>();
            illnessCounter = 0;
        }
    }

#region Sound classes
internal class ShroomSound : UpdatableAndDeletable
{
public ShroomSound(Player player)
{
    this.owner = player;
    this.shroomLoopSound = new DisembodiedDynamicSoundLoop(this);
    this.shroomLoopSound.sound = SoundID.Normal_Rain_LOOP;
    this.shroomLoopSound.Volume = Mathf.Lerp(0f, 1.2f, this.owner.Adrenaline);
}

public override void Update(bool eu)
{
    base.Update(eu);
    if (this.owner.Adrenaline == 0)
    {
        this.Destroy();
    }
    else
    {
        this.shroomLoopSound.Volume = Mathf.Lerp(0f, 0.3f, this.owner.Adrenaline);
        this.shroomLoopSound.Update();
    }
}

public DisembodiedDynamicSoundLoop shroomLoopSound;

public Player owner;
}

class SoundHooks
    {
        public static void SetHooks()
        {
            On.VirtualMicrophone.PlaySound_SoundID_PositionedSoundEmitter_bool_float_float_bool += VirtualMicrophone_PlaySound;
            On.SoundLoader.GetAudioClip += SoundLoader_GetAudioClip_hk;
        }

        public static void ClearHooks()
        {
            On.VirtualMicrophone.PlaySound_SoundID_PositionedSoundEmitter_bool_float_float_bool -= VirtualMicrophone_PlaySound;
            On.SoundLoader.GetAudioClip -= SoundLoader_GetAudioClip_hk;
        }


        static void VirtualMicrophone_PlaySound(On.VirtualMicrophone.orig_PlaySound_SoundID_PositionedSoundEmitter_bool_float_float_bool orig,
                VirtualMicrophone self, SoundID soundID, PositionedSoundEmitter controller, bool loop, float vol,
                float pitch, bool randomStartPosition)
        {
            if (NewSounds.SoundNames.Contains(soundID.ToString()))
            {
                SoundLoader.SoundData soundData = GetPerrySound(soundID);
                self.soundObjects.Add(new VirtualMicrophone.ObjectSound(self, soundData, loop, controller, vol, pitch, randomStartPosition));
            }
            else
            {
                orig(self, soundID, controller, loop, vol, pitch, randomStartPosition);
            }
        }

        static SoundLoader.SoundData GetPerrySound(SoundID soundID)
        {
            int audioClip = NewSounds.PerryAudio.FirstOrDefault(x => x.Value == soundID).Key;
            return new SoundLoader.SoundData(soundID, audioClip, 0.4f, 1, 1, 1);
        }

        static AudioClip SoundLoader_GetAudioClip_hk(On.SoundLoader.orig_GetAudioClip orig, SoundLoader self, int i)
        {
            if (NewSounds.PerryAudio.ContainsKey(i))
            {
                WAV wav = new WAV(GetWavData(i));
                AudioClip audioClip = AudioClip.Create(NewSounds.PerryAudio[i].ToString(), wav.SampleCount, wav.ChannelCount,
                        wav.Frequency, false, false);
                audioClip.SetData(wav.LeftChannel, 0);
                return audioClip;
            }
            else
            {
                return orig(self, i);
            }
        }

        static byte[] GetWavData(int i)
        {
            string wavName = NewSounds.PerryAudio[i].ToString();
            UnmanagedMemoryStream stream = SoundResources.ResourceManager.GetStream(wavName);

            byte[] bytes = new byte[stream.Length];
            for (int j = 0; j < stream.Length; j++)
            {
                bytes[j] = (byte)stream.ReadByte();
            }
            return bytes;
        }
    }

class NewSounds
{
    public static void Setup()
    {
        FieldInfo[] fields = typeof(EnumExt_SoundID).GetFields();
        List<string> fieldNames = new List<string>();
        foreach (FieldInfo fInfo in fields)
        {
            fieldNames.Add(fInfo.Name);
        }
        SoundNames = fieldNames;
    }

    public static List<string> SoundNames { get; private set; }

    public static List<int> SlugcatAudioIndexes { get => new List<int> { 47579, 47580 }; }

    public static Dictionary<int, SoundID> PerryAudio
    {
        get =>
            new Dictionary<int, SoundID>
            {
                { 47579, EnumExt_SoundID.Platypus_Noise },
                { 47580, EnumExt_SoundID.Theme_Song }
            };
    }
}

public class EnumExt_SoundID
{
    public static SoundID Platypus_Noise;
public static SoundID Theme_Song;
}

public class SoundResources
{

    private static global::System.Resources.ResourceManager resourceMan;

    private static global::System.Globalization.CultureInfo resourceCulture;

    internal SoundResources()
    {
    }

    /// <summary>
    ///   Returns the cached ResourceManager instance used by this class.
    /// </summary>
    public static global::System.Resources.ResourceManager ResourceManager
    {
        get
        {
            if (object.ReferenceEquals(resourceMan, null))
            {
                global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PerryCat.SoundResources", typeof(SoundResources).Assembly);
                resourceMan = temp;
            }
            return resourceMan;
        }
    }

    /// <summary>
    ///   Overrides the current thread's CurrentUICulture property for all
    ///   resource lookups using this strongly typed resource class.
    /// </summary>
    public static global::System.Globalization.CultureInfo Culture
    {
        get
        {
            return resourceCulture;
        }
        set
        {
            resourceCulture = value;
        }
    }

    /// <summary>
    ///   Looks up a localized resource of type System.IO.UnmanagedMemoryStream similar to System.IO.MemoryStream.
    /// </summary>
    public static System.IO.UnmanagedMemoryStream Platypus_Noise => ResourceManager.GetStream("Platypus_Noise", resourceCulture);
    public static System.IO.UnmanagedMemoryStream Theme_Song_Intro => ResourceManager.GetStream("Theme_Song_Intro", resourceCulture);
    public static System.IO.UnmanagedMemoryStream Theme_Song_Loop => ResourceManager.GetStream("Theme_Song_Loop", resourceCulture);
}

namespace CustomWAV 
{
    public class WAV
    {

        // convert two bytes to one float in the range -1 to 1
        static float bytesToFloat(byte firstByte, byte secondByte)
        {
            // convert two bytes to one short (little endian)
            short s = (short)((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            return s / 32768.0F;
        }

        static int bytesToInt(byte[] bytes, int offset = 0)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                value |= ((int)bytes[offset + i]) << (i * 8);
            }
            return value;
        }

        private static byte[] GetBytes(string filename)
        {
            return File.ReadAllBytes(filename);
        }
        // properties
        public float[] LeftChannel { get; internal set; }
        public float[] RightChannel { get; internal set; }
        public int ChannelCount { get; internal set; }
        public int SampleCount { get; internal set; }
        public int Frequency { get; internal set; }

        // Returns left and right double arrays. 'right' will be null if sound is mono.
        public WAV(string filename) :
            this(GetBytes(filename))
        { }

        public WAV(byte[] wav)
        {
            // Determine if mono or stereo
            ChannelCount = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

            // Get the frequency
            Frequency = bytesToInt(wav, 24);

            // Get past all the other sub chunks to get to the data subchunk:
            int pos = 12;   // First Subchunk ID from 12 to 16

            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;

            // Pos is now positioned to start of actual sound data.
            SampleCount = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)
            if (ChannelCount == 2) SampleCount /= 2;        // 4 bytes per sample (16 bit stereo)

            // Allocate memory (right will be null if only mono sound)
            LeftChannel = new float[SampleCount];
            if (ChannelCount == 2) RightChannel = new float[SampleCount];
            else RightChannel = null;

            // Write to double array/s:
            int i = 0;
            while (pos < wav.Length)
            {
                LeftChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
                pos += 2;
                if (ChannelCount == 2)
                {
                    RightChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
                    pos += 2;
                }
                i++;
            }
        }

        public override string ToString()
        {
            return string.Format("[WAV: LeftChannel={0}, RightChannel={1}, ChannelCount={2}, SampleCount={3}, Frequency={4}]", LeftChannel, RightChannel, ChannelCount, SampleCount, Frequency);
        }
    }
}
#endregion Sound classes

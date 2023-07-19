using BepInEx;
using System.Security;
using System.Security.Permissions;
using System;
using UnityEngine;
using RWCustom;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace AnyPounceRoll;

[BepInPlugin(MOD_ID, "Any Pounce Roll", "1.0")]
partial class RWAnyPounceRoll : BaseUnityPlugin
{
    public const string MOD_ID = "anypounceroll";

    public AttachedField<Player, bool> _chargeHopped = new();

    private bool _initialized;

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            On.Player.ctor += Player_ctor;
            On.Player.TerrainImpact += Player_TerrainImpact;
            On.Player.MovementUpdate += Player_MovementUpdate;
        };
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        // give every slugcat a corresponding jumpCount value
        _chargeHopped.Set(self, false);
    }

    private void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        bool otherRollReasons = speed > 12f || self.animation == Player.AnimationIndex.Flip || (self.animation == Player.AnimationIndex.RocketJump && self.rocketJumpFromBellySlide);
        bool rollRequirements = self.input[0].downDiagonal != 0 && self.animation != Player.AnimationIndex.Roll && 
            direction.y < 0 && self.allowRoll > 0 && self.consistentDownDiagonal > ((speed > 24f) ? 1 : 6);
        if ((self.animation == Player.AnimationIndex.RocketJump || _chargeHopped.Get(self)) && !otherRollReasons && rollRequirements)
        {
            if (self.animation == Player.AnimationIndex.RocketJump)
            {
                BodyChunk bodyChunk = self.bodyChunks[1];
                bodyChunk.vel.y += 3f;
                BodyChunk bodyChunk2 = self.bodyChunks[1];
                bodyChunk2.pos.y += 3f;
                BodyChunk bodyChunk3 = self.bodyChunks[0];
                bodyChunk3.vel.y -= 3f;
                BodyChunk bodyChunk4 = self.bodyChunks[0];
                bodyChunk4.pos.y -= 3f;
            }
            self.room.PlaySound(SoundID.Slugcat_Roll_Init, self.mainBodyChunk.pos, 1f, 1f);
            self.animation = Player.AnimationIndex.Roll;
            self.rollDirection = self.input[0].downDiagonal;
            self.rollCounter = 0;
            self.bodyChunks[0].vel.x = Mathf.Lerp(self.bodyChunks[0].vel.x, 9f * self.input[0].x, 0.7f);
            self.bodyChunks[1].vel.x = Mathf.Lerp(self.bodyChunks[1].vel.x, 9f * self.input[0].x, 0.7f);
            self.standing = false;
        }
        else
        {
            if (_chargeHopped.Get(self)) _chargeHopped.Set(self, false);
            orig(self, chunk, direction, speed, firstContact);
        }
    }

    private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
        if (!_chargeHopped.Get(self) && self.killSuperLaunchJumpCounter > 0)
        {
            _chargeHopped.Set(self, true);
        }
    }
}
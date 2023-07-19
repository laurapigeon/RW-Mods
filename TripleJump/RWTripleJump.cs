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

namespace TripleJump;

[BepInPlugin(MOD_ID, "Triple Jump", "1.0")]
partial class RWTripleJump : BaseUnityPlugin
{
    public const string MOD_ID = "triplejump";

    public AttachedField<Player, int> _jumpCount = new();

    private bool _initialized;

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            On.Player.ctor += Player_ctor;
            On.Player.Jump += Player_Jump;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.TerrainImpact += Player_TerrainImpact;
        };
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        // give every slugcat a corresponding jumpCount value
        _jumpCount.Set(self, 0);
    }

    private void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        int jumpCount = _jumpCount.Get(self);
        bool bypassTurnCheck = false;

        // if jumpCount is on the 3rd (index 2) jump or greater, set the turn-boost counter to 1 before calling orig, thus causing a flip
        if (jumpCount >= 2 && (self.slideCounter >= 10 && self.slideCounter <= 20) && self.standing && self.bodyMode == Player.BodyModeIndex.Stand)
        {
            self.slideCounter = 1;
            bypassTurnCheck = true;
        }
        orig(self);

        // increase player jumpBoost and do visual and audio for all relevant jumps that would increase jumpCounter
        if (bypassTurnCheck || (self.slideCounter >= 10 && self.slideCounter <= 20) && self.standing && self.bodyMode == Player.BodyModeIndex.Stand)
        {
            self.jumpBoost += 1.5f * jumpCount;
            _jumpCount.Set(self, jumpCount + 1);

            self.room.PlaySound(SoundID.Slugcat_Super_Jump, self.mainBodyChunk, false, 0.5f + (0.2f * jumpCount), 1f + (0.1f * jumpCount));
            for (int i = 0; i < 2 + (5 * jumpCount); i++)
            {
                self.room.AddObject(new WaterDrip(self.bodyChunks[1].pos + new Vector2(0f, -self.bodyChunks[1].rad + 1f),
                    Custom.DegToVec(self.slideDirection * Mathf.Lerp(30f, 70f, UnityEngine.Random.value)) * Mathf.Lerp(6f, 11f, UnityEngine.Random.value), false));
            }
        }
    }

    private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        orig(self, eu);

        // reset players jumpCounter if no longer in turn-boost
        if (self.slideCounter == 0 && _jumpCount.Get(self) != 0)
        {
            _jumpCount.Set(self, 0);
        }
    }

    private void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        orig(self, chunk, direction, speed, firstContact);

        // give player enough turn-boost upon landing holding up from roll-height to triple jump, but not flip
        if (direction.y < 0 && speed > 12f && self.input[0].y > 0 && self.input[0].x != 0)
        {
            self.slideDirection = -self.input[0].x;
            self.slideCounter = 10;
        }
    }
}
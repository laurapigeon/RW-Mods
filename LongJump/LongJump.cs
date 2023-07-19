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

namespace LongJump;

[BepInPlugin(MOD_ID, "Long Jump", "1.0")]
partial class RWLongJump : BaseUnityPlugin
{
    public const string MOD_ID = "longjump";

    public AttachedField<Player, int> _startedTurn = new();
    public AttachedField<Player, int> _turnBackCounter = new();

    private bool _initialized;

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            On.Player.ctor += Player_ctor;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.Jump += Player_Jump;
        };
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        _startedTurn.Set(self, 0);
        _turnBackCounter.Set(self, 0);
    }

    private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        if (self.slideCounter == 1)
        {
            _startedTurn.Set(self, 4);
        }
        int startedTurn = _startedTurn.Get(self);
        int turnBackCounter = _turnBackCounter.Get(self);
        if (startedTurn > 0)
        {
            if (self.input[0].x == self.slideDirection)
            {
                _turnBackCounter.Set(self, 10);
                _startedTurn.Set(self, 0);
            }
            else if ((self.slideCounter == 0 || self.slideCounter > 9))
            {
                _startedTurn.Set(self, startedTurn - 1);
            }
        }
        if (turnBackCounter > 0)
        {
            if (startedTurn > 0 && self.input[0].x != self.slideDirection || self.bodyMode != Player.BodyModeIndex.Stand)
            {
                _turnBackCounter.Set(self, 0);
            }
            else
            {
                _turnBackCounter.Set(self, turnBackCounter - 1);
            }
        }
        orig(self, eu);
    }

    private void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        orig(self);
        if (_turnBackCounter.Get(self) > 0)
        {
            self.bodyChunks[0].vel.x = 10f * self.slideDirection;
            self.bodyChunks[1].vel.x = 9f * self.slideDirection;

            self.standing = false;
            self.slideCounter = 0;
            _turnBackCounter.Set(self, 0);

            self.room.PlaySound(SoundID.Slugcat_Super_Jump, self.mainBodyChunk, false, 0.7f, 1.1f);
            for (int i = 0; i < 2 + 5; i++)
            {
                self.room.AddObject(new WaterDrip(self.bodyChunks[1].pos + new Vector2(0f, -self.bodyChunks[1].rad + 1f),
                Custom.DegToVec(-self.slideDirection * Mathf.Lerp(30f, 70f, UnityEngine.Random.value)) * Mathf.Lerp(6f, 11f, UnityEngine.Random.value), false));
            }
        }
    }
}
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

namespace EasyRollSlide;

[BepInPlugin(MOD_ID, "Easy Roll Slide", "1.0")]
partial class RWEasyRollSlide : BaseUnityPlugin
{
    public const string MOD_ID = "easyrollslide";

    public AttachedField<Player, int> _listenForRollSlideInput = new();
    public AttachedField<Player, int> _startRollSlide = new();

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
        _listenForRollSlideInput.Set(self, 0);
        _startRollSlide.Set(self, 0);
    }

    private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        int rollSlideReady = _listenForRollSlideInput.Get(self);
        int startRollSlide = _startRollSlide.Get(self);

        if (rollSlideReady > 0)
        {
            // initiate slide when able
            if (_listenForRollSlideInput.Get(self) > 0 && self.input[0].y == -1 && self.input[0].jmp)
            {
                //Debug.Log("ready to rollslide");
                self.animation = Player.AnimationIndex.None;
                _startRollSlide.Set(self, 10);
                startRollSlide = 10;
                _listenForRollSlideInput.Set(self, 0);
            }
            else if (self.animation != Player.AnimationIndex.Roll)
            {
                _listenForRollSlideInput.Set(self, rollSlideReady - 1);
                if (rollSlideReady - 1 == 0)
                {
                    //Debug.Log("stopped listening for rollslide input");
                }
            }
        }

        if (startRollSlide > 0)
        {
            self.standing = true;
            // start slide once slugcat is ready
            if (self.bodyMode == Player.BodyModeIndex.Stand && self.animation != Player.AnimationIndex.StandUp)
            {
                //Debug.Log("now standing, go rollslide!");
                self.animation = Player.AnimationIndex.BellySlide;
                self.rollDirection = self.flipDirection;
                self.rollCounter = 0;
                self.standing = false;
                self.room.PlaySound(SoundID.Slugcat_Belly_Slide_Init, self.mainBodyChunk, false, 1f, 1f);
                _startRollSlide.Set(self, 0);
                self.bodyChunks[1].vel.y -= 2f;
                self.input[1].jmp = true;
                self.wantToJump = 0;
            }
            else if (self.animation != Player.AnimationIndex.Roll)
            {
                _startRollSlide.Set(self, startRollSlide - 1);
                if (startRollSlide - 1 == 0)
                {
                    //Debug.Log("had to cancel rollslide");
                }
            }
        }

        if (rollSlideReady == 0 && startRollSlide == 0 && self.animation == Player.AnimationIndex.Roll && self.input[0].y == 0)
        {
            //Debug.Log("listening for rollslide input");
            _listenForRollSlideInput.Set(self, 6);
        }

        orig(self, eu);
    }

    private void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        if (_startRollSlide.Get(self) == 0)
        {
            orig(self);
        }
        else
        {
            //Debug.Log("caught bad jump");
        }
    }
}
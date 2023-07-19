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

namespace PullupFix;

[BepInPlugin(MOD_ID, "Pullup Fix", "1.0")]
partial class RWPullupFix : BaseUnityPlugin
{
    public const string MOD_ID = "pullupfix";

    private bool _initialized;

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            On.Player.UpdateAnimation += Player_UpdateAnimation;
        };
    }

    private void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
    {
        bool notGettingUp = false;
        if (self.animation != Player.AnimationIndex.GetUpOnBeam) notGettingUp = true;

        orig(self);

        if (notGettingUp && self.animation == Player.AnimationIndex.GetUpOnBeam)
        {
            if (self.input[0].x == 0) self.straightUpOnHorizontalBeam = true;
            else self.flipDirection = self.input[0].x;
        }
    }
}
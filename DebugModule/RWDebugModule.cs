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

namespace DebugModule;

[BepInPlugin(MOD_ID, "Debug Module", "1.0")]
partial class RWDebugModule : BaseUnityPlugin
{
    public const string MOD_ID = "debugmodule";

    private bool _initialized;

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            On.GlobalRain.DeathRain.DeathRainUpdate += DeathRain_DeathRainUpdate;
        };
    }

    private void DeathRain_DeathRainUpdate(On.GlobalRain.DeathRain.orig_DeathRainUpdate orig, GlobalRain.DeathRain self)
    {
        orig(self);
        Debug.Log(self.deathRainMode);
    }
}
using UnityEngine;
using BepInEx;
using System.Security;
using System.Security.Permissions;
using Fisobs.Core;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace Inchworms;

[BepInPlugin(MOD_ID, "Inchworm", "1.0")]
partial class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "inchworm";

    private bool _initialized;

    public void OnEnable()
    {
        Content.Register(new IContent[]
        {
            new InchwormCritob()
        });
    }

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            // Ons go here
        };
    }
}
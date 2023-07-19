using BepInEx;
using System.Security;
using System.Security.Permissions;
using System;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace DirectionalityFix;

[BepInPlugin(MOD_ID, "Directionality Fix", "1.0")]
partial class RWDirectionalityFix : BaseUnityPlugin
{
    public const string MOD_ID = "directionalityfix";

    private bool _initialized;

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            On.Player.UpdateAnimation += Player_UpdateAnimation;
            IL.BodyChunk.Update += BodyChunk_Update;
        };
    }

    private void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
    {
        Player.AnimationIndex oldAnimation = self.animation;
        orig(self);
        if (oldAnimation == Player.AnimationIndex.CrawlTurn)
        {
            if (self.input[0].x == 0 && self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x)
            {
                self.bodyChunks[0].vel.y -= 5f;
                if (self.bodyChunks[0].pos.y < self.bodyChunks[1].pos.y + 2f)
                {
                    self.animation = Player.AnimationIndex.None;
                    self.bodyChunks[0].vel.y -= 1f;
                }
            }
        }
    }

    private void BodyChunk_Update(ILContext context)
    {
        try
        {
            Logger.LogWarning("waterbounce directionality ilhook go");
            ILCursor c = new(context);

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<Room>("water"),
                x => x.MatchLdloc(0),
                x => x.MatchAnd(),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<BodyChunk>("vel"),
                x => x.MatchLdfld<Vector2>("x")
                );
            c.EmitDelegate((float vel_x) =>
            {
                return Mathf.Abs(vel_x);
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
}
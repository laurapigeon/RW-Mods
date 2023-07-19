using BepInEx;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace ThrowMovement;

[BepInPlugin(MOD_ID, "Throw Movement", "1.1")]
partial class RWThrowMovement : BaseUnityPlugin
{
    public const string MOD_ID = "throwmovement";

    public static Configurable<int> reqFramesOnGround = new(0);
    public static Configurable<int> reqFramesSinceThrow = new(20);
    public static Configurable<int> defRockLifespan = new(80);
    public static int rockReplenishInterval = 0;
    public Dictionary<Rock, int> rockLifespans = new();

    private bool _initialized;

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            MachineConnector.SetRegisteredOI(MOD_ID, new RWTMOptions());

            On.Player.ReleaseObject += Player_ReleaseObject;
            On.Player.ThrowObject += Player_ThrowObject;
            On.Player.GrabUpdate += Player_GrabUpdate;
            On.Rock.Update += Rock_Update;
            On.Rock.PickedUp += Rock_PickedUp;
        };
    }

    private void Player_ReleaseObject(On.Player.orig_ReleaseObject orig, Player self, int grasp, bool eu)
    {
        orig(self, grasp, eu);
        rockReplenishInterval = reqFramesSinceThrow.Value;
    }

    private void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        orig(self, grasp, eu);
        rockReplenishInterval = reqFramesSinceThrow.Value;
    }

    private void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.objectInStomach?.type == AbstractPhysicalObject.AbstractObjectType.Rock)
        {
            if (self.lowerBodyFramesOnGround > reqFramesOnGround.Value && self.FreeHand() > -1 && rockReplenishInterval <= 0)
            {
                AbstractPhysicalObject newRock = new(self.room.world, AbstractPhysicalObject.AbstractObjectType.Rock, null,
                                                     self.room.GetWorldCoordinate(self.firstChunk.pos), self.room.game.GetNewID());
                newRock.RealizeInRoom();
                self.SlugcatGrab(newRock.realizedObject, self.FreeHand());
                if (newRock.realizedObject is Rock rock) rockLifespans.Add(rock, defRockLifespan.Value);
                rockReplenishInterval = reqFramesSinceThrow.Value;
            }
            rockReplenishInterval -= 1;
        }
    }

    private void Rock_Update(On.Rock.orig_Update orig, Rock self, bool eu)
    {
        orig(self, eu);
        if (rockLifespans.ContainsKey(self) && Weapon.Mode.Carried != self.mode)
        {
            if (rockLifespans[self] <= 0) self.Destroy();
            rockLifespans[self] -= 1;
        }
    }

    private void Rock_PickedUp(On.Rock.orig_PickedUp orig, Rock self, Creature upPicker)
    {
        if (rockLifespans.ContainsKey(self) && Weapon.Mode.Carried != self.mode)
        {
            rockLifespans[self] = defRockLifespan.Value;
        }
    }
}
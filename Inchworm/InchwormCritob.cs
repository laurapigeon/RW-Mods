using DevInterface;
using Fisobs.Creatures;
using Fisobs.Properties;
using Fisobs.Sandbox;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static PathCost.Legality;
using CreatureType = CreatureTemplate.Type;

namespace Inchworms;

sealed class InchwormCritob : Critob
{
    public static readonly CreatureType Inchworm = new("Inchworm", true);
    public static readonly MultiplayerUnlocks.SandboxUnlockID InchwormUnlock = new("Inchworm", true);

    public InchwormCritob() : base(Inchworm)
    {
        LoadedPerformanceCost = 20f;
        SandboxPerformanceCost = new(linear: 0.6f, exponential: 0.1f);
        ShelterDanger = ShelterDanger.Safe;
        CreatureName = "Inchworm";

        RegisterUnlock(killScore: KillScore.Configurable(2), InchwormUnlock, parent: MultiplayerUnlocks.SandboxUnlockID.Leech, data: 0);
    }

    public override CreatureTemplate CreateTemplate()
    {
        // CreatureFormula does most of the ugly work for you when creating a new CreatureTemplate,
        // but you can construct a CreatureTemplate manually if you need to.

        CreatureTemplate t = new CreatureFormula(this)
        {
            TileResistances = new()
            {
                OffScreen = new(1f, Allowed),
                Floor = new(1f, Allowed),
                Corridor = new(1f, Allowed),
                Climb = new(1f, Allowed),
                Ceiling = new(1f, Allowed),
                Wall = new(1f, Allowed),
            },
            ConnectionResistances = new()
            {
                CeilingSlope = new(1.5f, Allowed),
                Standard = new(1f, Allowed),
                DropToFloor = new(10f, Allowed),
                DropToWater = new(10f, Allowed),
                DropToClimb = new(10f, Allowed),
                ShortCut = new(1.5f, Allowed),
                NPCTransportation = new(3f, Allowed),
                OffScreenMovement = new(1f, Allowed),
                BetweenRooms = new(5f, Allowed),
                Slope = new(1.5f, Allowed),
                SideHighway = new(1f, Allowed),
                OpenDiagonal = new(3f, Allowed),
                ReachOverGap = new(3f, Allowed),
                ReachUp = new(2f, Allowed),
                SemiDiagonalReach = new(2f, Allowed),
                ReachDown = new(2f, Allowed)
            },
            DefaultRelationship = new(CreatureTemplate.Relationship.Type.Uncomfortable, 0.25f),
            DamageResistances = new()
            {
                Base = 0.95f,
                Stab = 2f,
            },
            StunResistances = new()
            {
                Base = 5f,
            },
            HasAI = true,
            Pathing = PreBakedPathing.Ancestral(CreatureType.GreenLizard),
        }.IntoTemplate();

        // The below properties are derived from vanilla creatures, so you should have your copy of the decompiled source code handy.

        // Some notes on the fields of CreatureTemplate:

        // offScreenSpeed       how fast the creature moves between abstract rooms
        // abstractLaziness     how long it takes the creature to start migrating
        // smallCreature        determines if rocks instakill, if large predators ignore it, etc
        // dangerToPlayer       DLLs are 0.85, spiders are 0.1, pole plants are 0.5
        // waterVision          0..1 how well the creature can see through water
        // throughSurfaceVision 0..1 how well the creature can see through water surfaces
        // movementBasedVision  0..1 bonus to vision for moving creatures
        // lungCapacity         ticks until the creature falls unconscious from drowning
        // quickDeath           determines if the creature should die as determined by Creature.Violence(). if false, you must define custom death logic
        // saveCreature         determines if the creature is saved after a cycle ends. false for overseers and garbage worms
        // hibernateOffScreen   true for deer, miros birds, leviathans, vultures, and scavengers
        // bodySize             batflies are 0.1, eggbugs are 0.4, DLLs are 5.5, slugcats are 1

        t.abstractedLaziness = 10;  // mosq was 200
        t.instantDeathDamageLimit = float.MaxValue;
        t.offScreenSpeed = 1f;   // mosquito was 0.1
        t.waterPathingResistance = 2f;
        t.meatPoints = 3;
        t.communityInfluence = 0.1f;
        t.waterVision = 0.1f;  // surfacewalker is 2
        t.waterRelationship = CreatureTemplate.WaterRelationship.AirOnly;
        t.roamBetweenRoomsChance = 0.95f;  // 0.7
        t.bodySize = 0.4f;  // 0.3
        t.stowFoodInDen = false;
        t.shortcutSegments = 1;
        t.grasps = 1;
        t.visualRadius = 800f;
        t.movementBasedVision = 1f;  // 0.65
        t.canFly = false;
        t.dangerousToPlayer = 0.2f;  // 0.4

        return t;
    }

    public override void EstablishRelationships()
    {
        // You can use StaticWorld.EstablishRelationship, but the Relationships class exists to make this process more ergonomic.

        Relationships relationships = new(Inchworm);

        foreach (var template in StaticWorld.creatureTemplates)
        {
            if (template.quantified)
            {
                relationships.Ignores(template.type);
                relationships.IgnoredBy(template.type);
            }
        }

        relationships.Eats(CreatureType.EggBug, 0.8f);
        relationships.Eats(CreatureType.LanternMouse, 0.7f);
        relationships.Eats(CreatureType.Slugcat, 0.7f);
        relationships.Eats(CreatureType.Scavenger, 0.5f);
        relationships.Eats(CreatureType.DropBug, 0.4f);
        relationships.Eats(CreatureType.Centipede, 0.3f);
        relationships.Eats(CreatureType.SmallCentipede, 0.2f);
        relationships.Eats(CreatureType.LizardTemplate, 0.1f);

        relationships.Intimidates(CreatureType.TentaclePlant, 0.35f);

        relationships.AttackedBy(CreatureType.BigNeedleWorm, 1f);
        relationships.AttackedBy(CreatureType.Centipede, 0.2f);

        relationships.EatenBy(CreatureType.CicadaA, 1f);
        relationships.EatenBy(CreatureType.CicadaB, 1f);
        relationships.EatenBy(CreatureType.LizardTemplate, 0.4f);
        relationships.EatenBy(CreatureType.Vulture, 0.4f);

        relationships.Fears(CreatureType.Vulture, 0.6f);
        relationships.Fears(CreatureType.BigNeedleWorm, 0.8f);
        relationships.EatenBy(CreatureType.CicadaA, 0.8f);
        relationships.EatenBy(CreatureType.CicadaB, 0.8f);
    }

    public override ArtificialIntelligence CreateRealizedAI(AbstractCreature acrit)
    {
        return new InchwormAI(acrit, (Inchworm)acrit.realizedCreature);
    }

    public override Creature CreateRealizedCreature(AbstractCreature acrit)
    {
        return new Inchworm(acrit);
    }

    public override void ConnectionIsAllowed(AImap map, MovementConnection connection, ref bool? allowed)
    {
        // DLLs don't travel through shortcuts that start and end in the same room—they only travel through room exits.
        // To emulate this behavior, use something like:

        //ShortcutData.Type n = ShortcutData.Type.Normal;
        //if (connection.type == MovementConnection.MovementType.ShortCut) {
        //    allowed &=
        //        connection.startCoord.TileDefined && map.room.shortcutData(connection.StartTile).shortCutType == n ||
        //        connection.destinationCoord.TileDefined && map.room.shortcutData(connection.DestTile).shortCutType == n
        //        ;
        //} else if (connection.type == MovementConnection.MovementType.BigCreatureShortCutSqueeze) {
        //    allowed &=
        //        map.room.GetTile(connection.startCoord).Terrain == Room.Tile.TerrainType.ShortcutEntrance && map.room.shortcutData(connection.StartTile).shortCutType == n ||
        //        map.room.GetTile(connection.destinationCoord).Terrain == Room.Tile.TerrainType.ShortcutEntrance && map.room.shortcutData(connection.DestTile).shortCutType == n
        //        ;
        //}
    }

    public override void TileIsAllowed(AImap map, IntVector2 tilePos, ref bool? allowed)
    {
        // Large creatures like vultures, miros birds, and DLLs need 2 tiles of free space to move around in. Leviathans need 4! None of them can fit in one-tile tunnels.
        // To emulate this behavior, use something like:

        //allowed &= map.IsFreeSpace(tilePos, tilesOfFreeSpace: 2);

        // DLLs can fit into shortcuts despite being fat.
        // To emulate this behavior, use something like:

        //allowed |= map.room.GetTile(tilePos).Terrain == Room.Tile.TerrainType.ShortcutEntrance;
    }

    public override IEnumerable<string> WorldFileAliases()
    {
        yield return "inch";
        yield return "inchworm";
    }

    public override IEnumerable<RoomAttractivenessPanel.Category> DevtoolsRoomAttraction()
    {
        yield return RoomAttractivenessPanel.Category.Dark;
        yield return RoomAttractivenessPanel.Category.LikesInside;
        yield return RoomAttractivenessPanel.Category.LikesOutside;
    }

    public override string DevtoolsMapName(AbstractCreature acrit)
    {
        return "inch";
    }

    public override Color DevtoolsMapColor(AbstractCreature acrit)
    {
        // Default would return the mosquito's icon color (which is gray), which is fine, but red is better.
        return Color.HSVToRGB(.26f, .22f, .92f);
    }

    public override ItemProperties? Properties(Creature crit)
    {
        // If you don't need the `forObject` parameter, store one ItemProperties instance as a static object and return that.
        // The CentiShields example demonstrates this.
        if (crit is Inchworm inchworm)
        {
            return new InchwormProperties(inchworm);
        }

        return null;
    }
}
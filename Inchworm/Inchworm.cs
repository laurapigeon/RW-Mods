using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inchworms;

internal sealed class Inchworm : InsectoidCreature
{
    enum Mode
    {
        Free,
        StuckInChunk
    }

    public bool ChargingAttack
    {
        get
        {
            return chargeCounter > 0 && chargeCounter < 15;
        }
    }

    // Token: 0x1700034F RID: 847
    // (get) Token: 0x060014F1 RID: 5361 RVA: 0x00170FC1 File Offset: 0x0016F1C1
    public bool Attacking
    {
        get
        {
            return chargeCounter > 15;
        }
    }

    public BodyChunk ChunkInOrder(int i)
    {
        return bodyChunks[(i == 0) ? 1 : ((i == 1) ? 0 : 2)];
    }

    public InchwormAI AI = null!;
    public float runSpeed;
    public Vector2 needleDir;
    public Vector2 lastNeedleDir;

    // IntVector2 stuckTile;

    int stuckCounter;
    MovementConnection? lastFollowedConnection;
    Vector2 travelDir;
    Vector2 stuckPos;
    Vector2 stuckDir;
    Mode mode;

    public Vector2 swimDirection = new(1, 0);
    public int landWalkDir;
    public float landWalkCycle;
    private int chargeCounter = 0;
    IntVector2 lastAirTile;

    public Inchworm(AbstractCreature acrit) : base(acrit, acrit.world)
    {
        bodyChunks = new BodyChunk[3];
        bodyChunks[1] = new BodyChunk(this, 1, new Vector2(0f, 0f), 3f, 0.125f);
        bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 3.5f, 0.25f);
        bodyChunks[2] = new BodyChunk(this, 2, new Vector2(0f, 0f), 3f, 0.125f);
        bodyChunkConnections = new BodyChunkConnection[3];
        bodyChunkConnections[0] = new(bodyChunks[1], bodyChunks[0], 7f, BodyChunkConnection.Type.Normal, 1f, -1f);
        bodyChunkConnections[1] = new(bodyChunks[0], bodyChunks[2], 7f, BodyChunkConnection.Type.Normal, 1f, -1f);
        bodyChunkConnections[2] = new(bodyChunks[1], bodyChunks[2], 3.5f, BodyChunkConnection.Type.Push, 1f, -1f);

        needleDir = Custom.RNV();
        lastNeedleDir = needleDir;

        airFriction = 0.99f;
        gravity = 0.9f;
        bounce = 0.1f;
        surfaceFriction = 0.47f;
        collisionLayer = 0;
        waterFriction = 0.92f;
        buoyancy = 0.95f;

        landWalkDir = (Random.value < 0.5f) ? -1 : 1;
        landWalkCycle = Random.value;
        GoThroughFloors = true;

        ChangeCollisionLayer(0);
    }

    public override Color ShortCutColor()
    {
        return Color.HSVToRGB(.26f, .22f, .92f);
    }

    public override void InitiateGraphicsModule()
    {
        graphicsModule ??= new InchwormGraphics(this);
        graphicsModule.Reset();
    }

    public override void Update(bool eu)
    {
        WeightedPush(1, 2, Custom.DirVec(bodyChunks[2].pos, bodyChunks[1].pos), Custom.LerpMap(Vector2.Distance(bodyChunks[2].pos, bodyChunks[1].pos), 3.5f, 8f, 1f, 0f));
        if (!room.GetTile(mainBodyChunk.pos).Solid)
        {
            lastAirTile = room.GetTilePosition(mainBodyChunk.pos);
        }
        else
        {
            for (int i = 0; i < bodyChunks.Length; i++)
            {
                bodyChunks[i].HardSetPosition(room.MiddleOfTile(lastAirTile) + Custom.RNV());
            }
        }

        base.Update(eu);

        if (room == null)
        {
            return;
        }

        lastNeedleDir = needleDir;

        if (grasps[0] == null && mode == Mode.StuckInChunk)
        {
            ChangeMode(Mode.Free);
        }

        switch (mode)
        {
            case Mode.Free:
                needleDir = travelDir;
                needleDir.Normalize();
                break;
            case Mode.StuckInChunk:
                BodyChunk stuckInChunk = grasps[0].grabbedChunk;

                needleDir = Custom.RotateAroundOrigo(stuckDir, Custom.VecToDeg(stuckInChunk.Rotation));
                firstChunk.pos = StuckInChunkPos(stuckInChunk) + Custom.RotateAroundOrigo(stuckPos, Custom.VecToDeg(stuckInChunk.Rotation));
                firstChunk.vel *= 0f;

                if (stuckCounter > 0)
                {
                    stuckCounter -= Consious ? 1 : 3;
                }
                else
                {
                    ChangeMode(Mode.Free);
                    break;
                }
                break;
        }

        if (Consious)
        {
            Act();
        }
        else
        {
            GoThroughFloors = grabbedBy.Any();
        }
    }

    void Act()
    {
        AI.Update();

        Vector2 followingPos = bodyChunks[0].pos;

        var pather = AI.pathFinder as StandardPather;
        var movementConnection = pather!.FollowPath(room.GetWorldCoordinate(followingPos), true) ?? pather.FollowPath(room.GetWorldCoordinate(followingPos), true);
        if (movementConnection != null)
        {
            Run(movementConnection);
        }
        else
        {
            if (lastFollowedConnection != null)
            {
                MoveTowards(room.MiddleOfTile(lastFollowedConnection.DestTile));
            }
            if (Submersion > .5)
            {
                firstChunk.vel += new Vector2((Random.value - .5f) * .5f, Random.value * .5f);
                if (Random.value < .1)
                {
                    bodyChunks[0].vel += new Vector2((Random.value - .5f) * 2f, Random.value * 1.5f);
                }
            }
        }
    }

    void MoveTowards(Vector2 moveTo)
    {
        Vector2 dir = Custom.DirVec(firstChunk.pos, moveTo);
        travelDir = dir;
        bodyChunks[0].vel.y = bodyChunks[0].vel.y + Mathf.Lerp(gravity, gravity - buoyancy, bodyChunks[0].submersion);
        firstChunk.pos += dir;
        firstChunk.vel += dir;
        firstChunk.vel *= .85f;
    }

    void Run(MovementConnection followingConnection)
    {
        if (followingConnection.type is MovementConnection.MovementType.ShortCut or MovementConnection.MovementType.NPCTransportation)
        {
            enteringShortCut = new IntVector2?(followingConnection.StartTile);
            if (followingConnection.type == MovementConnection.MovementType.NPCTransportation)
            {
                NPCTransportationDestination = followingConnection.destinationCoord;
            }
        }
        else
        {
            MoveTowards(room.MiddleOfTile(followingConnection.DestTile));
        }
        lastFollowedConnection = followingConnection;
    }

    Vector2 StuckInChunkPos(BodyChunk chunk)
    {
        return chunk.owner?.graphicsModule is PlayerGraphics g ? g.drawPositions[chunk.index, 0] : chunk.pos;
    }

    public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        base.Collide(otherObject, myChunk, otherChunk);

        if (Consious && grasps[0] == null && otherObject is Creature c && c.State.alive && AI.preyTracker.MostAttractivePrey?.representedCreature == c.abstractCreature)
        {
            StickIntoChunk(otherObject, otherChunk);
        }
    }

    void StickIntoChunk(PhysicalObject otherObject, int otherChunk)
    {
        stuckCounter = otherObject switch
        {
            Creature { dead: false } => Random.Range(75, 150),
            Creature => Random.Range(50, 100),
            _ => Random.Range(25, 50),
        };

        BodyChunk chunk = otherObject.bodyChunks[otherChunk];

        firstChunk.pos = chunk.pos + Custom.DirVec(chunk.pos, firstChunk.pos) * chunk.rad + Custom.DirVec(chunk.pos, firstChunk.pos) * 11f;
        stuckPos = Custom.RotateAroundOrigo(firstChunk.pos - StuckInChunkPos(chunk), -Custom.VecToDeg(chunk.Rotation));
        stuckDir = Custom.RotateAroundOrigo(Custom.DirVec(firstChunk.pos, Custom.DirVec(firstChunk.pos, chunk.pos)), -Custom.VecToDeg(chunk.Rotation));

        Grab(otherObject, 0, otherChunk, Grasp.Shareability.CanOnlyShareWithNonExclusive, .5f, false, false);

        if (grasps[0]?.grabbed is Creature grabbed)
        {
            grabbed.Violence(firstChunk, Custom.DirVec(firstChunk.pos, chunk.pos) * 3f, chunk, null, DamageType.Stab, 0.06f, 7f);
        }
        else
        {
            chunk.vel += Custom.DirVec(firstChunk.pos, chunk.pos) * 3f / chunk.mass;
        }

        new DartMaggot.DartMaggotStick(abstractPhysicalObject, chunk.owner.abstractPhysicalObject);

        ChangeMode(Mode.StuckInChunk);
    }

    void ChangeMode(Mode newMode)
    {
        if (mode != newMode)
        {
            mode = newMode;
            CollideWithTerrain = mode == Mode.Free;

            if (mode == Mode.Free)
            {
                abstractPhysicalObject.LoseAllStuckObjects();
                LoseAllGrasps();
                Stun(20);
                room.PlaySound(SoundID.Spear_Dislodged_From_Creature, firstChunk, false, 0.8f, 1.2f);
            }
            else
            {
                room.PlaySound(SoundID.Dart_Maggot_Stick_In_Creature, firstChunk, false, 0.8f, 1.2f);
            }
        }
    }

    public override void Violence(BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, Appendage.Pos hitAppendage, DamageType type, float damage, float stunBonus)
    {
        base.Violence(source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);

        if (source?.owner is Weapon && directionAndMomentum.HasValue)
        {
            hitChunk.vel = source.vel * source.mass / hitChunk.mass;
        }
    }
}

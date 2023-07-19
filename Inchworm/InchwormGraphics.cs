using RWCustom;
using UnityEngine;

namespace Inchworms;

sealed class InchwormGraphics : GraphicsModule
{
    const int meshSegs = 9;

    readonly Inchworm inch;
    readonly float sizeFac;

    readonly TriangleMesh[] m = new TriangleMesh[2]; // mesh sprites 0 and 1

    private float Radius(float bodyPos)
    {
        return 1.5f + Mathf.Sin(bodyPos * 3.1415927f) * 1.5f;
    }

    private GenericBodyPart[] body;
    private float[] radiuses;
    private float sinCounter;
    private Color blackColor;
    private int vibrate;

    public InchwormGraphics(Inchworm inchworm) : base(inchworm, false)
    {
        inch = inchworm;

        var state = Random.state;
        Random.InitState(inchworm.abstractCreature.ID.RandomSeed);
        sizeFac = Custom.ClampedRandomVariation(0.8f, 0.2f, 0.5f);
        Random.state = state;

        body = new GenericBodyPart[11];
        bodyParts = new BodyPart[11];
        for (int i = 0; i < body.Length; i++)
        {
            body[i] = new GenericBodyPart(this, 1f, 0.7f, 1f, inch.mainBodyChunk);
            bodyParts[i] = body[i];
        }
        radiuses = new float[body.Length];
        for (int j = 0; j < body.Length; j++)
        {
            radiuses[j] = Radius(j / body.Length);
        }
        sinCounter = Random.value;
    }

    public override void Reset()
    {
        for (int i = 0; i < body.Length; i++)
        {
            body[i].pos = inch.mainBodyChunk.pos + Custom.DegToVec(Random.value * 360f);
            body[i].vel = inch.mainBodyChunk.vel;
        }
        base.Reset();
    }

    public override void Update()
    {
        base.Update();
        if (culled)
        {
            return;
        }
        float num = 4f;
        if (!inch.dead && !inch.ChargingAttack && !inch.Attacking)
        {
            sinCounter -= 0.06666667f;
        }
        if (vibrate > 0)
        {
            vibrate--;
        }
        Vector2 a = Custom.PerpendicularVector(inch.swimDirection);
        float d = Mathf.Lerp(0.99f, 0.8f, inch.mainBodyChunk.submersion);
        float num2 = 0.9f * (1f - inch.mainBodyChunk.submersion);
        for (int i = body.Length - 1; i >= 0; i--)
        {
            body[i].Update();
        }
        for (int j = body.Length - 1; j >= 0; j--)
        {
            body[j].vel *= d;
            GenericBodyPart genericBodyPart = body[j];
            genericBodyPart.vel.y -= num2;
            if (inch.mainBodyChunk.submersion == 0f && inch.mainBodyChunk.ContactPoint.y == -1)
            {
                GenericBodyPart genericBodyPart2 = body[j];
                genericBodyPart2.vel.x -= (float)inch.landWalkDir * inch.landWalkCycle;
                if (j == 2 || j == 1)
                {
                    GenericBodyPart genericBodyPart3 = body[j];
                    genericBodyPart3.vel.y += num2 * (1f + Mathf.Sin(inch.landWalkCycle * 3.1415927f * 2f));
                }
            }
            if (!Custom.DistLess(body[j].pos, inch.mainBodyChunk.pos, 7f * (j + 1)))
            {
                body[j].pos = inch.mainBodyChunk.pos + Custom.DirVec(inch.mainBodyChunk.pos, body[j].pos) * 7f * (float)(j + 1);
            }
            Vector2 pos = inch.mainBodyChunk.pos;
            if (j > 0)
            {
                pos = body[j - 1].pos;
            }
            body[j].vel += Custom.DirVec(pos, body[j].pos) * 0.1f - inch.swimDirection * 0.005f;
            if (!inch.dead && (inch.grasps[0] != null) && Random.value < 0.1f)
            {
                body[j].vel += Custom.DegToVec(Random.value * 360f) * Random.value * Random.value;
            }
            if (j == 0)
            {
                body[j].pos = inch.mainBodyChunk.pos + inch.swimDirection * 4f;
                if (inch.Consious)
                {
                    if (inch.grasps[0] == null && inch.mainBodyChunk.submersion > 0.5f)
                    {
                        body[j].pos += a * Mathf.Sin(sinCounter * 3.1415927f * 2f) * 2f;
                    }
                    else if (inch.mainBodyChunk.submersion == 0f && inch.mainBodyChunk.ContactPoint.y == -1)
                    {
                        body[j].pos += new Vector2(1.5f * Mathf.Sin(inch.landWalkCycle * 3.1415927f * 2f) * -(float)inch.landWalkDir, -3f);
                    }
                }
                body[j].vel = inch.mainBodyChunk.vel;
            }
            else
            {
                float num3 = Vector2.Distance(body[j].pos, body[j - 1].pos);
                Vector2 a2 = Custom.DirVec(body[j].pos, body[j - 1].pos);
                body[j].vel -= (num - num3) * a2 * 0.5f;
                body[j].pos -= (num - num3) * a2 * 0.5f;
                body[j - 1].vel += (num - num3) * a2 * 0.5f;
                body[j - 1].pos += (num - num3) * a2 * 0.5f;
                radiuses[j] = Radius(j / body.Length);
                if (!inch.Attacking)
                {
                    if (inch.mainBodyChunk.submersion == 1f && inch.grasps[0] == null)
                    {
                        body[j].vel += a * Mathf.Sin((sinCounter + j / 5f) * 3.1415927f * 2f) * 1.2f;
                    }
                    radiuses[j] *= 1f + Mathf.Sin((sinCounter + j / 4f) * 3.1415927f * 2f) * 0.2f;
                }
                if (num3 > num)
                {
                    radiuses[j] /= 1f + Mathf.Pow((num3 - num) * 0.06f, 1.5f);
                }
            }
        }
        for (int k = 1; k < body.Length; k++)
        {
            float num4 = Vector2.Distance(body[k].pos, body[k - 1].pos);
            Vector2 a3 = Custom.DirVec(body[k].pos, body[k - 1].pos);
            body[k].vel -= (num - num4) * a3 * 0.5f;
            body[k].pos -= (num - num4) * a3 * 0.5f;
            body[k - 1].vel += (num - num4) * a3 * 0.5f;
            body[k - 1].pos += (num - num4) * a3 * 0.5f;
        }
    }

    public void Vibrate()
    {
        if (inch.Consious)
        {
            vibrate = 5;
        }
        for (int i = 0; i < body.Length; i++)
        {
            body[i].pos += Custom.DegToVec(Random.value * 360f) * (inch.dead ? 3f : 6f);
            body[i].vel = Custom.DegToVec(Random.value * 360f) * (inch.dead ? 3f : 6f);
        }
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        TriangleMesh.Triangle[] array = new TriangleMesh.Triangle[(body.Length - 1) * 4 + 1];
        for (int i = 0; i < body.Length - 1; i++)
        {
            int num = i * 4;
            for (int j = 0; j < 4; j++)
            {
                array[num + j] = new TriangleMesh.Triangle(num + j, num + j + 1, num + j + 2);
            }
        }
        array[(body.Length - 1) * 4] = new TriangleMesh.Triangle((body.Length - 1) * 4, (body.Length - 1) * 4 + 1, (body.Length - 1) * 4 + 2);
        sLeaser.sprites[0] = new TriangleMesh("Futile_White", array, false, false);
        AddToContainer(sLeaser, rCam, null);

        base.InitiateSprites(sLeaser, rCam);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        if (culled)
        {
            return;
        }
        Vector2 vector = Vector2.Lerp(body[0].lastPos, body[0].pos, timeStacker) + inch.swimDirection * 2f;
        float num = radiuses[0];
        if (inch.grasps[0] != null)
        {
            vector = inch.grasps[0].grabbedChunk.pos + Custom.DirVec(inch.grasps[0].grabbedChunk.pos, inch.mainBodyChunk.pos) * inch.grasps[0].grabbedChunk.rad * 0.6f;
            num = 0f;
        }

        sLeaser.sprites[0].color = Color.Lerp(Color.HSVToRGB(.26f, .22f, .92f), blackColor, 0.85f);

        for (int i = 0; i < body.Length; i++)
        {
            Vector2 vector2 = Vector2.Lerp(body[i].lastPos, body[i].pos, timeStacker);
            if (vibrate > 0)
            {
                vector2 += Custom.DegToVec(Random.value * 360f) * Random.value * 5f;
            }
            Vector2 normalized = (vector2 - vector).normalized;
            Vector2 a = Custom.PerpendicularVector(normalized);
            float d = Vector2.Distance(vector2, vector) / 5f;
            if (sLeaser.sprites[0] is TriangleMesh triangleMesh)
            {
                triangleMesh.MoveVertice(i * 4, vector - a * (num + radiuses[i]) * 0.5f + normalized * d - camPos);
                triangleMesh.MoveVertice(i * 4 + 1, vector + a * (num + radiuses[i]) * 0.5f + normalized * d - camPos);
                if (i < body.Length - 1)
                {
                    triangleMesh.MoveVertice(i * 4 + 2, vector2 - a * radiuses[i] - normalized * d - camPos);
                    triangleMesh.MoveVertice(i * 4 + 3, vector2 + a * radiuses[i] - normalized * d - camPos);
                }
                else
                {
                    triangleMesh.MoveVertice(i * 4 + 2, vector2 - camPos);
                }
            }
            num = radiuses[i];
            vector = vector2;
        }
    }

    public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        blackColor = palette.blackColor;
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContainer)
    {
        newContainer ??= rCam.ReturnFContainer("Midground");

        for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            newContainer.AddChild(sLeaser.sprites[i]);
        }
    }
}
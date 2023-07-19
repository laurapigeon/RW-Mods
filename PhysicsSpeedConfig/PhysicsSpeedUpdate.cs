using UnityEngine;
using BepInEx;
using System.Security;
using System.Security.Permissions;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace PhysicsSpeedConfig;

[BepInPlugin(MOD_ID, "Physics Speed Config", "1.1")]
partial class PhysicsSpeedConfig : BaseUnityPlugin
{
    public const string MOD_ID = "physicsspeed";

    public static Configurable<float> slowDownMult = new(25f);
    public static Configurable<float> speedUpMult = new(300f);
    public static Configurable<KeyCode> slowDownKey = new(KeyCode.A);
    public static Configurable<KeyCode> speedUpKey = new(KeyCode.S);
    public static Configurable<KeyCode> killKey = new(KeyCode.D);
    public static Configurable<bool> doToggle = new(true);
    public bool slowKeyPressed = false;
    public bool speedKeyPressed = false;
    public bool isSlowed = false;
    public bool isSpeeded = false;

    private bool _initialized;

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            MachineConnector.SetRegisteredOI(MOD_ID, new Config());

            On.MainLoopProcess.RawUpdate += MainLoopProcess_RawUpdate;
        };
    }

    private void MainLoopProcess_RawUpdate(On.MainLoopProcess.orig_RawUpdate orig, MainLoopProcess self, float dt)
    {
        float accommodation = 1f;

        if (self.ID == ProcessManager.ProcessID.Game)
        {
            if (doToggle.Value)
            {
                if (Input.GetKey(slowDownKey.Value))
                {
                    if (!slowKeyPressed)
                    {
                        isSlowed = !isSlowed;
                        slowKeyPressed = true;
                    }
                }
                else if (slowKeyPressed) slowKeyPressed = false;

                if (Input.GetKey(speedUpKey.Value))
                {
                    if (!speedKeyPressed)
                    {
                        isSpeeded = !isSpeeded;
                        speedKeyPressed = true;
                    }
                }
                else if (speedKeyPressed) speedKeyPressed = false;
            }
            else
            {
                if (Input.GetKey(slowDownKey.Value)) isSlowed = true;
                else isSlowed = false;

                if (Input.GetKey(speedUpKey.Value)) isSpeeded = true;
                else isSpeeded = false;
            }

            if (isSlowed)
            {
                self.framesPerSecond = Mathf.RoundToInt(self.framesPerSecond * slowDownMult.Value / 100f);
            }
            if (isSpeeded)
            {
                self.framesPerSecond = Mathf.RoundToInt(self.framesPerSecond * speedUpMult.Value / 100f);
            }

            if (self.framesPerSecond > 60f)
            {
                accommodation = self.framesPerSecond / 60f;
            }
        }
        else
        {
            self.framesPerSecond = 40;
        }

        if (Input.GetKey(killKey.Value))
        {
            self.framesPerSecond = 40;
            accommodation = self.framesPerSecond / 60f;
            isSlowed = false;
            isSpeeded = false;
        }

        orig(self, dt * accommodation);
    }
}
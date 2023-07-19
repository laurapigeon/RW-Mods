using System;
using UnityEngine;
using BepInEx;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using MSCRedux;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using RWCustom;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace RWMovementMarker;

[BepInPlugin(MOD_ID, "Movement Marker", "1.2")]
internal partial class RWMovementMarker : BaseUnityPlugin
{
    public const string MOD_ID = "movementmarker";

    public static StatGraphic?[] statGraphics = new StatGraphic[1];
    public static Configurable<bool> eyeColour = new(false);
    public static Configurable<bool> doKeyBinds = new(false);
    public static Configurable<bool> aIncStorage = new(true);
    public static Configurable<bool> bIncJumps = new(false);
    public static Configurable<bool> cIncMoves = new(false);
    public static Configurable<bool> dIncTunnel = new(false);
    public static Configurable<bool> eIncShroom = new(false);
    public static Configurable<bool> fIncMSC = new(false);
    public static Configurable<bool> gIncState = new(false);
    public static Configurable<bool> hIncSpeed = new(false);
    public static Configurable<bool> permastore = new(false);
    public static Configurable<bool> storeordie = new(false);
    public static Configurable<Color> backColor = new(new Color(0.173f, 0.192f, 0.235f));
    public static Configurable<Color> onColor = new(new Color(0.561f, 0.6f, 0.678f));
    public static Configurable<Color> offColor = new(new Color(0.063f, 0.059f, 0.114f));
    public static Configurable<float> alpha = new(0.75f);
    public static Configurable<float> scale = new(0.50f);
    public static float _scale => scale.Value * 2f;

    public static Vector2 _origin = new(64f, 256f);

    private bool _initialized;

    private static readonly List<BaseUnityPlugin> _referencedPlugins = new();

    private static AttachedField<PlayerGraphics, Color> _eyeColor = new();

    delegate bool AttachedFieldTryGet(Player p, out float f);

    public void Awake()
    {
        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            if (_initialized) return;
            _initialized = true;

            MachineConnector.SetRegisteredOI(MOD_ID, new RWMMOptions());

            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
            On.Player.Update += Player_Update;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.ctor += Player_ctor;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.RoomCamera.ClearAllSprites += RoomCamera_ClearAllSprites;
            On.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate;
            On.RoomCamera.ctor += RoomCamera_ctor;
        };
    }

    private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig(self);
        string[] neededModIDs = { "redux" };
        if (ModManager.ActiveMods.Any(mod => neededModIDs.Contains(mod.id)))
        {
            Dictionary<string, PluginInfo> pluginDict = Chainloader.PluginInfos;
            foreach (PluginInfo pluginInfo in pluginDict.Values)
            {
                if (pluginInfo.Metadata.GUID is string thisID && neededModIDs.Contains(thisID))
                {
                    _referencedPlugins.Add(pluginInfo.Instance);
                }
            }
        }
    }

    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (doKeyBinds.Value)
        {
            if (Input.GetKeyDown(KeyCode.F6) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                self.initSlideCounter = 0;
                self.slideCounter = 0;
                self.jumpBoost = 0;
                self.ledgeGrabCounter = 0;
                self.stopRollingCounter = 0;
                self.exitBellySlideCounter = 0;
            }
            if (Input.GetKeyDown(KeyCode.F7) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                self.slideCounter = 1;
                self.slideDirection = 1;
            }
            if (Input.GetKeyDown(KeyCode.F8) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                self.slideCounter = 1;
                self.slideDirection = -1;
            }
            if (Input.GetKeyDown(KeyCode.F9) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                self.jumpBoost = 8;
            }
            if (Input.GetKeyDown(KeyCode.F10) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                self.ledgeGrabCounter = 20;
            }
        }
        orig(self, eu);
    }

    private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        if (permastore.Value)
        {
            if (self.input[0].x == -1)
            {
                self.slideCounter = 1;
                self.slideDirection = 1;
            }
            else if (self.input[0].x == 1)
            {
                self.slideCounter = 1;
                self.slideDirection = -1;
            }
        }
        if (!(self.slideCounter > 0 && self.slideCounter < 10))
        {
            if (storeordie.Value)
            {
                self.Die();
            }
        }

        orig(self, eu);
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        if (permastore.Value || storeordie.Value)
            self.slideCounter = 1;
        self.slideDirection = -1;
    }

    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (!_eyeColor.TryGet(self, out Color eyeColor)) _eyeColor.Set(self, sLeaser.sprites[9].color);

        float redColour = 0;
        float greenColour = 0;
        float blueColour = 0;
        if (self.player.initSlideCounter > 10) redColour = 1f;
        else if (self.player.initSlideCounter > 0) redColour = 0.5f;
        if (self.player.slideCounter >= 10) greenColour = 0.5f;
        else if (self.player.slideCounter > 0) greenColour = 1f;
        if (self.player.jumpBoost >= 8) blueColour = 1f;
        else if (self.player.jumpBoost >= 6) blueColour = 0.5f;

        if (eyeColour.Value && !(redColour == 0f && greenColour == 0f && blueColour == 0f))
        {
            sLeaser.sprites[9].color = new Color(redColour, greenColour, blueColour);
        }
        else
        {
            sLeaser.sprites[9].color = eyeColor;
        }
    }

    private void RoomCamera_ClearAllSprites(On.RoomCamera.orig_ClearAllSprites orig, RoomCamera self)
    {
        if (statGraphics[self.cameraNumber]?.cam == self)
        {
            statGraphics[self.cameraNumber]?.Remove();
            statGraphics[self.cameraNumber] = null;
        }
        orig(self);
    }

    private void RainWorldGame_GrafUpdate(On.RainWorldGame.orig_GrafUpdate orig, RainWorldGame self, float timeStacker)
    {
        orig(self, timeStacker);
        foreach (StatGraphic? display in statGraphics)
            display?.Update(self);
    }

    private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
    {
        orig(self, game, cameraNumber);
        if (statGraphics.Length <= cameraNumber) Array.Resize(ref statGraphics, cameraNumber + 1);
        statGraphics[self.cameraNumber]?.Remove();
        StatGraphic sg = new(self);
        statGraphics[cameraNumber] = sg;
        sg.Move();
    }

    public class StatGraphic
    {
        public RoomCamera cam;
        public List<StatButton> buttons;

        public bool IsMouseOver
        {
            get
            {
                foreach (StatButton button in buttons) if (button.IsMouseOver) return true;
                return false;
            }
        }

        private bool _dragging;
        private Vector2 _dragOffset;

        private readonly Camera _rtCam;
        private Rect _rtBounds;
        private RenderTexture? _rt;

        public FSprite? displaySprite;
        public FContainer offscreenContainer;

        public StatGraphic(RoomCamera cam)
        {
            buttons = null!;

            this.cam = cam;

            GameObject go = new("Movement Marker Camera");
            _rtCam = go.AddComponent<Camera>();
            _rtCam.depth = -100;
            _rtCam.orthographic = true;
            _rtCam.farClipPlane = 20f;
            _rtCam.nearClipPlane = 0.1f;
            _rtCam.clearFlags = CameraClearFlags.SolidColor;

            offscreenContainer = new FContainer();
            Futile.stage.AddChild(offscreenContainer);

            
            InitSprites();
        }

        public void InitSprites()
        {
            float spacing = StatButton.Size + Mathf.Floor(StatButton.Size / 6f);
            buttons = new List<StatButton>();
            float maxheight = 0f;
            float offset = 0f;
            // Storage
            if (aIncStorage.Value)
            {
                buttons.Add(new StatButton(this, new Vector2(offset, 0f) * spacing, "Jump", "jumpBoost"));
                buttons.Add(new StatButton(this, new Vector2(offset, 1f) * spacing, "Ledge", "ledgeGrabCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 2f) * spacing, "Run", "initSlideCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 3f) * spacing, "Turn", "slideCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 4f) * spacing, "TDrct", "slideDirection"));
                maxheight = Mathf.Max(maxheight, 5f);
                offset += 1f;
            }

            // Jump buffer and coyote
            if (bIncJumps.Value)
            {
                buttons.Add(new StatButton(this, new Vector2(offset, 0f) * spacing, "CanJmp", "canJump"));
                buttons.Add(new StatButton(this, new Vector2(offset, 1f) * spacing, "CanWll", "canWallJump"));
                buttons.Add(new StatButton(this, new Vector2(offset, 2f) * spacing, "WntJmp", "wantToJump"));
                maxheight = Mathf.Max(maxheight, 3f);
                offset += 1f;
            }

            // Move details
            if (cIncMoves.Value)
            {
                buttons.Add(new StatButton(this, new Vector2(offset, 0f) * spacing, "MvCntr", "rollCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 1f) * spacing, "EndRll", "stopRollingCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 2f) * spacing, "EndSld", "exitBellySlideCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 3f) * spacing, "DDiag", "consistentDownDiagonal"));
                buttons.Add(new StatButton(this, new Vector2(offset, 4f) * spacing, "CTDly", "crawlTurnDelay"));
                buttons.Add(new StatButton(this, new Vector2(offset, 5f) * spacing, "CanRll", "allowRoll"));
                buttons.Add(new StatButton(this, new Vector2(offset, 6f) * spacing, "ChPnce", "superLaunchJump"));
                buttons.Add(new StatButton(this, new Vector2(offset, 7f) * spacing, "SlwMvt", "slowMovementStun"));
                maxheight = Mathf.Max(maxheight, 8f);
                offset += 1f;
            }

            // Pole and tunnel details
            if (dIncTunnel.Value)
            {
                buttons.Add(new StatButton(this, new Vector2(offset, 0f) * spacing, "CanCrn", "canCorridorJump"));
                buttons.Add(new StatButton(this, new Vector2(offset, 1f) * spacing, "VBst", "verticalCorridorSlideCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 2f) * spacing, "HBst", "horizontalCorridorSlideCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 3f) * spacing, "ShUp", "shootUpCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 4f) * spacing, "PlSld", "slideUpPole"));
                maxheight = Mathf.Max(maxheight, 5f);
                offset += 1f;
            }

            // Mushroom details
            if (eIncShroom.Value)
            {
                buttons.Add(new StatButton(this, new Vector2(offset, 0f) * spacing, "ShrCnt", "mushroomEffect"));
                buttons.Add(new StatButton(this, new Vector2(offset, 1f) * spacing, "LBst", "directionBoosts", index: 0));
                buttons.Add(new StatButton(this, new Vector2(offset, 2f) * spacing, "RBst", "directionBoosts", index: 1));
                buttons.Add(new StatButton(this, new Vector2(offset, 3f) * spacing, "DBst", "directionBoosts", index: 2));
                buttons.Add(new StatButton(this, new Vector2(offset, 4f) * spacing, "UBst", "directionBoosts", index: 3));
                maxheight = Mathf.Max(maxheight, 5f);
                offset += 1f;
            }

            // MSC details
            if (fIncMSC.Value && ModManager.MSC)
            {
                buttons.Add(new StatButton(this, new Vector2(offset, 0f) * spacing, "Tired", "aerobicLevel"));
                buttons.Add(new StatButton(this, new Vector2(offset, 1f) * spacing, "SplJmpd", "pyroJumpped"));
                buttons.Add(new StatButton(this, new Vector2(offset, 2f) * spacing, "SplCnt", "pyroJumpCounter"));
                buttons.Add(new StatButton(this, new Vector2(offset, 3f) * spacing, "SplCld", "pyroJumpCooldown"));
                buttons.Add(new StatButton(this, new Vector2(offset, 4f) * spacing, "PryCld", "pyroParryCooldown"));
                if (ModManager.ActiveMods.Any(mod => mod.id == "redux"))
                {
                    buttons.Add(new StatButton(this, new Vector2(offset, 5f) * spacing, "SplWnd", "_pyroJumpWindups", modID: "redux"));
                    buttons.Add(new StatButton(this, new Vector2(offset, 6f) * spacing, "RivWet", "_soppingSoggyWetness", modID: "redux"));
                    maxheight = Mathf.Max(maxheight, 7f);
                }
                else
                {
                    maxheight = Mathf.Max(maxheight, 5f);
                }
                offset += 1f;
            }

            // Slugcat state details
            if (gIncState.Value)
            {
                buttons.Add(new StatButton(this, new Vector2(offset, 0f) * spacing, "BodyMode", "bodyMode", width: 2f));
                buttons.Add(new StatButton(this, new Vector2(offset, 1f) * spacing, "Animation", "animation", width: 2f));
                maxheight = Mathf.Max(maxheight, 2f);
                offset += 2f;
            }

            // Slugcat speed details
            if (hIncSpeed.Value)
            {
                buttons.Add(new StatButton(this, new Vector2(offset, 0f) * spacing, "Speed", "speed"));
                buttons.Add(new StatButton(this, new Vector2(offset, 1f) * spacing, "XSpeed", "speedX"));
                buttons.Add(new StatButton(this, new Vector2(offset, 2f) * spacing, "YSpeed", "speedY"));
                maxheight = Mathf.Max(maxheight, 3f);
                offset += 1f;
            }
            buttons.ToArray();

            _rtBounds = new Rect(-10f, -10f, spacing * offset - (gIncState.Value ? 12f : 8f) + 20f, spacing * maxheight - 8f + 20f);

            Move();
        }

        public void Remove()
        {
            Futile.atlasManager.UnloadAtlas("MovementMarker_" + cam.cameraNumber);
            offscreenContainer.RemoveFromContainer();
            displaySprite?.RemoveFromContainer();
            Destroy(_rtCam.gameObject);
        }

        public void Update(RainWorldGame rainWorldGame)
        {
            if (displaySprite?.container != cam.ReturnFContainer("HUD2"))
                cam.ReturnFContainer("HUD2").AddChild(displaySprite);

            // Move the stats display when right bracket is pressed
            if (Input.GetKey(KeyCode.RightBracket))
            {
                _origin = Input.mousePosition;
                Move();
            }
            
            // Allow dragging the stats display
            if (_dragging)
            {
                if (!Input.GetMouseButton(0))
                    _dragging = false;
                else
                {
                    _origin = (Vector2)Input.mousePosition + _dragOffset;
                    Move();
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0) && IsMouseOver)
                {
                    _dragging = true;
                    _dragOffset = _origin - (Vector2)Input.mousePosition;
                }
            }

            // Update each of the markers
            foreach (StatButton button in buttons)
                button.Update(rainWorldGame);

            displaySprite?.MoveToFront();
            offscreenContainer.MoveToFront();
        }

        private Vector2 DrawOrigin => new(-75000f, -75000f - cam.cameraNumber * 1000f);

        public void Move()
        {
            // Update RT and camera
            int rtW = Mathf.RoundToInt(_rtBounds.width);
            int rtH = Mathf.RoundToInt(_rtBounds.height);
            if (_rt == null)
            {
                _rt = new(rtW, rtH, 16) { filterMode = FilterMode.Point };

                if (displaySprite != null)
                {
                    Futile.atlasManager.UnloadAtlas("MovementMarker_" + cam.cameraNumber);
                    displaySprite?.RemoveFromContainer();
                }

                FAtlasElement element = Futile.atlasManager.LoadAtlasFromTexture("MovementMarker_" + cam.cameraNumber, _rt, false).elements[0];
                displaySprite = new FSprite(element) { anchorX = 0f, anchorY = 0f, alpha = alpha.Value };
                _rtCam.targetTexture = _rt;
            }
            
            if (_rt.width != rtW || _rt.height != rtH)
            {
                _rt.width = rtW;
                _rt.height = rtH;
            }

            // Update display sprite
            displaySprite?.SetPosition(_origin + _rtBounds.min - Vector2.one * 0.5f);

            // Update components
            Vector2 drawOrigin = DrawOrigin;
            foreach (StatButton button in buttons)
                button.Move(drawOrigin);

            _rtCam.transform.position = (Vector3)(drawOrigin + _rtBounds.center) + Vector3.forward * -10f;
            _rtCam.orthographicSize = _rtBounds.height / 2f;
        }
    }

    public class StatButton
    {
        public static float Size => Mathf.Floor(24f * _scale) * 2f;

        public StatGraphic parent;
        public Vector2 relPos;
        private readonly string _keyName;
        private readonly string _markerName;

        private readonly float _width;
        private readonly int? _index;
        private readonly string? _modID;
        private readonly FSprite _back;
        private readonly FSprite _front;
        private readonly FLabel _key;
        AttachedFieldTryGet? targetTryGet = null;

        public StatButton(StatGraphic parent, Vector2 pos, string keyName, string markerName, float width = 1f, int? index = null, string? modID = null)
        {
            this.parent = parent;
            relPos = pos;
            _keyName = keyName;
            _markerName = markerName;
            _width = width;
            _index = index;
            _modID = modID;

            _back = new FSprite("pixel") { anchorX = 0f, anchorY = 0f, scaleX = _width * Size, scaleY = Size, color = backColor.Value };
            _front = new FSprite("pixel") { anchorX = 0f, anchorY = 0f, scaleX = _width * Size - 2f, scaleY = Size - 2f };
            _key = new(Custom.GetFont(), _keyName);
            Move(Vector2.zero);
            AddToContainer();
            if (_scale < 0.75f)
            {
                _key.text = keyName.Substring(0, 1);
            }
        }

        public bool IsMouseOver
        {
            get
            {
                Vector2 mp = Input.mousePosition;
                mp.x -= _origin.x + relPos.x;
                mp.y -= _origin.y + relPos.y;
                if (mp.x < 0f || mp.y < 0f) return false;
                if (mp.x > _width * Size || mp.y > Size) return false;
                return true;
            }
        }

        public void AddToContainer()
        {
            FContainer c = parent.offscreenContainer;
            c.AddChild(_back);
            c.AddChild(_front);
            if (_key != null) c.AddChild(_key);
        }

        public void RemoveFromContainer()
        {
            _back.RemoveFromContainer();
            _front.RemoveFromContainer();
            _key?.RemoveFromContainer();
        }

        public void Move(Vector2 origin)
        {
            Vector2 pos = origin + relPos + Vector2.one * 0.01f;
            _back.SetPosition(pos);
            _front.x = pos.x + 1f;
            _front.y = pos.y + 1f;
            if (_key != null)
            {
                _key.x = pos.x + _width * Size / 2f;
                _key.y = pos.y + Size / 2f;
            }
        }
        
        public void Update(RainWorldGame rainWorldGame)
        {
            bool isDown = false;

            if (rainWorldGame.Players.Count > 0)
            {
                FieldInfo? fieldInfo = null;
                object? value = null;

                if (rainWorldGame.Players[0].realizedObject is not Player player) return;

                if (_modID is null)
                {
                    fieldInfo = typeof(Player).GetField(_markerName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (fieldInfo == null)
                    {
                        fieldInfo = typeof(PhysicalObject).GetField(_markerName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    }
                    List<string> speedNames = new() { "speed", "speedX", "speedY" };
                    if (speedNames.Contains(_markerName))
                    {
                        Vector2 speed = player.bodyChunks[0].vel + player.bodyChunks[1].vel / 2;
                        float preciseValue = new List<float> { speed.magnitude, speed.x, speed.y }[speedNames.IndexOf(_markerName)];
                        value = Math.Round((decimal)preciseValue, 3);
                    }
                    else if (fieldInfo != null)
                    {
                        value = fieldInfo.GetValue(player);
                    }
                    else
                    {
                        PropertyInfo propertyInfo = typeof(Player).GetProperty(_markerName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        if (propertyInfo != null)
                        {
                            value = propertyInfo.GetValue(player, null);
                        }
                    }
                }
                else
                {
                    if (ModManager.ActiveMods.Any(mod => mod.id == _modID))
                    {
                        foreach(BaseUnityPlugin plugin in _referencedPlugins)
                        {
                            if (targetTryGet is null)
                            {
                                BaseUnityPlugin mod = null!;
                                mod = plugin;
                                const BindingFlags ALL_CONTEXTS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                                FieldInfo fieldInModClass = mod.GetType().GetField(_markerName, ALL_CONTEXTS);
                                if (fieldInModClass is not null)
                                {
                                    value = fieldInModClass.GetValue(mod);
                                }
                                if (value is not null)
                                {
                                    MethodInfo attachedFieldTryGetMethod = value.GetType().GetMethod("TryGet", ALL_CONTEXTS);
                                    targetTryGet = (AttachedFieldTryGet)Delegate.CreateDelegate(typeof(AttachedFieldTryGet), value, attachedFieldTryGetMethod);
                                }
                            }
                            else
                            {
                                targetTryGet(player, out float fieldFloat);
                                value = fieldFloat;
                            }
                        }
                    }
                }

                if (_index.HasValue) // should fetch array to value
                {
                    if (value is float[] floatArray) value = floatArray[_index.Value];
                    else value = null;
                }
                else // something goes wrong
                {
                    if (value is object[]) value = null;
                    else if (value is AttachedField <object, object>) value = null;
                }

                if (value != null)
                {
                    string displayValue;

                    if (value is float single)
                    {
                        displayValue = Math.Round(single, 2).ToString();
                    }
                    else
                    {
                        displayValue = value.ToString();
                    }
                    if (_scale < 0.75f)
                    {
                        _key.text = _keyName.Substring(0, 1) + "\n" + displayValue;
                    }
                    else
                    {
                        _key.text = _keyName + "\n" + displayValue;
                    }
                    if ((_markerName == "jumpBoost" && (float)value > 0f) ||
                        (_markerName == "ledgeGrabCounter" && (int)value > 0) ||
                        (_markerName == "initSlideCounter" && (int)value > 10) ||
                        (_markerName == "slideCounter" && (int)value > 0 && (int)value < 10) ||
                        (_markerName == "slideDirection" && false) ||

                        (_markerName == "canJump" && (int)value > 0) ||
                        (_markerName == "canWallJump" && (int)value != 0) ||
                        (_markerName == "wantToJump" && (int)value > 0) ||

                        (_markerName == "stopRollingCounter" && (int)value > 0) ||
                        (_markerName == "exitBellySlideCounter" && (int)value > 0) ||
                        (_markerName == "consistentDownDiagonal" && (int)value > 6) ||
                        (_markerName == "crawlTurnDelay" && (int)value > 0 && (int)value < 6) ||
                        (_markerName == "allowRoll" && (int)value > 0) ||
                        (_markerName == "slowMovementStun" && (int)value > 0) ||
                        (_markerName == "superLaunchJump" && (int)value == 20) ||
                        (_markerName == "killSuperLaunchJumpCounter" && (int)value > 0) ||
                        (_markerName == "simulateHoldJumpButton" && (int)value > 0) ||

                        (_markerName == "canCorridorJump" && (int)value > 0) ||
                        (_markerName == "verticalCorridorSlideCounter" && (int)value > 0) ||
                        (_markerName == "horizontalCorridorSlideCounter" && (int)value > 0) ||
                        (_markerName == "shootUpCounter" && (int)value > 0) ||
                        (_markerName == "slideUpPole" && (int)value > 0) ||

                        (_markerName == "adrenaline" && (float)value > 0) ||
                        (_markerName == "directionBoosts" && (float)value == 1f) ||

                        (_markerName == "aerobicLevel" && player.gourmandExhausted || player.exhausted) ||
                        (_markerName == "pyroJumpCounter" && (int)value > 0) ||
                        (_markerName == "pyroJumpCooldown" && (float)value <= 0f) ||
                        (_markerName == "pyroParryCooldown" && (float)value <= 0f) ||

                        (value is bool boolean && boolean))
                    {
                        isDown = true;
                    }
                }
            }

            _front.color = isDown ? onColor.Value : offColor.Value;
            if (_key != null) _key.color = isDown ? offColor.Value : onColor.Value;
        }
    }
}
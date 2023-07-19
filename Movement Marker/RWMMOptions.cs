using UnityEngine;
using Menu.Remix.MixedUI;

namespace RWMovementMarker;

using static RWMovementMarker;
partial class RWMMOptions : OptionInterface
{
    private OpColorPicker? _backCol;
    private OpColorPicker? _onCol;
    private OpColorPicker? _offCol;

    public RWMMOptions()
    {

        eyeColour = config.Bind("change_eyecolour", false);
        doKeyBinds = config.Bind("do_keybinds", false);
        aIncStorage = config.Bind("include_storage", true);
        bIncJumps = config.Bind("include_jumps", false);
        cIncMoves = config.Bind("include_moves", false);
        dIncTunnel = config.Bind("include_tunnel", false);
        eIncShroom = config.Bind("include_shroom", false);
        fIncMSC = config.Bind("include_MSC", false);
        gIncState = config.Bind("include_state", false);
        hIncSpeed = config.Bind("include_speed", false);
        permastore = config.Bind("permastore", false);
        storeordie = config.Bind("storeordie", false);
        backColor = config.Bind("back_color", new Color(0.173f, 0.192f, 0.235f));
        onColor = config.Bind("on_color", new Color(0.561f, 0.6f, 0.678f));
        offColor = config.Bind("off_color", new Color(0.063f, 0.059f, 0.114f));
        alpha = config.Bind("alpha", 0.75f);
        scale = config.Bind("scale", 0.50f);
    }

    public override void Initialize()
    {
        base.Initialize();
        Tabs = new OpTab[] { new OpTab(this) };

        // Title
        Tabs[0].AddItems(new OpLabel(new Vector2(32f, 536f), new Vector2(256f, 32f), "Storage Marker Options", FLabelAlignment.Left, true));
        Tabs[0].AddItems(new OpLabel(20f, 570f, "MovementMarker mod by pinecubes", true));
        Tabs[0].AddItems(new OpLabel(20f, 500f, "laura#2871 for any questions or suggestions!", false));
        Tabs[0].AddItems(new OpLabel(200f, 530f, "Adds a configurable display of the important statistics about your slugcat, for movement and tech.", false));

        int horizNum;
        const float boolOffset = 200f * 0.8f;
        // Eye colour and storage keybinds
        horizNum = 0;
        string eyeDesc = "Change slugcats eye colour based on storage";
        Tabs[0].AddItems(new OpCheckBox(eyeColour, new Vector2(8f + boolOffset * horizNum, 504f)) { description = eyeDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 504f - 3f), new Vector2(boolOffset - 32f, 32f), "Eye Colour", FLabelAlignment.Left) { description = eyeDesc });
        horizNum++;
        string keybindDesc = "Use ctrl+shift+fn keys to clear or set storage (6 to destore everything, 7/8 to store a turn, 9 to store jump, 10 to store ledgeclimb)";
        Tabs[0].AddItems(new OpCheckBox(doKeyBinds, new Vector2(8f + boolOffset * horizNum, 504f)) { description = keybindDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 504f - 3f), new Vector2(boolOffset - 32f, 32f), "Keybinds", FLabelAlignment.Left) { description = keybindDesc });
            
        // What to include
        horizNum = 0;
        string storageDesc = "Shows jump and ledge storage, along with turn and run storage and their current direction";
        Tabs[0].AddItems(new OpCheckBox(aIncStorage, new Vector2(8f + boolOffset * horizNum, 474f)) { description = storageDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 474f - 3f), new Vector2(boolOffset - 32f, 32f), "Storage", FLabelAlignment.Left) { description = storageDesc });
        horizNum++;
        string jumpDesc = "Shows values affecting all coyote and airhops, along with buffering jump inputs for landing";
        Tabs[0].AddItems(new OpCheckBox(bIncJumps, new Vector2(8f + boolOffset * horizNum, 474f)) { description = jumpDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 474f - 3f), new Vector2(boolOffset - 32f, 32f), "Jumpability", FLabelAlignment.Left) { description = jumpDesc });
        horizNum++;
        string movesDesc = "Shows all values surrounding rolling, sliding and charge pouncing, including general movement slow";
        Tabs[0].AddItems(new OpCheckBox(cIncMoves, new Vector2(8f + boolOffset * horizNum, 474f)) { description = movesDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 474f - 3f), new Vector2(boolOffset - 32f, 32f), "Move info", FLabelAlignment.Left) { description = movesDesc });
        horizNum++;
        string tunnelDesc = "Shows info about movement in tunnels and on poles";
        Tabs[0].AddItems(new OpCheckBox(dIncTunnel, new Vector2(8f + boolOffset * horizNum, 474f)) { description = tunnelDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 474f - 3f), new Vector2(boolOffset - 32f, 32f), "Tunnel/pole info", FLabelAlignment.Left) { description = tunnelDesc });

        horizNum = 0;
        string shroomDesc = "Shows info around mushroom effect";
        Tabs[0].AddItems(new OpCheckBox(eIncShroom, new Vector2(8f + boolOffset * horizNum, 444f)) { description = movesDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 444f - 3f), new Vector2(boolOffset - 32f, 32f), "Shroom info", FLabelAlignment.Left) { description = shroomDesc });
        if (ModManager.MSC)
        {
            horizNum++;
            string MSCDesc = "Shows info about More Slugcats movement";
            Tabs[0].AddItems(new OpCheckBox(fIncMSC, new Vector2(8f + boolOffset * horizNum, 444f)) { description = tunnelDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 444f - 3f), new Vector2(boolOffset - 32f, 32f), "MSC info", FLabelAlignment.Left) { description = MSCDesc });
        }
        horizNum++;
        string stateDesc = "Shows slugcat's current bodyMode and animation states";
        Tabs[0].AddItems(new OpCheckBox(gIncState, new Vector2(8f + boolOffset * horizNum, 444f)) { description = stateDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 444f - 3f), new Vector2(boolOffset - 32f, 32f), "Slugcat state", FLabelAlignment.Left) { description = stateDesc });
        horizNum++;
        string speedDesc = "Shows slugcat's current velocity";
        Tabs[0].AddItems(new OpCheckBox(hIncSpeed, new Vector2(8f + boolOffset * horizNum, 444f)) { description = speedDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 444f - 3f), new Vector2(boolOffset - 32f, 32f), "Slugcat speed", FLabelAlignment.Left) { description = speedDesc });

        horizNum = 0;
        // Permastore mode
        string psDesc = "Gives slugcat turn storage at all times";
        Tabs[0].AddItems(new OpCheckBox(permastore, new Vector2(8f + boolOffset * horizNum, 50f)) { description = psDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 50f - 3f), new Vector2(boolOffset - 32f, 32f), "Permastore Mode", FLabelAlignment.Left) { description = psDesc });

        // Storeordie mode
        horizNum++;
        string sodDesc = "If slugcat ever doesnt have turn storage, it dies";
        Tabs[0].AddItems(new OpCheckBox(storeordie, new Vector2(8f + boolOffset * horizNum, 50f)) { description = sodDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * horizNum, 50f - 3f), new Vector2(boolOffset - 32f, 32f), "Storeordie Mode", FLabelAlignment.Left) { description = sodDesc });

        // Color pickers
        _backCol = new OpColorPicker(backColor, new Vector2(32f, 254f));
        Tabs[0].AddItems(_backCol, new OpLabel(new Vector2(32f, 412f), new Vector2(150f, 16f), "Outline Color"));
        _offCol = new OpColorPicker(offColor, new Vector2(225f, 254f));
        Tabs[0].AddItems(_offCol, new OpLabel(new Vector2(225f, 412f), new Vector2(150f, 16f), "Off Color"));
        _onCol = new OpColorPicker(onColor, new Vector2(418f, 254f));
        Tabs[0].AddItems(_onCol, new OpLabel(new Vector2(418f, 412f), new Vector2(150f, 16f), "On Color"));

        // Alpha slider
        string aDesc = "How opaque the display is (75 by default)";
        Tabs[0].AddItems(new OpLabel(new Vector2(8f, 220f), new Vector2(40f, 24f), "Alpha", FLabelAlignment.Right) { description = aDesc });
        Tabs[0].AddItems(new OpFloatSlider(alpha, new Vector2(8f + 48f, 220f - 3f), 200, 2) { description = aDesc });

        // Scale slider
        string sclDesc = "The scale factor of the display (10 by default)";
        Tabs[0].AddItems(new OpLabel(new Vector2(8f, 190f), new Vector2(40f, 24f), "Scale", FLabelAlignment.Right) { description = sclDesc });
        Tabs[0].AddItems(new OpFloatSlider(scale, new Vector2(8f + 48f, 190f - 3f), 200, 2) { description = sclDesc });
    }
}

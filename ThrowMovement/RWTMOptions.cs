using UnityEngine;
using Menu.Remix.MixedUI;

namespace ThrowMovement;

using static RWThrowMovement;
partial class RWTMOptions : OptionInterface
{
    public RWTMOptions()
    {
        reqFramesOnGround = config.Bind("grounded_frames", 0);
        reqFramesSinceThrow = config.Bind("rate_frames", 20);
        defRockLifespan = config.Bind("despawn_frames", 80);
    }

    public override void Initialize()
	{
		base.Initialize();
        Tabs = new OpTab[] { new OpTab(this) };

        Tabs[0].AddItems(new OpLabel(20f, 570f, "ThrowMovement mod by pinecubes", true));
        Tabs[0].AddItems(new OpLabel(20f, 500f, "laura#2871 for any questions or suggestions!", false));
        Tabs[0].AddItems(new OpLabel(200f, 530f, "Adds the configurable ability to generate rocks while on the ground, while one is in the stomach, for movement and tech!", false));

		int top = 200;

        Tabs[0].AddItems(new OpLabel(20f, 600 - top, "Grounded time until rocks", false));
        Tabs[0].AddItems(new OpLabel(356f, 600 - top + 2, " frames", false));
        Tabs[0].AddItems(new OpTextBox(reqFramesOnGround, new Vector2(300f, 600 - top), 50)
		{
			description = "How many frames should slugcat be on the ground before they get rocks? (40fps)",
			colorEdge = Color.clear,
			colorText = new Color(122f, 216f, 255f)
		});
        Tabs[0].AddItems(new OpLabel(20f, 600 - top - 30, "Throw time until rocks", false));
        Tabs[0].AddItems(new OpLabel(356f, 600 - top - 30 + 2, " frames", false));
        Tabs[0].AddItems(new OpTextBox(reqFramesSinceThrow, new Vector2(300f, 600 - top - 30), 50)
		{
			description = "At what frame interval after throwing should slugcat get rocks? (40fps)",
			colorEdge = Color.clear,
			colorText = new Color(122f, 216f, 255f)
		});
        Tabs[0].AddItems(new OpLabel(20f, 600 - top - 60, "Time until despawn", false));
        Tabs[0].AddItems(new OpLabel(356f, 600 - top - 60 + 2, " frames", false));
        Tabs[0].AddItems(new OpTextBox(defRockLifespan, new Vector2(300f, 600 - top - 60), 50)
		{
			description = "How many frames should rocks be thrown before they despawn? (40fps)",
			colorEdge = Color.clear,
			colorText = new Color(122f, 216f, 255f)
		});
	}
}
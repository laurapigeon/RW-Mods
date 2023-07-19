using UnityEngine;
using Menu.Remix.MixedUI;

namespace PhysicsSpeedConfig;

using static PhysicsSpeedConfig;
partial class Config : OptionInterface
{
	public override void Initialize()
	{
		base.Initialize();
		Tabs = new OpTab[] { new OpTab(this) };

		OpLabel labelAuthor = new(20f, 570f, "physicsspeedconfig mod by pinecubes", true);
		OpLabel labelPing = new(20f, 500f, "laura#2871 for any questions or suggestions!", false);
		OpLabel labelNote = new(200f, 530f, "Configure keys to change the physics speed while ingame!", false);

		int top = 200;

		OpLabel labelSlowMul = new(20f, 600 - top, "Slowdown multiplier", false);
		OpLabel labelSlowPercent = new(356f, 600 - top + 2, "%", false);
		OpTextBox typerSlowMul = new(slowDownMult, new Vector2(300f, 600 - top), 50)
		{
			description = "25% by default",
			colorEdge = Color.clear,
			colorText = new Color(122f, 216f, 255f)
		};
		OpLabel labelSpeedMul = new(20f, 600 - top - 30, "Speedup multiplier", false);
		OpLabel labelSpeedPercent = new(356f, 600 - top - 30 + 2, "%", false);
		OpTextBox typerSpeedMul = new(speedUpMult, new Vector2(300f, 600 - top - 30), 50)
		{
			description = "300% by default",
			colorEdge = Color.clear,
			colorText = new Color(122f, 216f, 255f)
		};
		OpLabel labelSlowKey = new(20f, 600 - top - 60, "Slowdown keybind", false);
		OpKeyBinder binderSlowKey = new(slowDownKey, new Vector2(300f, 600 - top - 60), new Vector2(50, 15))
		{ };
		OpLabel labelSpeedKey = new OpLabel(20f, 600 - top - 90, "Speedup keybind", false);
		OpKeyBinder binderSpeedKey = new(speedUpKey, new Vector2(300f, 600 - top - 90), new Vector2(50, 15))
		{ };
        OpLabel labelKillKey = new OpLabel(20f, 600 - top - 120, "Kill process keybind", false);
        OpKeyBinder binderKillKey = new(killKey, new Vector2(300f, 600 - top - 120), new Vector2(50, 15))
        { };
        OpLabel labelToggleToggle = new(20f, 600 - top - 150, "Toggle with key", false);
		OpCheckBox toggleToggleToggle = new(doToggle, new Vector2(300f, 600 - top - 150))
		{
			description = "On by default"
		};
		Tabs[0].AddItems(new UIelement[]
		{
			labelAuthor,
			labelPing,
			labelNote,
			labelSlowMul,
			labelSlowPercent,
			typerSlowMul,
			labelSpeedMul,
			labelSpeedPercent,
			typerSpeedMul,
			labelSlowKey,
			binderSlowKey,
			labelSpeedKey,
			binderSpeedKey,
            labelKillKey,
            binderKillKey,
            labelToggleToggle,
			toggleToggleToggle,
		});
	}
	public Config()
	{
		slowDownMult = config.Bind("slowdownmultiplier", 25f);
		speedUpMult = config.Bind("speedupmultiplier", 300f);
		slowDownKey = config.Bind("slowdownkeybind", KeyCode.A);
		speedUpKey = config.Bind("speedupkeybind", KeyCode.S);
		killKey = config.Bind("killkeybind", KeyCode.D);
        doToggle = config.Bind("toggleviakeybind", true);
    }
}
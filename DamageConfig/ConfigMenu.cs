using System;
using OptionalUI;
using UnityEngine;

namespace DamageConfig
{
	public class Config : OptionInterface
	{
		public Config() : base(DamageConfig.instance)
		{
		}

		public override bool Configuable()
		{
			return true;
		}

		public override void ConfigOnChange()
		{
			DamageConfig.SpearDamageMult = (float)(int.Parse(OptionInterface.config["D1"]) / 100f);
			DamageConfig.RockDamageMult = (float)(int.Parse(OptionInterface.config["D2"]) / 100f);
			DamageConfig.BombDamageMult = (float)(int.Parse(OptionInterface.config["D3"]) / 100f);
			DamageConfig.AllDamage = bool.Parse(OptionInterface.config["D4"]);
		}

		public override void Initialize()
		{
			base.Initialize();
			this.Tabs = new OpTab[]
			{
				new OpTab("Config")
			};
			OpLabel labelAuthor = new OpLabel(20f, 570f, "damageconfig mod by pinecubes", true);
			OpLabel labelPing = new OpLabel(20f, 500f, "laura#2871 for any questions or suggestions!", false);
			OpLabel labelNote = new OpLabel(200f, 530f, "Change the percentage damage rocks, spears and explosions do!", false);

			int top = 200;

			OpLabel labelSpearMul = new OpLabel(20f, (float)(600 - top), "Spear damage multiplier", false);
			OpLabel labelSpearPercent = new OpLabel(356f, (float)(600 - top + 2), "%", false);
			OpTextBox typerSpearMul = new OpTextBox(new Vector2(300f, (float)(600 - top)), 50, "D1", "100", OpTextBox.Accept.Int)
			{
				description = "100% by default",
				colorEdge = Color.clear,
				colorText = new Color(122f, 216f, 255f)
			};
			OpLabel labelRockMul = new OpLabel(20f, (float)(600 - top - 30), "Rock damage multiplier", false);
			OpLabel labelRockPercent = new OpLabel(356f, (float)(600 - top - 30 + 2), "%", false);
			OpTextBox typerRockMul = new OpTextBox(new Vector2(300f, (float)(600 - top - 30)), 50, "D2", "100", OpTextBox.Accept.Int)
			{
				description = "100% by default",
				colorEdge = Color.clear,
				colorText = new Color(122f, 216f, 255f)
			};
			OpLabel labelAllDamage = new OpLabel(20f, (float)(600 - top - 60), "Modify all damage (not just slugcat)", false);
			OpCheckBox toggleAllDamage = new OpCheckBox(new Vector2(300f, (float)(600 - top - 60)), "D4", true)
			{
				description = "Turn this on if you want scavs to have modified damage too"
			};
			OpLabel labelBombMul = new OpLabel(20f, (float)(600 - top - 90), "Explosion damage multiplier", false);
			OpLabel labelBombPercent = new OpLabel(356f, (float)(600 - top - 90 + 2), "%", false);
			OpTextBox typerBombMul = new OpTextBox(new Vector2(300f, (float)(600 - top - 90)), 50, "D3", "100", OpTextBox.Accept.Int)
			{
				description = "100% by default, this option is regardless of the tickbox above due to ownership stuff",
				colorEdge = Color.clear,
				colorText = new Color(122f, 216f, 255f)
			};
			this.Tabs[0].AddItems(new UIelement[]
			{
				labelAuthor,
				labelPing,
				labelNote,
				labelSpearMul,
				labelSpearPercent,
				typerSpearMul,
				labelRockMul,
				labelRockPercent,
				typerRockMul,
				labelAllDamage,
				toggleAllDamage,
				labelBombMul,
				labelBombPercent,
				typerBombMul
			});
		}
	}
}

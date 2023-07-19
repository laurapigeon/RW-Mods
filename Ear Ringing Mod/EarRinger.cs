using System.Collections.Generic;
using System.Linq;
using System.Text;
using OptionalUI;
using Partiality.Modloader;
using UnityEngine;

namespace EarRinger
{
    public class EarRingingMod : PartialityMod
    {
        public EarRingingMod()
        {
            instance = this;
            this.ModID = "Ear Ringing Mod";
            this.Version = "1";
            this.author = "laura#2871";
        }
        public static EarRingingMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            On.Player.Deafen += Player_Deafen;
            On.Room.PlaySound_SoundID_PositionedSoundEmitter_bool_float_float_bool += Room_PlaySound_SoundID_PositionedSoundEmitter_bool_float_float_bool;
        }

        private void Room_PlaySound_SoundID_PositionedSoundEmitter_bool_float_float_bool(On.Room.orig_PlaySound_SoundID_PositionedSoundEmitter_bool_float_float_bool orig, Room self, SoundID soundId, PositionedSoundEmitter em, bool loop, float vol, float pitch, bool randomStartPosition)
        {
            foreach (AbstractCreature absPly in self.game.Players)
            {
                if (absPly.realizedCreature == null) continue;
                Player player = (Player)absPly.realizedCreature;
                if (!player.dead) continue;
                int db;
                switch ((int)soundId)
                {
                    case 570: db = 6; break; // noot hit terrain
                    case 572: db = 4; break; // noot hit creature
                    case 525: db = 10; break; // bomb explodes
                    case 167: db = 8; break; // bullet rain strikes
                    case 334: db = 7; break; // centipede zaps
                    case 479: db = 10; break; // underhang heavy lightning loop
                    case 480: db = 10; break; // underhang lighting strikes
                    case 481: db = 10; break; // underhang lighting hits an object
                    case 497: db = 6; break; // firecracker pops
                    case 498: db = 8; break; // firecracker explodes
                    case 412: db = 7; break; // firespear pops
                    case 413: db = 9; break; // firespear explodes
                    case 609: db = 8; break; // vulture tusk fires
                    case 611: db = 4; break; // vulture tusk kills
                    case 613: db = 5; break; // vulture tusk sticks
                    case 614: db = 6; break; // vulture tusk bounces
                    case 244: db = 6; break; // leviathan bites
                    case 107: db = 5; break; // lizard head deflect
                    case 108: db = 3; break; // lizard bite misses
                    case 109: db = 2; break; // lizard bites to damage
                    case 264: db = 2; break; // miros leg hits ground
                    case 265: db = 4; break; // miros leg hits hard
                    case 266: db = 3; break; // miros leg scrapes
                    case 267: db = 5; break; // miros leg scrapes hard
                    case 268: db = 1; break; // miros bite misses
                    case 269: db = 4; break; // miros bites slugcat
                    case 270: db = 2; break; // miros bites creature
                    case 78: db = 6; break; // spear bounces off wall
                    case 85: db = 5; break; // rock hits wall
                    case 144: db = 7; break; // snail pops
                    case 414: db = 6; break; // spear fragment bounces
                    case 260: db = 10; break; // thunder hits close
                    case 395: db = 10; break; // zapper zaps
                    default: db = 0; break; // all other sounds
                }
                if (db != 0 && (em.pos - player.bodyChunks[0].pos).magnitude <= 1000f)
                {
                    Debug.Log($"{db} db from sound {soundId}");
                    db *= 100 / (int)Mathf.Sqrt((em.pos - player.bodyChunks[0].pos).magnitude);
                    player.Deafen(db);
                }
            }
            orig(self, soundId, em, loop, vol, pitch, randomStartPosition);
        }

        private void Player_Deafen(On.Player.orig_Deafen orig, Player self, int df)
        {
            orig(self, (df * 2) + 40);
        }
    }
}
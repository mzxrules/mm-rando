using MMRando.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MMRando.Models
{
    public class Settings : GenerationSettings
    {
        #region General settings

        /// <summary>
        /// Checks if a game needs to be built
        /// </summary>
        public bool OutputGame => OutputN64ROM || OutputVC;

        /// <summary>
        /// Checks if a spoiler needs to be generated
        /// </summary>
        public bool OutputSpoiler => OutputTextSpoiler || OutputHTMLSpoiler;

        /// <summary>
        ///  Outputs n64 rom if true (default: true)
        /// </summary>
        public bool OutputN64ROM
        {
            get => outputN64ROM;
            set => SetField(ref outputN64ROM, value);
        }
        private bool outputN64ROM = true;


        /// <summary>
        ///  Outputs virtual channel if true
        /// </summary>
        public bool OutputVC
        {
            get => outputVC;
            set => SetField(ref outputVC, value);
        }
        private bool outputVC;

        /// <summary>
        /// Filepath to the input ROM
        /// </summary>
        public string InputROMFilename
        {
            get => inputROMFilename;
            set => SetField(ref inputROMFilename, value);
        }
        private string inputROMFilename;

        /// <summary>
        /// Filepath to the input patch file
        /// </summary>
        public string InputPatchFilename
        {
            get => inputPatchFilename;
            set => SetField(ref inputPatchFilename, value);
        }
        private string inputPatchFilename;

        /// <summary>
        /// Filepath to the input logic file
        /// </summary>
        public string UserLogicFileName
        {
            get => userLogicFileName;
            set => SetField(ref userLogicFileName, value);
        }
        private string userLogicFileName;

        /// <summary>
        /// Default Filename for the output ROM
        /// </summary>
        public string DefaultOutputROMFilename
        {
            get
            {
                string settings = ToString();
                string appendSeed = OutputSpoiler ? $"{Seed}_" : "";
                string filename = $"MMR_{appendSeed}{settings}";

                return filename + ".z64";
            }
        }

        /// <summary>
        /// Filepath to the output ROM
        /// </summary>
        public string OutputROMFilename
        {
            get => outputROMFilename;
            set => SetField(ref outputROMFilename, value);
        }
        private string outputROMFilename;

        /// <summary>
        /// Generate spoiler log on randomizing
        /// </summary>
        public bool OutputTextSpoiler
        {
            get => outputTextSpoiler;
            set
            {
                SetField(ref outputTextSpoiler, value);
            }
        }
        private bool outputTextSpoiler = true;

        /// <summary>
        /// Generate HTML spoiler log on randomizing
        /// </summary>
        public bool OutputHTMLSpoiler
        {
            get => outputHTMLSpoiler;
            set => SetField(ref outputHTMLSpoiler, value);
        }
        private bool outputHTMLSpoiler;

        /// <summary>
        /// Use Custom Item list for the logic.
        /// </summary>
        public bool UseCustomItemList
        {
            get => useCustomItemList;
            set
            {
                SetField(ref useCustomItemList, value);

                //Todo: enable/disable ui elements instead, then have a "validate" stage for generating settings
                this.AddDungeonItems = false; // cDChests.Checked
                this.AddShopItems = false; // cShop.Checked
                this.RandomizeBottleCatchContents = false; //cBottled.Checked
                this.ExcludeSongOfSoaring = false; // cSoS.Checked
                this.AddOtherItems = false; //cAdditional.Checked
                this.AddMoonItems = false; //cMoonItems.Checked
            }
        }
        private bool useCustomItemList;

        /// <summary>
        /// Generate patch file
        /// </summary>
        public bool OutputROMPatch
        {
            get => outputROMPatch;
            set => SetField(ref outputROMPatch, value);
        }
        private bool outputROMPatch;

        /// <summary>
        /// Apply patch file during output generation
        /// </summary>
        public bool ApplyPatch
        {
            get => applyPatch;
            set => SetField(ref applyPatch, value);
        }
        private bool applyPatch;

        /// <summary>
        /// Checks if the InputROM is needed for generation
        /// </summary>
        public bool NeedInputROM => ApplyPatch | OutputGame | OutputROMPatch;

        /// <summary>
        /// Stores the byte order of the rom
        /// </summary>
        public ValidateRomResult InputROMFormat
        {
            get => inputROMFormat;
            set => SetField(ref inputROMFormat, value);
        }
        private ValidateRomResult inputROMFormat;

        #endregion

        #region Random Elements

        /// <summary>
        /// Randomize background music (includes bgm from other video games)
        /// </summary>
        public bool RandomizeBGM
        {
            get => randomizeBGM;
            set => SetField(ref randomizeBGM, value);
        }
        private bool randomizeBGM;

        /// <summary>
        /// Mute background music
        /// </summary>
        public bool NoBGM
        {
            get => noBGM;
            set => SetField(ref noBGM, value);
        }
        private bool noBGM;

        /// <summary>
        /// Prevent downgrades
        /// </summary>
        public bool PreventDowngrades
        {
            get => preventDowngrades;
            set => SetField(ref preventDowngrades, value);
        }
        private bool preventDowngrades = true;
        #endregion

        #region Gimmicks

        /// <summary>
        /// Modifies the damage value when Link is damaged
        /// </summary>
        public DamageMode DamageMode
        {
            get => damageMode;
            set => SetField(ref damageMode, value);
        }
        private DamageMode damageMode = DamageMode.Default;

        /// <summary>
        /// Adds an additional effect when Link is damaged
        /// </summary>
        public DamageEffect DamageEffect { get => damageEffect; set => SetField(ref damageEffect, value); }
        private DamageEffect damageEffect = DamageEffect.Default;

        /// <summary>
        /// Modifies Link's movement
        /// </summary>
        public MovementMode MovementMode { get => movementMode; set => SetField(ref movementMode, value); }
        private MovementMode movementMode = MovementMode.Default;

        /// <summary>
        /// Sets the type of floor globally
        /// </summary>
        public FloorType FloorType { get => floorType; set => SetField(ref floorType, value); }
        private FloorType floorType = FloorType.Default;

        /// <summary>
        /// Sets the clock speed.
        /// </summary>
        public ClockSpeed ClockSpeed { get => clockSpeed; set => SetField(ref clockSpeed, value); }
        private ClockSpeed clockSpeed = ClockSpeed.Default;

        /// <summary>
        /// Hides the clock UI.
        /// </summary>
        public bool HideClock { get => hideClock; set => SetField(ref hideClock, value); }
        private bool hideClock;

        #endregion

        #region Comfort / Cosmetics

        /// <summary>
        /// Certain cutscenes will play shorter, or will be skipped
        /// </summary>
        public bool ShortenCutscenes { get => shortenCutscenes; set => SetField(ref shortenCutscenes, value); }
        private bool shortenCutscenes = true;

        /// <summary>
        /// Text is fast-forwarded
        /// </summary>
        public bool QuickTextEnabled { get => quickTextEnabled; set => SetField(ref quickTextEnabled, value); }
        private bool quickTextEnabled = true;

        /// <summary>
        /// The color of Link's tunic
        /// </summary>
        public Color TunicColor { get => tunicColor; set => SetField(ref tunicColor, value); }
        private Color tunicColor = Color.FromArgb(0x1E, 0x69, 0x1B);

        /// <summary>
        /// Replaces Link's default model
        /// </summary>
        public Character Character { get => character; set => SetField(ref character, value); }
        private Character character = Character.LinkMM;

        /// <summary>
        /// Replaces Tatl's colors
        /// </summary>
        public TatlColorSchema TatlColorSchema { get => tatlColorSchema; set => SetField(ref tatlColorSchema, value); }
        private TatlColorSchema tatlColorSchema = TatlColorSchema.Default;


        /// <summary>
        ///  Custom item list selections
        /// </summary>
        public List<int> CustomItemList
        {
            get => customItemList;
            set => SetField(ref customItemList, value);
        }
        private List<int> customItemList = new List<int>();

        /// <summary>
        ///  Custom item list string
        /// </summary>
        public string CustomItemListString
        {
            get => customItemListString;
            set => SetField(ref customItemListString, value);
        }
        private string customItemListString;

        #endregion

        // Functions

        public void Update(string settings)
        {
            var parts = settings.Split('-')
                .Select(p => Base36Utils.Decode(p))
                .ToArray();

            if (parts.Any(p => p > int.MaxValue))
            {
                throw new ArgumentException(nameof(settings));
            }

            int part1 = (int)parts[0];
            int part2 = (int)parts[1];
            int part3 = (int)parts[2];
            int part4 = (int)parts[3];

            PreventDowngrades = (part1 & 524288) > 0;
            NoBGM = (part1 & 262144) > 0;
            HideClock = (part1 & 131072) > 0;
            ClearHints = (part1 & 65536) > 0;
            AddMoonItems = (part1 & 32768) > 0;
            FreeHints = (part1 & 16384) > 0;
            UseCustomItemList = (part1 & 8192) > 0;
            AddOtherItems = (part1 & 4096) > 0;
            EnableGossipHints = (part1 & 2048) > 0;
            ExcludeSongOfSoaring = (part1 & 1024) > 0;
            OutputTextSpoiler = (part1 & 512) > 0;
            AddSongs = (part1 & 256) > 0;
            RandomizeBottleCatchContents = (part1 & 128) > 0;
            AddDungeonItems = (part1 & 64) > 0;
            AddShopItems = (part1 & 32) > 0;
            RandomizeDungeonEntrances = (part1 & 16) > 0;
            RandomizeBGM = (part1 & 8) > 0;
            RandomizeEnemies = (part1 & 4) > 0;
            ShortenCutscenes = (part1 & 2) > 0;
            QuickTextEnabled = (part1 & 1) > 0;

            var damageMultiplierIndex = (int)((part2 & 0xF0000000) >> 28);
            var damageTypeIndex = (part2 & 0xF000000) >> 24;
            var modeIndex = (part2 & 0xFF0000) >> 16;
            var characterIndex = (part2 & 0xFF00) >> 8;
            var tatlColorIndex = part2 & 0xFF;

            var gravityTypeIndex = (int)((part3 & 0xF0000000) >> 28);
            var floorTypeIndex = (part3 & 0xF000000) >> 24;
            var tunicColor = Color.FromArgb(
                (part3 & 0xFF0000) >> 16,
                (part3 & 0xFF00) >> 8,
                part3 & 0xFF);

            var clockSpeedIndex = (byte)(part4 & 0xFF);

            DamageMode = (DamageMode)damageMultiplierIndex;
            DamageEffect = (DamageEffect)damageTypeIndex;
            LogicMode = (LogicMode)modeIndex;
            Character = (Character)characterIndex;
            TatlColorSchema = (TatlColorSchema)tatlColorIndex;
            MovementMode = (MovementMode)gravityTypeIndex;
            FloorType = (FloorType)floorTypeIndex;
            TunicColor = tunicColor;
            ClockSpeed = (ClockSpeed)clockSpeedIndex;

        }


        private int[] BuildSettingsBytes()
        {
            int[] parts = new int[4];

            if (PreventDowngrades) { parts[0] += 524288; }
            if (NoBGM) { parts[0] += 262144; }
            if (HideClock) { parts[0] += 131072; };
            if (ClearHints) { parts[0] += 65536; };
            if (AddMoonItems) { parts[0] += 32768; };
            if (FreeHints) { parts[0] += 16384; };
            if (UseCustomItemList) { parts[0] += 8192; };
            if (AddOtherItems) { parts[0] += 4096; };
            if (EnableGossipHints) { parts[0] += 2048; };
            if (ExcludeSongOfSoaring) { parts[0] += 1024; };
            if (OutputTextSpoiler) { parts[0] += 512; };
            if (AddSongs) { parts[0] += 256; };
            if (RandomizeBottleCatchContents) { parts[0] += 128; };
            if (AddDungeonItems) { parts[0] += 64; };
            if (AddShopItems) { parts[0] += 32; };
            if (RandomizeDungeonEntrances) { parts[0] += 16; };
            if (RandomizeBGM) { parts[0] += 8; };
            if (RandomizeEnemies) { parts[0] += 4; };
            if (ShortenCutscenes) { parts[0] += 2; };
            if (QuickTextEnabled) { parts[0] += 1; };

            parts[1] = (byte)LogicMode << 16
                | (byte)Character << 8
                | (byte)TatlColorSchema
                | (byte)DamageEffect << 24
                    | (byte)DamageMode << 28;

            parts[2] = TunicColor.R << 16
                | TunicColor.G << 8
                | TunicColor.B
                | (byte)FloorType << 24
                    | (byte)MovementMode << 28;

            parts[3] = (byte)ClockSpeed;

            return parts;
        }

        private string EncodeSettings()
        {
            var partsEncoded = BuildSettingsBytes()
                .Select(p => Base36Utils.Encode(p))
                .ToArray();

            return string.Join("-", partsEncoded);
        }

        public string GetGenerationSettings()
        {
            string settings = JsonConvert.SerializeObject(this);
            var settingsObj = JsonConvert.DeserializeObject<GenerationSettings>
                (settings);
            return JsonConvert.SerializeObject(settingsObj);
        }

        public override string ToString()
        {
            return EncodeSettings();
        }
    }
}

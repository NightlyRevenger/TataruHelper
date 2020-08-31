// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CurrentPlayer.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   CurrentPlayer.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core {
    using System.Collections.Generic;

    using Sharlayan.Core.Enums;
    using Sharlayan.Core.Interfaces;
    using Sharlayan.Extensions;

    public class CurrentPlayer : ICurrentPlayer {
        private string _name;

        public byte ACN { get; set; }

        public int ACN_CurrentEXP { get; set; }

        public byte ALC { get; set; }

        public int ALC_CurrentEXP { get; set; }

        public byte ARC { get; set; }

        public int ARC_CurrentEXP { get; set; }

        public byte ARM { get; set; }

        public int ARM_CurrentEXP { get; set; }

        public byte AST { get; set; }

        public int AST_CurrentEXP { get; set; }

        public short AttackMagicPotency { get; set; }

        public short AttackPower { get; set; }

        public short BaseDexterity { get; set; }

        public short BaseIntelligence { get; set; }

        public short BaseMind { get; set; }

        public short BasePiety { get; set; }

        public short BaseStrength { get; set; }

        public short BaseSubstat { get; set; }

        public short BaseVitality { get; set; }

        public byte BLU { get; set; }

        public int BLU_CurrentEXP { get; set; }

        public short BluntResistance { get; set; }

        public byte BSM { get; set; }

        public int BSM_CurrentEXP { get; set; }

        public byte BTN { get; set; }

        public int BTN_CurrentEXP { get; set; }

        public byte CNJ { get; set; }

        public int CNJ_CurrentEXP { get; set; }

        public short Control { get; set; }

        public int CPMax { get; set; }

        public byte CPT { get; set; }

        public int CPT_CurrentEXP { get; set; }

        public short Craftmanship { get; set; }

        public short CriticalHitRate { get; set; }

        public byte CUL { get; set; }

        public int CUL_CurrentEXP { get; set; }

        public short Defense { get; set; }

        public short Determination { get; set; }

        public short Dexterity { get; set; }

        public short DirectHit { get; set; }

        public byte DNC { get; set; }

        public int DNC_CurrentEXP { get; set; }

        public byte DRK { get; set; }

        public int DRK_CurrentEXP { get; set; }

        public short EarthResistance { get; set; }

        public List<EnmityItem> EnmityItems { get; } = new List<EnmityItem>();

        public short Evasion { get; set; }

        public short FireResistance { get; set; }

        public byte FSH { get; set; }

        public int FSH_CurrentEXP { get; set; }

        public short Gathering { get; set; }

        public byte GLD { get; set; }

        public int GLD_CurrentEXP { get; set; }

        public byte GNB { get; set; }

        public int GNB_CurrentEXP { get; set; }

        public int GPMax { get; set; }

        public byte GSM { get; set; }

        public int GSM_CurrentEXP { get; set; }

        public short HealingMagicPotency { get; set; }

        public int HPMax { get; set; }

        public short IceResistance { get; set; }

        public short Intelligence { get; set; }

        public Actor.Job Job { get; set; }

        public byte JobID { get; set; }

        public short LightningResistance { get; set; }

        public byte LNC { get; set; }

        public int LNC_CurrentEXP { get; set; }

        public byte LTW { get; set; }

        public int LTW_CurrentEXP { get; set; }

        public short MagicDefense { get; set; }

        public byte MCH { get; set; }

        public int MCH_CurrentEXP { get; set; }

        public byte MIN { get; set; }

        public int MIN_CurrentEXP { get; set; }

        public short Mind { get; set; }

        public int MPMax { get; set; }

        public byte MRD { get; set; }

        public int MRD_CurrentEXP { get; set; }

        public string Name {
            get => this._name;
            set => this._name = value.ToTitleCase();
        }

        public short Perception { get; set; }

        public byte PGL { get; set; }

        public int PGL_CurrentEXP { get; set; }

        public short PiercingResistance { get; set; }

        public short Piety { get; set; }

        public byte RDM { get; set; }

        public int RDM_CurrentEXP { get; set; }

        public byte ROG { get; set; }

        public int ROG_CurrentEXP { get; set; }

        public byte SAM { get; set; }

        public int SAM_CurrentEXP { get; set; }

        public short SkillSpeed { get; set; }

        public short SlashingResistance { get; set; }

        public short SpellSpeed { get; set; }

        public short Strength { get; set; }

        public short Tenacity { get; set; }

        public byte THM { get; set; }

        public int THM_CurrentEXP { get; set; }

        public int TPMax { get; set; }

        public short Vitality { get; set; }

        public short WaterResistance { get; set; }

        public short WindResistance { get; set; }

        public byte WVR { get; set; }

        public int WVR_CurrentEXP { get; set; }
    }
}
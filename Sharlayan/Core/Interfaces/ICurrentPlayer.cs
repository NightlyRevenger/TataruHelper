// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICurrentPlayer.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   ICurrentPlayer.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Interfaces {
    using System.Collections.Generic;

    using Sharlayan.Core.Enums;

    public interface ICurrentPlayer {
        byte ACN { get; set; }

        int ACN_CurrentEXP { get; set; }

        byte ALC { get; set; }

        int ALC_CurrentEXP { get; set; }

        byte ARC { get; set; }

        int ARC_CurrentEXP { get; set; }

        byte ARM { get; set; }

        int ARM_CurrentEXP { get; set; }

        byte AST { get; set; }

        int AST_CurrentEXP { get; set; }

        short AttackMagicPotency { get; set; }

        short AttackPower { get; set; }

        short BaseDexterity { get; set; }

        short BaseIntelligence { get; set; }

        short BaseMind { get; set; }

        short BasePiety { get; set; }

        short BaseStrength { get; set; }

        short BaseVitality { get; set; }

        short BluntResistance { get; set; }

        byte BSM { get; set; }

        int BSM_CurrentEXP { get; set; }

        byte BTN { get; set; }

        int BTN_CurrentEXP { get; set; }

        byte CNJ { get; set; }

        int CNJ_CurrentEXP { get; set; }

        short Control { get; set; }

        int CPMax { get; set; }

        byte CPT { get; set; }

        int CPT_CurrentEXP { get; set; }

        short Craftmanship { get; set; }

        short CriticalHitRate { get; set; }

        byte CUL { get; set; }

        int CUL_CurrentEXP { get; set; }

        short Defense { get; set; }

        short Determination { get; set; }

        short Dexterity { get; set; }

        short DirectHit { get; set; }

        byte DRK { get; set; }

        int DRK_CurrentEXP { get; set; }

        short EarthResistance { get; set; }

        List<EnmityItem> EnmityItems { get; }

        short Evasion { get; set; }

        short FireResistance { get; set; }

        byte FSH { get; set; }

        int FSH_CurrentEXP { get; set; }

        short Gathering { get; set; }

        byte GLD { get; set; }

        int GLD_CurrentEXP { get; set; }

        int GPMax { get; set; }

        byte GSM { get; set; }

        int GSM_CurrentEXP { get; set; }

        short HealingMagicPotency { get; set; }

        int HPMax { get; set; }

        short IceResistance { get; set; }

        short Intelligence { get; set; }

        Actor.Job Job { get; set; }

        byte JobID { get; set; }

        short LightningResistance { get; set; }

        byte LNC { get; set; }

        int LNC_CurrentEXP { get; set; }

        byte LTW { get; set; }

        int LTW_CurrentEXP { get; set; }

        short MagicDefense { get; set; }

        byte MCH { get; set; }

        int MCH_CurrentEXP { get; set; }

        byte MIN { get; set; }

        int MIN_CurrentEXP { get; set; }

        short Mind { get; set; }

        int MPMax { get; set; }

        byte MRD { get; set; }

        int MRD_CurrentEXP { get; set; }

        string Name { get; set; }

        short Perception { get; set; }

        byte PGL { get; set; }

        int PGL_CurrentEXP { get; set; }

        short PiercingResistance { get; set; }

        short Piety { get; set; }

        byte RDM { get; set; }

        int RDM_CurrentEXP { get; set; }

        byte ROG { get; set; }

        int ROG_CurrentEXP { get; set; }

        byte SAM { get; set; }

        int SAM_CurrentEXP { get; set; }

        short SkillSpeed { get; set; }

        short SlashingResistance { get; set; }

        short SpellSpeed { get; set; }

        short Strength { get; set; }

        short Tenacity { get; set; }

        byte THM { get; set; }

        int THM_CurrentEXP { get; set; }

        int TPMax { get; set; }

        short Vitality { get; set; }

        short WaterResistance { get; set; }

        short WindResistance { get; set; }

        byte WVR { get; set; }

        int WVR_CurrentEXP { get; set; }
    }
}
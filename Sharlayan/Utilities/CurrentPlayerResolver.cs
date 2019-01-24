// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CurrentPlayerResolver.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   CurrentPlayerResolver.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using System;

    using NLog;

    using Sharlayan.Core;
    using Sharlayan.Core.Enums;

    internal static class CurrentPlayerResolver {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static CurrentPlayer ResolvePlayerFromBytes(byte[] source) {
            var entry = new CurrentPlayer();
            try {
                entry.Name = MemoryHandler.Instance.GetStringFromBytes(source, 1);

                switch (MemoryHandler.Instance.GameLanguage) {
                    default:
                        entry.JobID = source[MemoryHandler.Instance.Structures.CurrentPlayer.JobID];
                        entry.Job = (Actor.Job) entry.JobID;

                        #region Job Levels

                        entry.PGL = source[MemoryHandler.Instance.Structures.CurrentPlayer.PGL];
                        entry.GLD = source[MemoryHandler.Instance.Structures.CurrentPlayer.GLD];
                        entry.MRD = source[MemoryHandler.Instance.Structures.CurrentPlayer.MRD];
                        entry.ARC = source[MemoryHandler.Instance.Structures.CurrentPlayer.ARC];
                        entry.LNC = source[MemoryHandler.Instance.Structures.CurrentPlayer.LNC];
                        entry.THM = source[MemoryHandler.Instance.Structures.CurrentPlayer.THM];
                        entry.CNJ = source[MemoryHandler.Instance.Structures.CurrentPlayer.CNJ];

                        entry.CPT = source[MemoryHandler.Instance.Structures.CurrentPlayer.CPT];
                        entry.BSM = source[MemoryHandler.Instance.Structures.CurrentPlayer.BSM];
                        entry.ARM = source[MemoryHandler.Instance.Structures.CurrentPlayer.ARM];
                        entry.GSM = source[MemoryHandler.Instance.Structures.CurrentPlayer.GSM];
                        entry.LTW = source[MemoryHandler.Instance.Structures.CurrentPlayer.LTW];
                        entry.WVR = source[MemoryHandler.Instance.Structures.CurrentPlayer.WVR];
                        entry.ALC = source[MemoryHandler.Instance.Structures.CurrentPlayer.ALC];
                        entry.CUL = source[MemoryHandler.Instance.Structures.CurrentPlayer.CUL];

                        entry.MIN = source[MemoryHandler.Instance.Structures.CurrentPlayer.MIN];
                        entry.BTN = source[MemoryHandler.Instance.Structures.CurrentPlayer.BTN];
                        entry.FSH = source[MemoryHandler.Instance.Structures.CurrentPlayer.FSH];

                        entry.ACN = source[MemoryHandler.Instance.Structures.CurrentPlayer.ACN];
                        entry.ROG = source[MemoryHandler.Instance.Structures.CurrentPlayer.ROG];

                        entry.MCH = source[MemoryHandler.Instance.Structures.CurrentPlayer.MCH];
                        entry.DRK = source[MemoryHandler.Instance.Structures.CurrentPlayer.DRK];
                        entry.AST = source[MemoryHandler.Instance.Structures.CurrentPlayer.AST];

                        entry.SAM = source[MemoryHandler.Instance.Structures.CurrentPlayer.SAM];
                        entry.RDM = source[MemoryHandler.Instance.Structures.CurrentPlayer.RDM];

                        #endregion

                        #region Current Experience

                        entry.PGL_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.PGL_CurrentEXP);
                        entry.GLD_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.GLD_CurrentEXP);
                        entry.MRD_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.MRD_CurrentEXP);
                        entry.ARC_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.ARC_CurrentEXP);
                        entry.LNC_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.LNC_CurrentEXP);
                        entry.THM_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.THM_CurrentEXP);
                        entry.CNJ_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.CNJ_CurrentEXP);

                        entry.CPT_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.CPT_CurrentEXP);
                        entry.BSM_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.BSM_CurrentEXP);
                        entry.ARM_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.ARM_CurrentEXP);
                        entry.GSM_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.GSM_CurrentEXP);
                        entry.LTW_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.LTW_CurrentEXP);
                        entry.WVR_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.WVR_CurrentEXP);
                        entry.ALC_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.ALC_CurrentEXP);
                        entry.CUL_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.CUL_CurrentEXP);

                        entry.MIN_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.MIN_CurrentEXP);
                        entry.BTN_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.BTN_CurrentEXP);
                        entry.FSH_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.FSH_CurrentEXP);

                        entry.ACN_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.ACN_CurrentEXP);
                        entry.ROG_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.ROG_CurrentEXP);

                        entry.MCH_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.MCH_CurrentEXP);
                        entry.DRK_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.DRK_CurrentEXP);
                        entry.AST_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.AST_CurrentEXP);

                        entry.SAM_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.SAM_CurrentEXP);
                        entry.RDM_CurrentEXP = BitConverter.TryToInt32(source, MemoryHandler.Instance.Structures.CurrentPlayer.RDM_CurrentEXP);

                        #endregion

                        #region Base Stats

                        entry.BaseStrength = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.BaseStrength);
                        entry.BaseDexterity = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.BaseDexterity);
                        entry.BaseVitality = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.BaseVitality);
                        entry.BaseIntelligence = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.BaseIntelligence);
                        entry.BaseMind = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.BaseMind);
                        entry.BasePiety = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.BasePiety);

                        #endregion

                        #region Base Stats (base+gear+bonus)

                        entry.Strength = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Strength);
                        entry.Dexterity = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Dexterity);
                        entry.Vitality = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Vitality);
                        entry.Intelligence = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Intelligence);
                        entry.Mind = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Mind);
                        entry.Piety = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Piety);

                        #endregion

                        #region Basic Info

                        entry.HPMax = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.HPMax);
                        entry.MPMax = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.MPMax);
                        entry.TPMax = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.TPMax);
                        entry.GPMax = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.GPMax);
                        entry.CPMax = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.CPMax);

                        #endregion

                        #region Offensive Properties

                        entry.DirectHit = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.DirectHit);
                        entry.CriticalHitRate = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.CriticalHitRate);
                        entry.Determination = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Determination);

                        #endregion

                        #region Defensive Properties

                        entry.Tenacity = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Tenacity);
                        entry.Defense = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Defense);
                        entry.MagicDefense = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.MagicDefense);

                        #endregion

                        #region Phyiscal Properties

                        entry.AttackPower = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.AttackPower);
                        entry.SkillSpeed = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.SkillSpeed);

                        #endregion

                        #region Mental Properties

                        entry.SpellSpeed = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.SpellSpeed);
                        entry.AttackMagicPotency = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.AttackMagicPotency);
                        entry.HealingMagicPotency = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.HealingMagicPotency);

                        #endregion

                        #region Elemental Resistances

                        entry.FireResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.FireResistance);
                        entry.IceResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.IceResistance);
                        entry.WindResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.WindResistance);
                        entry.EarthResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.EarthResistance);
                        entry.LightningResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.LightningResistance);
                        entry.WaterResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.WaterResistance);

                        #endregion

                        #region Physical Resistances

                        entry.SlashingResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.SlashingResistance);
                        entry.PiercingResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.PiercingResistance);
                        entry.BluntResistance = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.BluntResistance);

                        #endregion

                        #region Crafting

                        entry.Craftmanship = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Craftmanship);
                        entry.Control = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Control);

                        #endregion

                        #region Gathering

                        entry.Gathering = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Gathering);
                        entry.Perception = BitConverter.TryToInt16(source, MemoryHandler.Instance.Structures.CurrentPlayer.Perception);

                        #endregion

                        break;
                }
            }
            catch (Exception ex) {
                MemoryHandler.Instance.RaiseException(Logger, ex, true);
            }

            return entry;
        }
    }
}
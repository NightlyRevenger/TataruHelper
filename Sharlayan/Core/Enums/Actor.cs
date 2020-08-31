// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com


// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Actor.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   Actor.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Core.Enums {
    public class Actor {
        public enum ActionStatus : byte {
            Unknown = 0x0,

            Idle = 0x01,

            Dead = 0x02,

            Sitting = 0x03,

            Mounted = 0x04,

            Crafting = 0x05,

            Gathering = 0x06,

            Melding = 0x07,

            SMachine = 0x08,
        }

        public enum EventObjectType : ushort {
            Unknown,

            BronzeTrap = 5478,

            SilverTreasureCoffer = 5479,

            CairnOfPassage = 11292,

            CairnOfReturn = 11297,

            GoldTreasureCoffer = 11500,

            Banded = 12347,

            Hoard = 12353,
        }

        public enum Icon : byte {
            None = 0x0,

            Yoshida = 0x1,

            GM = 0x2,

            SGM = 0x3,

            Clover = 0x4,

            DC = 0x5,

            Smiley = 0x6,

            RedCross = 0x9,

            GreyDC = 0xA,

            Processing = 0xB,

            Busy = 0xC,

            Duty = 0xD,

            ProcessingYellow = 0xE,

            ProcessingGrey = 0xF,

            Cutscene = 0x10,

            Away = 0x12,

            Sitting = 0x13,

            WrenchYellow = 0x14,

            Wrench = 0x15,

            Dice = 0x16,

            ProcessingGreen = 0x17,

            Sword = 0x18,

            AllianceLeader = 0x1A,

            AllianceBlueLeader = 0x1B,

            AllianceBlue = 0x1C,

            PartyLeader = 0x1D,

            PartyMember = 0x1E,

            DutyFinder = 0x18,

            Recruiting = 0x19,

            Sprout = 0x1F,

            Gil = 0x20,
        }

        public enum Job : byte {
            Unknown = 0x0,

            GLD = 0x1,

            PGL = 0x2,

            MRD = 0x3,

            LNC = 0x4,

            ARC = 0x5,

            CNJ = 0x6,

            THM = 0x7,

            CPT = 0x8,

            BSM = 0x9,

            ARM = 0xA,

            GSM = 0xB,

            LTW = 0xC,

            WVR = 0xD,

            ALC = 0xE,

            CUL = 0xF,

            MIN = 0x10,

            BTN = 0x11,

            FSH = 0x12,

            PLD = 0x13,

            MNK = 0x14,

            WAR = 0x15,

            DRG = 0x16,

            BRD = 0x17,

            WHM = 0x18,

            BLM = 0x19,

            ACN = 0x1A,

            SMN = 0x1B,

            SCH = 0x1C,

            ROG = 0x1D,

            NIN = 0x1E,

            MCH = 0x1F,

            DRK = 0x20,

            AST = 0x21,

            SAM = 0x22,

            RDM = 0x23,

            BLU = 0x24,

            GNB = 0x25,

            DNC = 0x26,
        }

        public enum Sex : byte {
            Male = 0x0,

            Female = 0x1,
        }

        public enum Status : byte {
            Unknown = 0x0,

            Claimed = 0x01,

            Idle = 0x02,

            Crafting = 0x05,

            UnknownUnSheathed = 0x06,

            UnknownSheathed = 0x07,
        }

        public enum TargetType : byte {
            Unknown = 0x0,

            Own = 0x1,

            True = 0x2,

            False = 0x4,
        }

        public enum Type : byte {
            Unknown = 0x0,

            PC = 0x01,

            Monster = 0x02,

            NPC = 0x03,

            TreasureCoffer = 0x04,

            Aetheryte = 0x05,

            Gathering = 0x06,

            EventObject = 0x07,

            Mount = 0x08,

            Minion = 0x09,
        }
    }
}
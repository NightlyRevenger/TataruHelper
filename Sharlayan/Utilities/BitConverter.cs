// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BitConverter.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   BitConverter.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using System;

    internal static class BitConverter {
        public static bool TryToBoolean(byte[] value, int index) {
            try {
                return System.BitConverter.ToBoolean(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static char TryToChar(byte[] value, int index) {
            try {
                return System.BitConverter.ToChar(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static double TryToDouble(byte[] value, int index) {
            try {
                return System.BitConverter.ToDouble(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static long TryToDoubleToInt64Bits(double value) {
            try {
                return System.BitConverter.DoubleToInt64Bits(value);
            }
            catch (Exception) {
                return default;
            }
        }

        public static short TryToInt16(byte[] value, int index) {
            try {
                return System.BitConverter.ToInt16(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static int TryToInt32(byte[] value, int index) {
            try {
                return System.BitConverter.ToInt32(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static long TryToInt64(byte[] value, int index) {
            try {
                return System.BitConverter.ToInt64(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static double TryToInt64BitsToDouble(long value) {
            try {
                return System.BitConverter.Int64BitsToDouble(value);
            }
            catch (Exception) {
                return default;
            }
        }

        public static float TryToSingle(byte[] value, int index) {
            try {
                return System.BitConverter.ToSingle(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static string TryToString(byte[] value, int index) {
            try {
                return System.BitConverter.ToString(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static ushort TryToUInt16(byte[] value, int index) {
            try {
                return System.BitConverter.ToUInt16(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static uint TryToUInt32(byte[] value, int index) {
            try {
                return System.BitConverter.ToUInt32(value, index);
            }
            catch (Exception) {
                return default;
            }
        }

        public static ulong TryToUInt64(byte[] value, int index) {
            try {
                return System.BitConverter.ToUInt64(value, index);
            }
            catch (Exception) {
                return default;
            }
        }
    }
}
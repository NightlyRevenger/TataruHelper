// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XMLCleaner.cs" company="SyndicatedLife">
//   Copyright© 2007 - 2020 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (https://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   XMLCleaner.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using System.Linq;
    using System.Text;

    public static class XMLCleaner {
        public static string SanitizeXmlString(string xValue) {
            if (xValue == null) {
                return string.Empty;
            }

            var buffer = new StringBuilder(xValue.Length);
            foreach (var xChar in xValue.Where(xChar => IsLegalXmlChar(xChar))) {
                buffer.Append(xChar);
            }

            return buffer.ToString();
        }

        private static bool IsLegalXmlChar(int xChar) {
            return xChar == 0x9 || xChar == 0xA || xChar == 0xD || xChar >= 0x20 && xChar <= 0xD7FF || xChar >= 0xE000 && xChar <= 0xFFFD || xChar >= 0x10000 && xChar <= 0x10FFFF;
        }
    }
}
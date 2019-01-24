// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonUtilities.cs" company="SyndicatedLife">
//   Copyright(c) 2018 Ryan Wilson &amp;lt;syndicated.life@gmail.com&amp;gt; (http://syndicated.life/)
//   Licensed under the MIT license. See LICENSE.md in the solution root for full license information.
// </copyright>
// <summary>
//   JsonUtilities.cs Implementation
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sharlayan.Utilities {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public static class JsonUtilities {
        public static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static T Deserialize<T>(string value) {
            return JsonConvert.DeserializeObject<T>(value, DefaultSerializerSettings);
        }

        public static string Serialize<T>(T value) {
            return JsonConvert.SerializeObject(value, Formatting.None, DefaultSerializerSettings);
        }
    }
}
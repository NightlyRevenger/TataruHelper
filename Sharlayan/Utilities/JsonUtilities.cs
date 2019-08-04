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
    using System;
    using System.IO;
    using System.Reflection;

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

        public static bool SaveStaticToJson(Type static_class, string filename)
        {
            try
            {
                FieldInfo[] fields = static_class.GetFields(BindingFlags.Static | BindingFlags.Public);
                object[,] a = new object[fields.Length, 2];
                int i = 0;
                foreach (FieldInfo field in fields)
                {
                    a[i, 0] = field.Name;
                    a[i, 1] = field.GetValue(null);
                    i++;
                }

                string output = JsonConvert.SerializeObject(a, Formatting.Indented);
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    sw.WriteLine(output);
                }
                return true;
            }
            catch (System.Exception e)
            {
                return false;
            }
        }

        public static bool LoadStaticFromJson(Type static_class, string filename)
        {
            try
            {
                FieldInfo[] fields = static_class.GetFields(BindingFlags.Static | BindingFlags.Public);
                object[,] a;

                a = JsonConvert.DeserializeObject<object[,]>(File.ReadAllText(filename));

                int i = 0;
                foreach (FieldInfo field in fields)
                {
                    if (field.Name == (a[i, 0] as string))
                    {
                        if (field.FieldType.Name.Contains("List"))
                        {

                            var filedVal = field.GetValue(null);

                            Type itemType = filedVal.GetType().GetProperty("Item").PropertyType;

                            var backUpList = Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(itemType), filedVal);

                            try
                            {
                                Type typeTest = a[i, 1].GetType();

                                var arr = (Newtonsoft.Json.Linq.JArray)(a[i, 1]);

                                int len = arr.Count;

                                filedVal = field.GetValue(null);

                                filedVal.GetType().GetMethod("Clear").Invoke(filedVal, null);

                                MethodInfo voidMethodInfo = filedVal.GetType().GetMethod("Add");

                                for (int j = 0; j < len; j++)
                                {
                                    object obj = Convert.ChangeType(arr[j], itemType);

                                    voidMethodInfo.Invoke(filedVal, new object[] { obj });
                                }
                            }
                            catch (Exception e)
                            {
                                filedVal = backUpList;
                            }

                        }
                        else
                            field.SetValue(null, Convert.ChangeType(a[i, 1], field.FieldType));
                    }
                    else
                    {
                        throw new ArgumentException("Wrong Settings File. Rolling to default");
                    }
                    i++;
                };

                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }
    }
}
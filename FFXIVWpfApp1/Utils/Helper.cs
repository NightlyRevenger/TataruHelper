// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.WinUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace FFXIVTataruHelper
{
    static class Helper
    {
        public static T LoadJsonData<T>(string path)
        {
            T result = (T)Activator.CreateInstance(typeof(T));

            try
            {
                using (TextReader reader = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));

                try
                {
                    using (TextWriter writer = new StreamWriter(path))
                    {
                        writer.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
                    }
                }
                catch (Exception e1)
                { Logger.WriteLog(Convert.ToString(e1)); }
            }

            return result;
        }

        public static void SaveJson(object obj, string path)
        {
            try
            {
                using (TextWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
                    writer.Flush();
                }
            }
            catch (Exception e)
            { Logger.WriteLog(Convert.ToString(e)); }
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
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
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

                            var backUpList = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType), filedVal);

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
                                Logger.WriteLog("Wrong Settings File. Rolling to partially default");
                                Logger.WriteLog(Convert.ToString(e));
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
                Logger.WriteLog(Convert.ToString(e));
                return false;
            }

        }

        public static T GetLast<T>(this IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            if (list.Count == 0)
                throw new ArgumentException(
                    "Cannot get last item because the list is empty");

            int lastIdx = list.Count - 1;
            return list[lastIdx];
        }

        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key)) return false;

            dict.Add(key, value);

            return true;
        }

        public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }

        public static Key RealKey(this KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.System:
                    return e.SystemKey;

                case Key.ImeProcessed:
                    return e.ImeProcessedKey;

                case Key.DeadCharProcessed:
                    return e.DeadCharProcessedKey;

                default:
                    return e.Key;
            }
        }

        public static void Unminimize(Window window)
        {
            var hwnd = (HwndSource.FromVisual(window) as HwndSource).Handle;
            Win32Interfaces.ShowWindow(hwnd, Win32Interfaces.ShowWindowCommands.Restore);
        }

        public static void Unminimize(IntPtr window)
        {
            var hwnd = window;
            Win32Interfaces.ShowWindow(hwnd, Win32Interfaces.ShowWindowCommands.Restore);
        }

        public static string ClearBlackListString(string text)
        {
            return text;
        }

        public static bool IsStringLettersEqual(string str1, string str2)
        {
            String onlyLetters1 = new String(str1.Where(Char.IsLetter).ToArray());
            String onlyLetters2 = new String(str2.Where(Char.IsLetter).ToArray());

            return onlyLetters1== onlyLetters2;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        public static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
        }
    }
}

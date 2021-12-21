// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com


namespace Sharlayan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sharlayan.Core;
    using Sharlayan.Models.ReadResults;

    public static partial class Reader
    {
        public static bool CanGetDialogPanel()
        {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.DialogPanelName);
            canRead = canRead && Scanner.Instance.Locations.ContainsKey(Signatures.DialogPanelText);

            if (canRead)
            {
                // OTHER STUFF?
            }

            return canRead;
        }

        public static bool CanGetCutScene()
        {
            var canRead = Scanner.Instance.Locations.ContainsKey(Signatures.CutsceneText1);
            canRead = canRead && Scanner.Instance.Locations.ContainsKey(Signatures.CutsceneTextLength);
            canRead = canRead && Scanner.Instance.Locations.ContainsKey(Signatures.CutsceneDetector);

            if (canRead)
            {
                // OTHER STUFF?
            }

            return canRead;
        }

        private static System.Collections.Concurrent.ConcurrentQueue<ChatLogItem> DialogPanelsLog = new System.Collections.Concurrent.ConcurrentQueue<ChatLogItem>();
        private static System.Collections.Concurrent.ConcurrentQueue<ChatLogItem> CutScenesLog = new System.Collections.Concurrent.ConcurrentQueue<ChatLogItem>();
        private static System.Collections.Concurrent.ConcurrentQueue<ChatLogItem> DirectDialogLog = new System.Collections.Concurrent.ConcurrentQueue<ChatLogItem>();

        public static ChatLogResult GetDirectDialog()
        {
            var result = new ChatLogResult();

            var dialogPanel = GetDialogPanel();

            var cutsceneText = GetCutsceneText();

            var dialogRepeat = CheckRepetition(DialogPanelsLog, dialogPanel);
            var cutsceneRepeat = CheckRepetition(CutScenesLog, cutsceneText);

            if (CheckChatEquality(dialogPanel, cutsceneText))
            {
                if (dialogPanel.Bytes != null && dialogRepeat == false)
                {
                    if (!CheckRepetition(DirectDialogLog, dialogPanel) && !dialogPanel.IsTextEmpty())
                        result.ChatLogItems.Add(dialogPanel);
                }
            }
            else
            {
                if (dialogPanel != null && dialogPanel.Bytes != null && dialogRepeat == false)
                {
                    if (!CheckRepetition(DirectDialogLog, dialogPanel) && !dialogPanel.IsTextEmpty())
                        result.ChatLogItems.Add(dialogPanel);
                }

                if (cutsceneText != null && cutsceneText.Bytes != null && cutsceneRepeat == false)
                {
                    if (!CheckRepetition(DirectDialogLog, cutsceneText) && !cutsceneText.IsTextEmpty())
                        result.ChatLogItems.Add(cutsceneText);
                }
            }

            return result;

        }

        public static ChatLogItem GetDialogPanel()
        {
            var result = new ChatLogItem();

            if (!CanGetDialogPanel() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            Int16 chatCode = 0x003d;
            byte colonBytes = Convert.ToByte(':');
            byte[] chatCodeBytes = BitConverter.GetBytes(chatCode);

            var dialogPanelNamePointer = (IntPtr)Scanner.Instance.Locations[Signatures.DialogPanelName];
            var dialogPanelNameLengthPointer = IntPtr.Subtract(dialogPanelNamePointer, MemoryHandler.Instance.Structures.DialogPanelPointers.LengtsOffset);

            var dialogPanelTextPointer = (IntPtr)Scanner.Instance.Locations[Signatures.DialogPanelText];
            var dialogPanelText = new IntPtr(MemoryHandler.Instance.GetPlatformUInt(dialogPanelTextPointer));

            var dialogPanelTextLegthPointer = IntPtr.Add(dialogPanelTextPointer, MemoryHandler.Instance.Structures.DialogPanelPointers.TextLengthOffset);

            int nameLength = (int)MemoryHandler.Instance.GetPlatformInt(dialogPanelNameLengthPointer);
            int textLength = (int)MemoryHandler.Instance.GetPlatformInt(dialogPanelTextLegthPointer);

            if (textLength > 1 && nameLength > 1)
            {
                if (textLength > 512)
                    textLength = 512;
                if (nameLength > 128)
                    nameLength = 128;

                byte[] npcNameBytes = MemoryHandler.Instance.GetByteArray(dialogPanelNamePointer, nameLength);
                byte[] textBytes = MemoryHandler.Instance.GetByteArray(dialogPanelText, textLength);

                nameLength = GetRealTextLength(ref npcNameBytes);
                textLength = GetRealTextLength(ref textBytes);

                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                byte[] unixTimestampBytes = BitConverter.GetBytes(unixTimestamp).ToArray();

                List<byte> rawBytesList = new List<byte>(unixTimestampBytes.Length + chatCodeBytes.Length + 1 +
                    npcNameBytes.Length + 1 + textBytes.Length);

                rawBytesList.AddRange(unixTimestampBytes);
                rawBytesList.AddRange(chatCodeBytes);
                rawBytesList.AddRange(new Byte[] { 0x00, 0x00 });
                rawBytesList.Add(colonBytes);
                rawBytesList.AddRange(npcNameBytes);
                rawBytesList.Add(colonBytes);
                rawBytesList.AddRange(textBytes);


                ChatLogItem chatLogItem = ChatEntry.Process(rawBytesList.ToArray());

                String onlyLettersLine = new String(chatLogItem.Line.Where(Char.IsLetter).ToArray());

                if (onlyLettersLine.Length > chatLogItem.Line.Length / GlobalSettings.LineLettersCoefficient)
                    result = chatLogItem;

            }

            return result;
        }

        public static ChatLogItem GetCutsceneText()
        {
            ChatLogItem result = new ChatLogItem();

            if (!CanGetCutScene() || !MemoryHandler.Instance.IsAttached)
            {
                return result;
            }

            Int16 chatCode = 0x0044;
            byte colonBytes = Convert.ToByte(':');
            byte dotBytes = Convert.ToByte('.');
            byte spaceBytes = Convert.ToByte(' ');
            byte[] chatCodeBytes = BitConverter.GetBytes(chatCode);

            try
            {
                var cutsceneTextPointer1 = (IntPtr)Scanner.Instance.Locations[Signatures.CutsceneText1];

                var cutsceneTextLengthPointer = (IntPtr)Scanner.Instance.Locations[Signatures.CutsceneTextLength];
                var cutsceneDetector = (IntPtr)Scanner.Instance.Locations[Signatures.CutsceneDetector];

                int textLength = (int)MemoryHandler.Instance.GetPlatformInt(cutsceneTextLengthPointer);
                int isCutscene = (int)MemoryHandler.Instance.GetPlatformInt(cutsceneDetector);

                if (textLength < 2 || isCutscene == 1)
                    return result;


                byte[] cutsceneBytesRaw1 = MemoryHandler.Instance.GetByteArray(cutsceneTextPointer1, 256);

                int textEnd1 = GetRealTextLength(ref cutsceneBytesRaw1);

                if (textEnd1 > 2)
                {
                    byte[] cutsceneBytes1 = cutsceneBytesRaw1;

                    Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    byte[] unixTimestampBytes = BitConverter.GetBytes(unixTimestamp).ToArray();


                    byte[] npcNameBytes = new byte[3];
                    npcNameBytes[0] = dotBytes;
                    npcNameBytes[1] = dotBytes;
                    npcNameBytes[2] = dotBytes;

                    List<byte> rawBytesList = new List<byte>(unixTimestampBytes.Length + chatCodeBytes.Length + 1 +
                        npcNameBytes.Length + 1 + cutsceneBytes1.Length);

                    rawBytesList.AddRange(unixTimestampBytes);
                    rawBytesList.AddRange(chatCodeBytes);
                    rawBytesList.AddRange(new Byte[] { 0x00, 0x00 });
                    rawBytesList.Add(colonBytes);
                    rawBytesList.AddRange(npcNameBytes);
                    rawBytesList.Add(colonBytes);
                    rawBytesList.AddRange(cutsceneBytes1);

                    ChatLogItem chatLogItem = ChatEntry.Process(rawBytesList.ToArray());

                    if (textEnd1 > 2)
                    {
                        String onlyLettersLine = new String(chatLogItem.Line.Where(Char.IsLetter).ToArray());

                        if (onlyLettersLine.Length > chatLogItem.Line.Length / GlobalSettings.LineLettersCoefficient)
                            result = chatLogItem;
                    }

                }

                if (result?.Line != null)
                {
                    if (result.Line.Contains("]") && result.Line.Contains("["))
                        return new ChatLogItem();
                }
            }
            catch (Exception)
            {
                result = new ChatLogItem();
            }

            return result;
        }

        public static bool CheckChatEquality(ChatLogItem item1, ChatLogItem item2)
        {
            if (item1 == null && item2 == null)
                return true;
            if (item1 == null || item2 == null)
                return false;
            if (item1.Bytes == null && item2.Bytes == null)
                return true;
            if (item1.Bytes == null || item2.Bytes == null)
                return false;

            string str1 = item1.Line.ToString();
            string str2 = item2.Line.ToString();

            if (item1.Line.Contains(":"))
                str1 = item1.Line.Substring(item1.Line.IndexOf(':'));

            if (item2.Line.Contains(":"))
                str2 = item2.Line.Substring(item2.Line.IndexOf(':'));

            String onlyLetters1 = new String(str1.Where(Char.IsLetter).ToArray());
            String onlyLetters2 = new String(str2.Where(Char.IsLetter).ToArray());

            return onlyLetters1 == onlyLetters2;
        }

        private static bool CheckRepetition(System.Collections.Concurrent.ConcurrentQueue<ChatLogItem> Log, ChatLogItem item)
        {
            if (item == null)
                return false;
            if (item.Bytes == null)
                return false;

            bool repetitonFlag = true;

            if (item.Line.Length > 1)
            {
                ChatLogItem previusChatLogItem = null;

                if (Log.TryPeek(out previusChatLogItem))
                {
                    if (!CheckChatEquality(previusChatLogItem, item))
                    {
                        while (Log.TryDequeue(out previusChatLogItem)) ;

                        repetitonFlag = false;

                        Log.Enqueue(item);
                    }
                }
                else
                {
                    Log.Enqueue(item);

                    repetitonFlag = false;
                }
            }

            return repetitonFlag;
        }

        private static int GetRealTextLength(ref byte[] byteArray)
        {
            int textEnd = 0;

            for (int i = 0; i < byteArray.Length; i++)
            {
                if (byteArray[i] == 0)
                {
                    textEnd = i + 1;
                    break;
                }
            }

            if (textEnd != 0 && textEnd <= byteArray.Length)
            {
                byte[] newArr = new byte[textEnd];
                Array.Copy(byteArray, newArr, newArr.Length);
                byteArray = newArr;
            }
            else
            {
                byteArray = new byte[0];
            }

            return textEnd;
        }

        private static bool IsTextEmpty(this ChatLogItem chatLogItem)
        {
            bool result = true;

            if (chatLogItem == null)
                return result;

            if (chatLogItem.Bytes == null || chatLogItem.Line == null)
                return result;

            int indexOf = chatLogItem.Line.IndexOf(':');

            if (indexOf < 0)
                return result;
            else
            {
                if (chatLogItem.Line.Length - 1 != indexOf)
                    result = false;
            }

            return result;
        }
    }
}

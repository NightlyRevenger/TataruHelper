// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System.Windows.Media;

namespace FFXIITataruHelper
{
    public enum MsgType : int { Translate = 1, Check = 2, Skip = 3 };

    public class ChatMsgType
    {
        [JsonProperty]
        public string ChatCode { get; private set; }

        [JsonProperty]
        public MsgType MsgType { get; set; }

        [JsonProperty]
        public string Name { get; private set; }

        [JsonProperty]
        public Color Color { get; set; }

        public ChatMsgType()
        {
            ChatCode = "";
            MsgType = MsgType.Check;
            Name = "";
            Color = Color.FromArgb(255, 255, 255, 255);
        }

        public ChatMsgType(string chatCode, MsgType msgType, string name, Color color)
        {
            ChatCode = chatCode;
            MsgType = msgType;
            Name = name;
            Color = color;
        }

        public ChatMsgType(ChatMsgType chatMsgType)
        {
            ChatCode = chatMsgType.ChatCode;
            MsgType = chatMsgType.MsgType;
            Name = chatMsgType.Name;
            Color = chatMsgType.Color;
        }
    }
}

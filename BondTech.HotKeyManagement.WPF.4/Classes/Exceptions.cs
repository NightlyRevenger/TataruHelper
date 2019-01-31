// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Runtime.Serialization;

namespace BondTech.HotKeyManagement.WPF._4
{
    [Serializable]
    public class HotKeyAlreadyRegisteredException : Exception
    {
        public GlobalHotKey HotKey { get; private set; }
        public LocalHotKey LocalKey { get; private set; }
        public ChordHotKey ChordKey { get; private set; }

        public HotKeyAlreadyRegisteredException(string message, GlobalHotKey hotKey) : base(message) { HotKey = hotKey; }
        public HotKeyAlreadyRegisteredException(string message, GlobalHotKey hotKey, Exception inner) : base(message, inner) { HotKey = hotKey; }

        public HotKeyAlreadyRegisteredException(string message, LocalHotKey hotKey) : base(message) { LocalKey = hotKey; }
        public HotKeyAlreadyRegisteredException(string message, LocalHotKey hotKey, Exception inner) : base(message, inner) { LocalKey = hotKey; }

        public HotKeyAlreadyRegisteredException(string message, ChordHotKey hotKey) : base(message) { ChordKey = hotKey; }
        public HotKeyAlreadyRegisteredException(string message, ChordHotKey hotKey, Exception inner) : base(message, inner) { ChordKey = hotKey; }
        protected HotKeyAlreadyRegisteredException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class HotKeyUnregistrationFailedException : Exception
    {
        public GlobalHotKey HotKey { get; private set; }
        public HotKeyUnregistrationFailedException(string message, GlobalHotKey hotKey) : base(message) { HotKey = hotKey; }
        public HotKeyUnregistrationFailedException(string message, GlobalHotKey hotKey, Exception inner) : base(message, inner) { HotKey = hotKey; }
        protected HotKeyUnregistrationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class HotKeyRegistrationFailedException : Exception
    {
        public GlobalHotKey HotKey { get; private set; }
        public HotKeyRegistrationFailedException(string message, GlobalHotKey hotKey) : base(message) { HotKey = hotKey; }
        public HotKeyRegistrationFailedException(string message, GlobalHotKey hotKey, Exception inner) : base(message, inner) { HotKey = hotKey; }
        protected HotKeyRegistrationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class HotKeyInvalidNameException : Exception
    {
        public HotKeyInvalidNameException(string message) : base(message) { }
        public HotKeyInvalidNameException(string message, Exception inner) : base(message, inner) { }
        protected HotKeyInvalidNameException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}

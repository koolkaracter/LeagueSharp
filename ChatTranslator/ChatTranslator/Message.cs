using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;

namespace ChatTranslator
{
    internal class Message
    {
        public string translated;
        public string original;
        public Obj_AI_Base sender;

        public Message(string translated, Obj_AI_Base sender, string original)
        {
            this.translated = translated;
            this.sender = sender;
            this.original = original;
        }
    }
}
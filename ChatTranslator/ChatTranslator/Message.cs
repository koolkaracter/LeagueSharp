using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;

namespace ChatTranslator
{
    class Message
    {
        public string message;
        public Obj_AI_Base sender;

        public Message(string message, Obj_AI_Base sender)
        {
            this.message = message;
            this.sender = sender;
        }
    }
}

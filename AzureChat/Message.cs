using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureChat
{
    class Message
    {
        public String User { get; private set; }
        public String Text { get; private set; }

        public Message (String text, String user)
        {
            this.Text = text;
            this.User = user;
        }

        public Message (String text) : this (text, "(anonymous)")
        {

        }
    }
}

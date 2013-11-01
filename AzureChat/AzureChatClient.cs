using AzureBusClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureChat
{
    /// <summary>
    /// Allows bidirectional communication to the Azure Chat
    /// </summary>
    class AzureChatClient
    {
        protected ServiceBusClient listener;
        protected ServiceBusClient publisher;

        public AzureChatClient ()
        {
            // listener = new ServiceBusClient();
            // publisher = new ServiceBusClient();
        }
    }
}

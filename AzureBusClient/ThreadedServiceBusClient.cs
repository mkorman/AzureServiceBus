using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureBusClient
{
    /// <summary>
    /// Sends and receives messages to and from an Azure Bus
    /// Can start a thread and raise an event whenever a message is received
    /// </summary>
    public class ThreadedServiceBusClient <T> : ServiceBusClient
    {
        private Thread listenerThread = null;
        private bool stopThread = false;
        public event Action<T> MessageReceived;

        public ThreadedServiceBusClient (string connectionString, string topicName, string subscriptionName)
            : base(connectionString, topicName, subscriptionName)
        {

        }

        public ThreadedServiceBusClient(string topicName, string subscriptionName)
            : base(topicName, subscriptionName)
        {

        }

        public void StartListenerThread ()
        {
            listenerThread = new Thread(new ThreadStart (this.ListenForMessage));
            listenerThread.Start();
        }


        private void ListenForMessage()
        {
            while (!stopThread)
            {
                T message = Listen<T>(1);

                if (message != null)
                {
                    OnMessageReceived(message);
                }
            }
        }

        /// <summary>
        /// Listens for numMessages consecutive messages
        /// </summary>
        /// <param name="numMessages">Number of messages to listen to</param>
        /// <returns>The last received message</returns>
        public override T Listen<T>(int numMessages)
        {
            int messages = numMessages;
            T messageBody = default(T);

            // Continuously process messages received from the HighMessages subscription 
            while (messages > 0)
            {
                BrokeredMessage message = SubClient.Receive(TimeSpan.FromSeconds(15));

                messages--;

                if (message != null)
                {
                    try
                    {
                        messageBody = message.GetBody<T>();
                        // Remove message from subscription
                        message.Complete();
                    }
                    catch (Exception)
                    {
                        // Indicate a problem, unlock message in subscription
                        message.Abandon();
                    }
                }
            }
            return messageBody;
        }

        public void StopListenerThread()
        {
            if (listenerThread != null)
            {
                stopThread = true;
                listenerThread.Join();
                listenerThread = null;
            }
        }

        protected virtual void OnMessageReceived (T message)
        {
            if (MessageReceived !=null)
            {
                MessageReceived(message);
            }
        }
    }
}

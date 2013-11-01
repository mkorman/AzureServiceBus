using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;

namespace AzureBusClient
{
    /// <summary>
    /// Sends and receives messages to/from an Azure Service Bus
    /// </summary>
    public class ServiceBusClient : IDisposable
    {
        /// <summary>
        /// Name of the subscription to be used to listen to messages in the topic
        /// </summary>
        protected const string DEFAULT_SUBSCRIPTION_NAME = "AllMessages";
        
        /// <summary>
        /// Manager object to manage the bus
        /// </summary>
        protected NamespaceManager namespaceManager;

        /// <summary>
        /// Connection string to the Azure Bus itself
        /// </summary>
        public readonly string ConnectionString;
        
        /// <summary>
        /// Gets he name of the topic in the bus
        /// </summary>
        public string TopicName { get; private set; }

        /// <summary>
        /// Gets the name of the subscription in the bus
        /// </summary>
        public string SubscriptionName { get; private set; }

        /// <summary>
        /// An incremental ID to be used in messages
        /// </summary>
        public static int messageId = 0;

        /// <summary>
        /// Gets the topic client
        /// </summary>
        public TopicClient Client { get; private set; }

        /// <summary>
        /// Gets the subscription client
        /// </summary>
        public SubscriptionClient SubClient { get; private set; }

        public ServiceBusClient(string connectionString, string topicName, string subscriptionName)
        {
            if (string.IsNullOrEmpty(subscriptionName))
            {
                subscriptionName = DEFAULT_SUBSCRIPTION_NAME;
            }

            this.TopicName = topicName;
            this.SubscriptionName = subscriptionName;
            this.ConnectionString = connectionString;

            // Create namespace manager
            namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            // Connect to the bus and subscribe to topic
            Connect();
        }


        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="topicName">The name of the topic to subscribe to</param>
        public ServiceBusClient(string topicName, string subscriptionName)
            : this(CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString"), topicName, subscriptionName)
        {
        }

        /// <summary>
        /// Connects to service bus and subscribes to the topic
        /// </summary>
        protected void Connect ()
        {
            CreateTopic(TopicName);
            CreateSubscription(TopicName, SubscriptionName);

            // TODO: lazy load this?
            Client = TopicClient.CreateFromConnectionString(ConnectionString, TopicName);

            // TODO: lazy load this?
            SubClient = SubscriptionClient.CreateFromConnectionString(ConnectionString, TopicName, SubscriptionName);
        }

        /// <summary>
        /// Creates a topic in the service bus, if it does not exist
        /// </summary>
        /// <param name="topicName">The name of the topic to create</param>
        protected void CreateTopic (string topicName)
        {
            // Create topic
            if (namespaceManager.TopicExists(topicName))
            {
                Console.WriteLine(string.Format("Topic {0} already exists", topicName));
            }
            else
            {
                // Configure Topic Settings
                var topicDescription = new TopicDescription(topicName);
                topicDescription.MaxSizeInMegabytes = 5120;
                topicDescription.DefaultMessageTimeToLive = new TimeSpan(0, 0, 10);
                namespaceManager.CreateTopic(topicDescription);
                Console.WriteLine(string.Format("Topic {0} created!", topicName));
            }
        }

        /// <summary>
        /// Creates a subscription to a topic, if it does not exist
        /// </summary>
        /// <param name="topicName">The name of the topic to subscribe to</param>
        /// <param name="subscriptionName">The name of the subscription to create</param>
        protected void CreateSubscription (string topicName, string subscriptionName)
        {
            if (namespaceManager.SubscriptionExists(topicName, subscriptionName))
            {
                Console.WriteLine(string.Format ("Subscription {0} already exists", subscriptionName));
            }
            else
            {
                namespaceManager.CreateSubscription(topicName, subscriptionName, new TrueFilter ());
                Console.WriteLine(string.Format("Subscription {0} created!", subscriptionName));
            }
        }

        public virtual void SendMessage (object message)
        {
            var busMessage = new BrokeredMessage(message);
            busMessage.Properties["MessageNumber"] = messageId++;

            Client.Send(busMessage);
            Console.WriteLine("Sent");
        }

        /// <summary>
        /// Listens indefinitely for messages until the user supplies a blank line
        /// </summary>
        public virtual void Listen<T>()
        {
            bool finish = false;

            Console.WriteLine("Listening...");
            while (!finish)
            {
                var message = Listen<T>(1);
                if (message == null || message.Equals(default(T)))
                {
                    finish = true;
                }
            }
            Console.WriteLine("Done");
        }

        /// <summary>
        /// Listens for numMessages consecutive messages
        /// </summary>
        /// <param name="numMessages">Number of messages to listen to</param>
        /// <returns>The last received message</returns>
        public virtual T Listen <T>(int numMessages)
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
                        Console.WriteLine(string.Format("Message {0}: {1}" , message.Properties["MessageNumber"], messageBody));
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

        public void Close ()
        {
            SubClient.Close();
            Client.Close();
        }


        public void Dispose ()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose (bool disposing)
        {
            if (disposing)
            {
                // Get rid of managed resources
                Close();
            }
            // Get rid of unmanaged resources
        }
    }
}

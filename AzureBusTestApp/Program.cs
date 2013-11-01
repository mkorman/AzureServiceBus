using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureBusClient
{
    public class Program
    {
        protected string TopicName;
        protected string SubscriptionName;

        /// <summary>
        /// Creates a client to listen to N messages
        /// </summary>
        /// <param name="numMessages">The number of messages to listen to</param>
        public void Listen (int numMessages)
        {
            var listener = new ServiceBusClient(TopicName, SubscriptionName);
            listener.Listen<string>(numMessages);
            Console.WriteLine("Done");
        }

        /// <summary>
        /// Creates a client and starts a new thread to listen to messages indefinitely, until the user sends a blank line
        /// </summary>
        public Thread Listen ()
        {
            var listener = new ServiceBusClient(TopicName, SubscriptionName);
            var thread = new Thread(new ThreadStart (listener.Listen<string>));
            thread.IsBackground = true;
            thread.Start();
            return thread;
        }

        /// <summary>
        /// Runs in demo mode, creating a client and sending 4 messages to the bus
        /// </summary>
        public void Demo ()
        {
            var Publisher = new ServiceBusClient(TopicName, SubscriptionName);
            var thread1 = Listen();
            this.SubscriptionName += "Second";
            var thread2 = Listen();

            Publisher.SendMessage("Hello");
            Publisher.SendMessage("This is my first Azure service bus message");
            Publisher.SendMessage("Hope you liked it");
            Publisher.SendMessage("Bye!");
            Publisher.SendMessage(string.Empty);

            // Cleanup
            Publisher.Dispose();
            thread1.Join();
            thread2.Join();
        }

        public ThreadedServiceBusClient<string> ListenThreaded()
        {
            var listener = new ThreadedServiceBusClient<string>(TopicName, SubscriptionName);
            listener.MessageReceived += PrintMessage;
            listener.StartListenerThread();
            return listener;
        }

        /// <summary>
        /// Runs in demo mode, creating a client and sending 4 messages to the bus
        /// </summary>
        public void DemoThreaded()
        {
            var Publisher = new ServiceBusClient(TopicName, SubscriptionName);
            var thread1 = ListenThreaded();
            this.SubscriptionName += "Second";
            var thread2 = ListenThreaded();

            Publisher.SendMessage("Hello");
            Publisher.SendMessage("This is my first Azure service bus message");
            Publisher.SendMessage("Hope you liked it");
            Publisher.SendMessage("Bye!");
            Publisher.SendMessage(string.Empty);

            // Cleanup
            Publisher.Dispose();
            thread1.StopListenerThread();
            thread1.StopListenerThread();
        }

        public void PrintMessage (object msg)
        {
            Console.WriteLine("Message: " + msg);
        }

        public void Publish ()
        {
            var publisher = new ServiceBusClient(TopicName, SubscriptionName);

            Console.WriteLine("Please talk to me");
            bool finish = false;

            while (!finish)
            {
                Console.Write("> ");
                string message = Console.ReadLine();
                if (string.IsNullOrEmpty(message))
                {
                    message = null;
                    finish = true;
                }
                publisher.SendMessage(message);
            }

            publisher.Close();
        }

        static void ShowUsage ()
        {
            Console.WriteLine();
            Console.WriteLine(string.Format("USAGE: {0} <mode> <topic> <subscription>", Process.GetCurrentProcess().ProcessName));
            Console.WriteLine();
            Console.WriteLine("  Available modes:");
            Console.WriteLine("    -l   Listen");
            Console.WriteLine("    -w   Write");
            Console.WriteLine("    -d   Demo");
            Console.WriteLine("  <topic>: The name of the service bus topic");
            Console.WriteLine("  <subscription>: The name of the service bus subscription. Use listenerss with different subscriptions for multicast, and same subscription for round-robin behaviour");
            Environment.Exit (1);
        }
        
        protected void ParseArgs (string [] args)
        {
            if (args.Length < 3)
            {
                ShowUsage();
            }
            
            TopicName = args[1];
            SubscriptionName = args[2];
            
            // Choose mode according to arg[0]
            if (args[0] == "-d")
            {
                DemoThreaded();
            }
            else if (args[0] == "-l")
            {
                Listen();
            }
            else if (args[0] == "-w")
            {
                Publish();
            }
            else
            {
                ShowUsage();
            }
        }


        static void Main(string[] args)
        {
            var program = new Program();
            program.ParseArgs(args);
            Console.ReadLine();
        }
    }
}

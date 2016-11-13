using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TwitchBot
{
    public static class ReceiveThread
    {
// Don't like having to do this, but meh
#pragma warning disable 0649
        struct handler {
            public string HelpText;
            public CommandAPI.CommandHandler cmdhandler;
            public bool modOnly;
        };
#pragma warning restore 0649

        private static IRC _irc;
        private static Dictionary<string, handler> handlers = new Dictionary<string, handler>();

        private static void HelpHandler(string username, string command, string message)
        {
            if (message == "")
            {
                string helptext = "Available commands: ";
                foreach (KeyValuePair<string, handler> kvp in handlers)
                {
                    if (!kvp.Value.modOnly || (kvp.Value.modOnly && CommandAPI.isMod(username)))
                        helptext += String.Format("!{0} ", kvp.Key);
                }
                SendThread.AddMessage(helptext);
                helptext = " Type !help <command> to get help text for a specific command";
                SendThread.AddMessage(helptext);
            } else
            {
                string helptext;
                if (handlers.ContainsKey(message))
                {
                    helptext = String.Format("@{0} !{1} - {2}", username, message, handlers[message].HelpText);
                } else
                {
                    helptext = String.Format("That command doesn't exist @{0}", username);
                }
                SendThread.AddMessage(helptext);
            }
        }

        public static void SetupDefaultHandlers()
        {
            _irc.SetEventHandler(ChatHandler);
            AddHandler("help", "Displays the list of commands", HelpHandler, false);
        }

        public static void ThreadStart()
        {
            try
            {
                while (true)
                {
                    // Ask the IRC class to process events
                    _irc.ProcessEvents();

                    // Allow the OS to put us to sleep as we have no more events to process
                    Thread.Sleep(5);
                }
            }
            catch (ThreadInterruptedException e)
            {
                // Supresses compiler warning
                e.ToString();
                return;
            }
        }

        public static void ChatHandler(string username, string message)
        {
            // Check to see if we were thanked
            // NOTE: This code will be revised if a ban-list is implemented
            //       because it's stupidly ugly
            if (!message.StartsWith("!") && message.ToLower().Contains("thanks " + _irc.getUsername()))
            {
                SendThread.AddMessage(String.Format("You're welcome, @{0}!", username));
            }

            // Check the message starts with !
            if (!message.StartsWith("!"))
                return;

            // If it does, strip it and call the appropriate command handler

            // Split the string
            string[] split = message.Split(new char[] { ' ' }, 2);
            split[0] = split[0].Substring(1);
            split[0] = split[0].ToLower();


            // Call the appropriate handler if it exists
            if (handlers.ContainsKey(split[0]))
            {
                string command, param;

                command = split[0];
                if (split.Length == 1)
                    param = "";
                else
                    param = split[1];

                handlers[command].cmdhandler.Invoke(username, command, param);
            }
        }

        public static void SetIRC(IRC irc)
        {
            _irc = irc;
        }

        public static void AddHandler(string command, string HelpText, CommandAPI.CommandHandler handler, bool modOnly)
        {
            handler newhandler;
            newhandler.cmdhandler = handler;
            newhandler.HelpText = HelpText;
            newhandler.modOnly = modOnly;

            handlers.Add(command.ToLower(), newhandler);
        }
    }
}

using System;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace TwitchBot
{
    static class Program
    {
// Don't like having to do this, but meh
#pragma warning disable 0649
        struct handler
        {
            public string HelpText;
            public CommandAPI.ConsoleHandler cmdhandler;
        };
#pragma warning restore 0649
        private static Dictionary<string, handler> handlers = new Dictionary<string, handler>();

        public static void AddConsoleHandler(string command, string HelpText, CommandAPI.ConsoleHandler handler)
        {
            // Add the handler to the list
            handler consolehandler;
            consolehandler.cmdhandler = handler;
            consolehandler.HelpText = HelpText;

            handlers.Add(command.ToLower(), consolehandler);
        }

        public static void HelpHandler(string command, string text)
        {
            if (text == "")
            {
                Console.WriteLine("quit - Closes TwitchBot");
                Console.WriteLine("quitsilent - Closes TwitchBot without saying goodnight to chat");
                foreach (KeyValuePair<string, handler> kvp in handlers)
                {
                    Console.WriteLine("{0} - {1}", kvp.Key, kvp.Value.HelpText);
                }
            } else
            {
                if (handlers.ContainsKey(text.ToLower()))
                {
                    Console.WriteLine("{0} - {1}", text.ToLower(), handlers[text].HelpText);
                } else
                {
                    Console.WriteLine("That command does not exist, type \"help\" to see all available commands");
                }
            }
        }

        static bool getSetting(iniParser ini, string section, string name, ref string value, string def = "")
        {
            if (!ini.hasValue(section, name))
            {
                value = def;
                return false;
            } else
            {
                value = ini.getValue(section, name);
                return true;
            }
        }

        public static Thread s;
        public static void StartSend()
        {
            s = new Thread(new ThreadStart(SendThread.ThreadStart));
            s.Start();
        }

        static void Main(string[] args)
        {
            #region Read configuration
            // Read the configuration file
            string server = "", username = "", channel = "", password = "", _port = "";
            int port;
            iniParser ini = new iniParser("TwitchBot.ini");

            getSetting(ini, "IRC", "server", ref server, "irc.twitch.tv");
            getSetting(ini, "IRC", "port", ref _port, "6667");

            try
            {
                port = Int32.Parse(_port);
            } catch (Exception e)
            {
                // Surpesses compiler warning
                e.ToString();

                // Output error
                Console.WriteLine("Invalid port specified in TwitchBot.ini - cannot continue.\nPress enter to quit");
                Console.ReadLine();
                return;
            }

            if (!getSetting(ini, "IRC", "username", ref username))
            {
                Console.WriteLine("Missing username from TwitchBot.ini - cannot continue.\nPress enter to quit");
                Console.ReadLine();
                return;
            }

            if (!getSetting(ini, "IRC", "password", ref password))
            {
                Console.WriteLine("Missing password from TwitchBot.ini - cannot continue.\nPress enter to quit");
                Console.ReadLine();
                return;
            }

            if (!getSetting(ini, "IRC", "channel", ref channel, "#" + username))
            {
                Console.WriteLine("Missing channel from TwitchBot.ini - defaulting to #{0}", username);
            }

            // Get the moderator list
            CommandAPI.mods = File.ReadAllLines("data/moderators.txt");
            #endregion

            // Add the help command to the command list before processing scripts, so it's displayed after the quit command
            AddConsoleHandler("help", "Displays this help screen", HelpHandler);

            #region Compile and setup all scripts
            // Compile all the command scripts and add their commands
            string[] files = Directory.GetFiles("CommandScripts/");
            foreach (string f in files)
            {
                // Read the entire file
                string code = File.ReadAllText(f);

                // Compile it
                Assembly assembly = Script.CompileCode(f, code);

                if (assembly == null)
                {
                    Console.WriteLine("Warning: " + f + " failed to compile");
                    Console.ReadLine();
                    continue;
                }

                // Get an instance of the Command and call it's setup routine
                Command command = Script.GetCommand(assembly);
                command.setup();
            }
            #endregion

            #region Start IRC threads
            // Start the IRC client
            IRC irc = new IRC();
            bool success = irc.Connect(server, port, channel, username, password);
            if (!success)
            {
                Console.ReadLine();
                return;
            }

            // Start the sending thread
            SendThread.SetIRC(irc);
            StartSend();

            // Start the processing thread
            ReceiveThread.SetIRC(irc);
            ReceiveThread.SetupDefaultHandlers();
            Thread r = new Thread(new ThreadStart(ReceiveThread.ThreadStart));
            r.Start();
            #endregion

            #region Console interface
            // Do some waiting or some shit, maybe a console based config interface. Idk it could be anything.
            bool running = true;
            Console.Write("Welcome to TwitchBot\n\n");
            while (running)
            {
                Console.Write("> ");
                string line = Console.ReadLine();

                // Quick check to see if quit was called
                if (line.ToLower().Equals("quit"))
                {
                    running = false;
                    SendThread.AddMessage("Goodnight, chat!");
                    continue;
                }

                if (line.ToLower().Equals("quitsilent"))
                {
                    running = false;
                    continue;
                }

                // Check to see if a blank line was written
                if (line.Equals(""))
                    continue;

                // Now we can process the command correctly

                // Split the string in 2 to get the command and parameters seperately
                string[] split = line.Split(new char[] { ' ' }, 2);
                string command, param;
                command = split[0];
                if (split.Length == 1)
                    param = "";
                else
                    param = split[1];

                // Call the handler, if it exists, or print an error otherwise
                if (handlers.ContainsKey(command))
                {
                    handlers[command].cmdhandler.Invoke(command, param);
                } else
                {
                    Console.WriteLine("That command does not exist");
                }

                Console.WriteLine();
            }
            #endregion

            // Quit was requested, so stop the threads and disconnect

            #region Stop IRC threads and disconnect

            // Sleep for half a second to catch up with chat
            Thread.Sleep(500);

            // Stop the ReceiveThread and SendThread
            r.Interrupt();
            s.Interrupt();

            r.Join();
            s.Join();

            // Disconnect from the IRC server
            irc.Disconnect();
            #endregion
        }

        static void Test()
        {
            DateTime now = DateTime.MinValue;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBot
{
    public static class CommandAPI
    {
        public delegate void CommandHandler(string username, string command, string message);
        public static void AddChatHandler(string command, string HelpText, CommandHandler handler, bool modOnly = false)
        {
            ReceiveThread.AddHandler(command, HelpText, handler, modOnly);
        }

        public delegate void ConsoleHandler(string command, string param);
        public static void AddConsoleHandler(string command, string HelpText, ConsoleHandler handler)
        {
            Program.AddConsoleHandler(command, HelpText, handler);
        }

        public static void SendMessage(string message)
        {
            SendThread.AddMessage(message);
        }


        public static string[] mods;
        public static bool isMod(string username)
        {
            // Ensure the user is a mod
            foreach (String s in mods)
            {
                if (s.Equals(username))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

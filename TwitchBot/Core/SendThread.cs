using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TwitchBot
{
    public static class SendThread
    {
        private static List<string> _sendlist = new List<string>();
        private static IRC _irc;
        private static object _sendlock = new object();

        public static void ThreadStart()
        {
            /* 95 messages per 30000ms (30s)
             * Twitch moderators are allowed to send up to 100 messages through
             * IRC per 30 seconds, so 5 are taken off in case of mis-timing
             */
            float timeout = 1.0f/(95.0f/30000.0f);

            try
            {
                while (true)
                {
                    // Track whether a message is available or not
                    bool hasmsg = false;
                    string message = "";

                    // Lock the sendlist and check for a message
                    lock (_sendlock)
                    {
                        if (_sendlist.Count > 0)
                        {
                            hasmsg = true;
                            message = _sendlist[0];
                            _sendlist.RemoveAt(0);
                        }
                    }

                    // If a message was available, send it
                    if (hasmsg)
                    {
                        _irc.SendChat(message);
                    }

                    // If we sent a message, wait until we're allowed to send another message
                    // Otherwise wait until the OS gives us another time slot
                    if (hasmsg)
                        Thread.Sleep((int)timeout);
                    else
                        Thread.Sleep(5);
                }
            } catch (ThreadInterruptedException e)
            {
                // Supresses compiler warning
                e.ToString();
                return;
            }
        }

        public static void AddMessage(string message)
        {
            lock (_sendlock)
            {
                _sendlist.Add(message);
            }
        }

        public static void SetIRC(IRC irc)
        {
            _irc = irc;
        }
    }
}

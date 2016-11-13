using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetIO;

using System.Text;
using System.Collections.Generic;

namespace TwitchBot
{
    public class IRC : IDisposable
    {
        private Stream _network;
        StreamReader netreader;
        private NetWriter netwriter;
        private string _username, _channel;
        private TcpClient _client;
        private object _writerlock = new object();

        private string _server, _password;
        private int _port;

        public bool Connect(string server, int port, string channel, string username, string password)
        {
            try {
                // Set internal variables
                _username = username;
                _channel = channel;
                _server = server;
                _password = password;
                _port = port;

                // Connect to IRC server
                _client = new TcpClient(server, port);
                _network = _client.GetStream();
                _network.ReadTimeout = 100;
                netwriter = new NetWriter(_network);
                netreader = new StreamReader(_network);

                // Start IRC auth
                string passmsg = String.Format("PASS {0}\r\n", password);
                netwriter.writeStringRaw(passmsg);

                string nickmsg = String.Format("NICK {0}\r\n", _username);
                netwriter.writeStringRaw(nickmsg);

                string usermsg = String.Format("USER {0} 8 * :{0}\r\n", _username);
                netwriter.writeStringRaw(usermsg);

                // Join the channel
                string joinmsg = String.Format("JOIN :{0}\r\n", _channel);
                netwriter.writeStringRaw(joinmsg);

                // Return success
                return true;
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public void SendChat(string message)
        {
            lock (_writerlock)
            {
                string privmsg = String.Format("PRIVMSG {0} :{1}\r\n", _channel, message);
                netwriter.writeStringRaw(privmsg);
                //netwriter.WriteLine(privmsg);
            }
        }

        public void Disconnect()
        {
            string partmsg = String.Format("PART {0} :Quitting Twibot\r\n", _channel);
            netwriter.writeStringRaw(partmsg);
            //netwriter.Write(partmsg);

            string quitmsg = "QUIT :Quitting Twibot\r\n";
            netwriter.writeStringRaw(quitmsg);
            //netwriter.Write(quitmsg);

            _network.Flush();
            _client.Close();
        }

        // Processes incoming messages from the server and calls the event handler when a chat message is received
        public delegate void EventHandler(string sender, string message);
        private event EventHandler EventHandlerEvent;
        public void SetEventHandler(EventHandler handler)
        {
            EventHandlerEvent = handler;
        }

        public void ProcessEvents()
        {
            try {
                
                _network.ReadTimeout = 100;

                while (true)
                {
                    string line = netreader.ReadLine();

                    // Identify the message type (we deal with only PING and PRIVMSG, so this implementation is OK for now)

                    // If this is a PRIVMSG, start handling it
                    if (line.Contains("PRIVMSG"))
                    {
                        string username, message;

                        // Tokenize the line
                        string[] tokens = line.Split(new char[] { ' ' }, 4);

                        // Get the username
                        int length = tokens[0].IndexOf('!') - 1;
                        username = tokens[0].Substring(1, length);

                        // Check the channel is equal to the channel we're configured to
                        if (!tokens[2].Equals(_channel))
                            continue;

                        // Get the message
                        message = tokens[3].Substring(1);

                        // Print out to the console
                        //Console.WriteLine("{0}: {1}", username, message);

                        // Call the event handler
                        EventHandlerEvent.Invoke(username, message);

                        // Skip checking for other message types
                        continue;
                    }

                    // If this is a PING, start handling it
                    if (line.Contains("PING"))
                    {
                        // Strip the server ID if present
                        if (line.StartsWith(":"))
                        {
                            // Take out the server ID (really horrible code, sorry!)
                            line = line.Split(new char[] { ' ' }, 2)[1];
                        }

                        // Replace PING with PONG and send the line back
                        line.Replace("PING", "PONG");
                        line += "\r\n";

                        lock (_writerlock)
                        {
                            netwriter.writeStringRaw(line);
                        }

                        continue;
                    }

                    // Otherwise we don't need to do anything
                }
            } catch (IOException e)
            {
                // Suppress compiler warning
                e.ToString();

                if (_client.Connected == false)
                {
                    Console.Write("Connection to Twitch IRC lost, some responses may be lost. Attempting to reconnect...");

                    // Stop the send thread
                    Program.s.Interrupt();
                    Program.s.Join();

                    // Reconnect
                    if (!Connect(_server, _port, _channel, _username, _password))
                    {
                        Console.WriteLine("Failed to reconnect, retrying in 1 second");
                        Thread.Sleep(1000);
                    } else
                    {
                        Console.WriteLine("Successfully reconnected");
                        Program.StartSend();
                    }
                }
                return;
            }
        }

        public string getUsername()
        {
            return _username;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                netreader.Close();
                _client.Close();
            }
        }
    }
}

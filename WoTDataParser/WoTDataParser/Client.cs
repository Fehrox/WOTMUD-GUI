using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

namespace WoTConsoleClient {

    /// <summary>
    /// Binds to WOTMUD server and handles communications.
    /// </summary>
    class Client {

        static Socket s;

        static void Main(string[] args) {
            //Console.WriteLine("Press any key to attempt to connect to session");
            //Debugger.Break();

            if(ConnectToServer("www.wotmud.org", 2222)){
                Thread send = new Thread(new ThreadStart(Send));
                Thread recieve = new Thread(new ThreadStart(Recieve));

                send.Start();
                recieve.Start();
                Login();
            }
        }

        /// <summary>
        /// Reads input from the user and sends it on to the server.
        /// </summary>
        private static void Send() {
            while (true) {
                string message = Console.ReadLine() + "\n";
                //TEMP: Make sure that exits is run with look for valid Place formatting.
                if (message == "look\n" || message == "n\n" || message == "e\n" || message == "s\n" || message == "w\n")  message += "exits\n";
                if (message == "SaveWorld\n") WoTParser.World.Persistance.SaveWorld();
                SendMessage(message);
            }
        }

        private static void SendMessage(string message) {
            s.Send( Encoding.ASCII.GetBytes(message) );
        }

        /// <summary>
        /// Recieves messages from telnet host and displays them to the console.
        /// </summary>
        /// <param name="s"></param>
        private static void Recieve() {

            string lastLine = "", currentLine = "";
            int recievedBytes;
            do {
                //Get and decode server's message.
                byte[] servedByte = new byte[1];
                recievedBytes = s.Receive(servedByte);
                char recievedChar = Encoding.ASCII.GetString(servedByte)[0];

                //Handle messages from server one line at a time
                if (recievedChar == '\n') {
                    lastLine = currentLine;
                    currentLine = "";
                    //TODO: Replace this direct call with an event, 
                    //      and subscirbe to it from within Interpreter.
                    WoTParser.Interpreter.InterpretLine(lastLine);
                    //Debugger.Log(lastLine);
                } else {
                    currentLine += recievedChar;
                }
            } while (recievedBytes > 0);
        }

        /// <summary>
        /// Performs a connection to telnet server.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static bool ConnectToServer(string server, int port) {
            Debugger.Log("Connecting....");
            try {
                IPEndPoint endPoint = new IPEndPoint(Dns.GetHostAddresses(server)[0], port);
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Object connectingLock = new Object();
                while (!s.Connected) {
                    //Only one connection attempt at a time.
                    try {
                        s.Connect(endPoint);
                    } catch (SocketException se) {
                        Debugger.Log(se.Message, ConsoleColor.Red);
                    }
                }
                Debugger.Log("Connected to " + endPoint);
                return true;
            } catch (Exception e){
                Debugger.Log(e.Message, ConsoleColor.Red);
                Debugger.Break();
                return false;
            }
        }

        //Clean up after running
        ~Client() {
            s.Close();//Close connection
        }
        #region hide
        private static void Login() {
            //SendMessage("Seb\ncsviper88\n");
            SendMessage("Lycan\np4ssw0rdlol\n");
            Init();
        }
        #endregion

        private static void Init() {
            SendMessage("color complete");
            ReSync();
        }

        private static void ReSync() {
            WoTParser.World.Persistance.LoadWorld();
            SendMessage("\nstats\nscore\neq\ninv\ntime\nlook\nexits\n");
        }
    }
}

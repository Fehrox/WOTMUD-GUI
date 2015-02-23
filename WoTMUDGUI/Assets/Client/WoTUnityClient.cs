using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Linq;

public class WoTUnityClient : MonoBehaviour {

    private static WoTUnityClient _instance;
    static Socket s;

    IEnumerator Start() {

        _instance = this;

        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        while (!ConnectToServer("www.wotmud.org", 2222)) {
            var elapsed = sw.Elapsed;
            Debug.Log("Retrying(" + elapsed.Hours.ToString("00") 
                + ":" + elapsed.Minutes.ToString("00")
                + ":" + elapsed.Seconds.ToString("00") + ")");
            yield return new WaitForSeconds(10.0f);
        }
            
        
        StartCoroutine(Recieve());

        //Login("Tao", "csviper88");
        //Login("Seb", "csviper88");
        Login("Noder", "csviper88");
        //Login("Lycan", "csviper88");
    }

    private void OnApplicationQuit() {
        Send("quit");
        Debug.Log(WoTParser.World.places.Count + " World places" );
        if(WoTParser.World.places.Any())//make sure memory wasn't dropped on recompile
            WoTParser.World.Persistance.SaveWorld();
    }

    /// <summary>
    /// Reads input from the user and sends it on to the server.
    /// </summary>
    public static void Send(string message) {
            SendToServer(message + "\n");
    }

    private static void SendToServer(string message) {
        Debug.Log("SendToServer " + message);
        s.Send( Encoding.ASCII.GetBytes(message) );
    }

    /// <summary>
    /// Recieves messages from telnet host and displays them to the console.
    /// </summary>
    /// <param name="s"></param>
    private IEnumerator Recieve() {

        string lastLine = "", currentLine = "";
        do {

            if (s.Available > 0) {
                //Get and decode server's message.
                byte[] servedByte = new byte[1];

                s.Receive(servedByte);
                char recievedChar = Encoding.ASCII.GetString(servedByte)[0];

                //Handle messages from server one line at a time
                if (recievedChar == '\n') {
                    lastLine = currentLine;
                    currentLine = "";
                    WoTParser.Interpreter.InterpretLine(lastLine);
                    yield return null;
                } else {
                    currentLine += recievedChar;
                }
            } else {
                yield return null;
            }
        } while (true);
    }

    /// <summary>
    /// Performs a connection to telnet server.
    /// </summary>
    /// <param name="endPoint"></param>
    /// <returns></returns>
    private static bool ConnectToServer(string server, int port) {
        Debug.Log("Connecting....");
        try {
            IPEndPoint endPoint = new IPEndPoint(Dns.GetHostAddresses(server)[0], port);
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            while (!s.Connected) {
                //Only one connection attempt at a time.
                try {
                    s.Connect(endPoint);
                } catch (SocketException se) {
                    Debug.LogError(se.Message);
                    return false;
                }
            }
            Debug.Log("Connected to " + endPoint);
            return true;
        } catch (System.Exception e){
            Debug.LogError(e.Message);
            Debug.Break();
            return false;
        }
    }

    //Clean up after running
    ~WoTUnityClient() {
        s.Close();//Close connection
    }

    #region hide
    public static void Login(string userName, string password) {
        Debug.Log("Login");
        SendToServer(userName+"\n"+password+"\n");
        Init();
    }
    #endregion

    private static void Init() {
        SendToServer("color complete");
        Coroutiner.StartCoroutine(ReSync());
    }

    //TODO: Optimise this to reduce load from the game server.
    private static IEnumerator ReSync() {
        SendToServer("\nstats\nscore\neq\ninv\ntime\nlook\nexits\n");
        while (System.String.IsNullOrEmpty(WoTParser.Player.me.character.name)) {
            yield return new WaitForSeconds(0.5f);
        }
        WoTParser.World.Persistance.LoadWorld();
    }
}

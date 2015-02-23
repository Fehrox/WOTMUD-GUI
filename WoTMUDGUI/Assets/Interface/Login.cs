using UnityEngine;
using System.Collections;

namespace Interface {

    public class Login : MonoBehaviour {

        string userName = "Seb";
        //string userName = "Lycan";
        //string userName = "Fid";
        string password = "p4ssw0rdlol";

        void Start() {
            WoTParser.Player.OnLogin += OnLoginAttempt;
        }

        void OnLoginAttempt(bool success) {
            Debug.Log("Login " + success.ToString());
            if (success) {
                //Application.LoadLevel("World");
                Destroy(this.gameObject);
            }
        }

        void OnGUI() {
            //Username.
            GUILayout.BeginHorizontal();
            GUILayout.Label("Username:");
            userName = GUILayout.TextField(userName);
            GUILayout.EndHorizontal();
            //Password.
            GUILayout.BeginHorizontal();
            GUILayout.Label("Password:");
            password = GUILayout.TextField(password);
            GUILayout.EndHorizontal();
            //Login button
            if (GUILayout.Button("Login")) {
                WoTUnityClient.Login(userName, password);
            }
        }

    }
}
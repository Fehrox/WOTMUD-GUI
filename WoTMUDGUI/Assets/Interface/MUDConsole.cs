using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Interface {

    public class MUDConsole : MonoBehaviour {

        public List<string> consoleHistory = new List<string>();

        void Start() {
            WoTParser.Interpreter.OnBlockProcessed += OnBlockProcessed;
        }

        /// <summary>
        /// When a line has been processed by the Data handler, and is ready for display.
        /// If a pattern match is found, its data will have been displayed on the UI
        /// if a patter match has not been found, it will be printed to te user console
        /// for reading.
        /// </summary>
        /// <param name="matchFound"></param>
        /// <param name="processedBlock"></param>
        void OnBlockProcessed(bool matchFound, string processedBlock) {
            if (matchFound) {
                Debug.Log(processedBlock);
            } else {
                if (processedBlock.Trim() == "") return;
                Debug.LogError(processedBlock);
                if (processedBlock != "")
                    AddLine(processedBlock);
            }
        }

        /// <summary>
        /// Listens for the enter key when and sends the input from the interface
        /// to the WOTMud servers.
        /// </summary>
        void OnGUI() {
            //Send messages on enter.
            if (Event.current.keyCode == KeyCode.Return && input != "") {
                //Make sure we look to identify the place properly.
                if (input == "n" || input == "e" || input == "s" || input == "w") input += "\nexits";
                WoTUnityClient.Send(input + "\n");
                input = "";
                scrollPosition = new Vector2(0, int.MaxValue);
            }

            GUILayout.Window(0,
                new Rect(
                    0.0f * Screen.width,
                    0.6f * Screen.height,
                    1.0f * Screen.width,
                    0.4f * Screen.height
                ),
                DrawConsoleHistory,
                GUIContent.none
            );
        }

        string input = "";
        Vector2 scrollPosition;
        private void DrawConsoleHistory(int id) {
            scrollPosition = GUILayout.BeginScrollView(
               scrollPosition,
               GUILayout.ExpandWidth(true),
               GUILayout.ExpandHeight(true)
            );
            foreach (string line in consoleHistory) {
                GUILayout.Label(line);
            }
            GUILayout.EndScrollView();
            input = GUILayout.TextField(input, GUILayout.ExpandWidth(true));
        }

        public void AddLine(string consoleLine) {
            consoleLine = consoleLine
                        .Replace("[0m", "")
                        .Replace("[32m", "")
                        .Replace("[33m", "")
                        .Replace("[36m", "")
                        .Trim();
            var isBlankLine = consoleLine == "\r" || consoleLine == "\n";
            if (!string.IsNullOrEmpty(consoleLine) && !isBlankLine) {
                consoleHistory.Add(consoleLine);
                scrollPosition = new Vector2(0, int.MaxValue);
            }
        }
    }
}
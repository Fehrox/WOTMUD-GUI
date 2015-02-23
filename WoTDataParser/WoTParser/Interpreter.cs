using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using System.Threading.Tasks;

namespace WoTParser {

    public class Interpreter {

        /// <summary>
        /// Process and extract relevent information from the messages coming in from server
        /// int relevent classes for later use.
        /// </summary>
        /// <note>Could be moved to its own class (Interpreter)</note>
        /// <param name="line"></param>
        public delegate void EventArgsBlockProcessed(bool matchFound, string processedBlock);
        public static event EventArgsBlockProcessed OnBlockProcessed;
        static List<string> lineBuffer = new List<string>();
        public static void InterpretLine(string line) {

            Console.WriteLine(line, ConsoleColor.DarkGray);

            //Process lines one at a time (for quickest possible data interpretation).
            if (FindPatternMatches(line, Patterns.lineMediator)) {
                if (OnBlockProcessed != null) OnBlockProcessed(true, line);
                return;
            }

            //Process sections of text togeather (for multi-line patterns).
            if (line == "\r") {//When a section end is detected
                //Search the buffer for multi-line patterns.
                string block = String.Join("\n", lineBuffer.ToArray<string>());
                bool matchFound = FindPatternMatches(block,Patterns.blockMediator);

                //Regester string to history.
                if (OnBlockProcessed != null) {
                    if (matchFound) {
                        OnBlockProcessed(true, block);
                    } else {
                        OnBlockProcessed(false, block);
                    }
                }

                //Clear the buffer
                lineBuffer = new List<string>();
            } else {
                //Add line to buffer for later processing.
                lineBuffer.Add(line);
            }
        }
 
        private static bool FindPatternMatches(string line, Dictionary<string[], Action<string>> mediator) {
            foreach (KeyValuePair<string[], Action<string>> searchPatternSet in mediator) {
                //and each pattern with in that type in each type of test
                foreach (string searchPattern in searchPatternSet.Key) {
                    //Search for matches
                    Regex regEx = new Regex(searchPattern);
                    if (regEx.IsMatch(line)) {
                        //try {
                            //If match found, Call method to handle it.
                            searchPatternSet.Value.Invoke(line);
                            //Don't search for additional matches once one is found.
                        //} catch (Exception e){
                        //    Debugger.Log("Pattern match found, but error during processing\n"+e.Message, ConsoleColor.Red);
                        //}
                        return true;
                    }
                }
            }

            return false;
        } 

        public static int[] IsolateNumbersToArray(string numStr) {
            return new Regex(@"[^\d]").Replace(numStr, " ")
                .Split(' ').Where(s => !s.Equals("")).Select(i => int.Parse(i)).ToArray<int>();
        }

        public static double lbsToKgs(double lbs) {
            return lbs * 0.453592371;
        }

        public static string[] GetItemsFromList(string items){
            if (items.Contains("Nothing.")) {
                return null;
            } else {
                string[] splitItems = items.Split('\n');
                List<string> itemsList = new List<string>();
                for (int i = 1; i < splitItems.Length; i++) {
                    itemsList.Add(splitItems[i]);
                }
                return itemsList.Select(i => i.Replace("\r", "")).ToArray<string>();
            }
        }

    }
}

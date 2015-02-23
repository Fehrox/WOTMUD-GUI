using System;

namespace WoTConsoleClient {
    static class Debugger {

        static Func<byte[], string> PrintBytes = delegate(byte[] inBytes) {
            string outString = "";
            foreach (byte b in inBytes) {
                outString += b.ToString();
            }
            return outString;
        };

        public static void LogCharacter(Char c) {
            Console.Write(c);
        }

        public static void Log(string message, ConsoleColor color = ConsoleColor.Gray) {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void Break() {
            Console.ReadKey();
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WoTParser {
    /// <summary>
    /// Contains all the regular expressions against 
    /// which the output from the server is tested.
    /// </summary>
    public class Patterns {
        /// <summary>
        /// Compile a list of patterns to search, and attach the appropriate
        /// action to that pattern.
        /// NOTE:   For the best performance, place most commonly 
        ///         occuring patterns first.
        /// </summary>
        public static Dictionary<string[], Action<string>> lineMediator = new Dictionary<string[], Action<string>>();
        public static Dictionary<string[], Action<string>> blockMediator = new Dictionary<string[], Action<string>>();   
        static Patterns() {

            //build catalogue of single line patterns
            //Payer
            lineMediator.Add(PlayerPatterns.loginPatterns, new Action<string>(Player.me.Login));
            lineMediator.Add(PlayerPatterns.healthPatterns, new Action<string>(Player.me.SetHealth));
            lineMediator.Add(PlayerPatterns.hungerPatterns, new Action<string>(Player.me.SetHunger));
            lineMediator.Add(PlayerPatterns.posturePatterns, new Action<string>(Player.me.SetPosture));
            lineMediator.Add(PlayerPatterns.statPatterns, new Action<string>(Player.me.SetStats));
            lineMediator.Add(PlayerPatterns.xpPatterns, new Action<string>(Player.me.SetXp));
            lineMediator.Add(new string[] { PlayerPatterns.levelPattern }, new Action<string>(Player.me.SetLevel));
            lineMediator.Add(new string[] { PlayerPatterns.playTimePattern }, new Action<string>(Player.me.SetPlayTime));
            //World
            lineMediator.Add(new string[] { WorldPatterns.timePattern }, new Action<string>(World.SetTime));
            //Trash
            lineMediator.Add(PlayerPatterns.trashLinePatterns, new Action<string>(DisposeTrashLines));

            //build catalouge of block line patterns
            //Place
            blockMediator.Add(new string[] { PlacePatterns.placePattern }, new Action<string>(Place.SetPlace));
            //Player
            blockMediator.Add(new string[] { PlayerPatterns.inventoryPattern }, new Action<string>(Player.me.SetInventory));
            blockMediator.Add(new string[] { PlayerPatterns.equippedPattern }, new Action<string>(Player.me.SetEquipped));

        }

        //Frequently used Regex patterns
        public const string WORD = "[a-zA-Z]{1,15}",
                            NUMBER = "[0-9]{1,6}",
                            DOUBLE = NUMBER+"."+NUMBER,
                            ANY_CHARACTERS = "((.|\n)*)";

        public class PlacePatterns {
            public const string startHeadingPattern = ".\\[36m",
                                endHeadingPattern = ".\\[0m",
                                headingPattern = startHeadingPattern+ANY_CHARACTERS+endHeadingPattern,
                                exitsPattern = "(o|O)bvious exits:",
                                exitsSimplePattern = "\\[ "+exitsPattern+" (.*) \\]",
                                exitsVerbosePattern = "Obvious exits:",
                                placePattern = headingPattern+ANY_CHARACTERS 
                               +exitsSimplePattern+ANY_CHARACTERS+exitsPattern;
            public const string darkExitPattern = "Too dark to tell";
        }

        public class PlayerPatterns {

            //Login
            public static string[] loginPatterns = new string[]{
                "Welcome to the Wheel of Time!  Type 'help' for information.",//success
                "Wrong passphrase."//failure
            };

            //Health
            public static string[] healthPatterns = new string[]{
                " HP:"+WORD+" MV:"+WORD,//+" >",

                "You have "+NUMBER+"\\("+NUMBER+"\\) hit " +
                "and "+NUMBER+"\\("+NUMBER+"\\) movement points."
            };

            //Food & Drink
            public static string[] hungerPatterns = new string[]{
                String.Format("^You are( already| )({0}|{1}).",
                    Player.Condition.FoodStates.hungry.ToString(),
                    Player.Condition.DrinkStates.thirsty.ToString()
                ),
                String.Format("^You don't feel ({0}|{1}) any more.",
                    Player.Condition.FoodStates.hungry.ToString(),
                    Player.Condition.DrinkStates.thirsty.ToString()
                )
            };

            //XP
            public static string[] xpPatterns = new string[]{
                "You have scored "+NUMBER+" experience points and "+NUMBER+" quest points.",
                "You need "+NUMBER+" exp to reach the next level."
            };

            //Level
            public const string levelPattern = "This ranks you as "+WORD+" (of|the) "+WORD;//+" \\(Level "+NUMBER+"\\).";

            //Posture
            public static string[] posturePatterns = new string[]{
                String.Format("You are ({0}|{1}|{2}).",
                    Player.Condition.PostureStates.sitting.ToString(),
                    Player.Condition.PostureStates.standing.ToString(),
                    Player.Condition.PostureStates.sleeping.ToString()
                ),
                String.Format("You're ({0}|{1}|{2}) already.",
                    Player.Condition.PostureStates.sitting.ToString(),
                    Player.Condition.PostureStates.standing.ToString(),
                    Player.Condition.PostureStates.sleeping.ToString()
                ),
                "You ((sit|stand) (up|down)|go to sleep).",
                "You wake, and sit up."
            };

            //Play time
            public const string playTimePattern = "You have played "+NUMBER+" days and "+NUMBER+" hours \\(real time\\).";

            //Stats
            public static string[] statPatterns = new string[]{
               "You are a "+NUMBER+" year old "+WORD+" "+WORD+" "+WORD+".",
               "Your height is "+NUMBER+" feet, "+NUMBER+" inches, and you weigh "+DOUBLE+" lbs",
               "You are carrying "+DOUBLE+" lbs and wearing "+DOUBLE+" lbs",
               "Offensive bonus: "+NUMBER+", Dodging bonus: "+NUMBER+", Parrying bonus: "+NUMBER+"",
               "Your mood is: "+WORD+". You will flee below: "+NUMBER+" Hit Points",
               "Your armor absorbs about \\s"+NUMBER+"% on average.",
               "Your base abilities are: Str:"+NUMBER+" Int:"+NUMBER+" Wil:"+NUMBER+" Dex:"+NUMBER+" Con:"+NUMBER+"."
            };

            //Items
            public const string inventoryPattern = "You are carrying:";
            public const string equippedPattern = "You are using:";

            //Trash Lines
            public static string[] trashLinePatterns = new string[] { 
                "According to legend and prophecy, this is the "+WORD+" Turn of the Wheel.",
                "Server: "+ANY_CHARACTERS+", up for "+NUMBER+" hours, "+NUMBER+" minutes",
                "You are subjected to the following effects:",
                "The official forums are back at 'http://www.wotmud.org/forums/' enjoy!",
                "Your "+ANY_CHARACTERS+"color"+ANY_CHARACTERS+" is now complete."
                //"(\n|\r)"
            };
        }

        public class WorldPatterns {
            public const string timePattern = "It is "+NUMBER+" o'clock (a|p)m, on the "
                             +NUMBER+"(st|nd|rd|th) day of the month of "+WORD+", year "+NUMBER+".";
        }

        private static void DisposeTrashLines(string trashline){/*These are undeeded lines, do nothing*/}

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WoTParser {

    /// <summary> 
    /// Stores and handles all player related information.
    /// </summary>
    public class Player {

        public static Player me = new Player();

        public delegate void LocationChangedEvent(Place newLocation);
        public static event LocationChangedEvent OnLocationChanged;
        private static Place location;
        public static Place Location {
            set {
                if (OnLocationChanged != null)
                    OnLocationChanged(value);
                location = value;
            }get {
                return location;
            }
        }

        public Character character;
        public struct Character {
            public string name, origin;
            public int age, playTimeHours;
            public double height, weight;
            public enum GenderStates { male, female };
            public GenderStates gender;
            public enum RaceStates { human , trolloc}
            public RaceStates race;
            public enum ClassStates { warrior, hunter }
            public ClassStates classType;
        }

        public Condition condition;
        public struct Condition {
            public enum HPStates { Healthy, Scratched, Wounded, Hurt, Battered, Beaten, Critical, Incapacitated };
            public HPStates hpState;
            public enum MVStates { Full, Fresh, Strong, Tiring, Winded, Weary, Haggard };
            public MVStates mvState;
            public enum FoodStates { notHungry, hungry };
            public FoodStates foodState;
            public enum DrinkStates { notThirsty, thirsty };
            public DrinkStates drinkState;
            public enum PostureStates { standing, sitting, sleeping };
            public PostureStates postureState;
        }

        public Level level;
        public struct Level {
            public int level, xpEarned, qpEarned, xpRemaining;
        }

        public Combat combat;
        public struct Combat{
            public int hpTotalCount, hpCount, mvCount, mvTotalCount,
                        offence, dodge, parry, fleeAt, armourObsorbtion,
                        strength, inteligence, will, dexterity, constitution;
            public enum Mood { Wimpy, Brave }
            public Mood mood;
        }

        public Equipment equipment;
        public struct Equipment{        
            public string[] inventory, equipped;
            public double encumberanceCarried, encumberanceWorn;
        }

        public void SetHealth(string healthMatch) {
            //Debugger.Log("health Pattern found", ConsoleColor.Green);
            try {
                if (new Regex(Patterns.PlayerPatterns.healthPatterns[1]).IsMatch(healthMatch)) {
                    //Convert it to an int array
                    int[] hpMvStats = Interpreter.IsolateNumbersToArray(healthMatch);
                    //Save out the information
                    combat.hpCount = hpMvStats[0];
                    combat.hpTotalCount = hpMvStats[1];
                    combat.mvCount = hpMvStats[2];
                    combat.mvTotalCount = hpMvStats[3];
                }

                if (new Regex(Patterns.PlayerPatterns.healthPatterns[0]).IsMatch(healthMatch)) {
                    //Handle broad health info
                    string[] healthInfo = healthMatch.Split(' ').Where(c => c.Contains(':')).ToArray<String>();
                    foreach (string s in healthInfo) {
                        string[] infoSections = s.Split(':');
                        if (infoSections[0] == "HP") {
                            condition.hpState = (Condition.HPStates)Enum.Parse(typeof(Condition.HPStates), infoSections[1]);
                        } else if (infoSections[0] == "MV") {
                            condition.mvState = (Condition.MVStates)Enum.Parse(typeof(Condition.MVStates), infoSections[1]);
                        }
                    }

                    //Process the rest of the stirng, as info often follows health
                    Interpreter.InterpretLine(healthMatch.Split('>').Last());
                }
            } catch (IndexOutOfRangeException ioore) {
                throw new IndexOutOfRangeException("Handling " + healthMatch);
            }
        }

        public void SetHunger(string hungerMatch) {
            //Debugger.Log("Hunger Pattern found", ConsoleColor.Green);

            //TODO: see if the overhead spend doign this great enough to justify
            //      doing this like in SetConditionHealth instead.
            bool affirmative = hungerMatch.Contains("You don't feel");
            if (hungerMatch.Contains(Condition.FoodStates.hungry.ToString())) {
                if (affirmative) {
                    condition.foodState = Condition.FoodStates.notHungry;
                } else {
                    condition.foodState = Condition.FoodStates.hungry;
                }
            } else if (hungerMatch.Contains(Condition.DrinkStates.thirsty.ToString())) {
                if (affirmative) {
                    condition.drinkState = Condition.DrinkStates.notThirsty;
                } else {
                    condition.drinkState = Condition.DrinkStates.thirsty;
                }
            }
        }

        public void SetXp(string xpMatch) {
            //Debugger.Log("XP Pattern found", ConsoleColor.Green);

            if (new Regex(Patterns.PlayerPatterns.xpPatterns[0]).IsMatch(xpMatch)) { //XP earned
                int[] xpQpEarned = Interpreter.IsolateNumbersToArray(xpMatch);
                level.xpEarned = xpQpEarned[0];
                level.qpEarned = xpQpEarned[1];
            }
            else if (new Regex(Patterns.PlayerPatterns.xpPatterns[1]).IsMatch(xpMatch)) {//XP required
                level.xpRemaining = Interpreter.IsolateNumbersToArray(xpMatch)[0];
            }
        }

        public void SetPosture(string postureMatch) {
            //Debugger.Log("Posture pattern found", ConsoleColor.Green);

            string[] postures = new string[]{
                Condition.PostureStates.sitting.ToString(), 
                Condition.PostureStates.standing.ToString(), 
                Condition.PostureStates.sleeping.ToString()
            };

            //Sanatise input to match patterns
            postureMatch = postureMatch.Replace("sit ", "sitt ");//because siting does not match sitting
            string[] givenWords = postureMatch.Replace(".", " ").Split(' ');
            //Add ing to anything without it
            string[] posturesMatchArrIng = givenWords.Where(s => !s.Contains("ing"))
                                                     .Select(s => s + "ing").ToArray<string>();
            string newPosture = postures.Intersect(posturesMatchArrIng.Concat(givenWords)).ToArray<string>()[0];

            //find the present posture string, and convert and save it as a PostureState
            condition.postureState = (Condition.PostureStates)Enum.Parse(
                typeof(Condition.PostureStates),
                newPosture
            );
        }

        public void SetLevel(string levelMatch) {
            //Debugger.Log("Level pattern found", ConsoleColor.Green);

            //Seperate out the name
            string[] ignoredText = Patterns.PlayerPatterns.levelPattern.Replace(Patterns.WORD, " ").Split(' '),
                     levelText = levelMatch.Split(' '),
                     levelInfo = levelText.Except(ignoredText).ToArray<string>();
            character.name = levelInfo[0];

            //Seperate out home
            levelMatch = String.Join(" ", levelInfo.Except(new string[] { levelInfo[0] }).ToArray<string>());
            character.origin = levelMatch.Split('(')[0];

            //Seperate out the level
            level.level = int.Parse(new Regex(@"[^\d]").Replace(levelMatch, " "));
        }

        internal void SetPlayTime(string playTimeMatch) {
            //Seperate out playtime
            int[] playTimeInfo = Interpreter.IsolateNumbersToArray(playTimeMatch);
            //Combine days and hours to save out to a single value
            character.playTimeHours = playTimeInfo[0] * 24 + playTimeInfo[1];
        }

        internal void SetStats(string statMatch) {
            //Debugger.Log("Stat pattern found", ConsoleColor.Green);
            string onlyLettersAndNumbers = "[^a-zA-Z0-9.]";

            string preModStatsMatch = statMatch;
            //TODO: during a fight I called stats, and the first line triggered the regex,
            //      but the health pattern was still on the same line . .. 
            //      this should probably be solved with more stringent regex instead, but 
            //      for now this hack will suffice.
            statMatch = statMatch.Replace(Patterns.PlacePatterns.endHeadingPattern, "");
            statMatch = statMatch.Replace(Patterns.PlacePatterns.headingPattern, "");
            if (new Regex(Patterns.PlayerPatterns.healthPatterns[0]).IsMatch(statMatch))
                statMatch= statMatch.Split('>').Last();


            //Determine what kind of info we have
            int matchingPatternIndex = 0;
            string[] unNeededInfo = new string[0];
            foreach (string pattern in Patterns.PlayerPatterns.statPatterns) {
                if (new Regex(pattern).IsMatch(statMatch)) {
                    //sanatise pattern
                    string sanatisedPattern =
                        Regex.Replace(pattern.Replace(Patterns.NUMBER, "")
                                                .Replace(Patterns.WORD, ""),
                                        onlyLettersAndNumbers, " ");
                    unNeededInfo = sanatisedPattern.Split(' ').Where(s => s != "").ToArray<string>();
                    break;
                } else {
                    matchingPatternIndex++;
                }
            }

            //Sanatise the stat string (only letters and decimal numbers)
            statMatch = Regex.Replace(statMatch, onlyLettersAndNumbers, " ");

            //Isolate relevent values.
            string[] splitStatsMatch = statMatch.Split(' ').Where(s => s != "").ToArray<string>(); ;
            List<string> validInfo = new List<string>();
            foreach (string s in splitStatsMatch)
                if (!unNeededInfo.Contains(s))
                    validInfo.Add(s);
            try{
                //Save remaining information to player.
                switch (matchingPatternIndex) {
                    case 0://Age, sex, race, classType
                        character.age = int.Parse(validInfo[0]);
                        character.gender = (Character.GenderStates)Enum.Parse(typeof(Character.GenderStates), validInfo[1]);
                        character.race = (Character.RaceStates)Enum.Parse(typeof(Character.RaceStates), validInfo[2]);
                        character.classType = (Character.ClassStates)Enum.Parse(typeof(Character.ClassStates), validInfo[3].Replace(".", ""));
                        break;
                    case 1://height , weight
                        int heightInches = int.Parse(validInfo[0]) * 12 + int.Parse(validInfo[1]);
                        character.height = heightInches * 2.54; //convert to Cms
                        character.weight = Interpreter.lbsToKgs(double.Parse(validInfo[2])); //conver to kgs
                        break;
                    case 2://weight carried, weight worn
                        equipment.encumberanceCarried = Interpreter.lbsToKgs(double.Parse(validInfo[0]));
                        equipment.encumberanceWorn = Interpreter.lbsToKgs(double.Parse(validInfo[1]));
                        break;
                    case 3://Combat stats - offence, dodge, defence
                        int[] validInfoInt = validInfo.Select(s => int.Parse(s)).ToArray<int>();
                        combat.offence = validInfoInt[0];
                        combat.dodge = validInfoInt[1];
                        combat.parry = validInfoInt[2];
                        break;
                    case 4://Mood and flee threshold
                        combat.mood = (Combat.Mood)Enum.Parse(typeof(Combat.Mood), validInfo[0].Replace(".", ""));
                        combat.fleeAt = int.Parse(validInfo[1]);
                        break;
                    case 5://Armour obsortion
                        combat.armourObsorbtion = int.Parse(validInfo[0]);
                        break;
                    case 6://Stats Str, Int, Wil, Dex, Con.
                        int i = 0;
                        combat.strength = int.Parse(validInfo[i++]);
                        combat.inteligence = int.Parse(validInfo[i++]);
                        combat.will = int.Parse(validInfo[i++]);
                        combat.dexterity = int.Parse(validInfo[i++]);
                        combat.constitution = int.Parse(validInfo[i++].Replace(".", ""));
                        break;
                }
            } catch {
                throw new Exception(preModStatsMatch + " - " + String.Join("|", validInfo.ToArray<string>()));
            }
        }

        internal void SetInventory(string inventoryMatch) {
            equipment.inventory = Interpreter.GetItemsFromList(inventoryMatch);
        }

        internal void SetEquipped(string equippedMatch) {
            equipment.equipped = Interpreter.GetItemsFromList(equippedMatch);
        }

        public delegate void FoundPlaceEvent(Place newLocation);
        public static event FoundPlaceEvent OnFoundNewPlace;
        internal void SetLocation(Place newPlace) {
            Console.WriteLine("SetLocation", ConsoleColor.Yellow);
            //Check for known places.
            //See if the place is different from current location
            if (!World.places.Any()) {
                Console.WriteLine("Its a brave new world");
                //This is a new world
                if (OnFoundNewPlace != null)
                    OnFoundNewPlace(newPlace);
                Location = newPlace;
                World.places.Add(newPlace);
            } else if (Location != null){ //We are not starting a new world.
                Place returningPlace = World.places.Find(p => p.placeHash == newPlace.placeHash);
                if (returningPlace != null) {//This is a know place
                    UpdatePlaceExits(ref returningPlace);
                    Location = returningPlace;
                    Console.WriteLine("Returned to " + location.heading);
                } else {//This is a new place, and we have moved.
                    Console.WriteLine("Found new place " + location.heading);
                    if (World.ConnectPlaces(ref newPlace, ref location)) {
                        //This place is connected to a known place, add it to mapped world.
                        World.places.Add(newPlace);
                        if (OnFoundNewPlace != null)
                            OnFoundNewPlace(newPlace);
                        //Move to new location
                        Location = newPlace;
                    } else {//Could not connect to known place, we are lost.
                        //Lost
                        Location = World.places.First();
                        //TODO: Impliment Lost state.
                    }
                }
            } else { //Starged game, location not known
                if (World.HasPlace(newPlace)) { //place is known
                    Location = newPlace;
                } else { //Started at unknown location, we are Lost
                    //TODO: Impliment Lost state.
                    //FORNOW: Just go to first know place
                    Location = World.places.First();                  
                }
            }
        }

        private void UpdatePlaceExits(ref Place returningPlace) {
            //check if the exits need updating in case they where added during the night.
            if (!Enumerable.SequenceEqual(returningPlace.exitHeadings, location.exitHeadings)) {
                //BUG: Don't just compare the exit headings, but the hashes and connected places also. 
                for (int i = 0; i < returningPlace.exitHeadings.Length; i++) {
                    //if a night exit is found
                    if (Location.exitHeadings[i] == Patterns.PlacePatterns.darkExitPattern) {
                        //update it in hopes of getting a real location.
                        Location.exitHeadings[i] = returningPlace.exitHeadings[i];
                    }
                }
            }

            if (!Enumerable.SequenceEqual(returningPlace.connectedPlaceHashes, location.connectedPlaceHashes)) {
                if (location.connectedPlaces.Count() < returningPlace.connectedPlaces.Count())
                    Location.connectedPlaces = returningPlace.connectedPlaces;
            }

            if (Enumerable.SequenceEqual(returningPlace.connectedPlaceHashes, location.connectedPlaceHashes)) {
                if (location.connectedPlaceHashes.Count() < returningPlace.connectedPlaceHashes.Count())
                    Location.connectedPlaceHashes = returningPlace.connectedPlaceHashes;
            }
        }

        public delegate void LoginEvent(bool success);
        public static event LoginEvent OnLogin;
        public void Login(string loginMatch){
            if(OnLogin != null)
                OnLogin(loginMatch.Contains(Patterns.PlayerPatterns.loginPatterns[0]));
        }
    }
}

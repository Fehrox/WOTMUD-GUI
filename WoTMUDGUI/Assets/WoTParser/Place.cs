using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
//using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WoTParser {

    [Serializable, XmlRoot("Place")]
    public class Place : IComparable, ISerializable {

        public string heading = "";
        public string description = "";
        public string[] Interactables { get; set; }

        //Exits and connections.
        public enum ExitDirections { North, East, South, West }
        public string[/*North, East, South, West*/] exitHeadings;
        public int placeHash;
        public int[] connectedPlaceHashes;
        public int gridRefX, gridRefZ;
        [XmlIgnore]
        public Place[/*North, East, South, West*/] connectedPlaces;

        [XmlIgnore]
        public string PlaceGUID {
            get {
                string del = "|";
                return heading + del +
                       description + del + 
                       String.Join(del, ExitHeadingsBool.Select(b => b ? "T":"F").ToArray<string>());
            }
        }

        [XmlIgnore]
        public GridRef PlaceGridRef {
            get { return new GridRef(gridRefX, gridRefZ); }
            set { gridRefX = value.x; gridRefZ = value.z; }
        }

        [XmlIgnore]
        public bool[] ExitHeadingsBool {
            get {
                //It can be too dark to tell if there are exits at night, 
                //but still possible to see the room's description, so
                //just check that the directions match, instead of names.
                bool[] exitHeadingsBool = new bool[exitHeadings.Length];
                for (int i = 0; i < exitHeadings.Length; i++) {
                    if (exitHeadings[i] == null)
                        exitHeadingsBool[i] = false;
                    else
                        exitHeadingsBool[i] = true;
                }
                return exitHeadingsBool;
            }
        }

        public Place() { }
        private Place(string heading, string description, string[] interactables, string[] exitHeadings) {
            this.heading = heading;
            this.description = description;
            this.Interactables = interactables;
            this.exitHeadings = exitHeadings;
            connectedPlaces = new Place[exitHeadings.Length];
            connectedPlaceHashes = new int[exitHeadings.Length];
            placeHash = GetHashCode();
        }
        public Place(Place placeInfo) {
            this.heading = placeInfo.heading;
            this.description = placeInfo.heading;
            this.Interactables = placeInfo.Interactables;
            this.exitHeadings = placeInfo.exitHeadings;
            this.connectedPlaces = placeInfo.connectedPlaces;
            this.connectedPlaceHashes = placeInfo.connectedPlaceHashes;
        }

        public static ExitDirections InvertExitState(ExitDirections direction) {
            switch (direction) {
                default:
                case ExitDirections.North:
                    return ExitDirections.South;
                case ExitDirections.South:
                    return ExitDirections.North;
                case ExitDirections.East:
                    return ExitDirections.West;
                case ExitDirections.West:
                    return ExitDirections.East;
            }
        }

        /// <summary>
        /// Converts 
        /// </summary>
        /// <param name="placePattern"></param>
        //TODO: Don't let new lines in room names cause the chunk to process before its ready.
        public static void SetPlace(string placePattern) {
            string[] lines = placePattern.Split('\n').ToArray<string>();

            int i = 0;
            string heading = "";    
            //Heading
            foreach (string line in lines) {
                //Find heading and save it out.
                if (new Regex(Patterns.PlacePatterns.headingPattern).IsMatch(line)) {
                    string newPlaceHeading;
                    newPlaceHeading = Regex.Replace(line, Patterns.PlacePatterns.startHeadingPattern, "");
                    newPlaceHeading = Regex.Replace(newPlaceHeading, Patterns.PlacePatterns.endHeadingPattern, "");
                    heading = newPlaceHeading.Replace("\r", "").Trim();
                    i++;//move on to next line
                    break;
                } else {
                    Console.WriteLine("Heading not found in place pattern.", ConsoleColor.Red);
                    return;
                }
            }

            string description = "";
            //Description
            while (!new Regex(Patterns.PlacePatterns.exitsPattern).IsMatch(lines[i])) {
                description += lines[i];
                i++;
            }
            i++;//skip over simple exits, this info can be populated when the verbose data is printed

            string[] interactables = new string[0];
            //Interactable objects
            List<string> interactablesList = new List<string>();
            while (!new Regex(Patterns.PlacePatterns.exitsPattern).IsMatch(lines[i])) {
                interactablesList.Add(lines[i]);
                i++;//Move to next line
            }
            interactables = interactablesList.ToArray<string>();
            i++;//skip over exits verbose heading

            //Exits
            int NUM_EXIT_OPTIONS = Enum.GetValues(typeof(ExitDirections)).Length;
            string[] exitHeadings = new string[NUM_EXIT_OPTIONS];
            Place[] connectedPlaces = new Place[NUM_EXIT_OPTIONS];
            while (i < lines.Length) {
                string[] exit = lines[i].Split('-');
                for (int j = 0; j < exitHeadings.Length; j++) {
                    if (exit[0].Contains(((Place.ExitDirections)j).ToString())) 
                        exitHeadings[j] = exit[1].Replace("\r", "").Trim();
                }
                i++;
            }

            Place newPlace = new Place(heading, description, interactables, exitHeadings);
            Player.me.SetLocation(newPlace);

        }

        internal static void SetDarkPlace(string darkPlace) {

            //TODO: Set it up so that dark places are added to the map by using the 
            //      direction in Mover! Still populate the exits with the available
            //      exit information. 
            //      When a new place is found, Check that its grid ref was not 
            //      previously a dark place! If it was a dark place, update
            //      that dark place's information to be the actual info for the place.
            //      Do something fancy with the map to show it's dark.

            UnityEngine.Debug.Log("SetDarkPlace" + darkPlace);

            // Set our location to a dark place relative us
            // vial the last requested move direcion.
            if (Player.Location != null) {
                Player.me.SetLocation(new DarkPlace(Player.Location));
                Player.MovedToDarkPlace();
            }
        }

        public static void NewArrival(string arrivalStr) {
            //Re-check location
            UnityEngine.Debug.LogWarning("New Arrival detected! " + arrivalStr);
            //TODO:Set this up so that the new arrival is added to the place's record.
        }

        internal void UpdateExits(Place returningPlace) {
            //check if the exits need updating in case they where added during the night.
            if (!Enumerable.SequenceEqual(returningPlace.exitHeadings, exitHeadings)) {
                //BUG: Don't just compare the exit headings, but the hashes and connected places also. 
                for (int i = 0; i < returningPlace.exitHeadings.Length; i++) {
                    //if a night exit is found
                    if (exitHeadings[i] == Patterns.PlacePatterns.darkExitPattern) {
                        //update it in hopes of getting a real location.
                        exitHeadings[i] = returningPlace.exitHeadings[i];
                    }
                }
            }

            if (!Enumerable.SequenceEqual(returningPlace.connectedPlaceHashes, connectedPlaceHashes)) {
                var thisPlaceCount = connectedPlaces.Where(p => p != null).Count();
                var returningPlaceHahsCount = returningPlace.connectedPlaces.Where(p => p != null).Count();
                if (thisPlaceCount < returningPlaceHahsCount)
                    connectedPlaces = returningPlace.connectedPlaces;
            }

            if (Enumerable.SequenceEqual(returningPlace.connectedPlaceHashes, connectedPlaceHashes)) {
                var thisPlaceCount = connectedPlaceHashes.Where(p => p != 0).Count();
                var returningPlaceHashCount = returningPlace.connectedPlaceHashes.Where(p => p != 0).Count();
                if (thisPlaceCount < returningPlaceHashCount)
                    connectedPlaceHashes = returningPlace.connectedPlaceHashes;
            }
        }

        internal void UpdateInteractables(Place returningPlace) {
            var sequenceEqual = true;
            foreach (string interactableOld in Interactables)
                foreach (string interactableNew in returningPlace.Interactables)
                    if (interactableOld != interactableNew) sequenceEqual = false;

            if (!sequenceEqual) {
                UnityEngine.Debug.LogWarning(
                    "Updating interactables!\n" 
                    + String.Join("\n",returningPlace.Interactables) 
                    + "\n---------\n"
                    + String.Join("\n", Interactables)
                );
                Interactables = returningPlace.Interactables;
            } 
        }

        //TODO: apparently GetHashCode does not guarentee a unique 
        //      key, so we should use something else here. For now it will do.
        public override int GetHashCode() {
            return PlaceGUID.GetHashCode();
        }

        public int CompareTo(object other) {
            if(other == null) return 1;
            Place otherPlace = (Place)other;
            return string.Compare(PlaceGUID, otherPlace.PlaceGUID);
        }

        protected Place(SerializationInfo info, StreamingContext context) {
            connectedPlaceHashes = info.GetValue("connectedPlaceHashes", typeof(int[])) as int[];
            exitHeadings = info.GetValue("exitHeadings", typeof(string[])) as string[];
            heading = info.GetString("heading");
            description = info.GetString("description");
            Interactables = info.GetValue("interactables", typeof(string[])) as string[];
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("connectedPlaceHashes", connectedPlaceHashes, typeof(Place[]));
            info.AddValue("exitHeadings", exitHeadings, typeof(string[]));
            info.AddValue("heading", connectedPlaces, typeof(string));
            info.AddValue("description", description, typeof(string));
            info.AddValue("interactables", connectedPlaces, typeof(string[]));
        }

        public GridRef GetOffsetGridRef(ExitDirections direction) {
            //Generate grid reference
            GridRef[] positionOffsets = new GridRef[]{
                new GridRef(0,1),//Y = N
                new GridRef(1,0),//X = E
                new GridRef(0,-1),//-Y = S
                new GridRef(-1,0)//-X = W
            };

            return new Place.GridRef(
                PlaceGridRef.x + positionOffsets[(int)direction].x,
                PlaceGridRef.z + positionOffsets[(int)direction].z
            );
        }


        public class GridRef : IComparable {
            public int x;
            public int z;
            public GridRef(int x, int z) {
                this.x = x;
                this.z = z;
            }

            public int CompareTo(object obj) {
                if (obj == null) return 1;
                GridRef otherGridRef = obj as GridRef;
                var xMatches = otherGridRef.x == this.x;
                var zMatchex = otherGridRef.z == this.z;
                return xMatches && zMatchex ? 1 : 0;
            }

            public override string ToString() {
                return x +""+ z;
            }
        }

        public override string ToString() {
            return heading + " " + placeHash;
        }
    }

    /// <summary>
    /// Used to handle the occurance of dark places, where 
    /// exits cannot be reliably determined.
    /// </summary>
    public class DarkPlace : Place {
        public DarkPlace(Place currentPlace) {
            heading = "Too dark to tell";
            var NUM_EXITS = Enum.GetNames(typeof(Place.ExitDirections)).Length;
            exitHeadings = new string[NUM_EXITS];
            connectedPlaces = new Place[NUM_EXITS];
            connectedPlaceHashes = new int[NUM_EXITS];
            Interactables = new string[0];
            description = "";
            PlaceGridRef = currentPlace.GetOffsetGridRef(Player.me.attemptedMoveDirection);
            placeHash = PlaceGUID.GetHashCode();
        }
    }

}

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
        public string[] interactables;

        //Exits and connections.
        public enum ExitDirections { North, East, South, West }
        public string[/*North, East, South, West*/] exitHeadings;
        public int placeHash;
        public int[] connectedPlaceHashes;
        public int gridRefX, gridRefY;
        [XmlIgnore]
        public Place[/*North, East, South, West*/] connectedPlaces;

        [XmlIgnore]
        public string PlaceGUID {
            get {
                string del = "|";
                return heading + del +
                        description + del +
                        String.Join(del, ExitHeadingsBool.Select(b => b.ToString()).ToArray<string>());
            }
        }

        [XmlIgnore]
        public GridRef PlaceGridRef {
            get { return new GridRef(gridRefX, gridRefY); }
            set { gridRefX = value.x; gridRefY = value.y; }
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

        //TODO: apparently GetHashCode does not guarentee a unique 
        //      key, so we should use something else here. For now it will do.
        public override int GetHashCode() {
            return PlaceGUID.GetHashCode();
        }

        private Place() {}
        private Place(string heading, string description, string[] interactables, string[] exitHeadings) {
            this.heading = heading;
            this.description = description;
            this.interactables = interactables;
            this.exitHeadings = exitHeadings;
            connectedPlaces = new Place[exitHeadings.Length];
            connectedPlaceHashes = new int[exitHeadings.Length];
            placeHash = GetHashCode();
        }
        protected Place(Place placeInfo) { }

        /// <summary>
        /// Converts 
        /// </summary>
        /// <param name="placePattern"></param>
        //TODO: Don't let new lines in room names cause the chunk to process before its ready.
        public static void SetPlace(string placePattern) {
            string[] lines = placePattern.Split('\n').ToArray<string>();
            int i = 0;

            try {
                string heading = "";
                //Heading
                foreach (string line in lines) {
                    //Find heading and save it out.
                    if (new Regex(Patterns.PlacePatterns.headingPattern).IsMatch(line)) {
                        string newPlaceHeading;
                        newPlaceHeading = Regex.Replace(line, Patterns.PlacePatterns.startHeadingPattern, "");
                        newPlaceHeading = Regex.Replace(newPlaceHeading, Patterns.PlacePatterns.endHeadingPattern, "");
                        heading = newPlaceHeading.Replace("\r", "");
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

                //Interactable objects
                string[] interactables;
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
                        if (exit[0].Contains(((Place.ExitDirections)j).ToString())) exitHeadings[j] = exit[1].Replace("\r", "");
                    }
                    i++;
                }

                Place newPlace = new Place(heading, description, interactables, exitHeadings);
                Player.me.SetLocation(newPlace);
            } catch (IndexOutOfRangeException e){
                throw new IndexOutOfRangeException(placePattern + " " + e.Message);
            }
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
            interactables = info.GetValue("interactables", typeof(string[])) as string[];
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
                PlaceGridRef.y + positionOffsets[(int)direction].y
            );
        }


        public class GridRef : IComparable {
            public int x;
            public int y;
            public GridRef(int x, int y) {
                this.x = x;
                this.y = y;
            }

            public int CompareTo(object obj) {
                GridRef otherGridRef = obj as GridRef;
                return otherGridRef.x - this.x + otherGridRef.y - this.y;
            }
        }


    }
}

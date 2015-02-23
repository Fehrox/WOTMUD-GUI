using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
//using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace WoTParser {
    public class World{
        public static bool mapMode = true;
        public static List<Place> places = new List<Place>();
        public static WOTDateTime time;
        public class WOTDateTime {
            int hour, year;
            string month, day;

            public WOTDateTime(int hour, string day, string month, int year) {
                this.hour = hour;
                this.day = day;
                this.year = year;
                this.month = month;
            }

            public override string ToString() {
                return hour + ":00 " + day + "/" + month + "/" + year;
            }
        }

        public static void SetTime(string timeMatch) {
            //Isolate the info we don't need
            string[] requiredInfo = new string[]{Patterns.NUMBER, Patterns.WORD};
            requiredInfo = Patterns.WorldPatterns.timePattern.Split(' ').Except(requiredInfo).ToArray<string>();
            //remove that from the all the info
            string[] timeInfo = timeMatch.Split(' ').Except(requiredInfo).ToArray<string>();
            //Sanatise remaining info
            timeInfo = timeInfo.Select(t => t.Replace(",", "").Replace(".", "").Replace("\r", ""))
                               .Where(t => t != "").ToArray<string>();
            int hour = timeInfo[1] == "am" ? int.Parse(timeInfo[0]) : int.Parse(timeInfo[0]) + 12;
            time = new WOTDateTime(hour, timeInfo[2], timeInfo[3], int.Parse(timeInfo[4]));
        }

        public static bool ConnectPlaces(ref Place newPlace, ref Place currentPlace) {
            //Don't bother connecting a place to itself
            if (newPlace.placeHash == currentPlace.placeHash) return true;
            if (newPlace is DarkPlace || currentPlace is DarkPlace) return CreateConnection(newPlace, currentPlace);
            //var newPlaceHasDuplicateExits = newPlace.exitHeadings.Where(x => !String.IsNullOrEmpty(x)).GroupBy(x => x).Any(c => c.Count() > 1);
            //var oldPlaceHasDuplicateExits = currentPlace.exitHeadings.Where(x => !String.IsNullOrEmpty(x)).GroupBy(x => x).Any(c => c.Count() > 1);
            //if (newPlaceHasDuplicateExits || oldPlaceHasDuplicateExits) {
            //    int direction = (int)Player.me.attemptedMoveDirection;
            //    int invertDirection = (int)Place.InvertExitState(Player.me.attemptedMoveDirection);
            //    //Connect backward
            //    newPlace.connectedPlaces[invertDirection] = currentPlace;
            //    newPlace.connectedPlaceHashes[invertDirection] = currentPlace.GetHashCode();
            //    //Connect forward
            //    currentPlace.connectedPlaces[direction] = newPlace;
            //    currentPlace.connectedPlaceHashes[direction] = newPlace.GetHashCode();
            //    //Set up grid ref by backward connection
            //    var newGridRef = currentPlace.GetOffsetGridRef((Place.ExitDirections)direction);
            //    newPlace.PlaceGridRef = newGridRef;
            //    return true;
            //} else {

                //For every exit in the current place
                var numExitDirections = Enum.GetValues(typeof(WoTParser.Place.ExitDirections)).Length;
                for (int i = 0; i < numExitDirections; i++) {
                    //find the direction of the new place.
                    if (currentPlace.exitHeadings[i] == newPlace.heading) {
                        //if the direction of the new place, aims back at the old place
                        var j = (int)Place.InvertExitState((Place.ExitDirections)i);
                        if (newPlace.exitHeadings[j] == currentPlace.heading) {
                            return CreateConnection(newPlace, currentPlace);
                        }
                    }
                }
            //}

            Debug.LogError("Could not connect places " + currentPlace + " " + newPlace);
            return false;

        }

        private static bool CreateConnection(Place newPlace, Place currentPlace) {
            int direction = (int)Player.me.attemptedMoveDirection;
            int invertDirection = (int)Place.InvertExitState(Player.me.attemptedMoveDirection);
            //Set up grid ref by backward connection
            var newGridRef = currentPlace.GetOffsetGridRef((Place.ExitDirections)direction);
            if (!World.places.Any(p => p.gridRefX == newGridRef.x && p.gridRefZ == newGridRef.z)) {
                newPlace.PlaceGridRef = newGridRef;
                //Connect backward
                newPlace.connectedPlaces[invertDirection] = currentPlace;
                newPlace.connectedPlaceHashes[invertDirection] = currentPlace.GetHashCode();
                //Connect forward
                currentPlace.connectedPlaces[direction] = newPlace;
                currentPlace.connectedPlaceHashes[direction] = newPlace.GetHashCode();
                return true;
            } else {
                var conflictingPlace = World.places.Single(p => p.gridRefX == newGridRef.x && p.gridRefZ == newGridRef.z);
                Debug.LogWarning("Something already at " + conflictingPlace.PlaceGridRef + " called "+conflictingPlace.heading+". Refusing to make connection.");
                return false;
            }
        }

        public static bool HasPlace(Place place) {
            return places.Any(p => p.placeHash == place.placeHash);
        }

        public static void UpdatePlace(Place place) {
            places.RemoveAll(p => p.PlaceGridRef == place.PlaceGridRef);
            places.Add(place);
        }

        public static Place HasPlace(Place.GridRef gridRef){
            try {
                return places.First(p => p.gridRefX == gridRef.x && p.gridRefZ == gridRef.z);
            } catch (InvalidOperationException) {
                return null;
            }
        }

        public static Place GetPlaceAt(Place.GridRef gridRef) {
            return places.Where(p => p.PlaceGridRef == gridRef) as Place;
        }

        public class Persistance {
            private static string SavePath {
                get { return Player.me.character.name+".wdb"; }//return Path.Combine(Directory.GetCurrentDirectory(),"World.wdb"); }
            }

            public static void SaveWorld() {
                if (places.Count() > 0)//if recompile hasn't dropped world.
                    SerializeObject<List<Place>>(places, SavePath);
                places = null;
            }

            //World has been loaded.
            public delegate void EventArgsWorld(List<Place> world);
            public static event EventArgsWorld OnWorldLoaded;
            public static void LoadWorld() {
                places = DeSerializeObject<List<Place>>(SavePath);
                if (places == null) {
                    places = new List<Place>();
                } else {
                    ReconnectPlaces();
                }
                if (OnWorldLoaded != null)
                    OnWorldLoaded(places);
            }

            private static void ReconnectPlaces() {

                //Initialise Connected places
                foreach (Place disconnectedPlace in places) {
                    disconnectedPlace.connectedPlaces = new Place[disconnectedPlace.connectedPlaceHashes.Length];
                }

                //For every place
                foreach (Place thisPlace in places) {
                    //For every connectedPlace hash
                    foreach (Place otherPlace in places) {
                        //if we're not comparign the same place to itself
                        if (thisPlace.placeHash != otherPlace.placeHash) {
                            for (int i = 0; i < otherPlace.connectedPlaceHashes.Length; i++) {
                                if (otherPlace.connectedPlaceHashes[i] == thisPlace.placeHash) {
                                    otherPlace.connectedPlaces[i] = thisPlace;
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Serializes an object.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="serializableObject"></param>
            /// <param name="fileName"></param>
            public static void SerializeObject<T>(T serializableObject, string fileName) {
                if (serializableObject == null) {
                    Debug.LogWarning("The world contains no places");
                    return;
                }

                //try{
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                using (MemoryStream stream = new MemoryStream()) {
                    serializer.Serialize(stream, serializableObject);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(fileName);
                    stream.Close();
                }
                //}catch (Exception ex){
                //    //Log exception here
                //    Debug.Log(ex.Message, ConsoleColor.Red);
                //}
            }


            /// <summary>
            /// Deserializes an xml file into an object list
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="fileName"></param>
            /// <returns></returns>
            public static T DeSerializeObject<T>(string fileName) {
                if (string.IsNullOrEmpty(fileName)) { return default(T); }

                T objectOut = default(T);

                try {
                    string attributeXml = string.Empty;

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(fileName);
                    string xmlString = xmlDocument.OuterXml;

                    using (StringReader read = new StringReader(xmlString)) {
                        Type outType = typeof(T);

                        XmlSerializer serializer = new XmlSerializer(outType);
                        using (XmlReader reader = new XmlTextReader(read)) {
                            objectOut = (T)serializer.Deserialize(reader);
                            reader.Close();
                        }

                        read.Close();
                    }
                } catch (Exception ex) {
                    Debug.LogError(ex.Message);
                }

                return objectOut;
            }

        }
    }
}

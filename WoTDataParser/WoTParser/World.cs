using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
//using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace WoTParser {
    public class World {
        public static List<Place> places = new List<Place>();
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

        public static WOTDateTime time;
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
            //Don't connect places with ambiguous exit direcions
            string currentPlaceHeading = currentPlace.heading;
            if (newPlace.exitHeadings.Where(e => e == currentPlaceHeading).Count() > 1) {
                //UnityEngine.Debug.LogWarning(newPlace.heading + " has multiple exits named " + currentPlace.heading
                //    + ", direction can not be determined");
                return false;
            }
            //Connect this location to the last
            Place.ExitDirections direction = Place.ExitDirections.North;
            foreach (string newExitHeading in newPlace.exitHeadings) {
                //If there is an exit in this direcion
                if (newExitHeading != null) {
                    Console.WriteLine(newExitHeading + " " + currentPlace.heading);
                    //And its name matches our last new location
                    if (newExitHeading.Contains(currentPlace.heading) ){
                        //TODO: Check that the last direction moved matches this heading also.
                        //Connect these two places
                        Console.WriteLine("Connected " + newPlace.heading + " to "
                                     + currentPlace.heading, ConsoleColor.Yellow);
                        //Connect backward
                        int directionIntBackward = (int)direction;
                        newPlace.connectedPlaces[directionIntBackward] = currentPlace;
                        newPlace.connectedPlaceHashes[directionIntBackward] = currentPlace.GetHashCode();
                        //Connect forward
                        int directionIntForward = (int)Place.InvertExitState(direction);
                        currentPlace.connectedPlaces[directionIntForward] = newPlace;
                        currentPlace.connectedPlaceHashes[directionIntForward] = newPlace.GetHashCode();

                        //Set up grid ref
                        newPlace.PlaceGridRef = currentPlace.GetOffsetGridRef(direction);
                        
                        return true;//Connection created, stop processing.
                    }
                }
                direction = (Place.ExitDirections)(int)(direction + 1);
            }
            Console.WriteLine("Unable to connect given places " + newPlace.heading 
                                    + " and " + currentPlace.heading, ConsoleColor.Red);
            return false;
        }

        public static bool HasPlace(Place place) {
            foreach (Place knownPlaces in places) {
                if (knownPlaces.placeHash == place.placeHash) {
                    return true;
                }
            }
            return false;
        }

        public class Persistance {
            private static string ExecutablePath{
                get { return "World.wdb"; }//return Path.Combine(Directory.GetCurrentDirectory(),"World.wdb"); }
            }

            public static void SaveWorld() {
                if(places.Count() > 0)//if recompile hasn't dropped world.
                    SerializeObject<List<Place>>(places, ExecutablePath);
            }

            //World has been loaded.
            public delegate void EventArgsWorld(List<Place> world);
            public static event EventArgsWorld OnWorldLoaded;
            public static void LoadWorld() { 
                places = DeSerializeObject<List<Place>>(ExecutablePath); 
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
            public static void SerializeObject<T>(T serializableObject, string fileName){
                if (serializableObject == null) {
                    Console.WriteLine("The world contains no places", ConsoleColor.Red);    
                    return; 
                }

                //try{
                    XmlDocument xmlDocument = new XmlDocument();
                    XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                    using (MemoryStream stream = new MemoryStream()){
                        serializer.Serialize(stream, serializableObject);
                        stream.Position = 0;
                        xmlDocument.Load(stream);
                        xmlDocument.Save(fileName);
                        stream.Close();
                    }
                //}catch (Exception ex){
                //    //Log exception here
                //    Console.WriteLine(ex.Message, ConsoleColor.Red);
                //}
            }


            /// <summary>
            /// Deserializes an xml file into an object list
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="fileName"></param>
            /// <returns></returns>
            public static T DeSerializeObject<T>(string fileName){
                if (string.IsNullOrEmpty(fileName)) { return default(T); }

                T objectOut = default(T);

                try{
                    string attributeXml = string.Empty;

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(fileName);
                    string xmlString = xmlDocument.OuterXml;

                    using (StringReader read = new StringReader(xmlString)){
                        Type outType = typeof(T);

                        XmlSerializer serializer = new XmlSerializer(outType);
                        using (XmlReader reader = new XmlTextReader(read))
                        {
                            objectOut = (T)serializer.Deserialize(reader);
                            reader.Close();
                        }

                        read.Close();
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message, ConsoleColor.Red);
                }

                return objectOut;
            }

        }

    }
}

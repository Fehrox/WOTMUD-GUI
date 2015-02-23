using UnityEngine;
using System.Collections;
using WoTParser;
namespace Environment {
    public class PlaceNode : MonoBehaviour {

        public Place placeInfo;
        public PlaceNode[] connectedNodes = new PlaceNode[4];
        public GameObject[] exitIndicators = new GameObject[4];
        public GameObject darkPlaceIndicator;

        Vector3[] positionOffsets = new Vector3[]{
            new Vector3(0,0,1),//Z = N
            new Vector3(1,0,0),//X = E
            new Vector3(0,0,-1),//-Z = S
            new Vector3(-1,0,0)//-X = W
        };

        void Start() {
            DisplayExits();
        }

        public void DisplayExits() {
            var isDarkPlace = placeInfo is DarkPlace;
            darkPlaceIndicator.SetActive(isDarkPlace);
            if(!isDarkPlace)
                for (int exit = 0; exit < exitIndicators.Length; exit++) {
                    exitIndicators[exit].name = (WoTParser.Place.ExitDirections)exit + "_" + placeInfo.exitHeadings[exit];
                    exitIndicators[exit].SetActive(placeInfo.ExitHeadingsBool[exit]);
                }
        }

        public void ConnectPlaceTo(PlaceNode otherPlaceNode) {
            const int NORTH = (int)WoTParser.Place.ExitDirections.North,
                      WEST = (int)WoTParser.Place.ExitDirections.West;

            transform.position = new Vector3(placeInfo.gridRefX, 0, placeInfo.gridRefZ);

            //Determine the direction they connect
            for (int direction = NORTH; direction < WEST + 1; direction++) {
                if (placeInfo.connectedPlaces[direction] != null) {
                    //Test if it is the given other place 
                    if (placeInfo.connectedPlaces[direction].placeHash == otherPlaceNode.placeInfo.placeHash) {
                        //Connect places
                        connectedNodes[direction] = otherPlaceNode;
                        return;
                    }
                }
            }
            Debug.LogError(placeInfo.heading + " failed to connect to " + otherPlaceNode.placeInfo.heading, otherPlaceNode.gameObject);
        }

        public override string ToString() {
            return placeInfo.heading + " " + placeInfo.gridRefX + " " + placeInfo.gridRefZ;
        }

    }
}
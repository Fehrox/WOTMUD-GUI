using UnityEngine;
using System.Collections;
using WoTParser;
using System.Collections.Generic;
using System.Linq;

namespace Environment {

    public class WorldBuilder : MonoBehaviour {

        [SerializeField]
        List<PlaceNode> worldNodes = new List<PlaceNode>();
        GameObject placeNodePrefab;

        void Awake() {
            placeNodePrefab = Resources.Load("PlaceNode") as GameObject;
            if (placeNodePrefab == null) Debug.LogError("PlaceNode failed to load!");
            World.Persistance.OnWorldLoaded += OnWorldLoaded;
            Player.OnLocationChanged += OnLocationChanged;
            Player.OnFoundNewPlace += OnFoundNewPlace;
            Player.OnMovedToDarkPlace += OnMovedToDarkPlace;
            Player.OnLitPlace += OnLitPlace;
        }

        void OnMovedToDarkPlace() {
            //WoTUnityClient.Send("exits");
            Debug.LogWarning("Moved to dark place!");
        }

        void OnFoundNewPlace(Place newLocation) {
            PlaceNode newLocationNode = AddPlaceToWorld(newLocation);
            Debug.LogWarning("OnFoundNewPlace" + newLocation, newLocationNode.gameObject);
            MoveCameraToPlace(newLocationNode);
        }

        void OnLocationChanged(Place newLocation) {
            Debug.LogWarning("OnLocationChanged " + newLocation);
            //Debug.LogWarning("Location changed " + newLocation.heading + " " + newLocation.PlaceGridRef.x + " " + newLocation.PlaceGridRef.y);
            PlaceNode newLocationNode = worldNodes.Find(
                n => n.placeInfo.gridRefX == newLocation.gridRefX 
                    && n.placeInfo.gridRefZ == newLocation.gridRefZ);
            if (newLocationNode != null) {
                MoveCameraToPlace(newLocationNode);
                //Move player avatar to place
                PlayerAvatar.Movement.MoveToPlace(newLocationNode);
                //check if place reconnection is required
                ReConnectPlace(newLocationNode);
            } 
            //else {
            //    //TODO: we could make the new location here, but better to find how these are sliping though.
            //    Debug.LogError("Moved to unknown location " + newLocation);
            //    MoveCameraToPlace(AddPlaceToWorld(newLocation));
            //    PlayerAvatar.Movement.MoveToPlace(newLocationNode);
            //}

        }

        private void OnLitPlace(Place place) {
            Debug.LogWarning("***** LIT UP THIS BITCH: " + place.heading + "******");
        }

        void MoveCameraToPlace(PlaceNode newLocationNode) {
            if (newLocationNode != null) {
                Debug.Log("Moving camera to " + newLocationNode);
                Vector3 location = newLocationNode.gameObject.transform.position;
                Vector3 aboveLocation = new Vector3(location.x, location.y + 5.0f, location.z);
                Camera.main.gameObject.transform.position = aboveLocation;
                Debug.Log(Camera.main.gameObject.transform.position.ToString() + " " + aboveLocation.ToString());
            }
        }

        void OnWorldLoaded(List<Place> world) {
            Debug.LogWarning("World Loaded! " + world.Count);
            foreach (Place place in world) {
                AddPlaceToWorld(place);
            }

            //Find a list of disconnected nodes
            PlaceNode[] disconnectedPlaceNodes = worldNodes.Where(
                n => n.gameObject.transform.parent == null
            ).ToArray<PlaceNode>();
            //Connect them
            foreach (PlaceNode placeNode in disconnectedPlaceNodes) {
                ReConnectPlace(placeNode);
            }
            Debug.LogWarning(disconnectedPlaceNodes.Length);
        }

        PlaceNode AddPlaceToWorld(Place place) {
            
            //Create the node in the world
            GameObject placeNodeGO = Instantiate(placeNodePrefab) as GameObject;
            PlaceNode placeNode = placeNodeGO.GetComponent<PlaceNode>();
            placeNode.placeInfo = place;
            placeNodeGO.name = placeNode.placeInfo.heading;

            Debug.LogWarning("Adding place to world!" + place, placeNodeGO);

            //Cache a reference to it for later use
            worldNodes.Add(placeNode);
            ReConnectPlace(placeNode);
            return placeNode;
        }

        void ReConnectPlace(PlaceNode placeNode) {
            //if there's more than one place in the world
            if (placeNode.placeInfo.connectedPlaces.Any()) {
                // for all the palces this place knows about 
                foreach (Place connectedPlace in placeNode.placeInfo.connectedPlaces) {
                    if (connectedPlace != null) {
                        // find the place that matches it in the world.
                        foreach (PlaceNode worldPlaceNode in worldNodes) {
                            if (worldPlaceNode.placeInfo.placeHash == connectedPlace.placeHash) {
                                Debug.Log("Reconnecting " + worldPlaceNode.placeInfo.heading + " " + placeNode.placeInfo.heading);
                                //Reconnect those places
                                placeNode.ConnectPlaceTo(worldPlaceNode);
                            }
                        }
                    }
                }
            } else {
                Debug.LogError(placeNode.placeInfo.heading + " has no connected places");
            }
            //if (worldNodes.Count > 1) {
            //    //Find a node that has this place.
            //    IEnumerable<PlaceNode> connectedPlaceNodes =
            //        from node in worldNodes
            //        where node.placeInfo.connectedPlaces.Contains(placeNode.placeInfo)
            //        select node;

            //    if (connectedPlaceNodes.Count() > 0) {
            //        foreach (PlaceNode connectedPlace in connectedPlaceNodes.ToArray<PlaceNode>()) {
            //            Debug.Log(connectedPlace.placeInfo.heading
            //                + " " + placeNode.placeInfo.heading, placeNode.gameObject);
            //            //Move it into position
            //            placeNode.ConnectPlaceTo(connectedPlace);
            //        }
            //    }else {
            //        Debug.LogError("No places connected to " + placeNode.placeInfo.heading + " found");
            //    }

            //}
            //else {
            //    Debug.LogError("No places to connect to");
            //}
        }
    }
}

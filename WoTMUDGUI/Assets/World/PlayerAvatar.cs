using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WoTParser;

namespace Environment {

    public class PlayerAvatar : MonoBehaviour {
        
        public struct Position {

            public static int PlayerGridRefX {
                get { return (int)PlayerAvatarInstance.transform.position.x; }
                set {
                    PlayerAvatarInstance.transform.position = new Vector3(
                        value,
                        playerAvatarInstance.transform.position.y,
                        playerAvatarInstance.transform.position.z
                    );
                }
            }

            public static int PlayerGridRefZ {
                get { return (int)PlayerAvatarInstance.transform.position.z; }
                set {
                    PlayerAvatarInstance.transform.position = new Vector3(
                        playerAvatarInstance.transform.position.x,
                        playerAvatarInstance.transform.position.y,
                        value
                    );
                }
            }
        }

        public struct Movement {

            public static void Move(WoTParser.Place.ExitDirections dir) {
                switch (dir) {
                    case WoTParser.Place.ExitDirections.North:
                        MoveNorth();
                        break;
                    case WoTParser.Place.ExitDirections.East:
                        MoveEast();
                        break;
                    case WoTParser.Place.ExitDirections.South:
                        MoveSouth();
                        break;
                    case WoTParser.Place.ExitDirections.West:
                        MoveWest();
                        break;
                }
            }

            public static void MoveNorth() {
                Position.PlayerGridRefZ += 1;
            }

            public static void MoveEast() {
                Position.PlayerGridRefX += 1;
            }

            public static void MoveSouth() {
                Position.PlayerGridRefZ -= 1;
            }

            public static void MoveWest() {
                Position.PlayerGridRefX -= 1;
            }

            public static void MoveToPlace(PlaceNode newLocation) {
                Debug.LogWarning("Moving Player to place " + newLocation, newLocation.gameObject);
                PlayerAvatarInstance.transform.position = newLocation.transform.position;
                Position.PlayerGridRefX = newLocation.placeInfo.gridRefX;
                Position.PlayerGridRefZ = newLocation.placeInfo.gridRefZ;
            }

            public static void MoveToGridRef(int gridRefX, int gridRefZ) {
                PlayerAvatarInstance.transform.position = new Vector3(
                    gridRefX,
                    playerAvatarInstance.transform.position.y,
                    gridRefZ
                );
            }
        }

        static GameObject playerAvatarInstance;
        static GameObject PlayerAvatarInstance {
            get {
                if (playerAvatarInstance == null) {
                    playerAvatarInstance = Resources.Load("PlayerAvatar") as GameObject;
                    playerAvatarInstance = Instantiate(playerAvatarInstance) as GameObject;
                }
                return playerAvatarInstance;
            }
        }

    }
}
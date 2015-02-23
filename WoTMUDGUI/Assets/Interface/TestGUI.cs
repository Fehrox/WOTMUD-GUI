using UnityEngine;
using System.Collections;
using WoTParser;

namespace Interface {

    public class TestGUI : MonoBehaviour {

        // Update is called once per frame
        void Update() {
            if (Input.GetKey(KeyCode.UpArrow)) Mover.Move(Place.ExitDirections.North);
            if (Input.GetKey(KeyCode.RightArrow)) Mover.Move(Place.ExitDirections.East);
            if (Input.GetKey(KeyCode.DownArrow)) Mover.Move(Place.ExitDirections.South);
            if (Input.GetKey(KeyCode.LeftArrow)) Mover.Move(Place.ExitDirections.West);
        }

        void OnGUI() {

            GUILayout.Label("\n Locations in world: " + WoTParser.World.places.Count);
           

            float infoBoxWidth = 0.14f * Screen.width, infoBoxHeight = 0.05f * Screen.height;
            if (WoTParser.World.time != null) {
                Rect timeRect = new Rect(Screen.width - infoBoxWidth, 0.0f, infoBoxWidth, infoBoxHeight);
                GUI.Box(timeRect, WoTParser.World.time.ToString());
            }

            int i = 1;
            Rect nxtMoveRect = new Rect(Screen.width - infoBoxWidth, i++ * infoBoxHeight, infoBoxWidth, infoBoxHeight*2);
            GUI.Box(nxtMoveRect, "Next Move: " + Player.me.attemptedMoveDirection.ToString() 
                + "\n" + (Mover.CruiseControl.waitingForConfirmation ? "Waiting" : "Confirmed"));
            if (WoTParser.Player.me != null) {
                Rect hpRect = new Rect(Screen.width - infoBoxWidth, i++ + i++ * infoBoxHeight, infoBoxWidth, infoBoxHeight);
                GUI.Box(hpRect, WoTParser.Player.me.condition.hpState.ToString());
                Rect mvRect = new Rect(Screen.width - infoBoxWidth, i++ * infoBoxHeight, infoBoxWidth, infoBoxHeight);
                GUI.Box(mvRect, WoTParser.Player.me.condition.mvState.ToString());
                Rect postureRect = new Rect(Screen.width - infoBoxWidth, i++ * infoBoxHeight, infoBoxWidth, infoBoxHeight);
                GUI.Box(postureRect, WoTParser.Player.me.condition.postureState.ToString());
                Rect foodRect = new Rect(Screen.width - infoBoxWidth, i++ * infoBoxHeight, infoBoxWidth, infoBoxHeight);
                GUI.Box(foodRect, WoTParser.Player.me.condition.foodState.ToString());
                Rect drinkRect = new Rect(Screen.width - infoBoxWidth, i++ * infoBoxHeight, infoBoxWidth, infoBoxHeight);
                GUI.Box(drinkRect, WoTParser.Player.me.condition.drinkState.ToString());
            }

            if (WoTParser.Player.me.equipment.inventory != null) {
                Rect invRect = new Rect(
                    Screen.width - 2 * infoBoxWidth,
                    infoBoxHeight,
                    infoBoxWidth,
                    (WoTParser.Player.me.equipment.inventory.Length / 2) * infoBoxHeight
                );
                GUI.Box(invRect, System.String.Join("\n", WoTParser.Player.me.equipment.inventory));
            }
            if (WoTParser.Player.me.equipment.equipped != null) {
                Rect invRect = new Rect(
                    Screen.width - 3 * infoBoxWidth,
                    infoBoxHeight,
                    infoBoxWidth,
                    (WoTParser.Player.me.equipment.equipped.Length / 2) * infoBoxHeight
                );
                GUI.Box(invRect, System.String.Join("\n", WoTParser.Player.me.equipment.equipped));
            }

            if (WoTParser.Player.Location != null) {
                var placeRect = new Rect(0, 10.0f, 0.4f * Screen.width, WoTParser.Player.Location.Interactables.Length / 2 * infoBoxHeight + infoBoxHeight);
                var currentPlaceInfo = WoTParser.Player.Location.heading + "\n";
                foreach (var interactable in WoTParser.Player.Location.Interactables)
                    currentPlaceInfo += interactable + "\n";
                currentPlaceInfo += WoTParser.Player.Location.description;
                GUI.Box(placeRect, currentPlaceInfo);
            }
        }

    }
}
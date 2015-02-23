using System.Collections;
using WoTParser;
using UnityEngine;

public class Mover{

    static Mover() {
        //Listen for move relevent results
        Player.OnLocationChanged += CruiseControl.OnLocationChanged;
        Player.OnFoundNewPlace += CruiseControl.OnFoundNewPlace;
        Player.OnBadMove += CruiseControl.OnBadMove;
        Player.OnMovedToDarkPlace += CruiseControl.OnMovedToDarkPlace;
    }

    public struct CruiseControl {
        internal static bool waitingForConfirmation = false;

        internal static void ConfirmMove() { waitingForConfirmation = false; }
        internal static void ConfirmationPending() { waitingForConfirmation = true; }
        
        //Handle results of moves and flag move as complete
        internal static void OnLocationChanged(Place newLocation) {ConfirmMove();}
        internal static void OnFoundNewPlace(Place newLocation) {ConfirmMove();}
        internal static void OnBadMove() {ConfirmMove();}
        internal static void OnMovedToDarkPlace() { ConfirmMove(); }

    }

    internal static void Move(Place.ExitDirections direction) {
        if (CruiseControl.waitingForConfirmation) return;
        else {
            switch (direction) { 
                case Place.ExitDirections.North:
                    WoTUnityClient.Send("n\nexits");
                    break;
                case Place.ExitDirections.East:
                    WoTUnityClient.Send("e\nexits");
                    break;
                case Place.ExitDirections.South:
                    WoTUnityClient.Send("s\nexits");
                    break;
                case Place.ExitDirections.West:
                    WoTUnityClient.Send("w\nexits");
                    break;
            }
            Player.me.attemptedMoveDirection = direction;
            CruiseControl.ConfirmationPending();
            Coroutiner.StartCoroutine(TimeoutMove());
        }
    }

    internal static IEnumerator TimeoutMove(){
        var moveTime = Time.time;
        while (moveTime + 2.0f > Time.time && CruiseControl.waitingForConfirmation)
            yield return new WaitForSeconds(0.5f);
        if (CruiseControl.waitingForConfirmation) {
            Debug.LogError("Move requested but not confirmed.");
            //Moved but failed to recognise new place, tentatively move :\
            Environment.PlayerAvatar.Movement.Move(Player.me.attemptedMoveDirection);
            //CruiseControl.ConfirmMove();
            ReOrient();
        }   
    }

    internal static void ReOrient() {
        //Attempt to get the place again
        UnityEngine.Debug.LogWarning("Re-Orienting!");
        WoTUnityClient.Send("look\nexits");
        //Free up the player to move again, in case it fails.
        CruiseControl.ConfirmMove();
    }

}

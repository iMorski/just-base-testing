using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private FirebaseController FB;
    
    private void Start()
    {
        FB.MyData.Add("Position", "0:0");
        
        FB.ConnectionStepChange += OnConnectionStepChange;
        FB.RoomDataChange += OnRoomDataChange;
    }

    private void OnConnectionStepChange()
    {
        double ConnectionStep = FB.ConnectionStep;

        if (!(ConnectionStep != 1.0))
        {
            FB.Connect();
        }
        else if (!(ConnectionStep != 5.0))
        {
            
        }
    }

    private void OnRoomDataChange()
    {
        foreach (KeyValuePair<Dictionary<string, string>, string> Player in FB.RoomData)
        {
            string Name = Player.Value;
            
            foreach (KeyValuePair<string, string> Data in Player.Key)
            {
                
            }
        }
    }
    
    #if UNITY_EDITOR

        private void OnApplicationQuit()
        {
            //FB.Disconnect();
        }

    #else
        
        bool OnPauseSkip;
    
        private void OnApplicationPause(bool OnPause)
        {
            if (OnPause && OnPauseSkip)
            {
                //FirebaseController.Disconnect();
            }
            else
            {
                OnPauseSkip = true;
            }
        }
    #endif
}
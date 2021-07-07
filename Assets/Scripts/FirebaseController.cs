using System;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class FirebaseController : MonoBehaviour
{
    [SerializeField] private string BaseGroupNamePlayer;
    [SerializeField] private string BaseGroupNameRoom;
    [SerializeField] private string BaseNameLobby;
    [SerializeField] private string BaseNamePlayer;
    [SerializeField] private string BaseNameRoom;
    [SerializeField] private int BaseNameNumberCount;
    [SerializeField] private int RoomCapacity;
    
    private DatabaseReference BaseReference;
    private DatabaseReference BaseTracking;

    private readonly System.Random Random = new System.Random();

    [NonSerialized] public string MyName = "";
    [NonSerialized] public string MyRoom = "";
    
    [NonSerialized] public readonly Dictionary<string, string> MyData = new Dictionary<string, string>();
    [NonSerialized] public readonly Dictionary<Dictionary<string, string>, string> RoomData = new Dictionary<Dictionary<string, string>, string>();
    
    [NonSerialized] public double ConnectionStep;

    public delegate void OnConnectionStepChange();
    public event OnConnectionStepChange ConnectionStepChange;
    
    public delegate void OnConnectionChange(bool Connection);
    public event OnConnectionChange ConnectionChange;

    public delegate void OnRoomDataChange();
    public event OnRoomDataChange RoomDataChange;

    private bool ConnectionChangeSkip;
    
    private void Awake()
    {
        DontDestroyOnLoad(this);
        
        Debug.Log("Step: 0.0 - Checking resources");
        
        ConnectionStep = 0.0;
        ConnectionStepChange?.Invoke();
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(Task => 
        {
            if (Task.Result != DependencyStatus.Available) return;
            
            FirebaseDatabase.DefaultInstance.GetReference(".info/connected").ValueChanged += CheckConnectionChange;

            void CheckConnectionChange(object Sender, ValueChangedEventArgs Argument)
            {
                if (!ConnectionChangeSkip) { ConnectionChangeSkip = true; return; }
                
                bool Connection;
                
                if (Argument.Snapshot.Value.ToString() != "True") Connection = false;
                else Connection = true;

                ConnectionChange?.Invoke(Connection);
            }

            BaseReference = FirebaseDatabase.DefaultInstance.RootReference;
            
            Debug.Log("Step: 1.0 - Checking done");
            
            ConnectionStep = 1.0;
            ConnectionStepChange?.Invoke();
        });
    }
    
    private string GenerateKey(string BaseName)
    {
        string Key = BaseName;
        
        for (int i = 0; i < BaseNameNumberCount; i++)
        {
            Key = Key + Random.Next(0, 10);
        }

        return Key;
    }

    private bool Free(DataSnapshot Snapshot, string Key)
    {
        foreach (DataSnapshot Child in Snapshot.Children)
        {
            if (Child.Key != Key)
            {
                continue;
            }
                        
            return false;
        }
                    
        return true;
    }

    public void Connect()
    {
        Debug.Log("Step: 2.0 - Connecting");

        ConnectionStep = 2.0;
        ConnectionStepChange?.Invoke();
        
        BaseTracking = BaseReference;
        BaseTracking.ValueChanged += OnBaseChange;
    }
    
    private void OnBaseChange(object Sender, ValueChangedEventArgs Argument)
    {
        foreach (DataSnapshot Data in Argument.Snapshot.Children)
        {
            Debug.Log($"{Data.Key} - {Data.Value}");
        }
        
        Debug.Log(Argument.Snapshot.Children.Count());
    }

    /*

    public void Connect()
    {
        Debug.Log("Step: 2.0 - Connecting");
        
        ConnectionStep = 2.0;
        ConnectionStepChange?.Invoke();
        
        BaseReference.GetValueAsync().ContinueWith(Task =>
        {
            if (Task.IsFaulted) return;
                
            DataSnapshot GroupPlayer = Task.Result.Child(BaseGroupNamePlayer);
            DataSnapshot GroupRoom = Task.Result.Child(BaseGroupNameRoom);
            DataSnapshot Lobby = Task.Result.Child(BaseNameLobby);

            if (!(MyName != ""))
            {
                MyName = GenerateKey(BaseNamePlayer);

                while (!Free(GroupPlayer, MyName))
                {
                    MyName = GenerateKey(BaseNamePlayer);
                }
                
                Debug.Log($"Step: 2.0 - {MyName}");

                foreach (KeyValuePair<string, string> Data in MyData)
                {
                    BaseReference.Child(BaseGroupNamePlayer).Child(MyName).Child(Data.Key).SetValueAsync(Data.Value);
                }
            }

            if (Lobby.Children.Count() + 1 >= RoomCapacity)
            {
                Debug.Log("Step: 4.0 - Creating room");
                
                ConnectionStep = 4.0;
                ConnectionStepChange?.Invoke();
                
                MyRoom = GenerateKey(BaseNameRoom);

                while (!Free(GroupRoom, MyRoom))
                {
                    MyRoom = GenerateKey(BaseNameRoom);
                }
                
                Debug.Log($"Step: 4.0 - {MyRoom}");

                BaseTracking = BaseReference.Child(BaseGroupNameRoom).Child(MyRoom);
                BaseTracking.ValueChanged += OnRoomChange;
            }
            else
            {
                Debug.Log("Step: 3.0 - Joining lobby");
                
                ConnectionStep = 3.0;
                ConnectionStepChange?.Invoke();
                
                BaseTracking = BaseReference.Child(BaseNameLobby).Child(MyName);
                BaseTracking.ValueChanged += OnLobbyChange;
            }
        });
    }

    private void OnLobbyChange(object Sender, ValueChangedEventArgs Argument)
    {
        if (!(ConnectionStep != 3.1) && !(Argument.Snapshot.Value != null))
        {
            Debug.Log("Step: 3.2 - Receiving invitation");
            
            ConnectionStep = 3.2;
            ConnectionStepChange?.Invoke();
            
            BaseTracking.ValueChanged -= OnLobbyChange;
            BaseTracking = BaseReference.Child(BaseGroupNameRoom);
            BaseTracking.ValueChanged += OnGroupRoomChange;
        }
        else if (ConnectionStep != 3.1)
        {
            foreach (KeyValuePair<string, string> Data in MyData)
            {
                BaseTracking.Child(Data.Key).SetValueAsync(Data.Value);
            }
            
            Debug.Log("Step: 3.1 - Waiting in lobby");

            ConnectionStep = 3.1;
            ConnectionStepChange?.Invoke();
        }
    }

    private void OnGroupRoomChange(object Sender, ValueChangedEventArgs Argument)
    {
        string Search(DataSnapshot Snapshot, string Key)
        {
            string Room = "";
                        
            foreach (DataSnapshot Child in Snapshot.Children)
            {
                if (Child.Key != Key)
                {
                    Room = Search(Child, Key);

                    if (Room != "")
                    {
                        break;
                    }
                }
                else
                {
                    Room = Snapshot.Key;
                    
                    break;
                }
            }

            return Room;
        }
        
        Debug.Log("Step: 3.3 - Checking room list");
        
        ConnectionStep = 3.3;
        ConnectionStepChange?.Invoke();
        
        DataSnapshot GroupRoom = Argument.Snapshot;
        MyRoom = Search(GroupRoom, MyName);

        if (MyRoom != "")
        {
            Debug.Log($"Step: 3.4 - {MyRoom}");
            
            ConnectionStep = 3.4;
            ConnectionStepChange?.Invoke();
            
            BaseTracking.ValueChanged -= OnGroupRoomChange;
            BaseTracking = BaseReference.Child(BaseGroupNameRoom).Child(MyRoom);
            BaseTracking.ValueChanged += OnRoomChange;
        }
    }

    private void OnRoomChange(object Sender, ValueChangedEventArgs Argument)
    {
        void Collect(DataSnapshot Room)
        {
            RoomData.Clear();
        
            foreach (DataSnapshot Player in Room.Children)
            {
                Dictionary<string, string> Dictionary = new Dictionary<string, string>();
                
                foreach (DataSnapshot Data in Player.Children)
                {
                    Dictionary.Add(Data.Key, Data.Value.ToString());
                }
            
                RoomData.Add(Dictionary, Player.Key);
            }

            RoomDataChange?.Invoke();
        }
        
        DataSnapshot Room = Argument.Snapshot;
        
        if (Room.Children.Any())
        {
            if (ConnectionStep < 4 && ConnectionStep != 3.5)
            {
                Debug.Log("Step: 3.5 - Waiting in room");
                
                ConnectionStep = 3.5;
                ConnectionStepChange?.Invoke();
            }
            else if (ConnectionStep < 5 && ConnectionStep != 4.3)
            {
                Debug.Log("Step: 4.3 - Waiting in room");
                
                ConnectionStep = 4.3;
                ConnectionStepChange?.Invoke();
            }
            
            if (!(ConnectionStep != 5))
            {
                if (Room.Children.Count() < RoomCapacity)
                {
                    Disconnect();
                }
                else
                {
                    Collect(Room);
                }
            }
            else if (!(Room.Children.Count() != RoomCapacity))
            {
                Collect(Room);
                
                Debug.Log("Step: 5.0 - Ready");

                ConnectionStep = 5.0;
                ConnectionStepChange?.Invoke();
            }
        }
        else
        {
            Debug.Log("Step: 4.1 - Checking lobby");
            
            ConnectionStep = 4.1;
            ConnectionStepChange?.Invoke();

            BaseReference.Child(BaseNameLobby).GetValueAsync().ContinueWith(Task =>
            {
                if (Task.IsFaulted) return;
                
                DataSnapshot Lobby = Task.Result;
                
                foreach (KeyValuePair<string, string> Data in MyData)
                {
                    BaseTracking.Child(MyName).Child(Data.Key).SetValueAsync(Data.Value);
                }

                for (int i = 1; i < RoomCapacity; i++)
                {
                    string Player = Lobby.Children.ElementAt(Lobby.Children.Count() - i).Key;
                    
                    foreach (DataSnapshot Data in Lobby.Child(Player).Children)
                    {
                        BaseTracking.Child(Player).Child(Data.Key).SetValueAsync(Data.Value);
                    }
                    
                    Debug.Log($"Step: 4.2 - Inviting {Player}");
                
                    ConnectionStep = 4.2;
                    ConnectionStepChange?.Invoke();

                    BaseReference.Child(BaseNameLobby).Child(Player).SetValueAsync(null);
                }
            });
        }
    }

    public void Write()
    {
        if (MyName != "")
        {
            foreach (KeyValuePair<string, string> Data in MyData)
            {
                BaseReference.Child(BaseGroupNamePlayer).Child(MyName).Child(Data.Key).SetValueAsync(Data.Value);
            
                if (MyRoom != "")
                {
                    BaseReference.Child(BaseGroupNameRoom).Child(MyRoom).Child(MyName).Child(Data.Key).SetValueAsync(Data.Value);
                }
            }
        }
    }
    
    public void Disconnect()
    {
        if (MyName != "")
        {
            if (MyRoom != "")
            {
                if (BaseTracking != null)
                {
                    BaseTracking.ValueChanged -= OnRoomChange;
                }
                
                BaseReference.Child(BaseGroupNameRoom).Child(MyRoom).Child(MyName).SetValueAsync(null);
                
                MyRoom = "";
            }
            else
            {
                if (BaseTracking != null)
                {
                    BaseTracking.ValueChanged -= OnLobbyChange;
                    BaseTracking.ValueChanged -= OnGroupRoomChange;
                }

                BaseReference.Child(BaseNameLobby).Child(MyName).SetValueAsync(null);
            }
            
            BaseReference.Child(BaseGroupNamePlayer).Child(MyName).SetValueAsync(null);

            MyName = "";
        }
        
        MyData.Clear();
        RoomData.Clear();

        ConnectionStep = 0;
    }
    
    */
}
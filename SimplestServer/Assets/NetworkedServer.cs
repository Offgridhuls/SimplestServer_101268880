using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using System.Linq;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

    private List<PlayerAccount> playerAccounts = new List<PlayerAccount>();

    Match newMatch = new Match();
    // Start is called before the first frame update

    void Start()
    {

        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);


    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                var player = newMatch.connectedAccounts.FirstOrDefault(a => a.connectionID == recConnectionID);
                if(player != null)
                {
                    newMatch.connectedAccounts.Remove(player);
                }
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }
  
    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);

    }

    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        string[] csv = msg.Split(',');

        int signifier = int.Parse(csv[0]);


        if (signifier == ClientToServerSignifiers.createAccount)
        {
            string n = csv[1];
            string p = csv[2];
            bool nameIsInUse = false;

            foreach (PlayerAccount pa in playerAccounts)
            {
                if (pa.name == n)
                    nameIsInUse = true;
            }

            if (nameIsInUse)
            {
                SendMessageToClient(ServerToClientSignifiers.accountCreationFailed + ",", id);
               
            }

            else
            {
                PlayerAccount newPlayerAccount = new PlayerAccount(n, p, id);

                playerAccounts.Add(newPlayerAccount);

                SendMessageToClient(ServerToClientSignifiers.accountCreationComplete + ",", id);
            }
        }
        else if (signifier == ClientToServerSignifiers.login)
        {
            var account = playerAccounts.FirstOrDefault(p => p.name == csv[1] && p.password == csv[2]);
            if(account != null)
            {
                SendMessageToClient(ServerToClientSignifiers.loginComplete + "," , id);
                newMatch.connectedAccounts.Add(account);

                if(newMatch.connectedAccounts.Count > Match.maxGameClients)
                {
                    SendMessageToClient(ServerToClientSignifiers.waiting + ",0", id);

                }
                else if(newMatch.connectedAccounts.Count > 1)
                {
                    int randomUser = UnityEngine.Random.Range(0, 1);
                    SendMessageToClient(ServerToClientSignifiers.playTurn + ",1", newMatch.connectedAccounts[randomUser].connectionID);
                    newMatch.connectedAccounts[randomUser].isO = true;

                    newMatch.connectedAccounts[(randomUser == 0 ? 1 : 0)].isO = false;
                    SendMessageToClient(ServerToClientSignifiers.playTurn + ",-1", newMatch.connectedAccounts[(randomUser == 0 ? 1 : 0)].connectionID);

                }
                else if(newMatch.connectedAccounts.Count == 1)
                {
                    SendMessageToClient(ServerToClientSignifiers.waiting + ",1", id);

                }
             

            }
        }
        else if(signifier == ClientToServerSignifiers.sendPlay)
        {
            if(int.TryParse(csv[1], out var result))
            {
                var player = newMatch.connectedAccounts.FirstOrDefault(x => x.connectionID == id);
                if(player != null)
                {
                    newMatch.gameData[result] = player.isO ? 1 : 0;
                    newMatch.isWinner(player.isO ? 1 : 0);
                    if (newMatch.isWinner(player.isO ? 1 : 0))
                    {
                        foreach(var p in newMatch.connectedAccounts)
                        {
                            if(p == player)
                            {
                                SendMessageToClient(ServerToClientSignifiers.winner + ",1", p.connectionID);
                            }
                            else
                            {
                                SendMessageToClient(ServerToClientSignifiers.winner + ",0," + player.name, p.connectionID);
                            }
                        }
                    }
                    else
                    {

                        SendMessageToClient(ServerToClientSignifiers.playTurn + ",0", )
                    }
                }
            }
        }
    }
}


public class PlayerAccount
{
    public string name, password;

    public int connectionID;

    public bool isO;
    public PlayerAccount(string Name, string Password, int ID)
    {
        name = Name;
        password = Password;

        connectionID = ID;
    }
}

public static class ClientToServerSignifiers
{
    public const int createAccount = 1;

    public const int login = 2;

    public const int sendPlay = 3;
}

public static class ServerToClientSignifiers
{
    public const int loginComplete = 1;

    public const int loginFailed = 2;

    public const int accountCreationComplete = 3;

    public const int accountCreationFailed = 4;

    public const int isSpectator = 5;

    public const int waiting = 6;

    public const int playTurn = 7;

    public const int winner = 8;

}

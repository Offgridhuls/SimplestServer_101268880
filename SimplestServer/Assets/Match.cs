using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match
{

    public List<PlayerAccount> connectedAccounts = new List<PlayerAccount>();

    public static int maxGameClients = 2;

    public int[] gameData = {-1,-1,-1,-1,-1,-1,-1,-1,-1};


    public bool isWinner(int id)
    {
        if(gameData[0] == id && gameData[1] == id && gameData[2] == id)
        {
            return true;
        }
        else if(gameData[3] == id && gameData[4] == id && gameData[5] == id)
        {
            return true;
        }
        else if (gameData[6] == id && gameData[7] == id && gameData[8] == id)
        {
            return true;
        }
        else if (gameData[0] == id && gameData[3] == id && gameData[6] == id)
        {
            return true;
        }
        else if (gameData[1] == id && gameData[4] == id && gameData[7] == id)
        {
            return true;
        }
        else if (gameData[2] == id && gameData[5] == id && gameData[8] == id)
        {
            return true;
        }
        else if (gameData[0] == id && gameData[4] == id && gameData[8] == id)
        {
            return true;
        }
        else if (gameData[2] == id && gameData[4] == id && gameData[6] == id)
        {
            return true;
        }

        return false;
    }
}

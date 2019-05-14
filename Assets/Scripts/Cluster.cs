using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Cluster : NetworkBehaviour
{
    public List<NetworkPiece> pieces;
    [SyncVar]
    public int id;
    [SyncVar]
    public bool grabbed;

    void Awake(){
        pieces = new List<NetworkPiece>();
    }
}

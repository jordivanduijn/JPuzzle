using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkBehaviour
{
    public GameObject networkPiecePrefab;
    public GameObject clusterPrefab;
    public Puzzle puzzle;
        
    public override void OnStartServer()
    {        
        List<PieceData> piecesData = puzzle.GeneratePiecesData();
        
        foreach(PieceData data in piecesData){

            GameObject clusterGameObject = Instantiate(clusterPrefab);
            clusterGameObject.name = "Cluster"+data.id;
            Cluster cluster = clusterGameObject.GetComponent<Cluster>();
            cluster.id = data.id;

            GameObject networkPieceGameObject = Instantiate(networkPiecePrefab);
            NetworkPiece networkPiece = networkPieceGameObject.GetComponent<NetworkPiece>();
            
            networkPiece.Setup(data);
            networkPieceGameObject.transform.position = data.position;
            networkPieceGameObject.name = "NetworkPiece"+data.id;
            networkPiece.clusterID = cluster.id;
            
            NetworkServer.Spawn(clusterGameObject);
            NetworkServer.Spawn(networkPieceGameObject);
        }
    }

    public string getLocalPlayerID(){
        Player[] players = FindObjectsOfType<Player>();
        Debug.Log(players.Length);
        foreach(Player p in players){
            if(p.isLocalPlayer) {
                Debug.Log("Local Player id: "+p.netId.ToString());
                return p.netId.ToString();
            }
        }

        Debug.Log("No local player found.");
        return null;
    }
}

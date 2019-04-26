using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkBehaviour
{
    public GameObject piecePrefab;
    public Puzzle puzzle;
    
    private List<GameObject> pieces;

    public override void OnStartServer()
    {
        Debug.Log("Game Started");
        
        //generate all the piece data in a seperate class
        
        List<PieceData> piecesData = puzzle.GeneratePiecesData();

        pieces = new List<GameObject>();  
        foreach(PieceData data in piecesData){
            GameObject pieceObject = Instantiate(piecePrefab);

            //only set id and position for the server spawn, the rest is setup on the clients side (in Piece.Start())
            pieceObject.GetComponent<Piece>().id = data.id;
            pieceObject.transform.position = data.position;
            
            NetworkServer.Spawn(pieceObject);
            pieces.Add(pieceObject);
        }
    }
}

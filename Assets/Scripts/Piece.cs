using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class Piece : NetworkBehaviour
{
    //synchronise id so the pieceData of this piece can be retrieved from the puzzle when this piece is instantiated at a client
    [SyncVar]
    public int id;

    public List<int> neighbours;
    public Vector3 correctPosition;

    public float rotationThreshold = 6f;
    public float distanceThreshold = 0.1f;

    void Start(){
        Puzzle puzzle = GameObject.Find("Puzzle").GetComponent<Puzzle>();
        PieceData data = puzzle.GetPieceData(id);
        Setup(data);
    }

    public void Setup(PieceData data){
        correctPosition = data.position;
        neighbours = data.neighbours;
        GetComponent<MeshFilter>().mesh = data.mesh;

        BoxCollider2D boxCol = gameObject.GetComponent<BoxCollider2D>();
        boxCol.size = data.mesh.bounds.size;
    }

    public bool isCorrect(Piece other){

        //check if is neighbour
        if(!neighbours.Contains(other.id)) return false;

        //check relative rotation
        float zRot = transform.rotation.eulerAngles.z;
        float zRotOther = other.transform.rotation.eulerAngles.z;
        if(Mathf.Abs(zRot - zRot) > rotationThreshold) return false;

        //check relative position
        float distance = (transform.position - other.transform.position).magnitude;
        float correctDistance = (correctPosition - other.correctPosition).magnitude;
        if(Mathf.Abs(distance - correctDistance) > distanceThreshold) return false;

        return true;
    }
}

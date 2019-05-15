using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public NetworkPiece networkPiece;

    void Start(){
        transform.position = networkPiece.transform.position;
        transform.rotation = networkPiece.transform.rotation;

        Puzzle puzzle = GameObject.Find("Puzzle").GetComponent<Puzzle>();
        PieceData data = puzzle.GetPieceData(networkPiece.id);
        Setup(data);

        Renderer renderer = GetComponent<MeshRenderer>();
        renderer.materials = new Material[]{
            puzzle.imageMaterial,
            puzzle.backsideMaterial
        };
    }

    void Update(){   
        //moveTowardsNetworkTransform();
    }

    // void moveTowardsNetworkTransform(){
    //     //if the piece is grabbed by the local player, instantly move it
    //     if(networkPiece.grabbedByLocalPlayer && false){
    //         transform.position = networkPiece.transform.position;
    //         transform.rotation = networkPiece.transform.rotation;
    //     }
        
    //     //if not, smoothly move it towards the network position (in world space)
    //     else {
    //         transform.rotation = networkPiece.transform.rotation;
    //         Vector3 dir = networkPiece.transform.position - transform.position;
    //         transform.Translate(dir * 0.4f, Space.World);
    //     }

    //     if(transform.position.z > 0.01f) transform.position = new Vector3(transform.position.x, transform.position.y, 0.0f);
    // }

    public void Setup(PieceData data){
        GetComponent<MeshFilter>().mesh = data.mesh;
        MeshCollider collider = gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = data.colliderMesh;
    }
}

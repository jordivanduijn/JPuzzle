using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkPiece : NetworkBehaviour
{
    public GameObject piecePrefab;

    [SyncVar]
    public int id;
    //public bool grabbedByLocalPlayer = false;
    [SyncVar(hook = "ClusterIDChanged")]
    public int clusterID;
    public Cluster cluster;

    private Piece physicalPiece;

    public void Setup(PieceData p){
        this.id = p.id;
    }
    
    void Start()
    {
        //make child of the cluster
        SetCluster(clusterID);

        //instantiate the physical piece (the one the player will interact with)
        GameObject pieceObject = Instantiate(piecePrefab);

        //create a mutual connection between the physical piece and it's networked counterpart
        physicalPiece = pieceObject.GetComponent<Piece>();
        physicalPiece.networkPiece = this;

        //set self as parent of the physical piece
        physicalPiece.transform.SetParent(transform);

        //add self to the puzzle       
        Puzzle puzzle = GameObject.Find("Puzzle").GetComponent<Puzzle>();
        puzzle.addNetworkPiece(this);
    }
    
    void ClusterIDChanged(int newClusterID){
        clusterID = newClusterID;
        SetCluster(clusterID);
    }

    void SetCluster(int parentClusterID){
        foreach(Cluster c in GameObject.FindObjectsOfType<Cluster>()){
            if(c.id == parentClusterID){
                if(cluster != c){

                    //clear parent
                    transform.SetParent(null);

                    //remove this piece from the old cluster
                    RemoveMeFromMyCluster();

                    //then add it to the new cluster
                    cluster = c;
                    transform.SetParent(cluster.transform);
                    if(!cluster.pieces.Contains(this)) cluster.pieces.Add(this);
                }
                break;
            }
        }
    }

    void OnDestroy(){
        RemoveMeFromMyCluster();
        if(physicalPiece != null) Destroy(physicalPiece.gameObject);
    }

    void RemoveMeFromMyCluster(){
        if(cluster != null){
            cluster.pieces.Remove(this);

            //if the cluster doesn't hold any more pieces, destroy it after a short delay
            if(cluster.pieces.Count == 0) Destroy(cluster.gameObject, 1);
        }
    }
}

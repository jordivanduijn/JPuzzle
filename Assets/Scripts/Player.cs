using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
    public Puzzle puzzle;
    public GameObject clusterPrefab;
    
    private NetworkPiece draggingPiece = null;
    private Piece lastPiece = null;
    private Vector3 mousePos;

    private Ray ray;
    private RaycastHit hit;

    private Plane playerHeightPlane;

    public override void OnStartLocalPlayer(){
        puzzle = GameObject.Find("Puzzle").GetComponent<Puzzle>();
        playerHeightPlane = new Plane(new Vector3(0,0,-1), new Vector3(0,0,-puzzle.thickness));
    }

    void Update()
    {
        if(!isLocalPlayer) return;        
        
        //every frame, update this player's position to the mouse position
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float enter;
        playerHeightPlane.Raycast(ray, out enter);
        transform.position = ray.GetPoint(enter);

        //rotate player (and effectively a piece when the player is holding one) on scroll
        float mouseRotation = Input.GetAxis("Mouse ScrollWheel");
        if (mouseRotation != 0f) {
            transform.Rotate(0,0,Mathf.Sign(mouseRotation) * -5f);
        }

        //if a piece was being dragged and the player releases it, detach it from the player
        if(draggingPiece != null && Input.GetMouseButtonUp(0)){
            ReleasePiece();
        }

        //check if and which piece is clicked on, and attach it to the player
        Physics.Raycast(ray, out hit);
        if(hit.collider != null && Input.GetMouseButtonDown(0) && hit.collider.gameObject.GetComponent<Piece>() != null){
            GrabPiece(hit.collider.gameObject);
        }
    }

    void ReleasePiece(){       
        
        //tell the server to unparent this piece from this player (and check/connect if the piece fits other pieces)
        CmdReleasePiece(draggingPiece.GetComponent<NetworkIdentity>().netId);

        //re-enable the network transform of this piece's cluster so it get's updated when other players move it
        EnableNetworkTransform(draggingPiece.cluster.gameObject);

        //locally unparent this piece or its cluster
        draggingPiece.cluster.transform.SetParent(null);

        //forget that this player is dragging a piece
        draggingPiece.cluster.grabbed = false;
        //draggingPiece.grabbedByLocalPlayer = false;
        draggingPiece = null;
    }

    void GrabPiece(GameObject obj){

        //keep track of the piece that this player is dragging
        draggingPiece = obj.GetComponent<Piece>().networkPiece;
        //don't grab the piece if it's already grabbed by another player
        if(draggingPiece.cluster.grabbed) return;

        draggingPiece.cluster.grabbed = true;
        //draggingPiece.grabbedByLocalPlayer = isLocalPlayer;

        //disable the network transform for this piece (for this player only) to prevent updates from the server while dragging
        DisableNetworkTransform(draggingPiece.cluster.gameObject);

        //locally parent this piece's to the player
        draggingPiece.cluster.transform.SetParent(transform);

        //tell the server to parent this piece to the player so the other players will know
        CmdGrabPiece(draggingPiece.GetComponent<NetworkIdentity>().netId, netId);
    }

    [Command]
    void CmdGrabPiece(NetworkInstanceId pieceId, NetworkInstanceId parentId){
        NetworkPiece piece = NetworkServer.FindLocalObject(pieceId).GetComponent<NetworkPiece>();

        //don't grab the piece if it's already grabbed by another player
        if(piece.cluster.grabbed) return;

        GameObject parent = NetworkServer.FindLocalObject(parentId);
        piece.cluster.transform.SetParent(parent.transform);
        piece.cluster.grabbed = true;
    }

    [Command]
    void CmdReleasePiece(NetworkInstanceId pieceId){
        NetworkPiece releasedPiece = NetworkServer.FindLocalObject(pieceId).GetComponent<NetworkPiece>();
        releasedPiece.cluster.grabbed = false;
        releasedPiece.cluster.transform.SetParent(null);
        
        Puzzle puzzle = GameObject.Find("Puzzle").GetComponent<Puzzle>();
        List<Cluster> clustersThatFit = puzzle.possibleFits(releasedPiece.id);

        //stop here if no fits were found
        if(clustersThatFit.Count == 0) return;

        //find the biggest cluster
        Cluster biggestCluster = releasedPiece.cluster;
        foreach(Cluster c in clustersThatFit){
            if(c.pieces.Count >= biggestCluster.pieces.Count) biggestCluster = c;
        }

        if(biggestCluster != releasedPiece.cluster){
            clustersThatFit.Remove(biggestCluster);
            clustersThatFit.Add(releasedPiece.cluster);
        }

        NetworkPiece snapPiece = biggestCluster.pieces[0];
        Vector3 positionalSnapOffset;
        float rotationalSnapOffset;
        
        //merge the clusters of the fitting pieces
        foreach(Cluster fittingCluster in clustersThatFit){

            NetworkPiece firstPiece = fittingCluster.pieces[0];
            positionalSnapOffset = GetSnapOffset(firstPiece, snapPiece);
            rotationalSnapOffset = snapPiece.transform.rotation.eulerAngles.z - firstPiece.transform.rotation.eulerAngles.z;

            fittingCluster.transform.RotateAround(firstPiece.transform.position,new Vector3(0,0,1),rotationalSnapOffset);
            fittingCluster.transform.Translate(positionalSnapOffset);
            
            List<NetworkPiece> piecesToBeMerged = new List<NetworkPiece>();                
            fittingCluster.pieces.ForEach(p => piecesToBeMerged.Add(p));
            //change the id an a seperate loop, because changing the id results in the cluster.pieces List to be altered
            //and looping through a list while also altering it results in errors
            foreach(NetworkPiece p in piecesToBeMerged){
                p.clusterID = biggestCluster.id;
            }
        }
    }

    public Vector3 GetSnapOffset(NetworkPiece p1, NetworkPiece p2){
        PieceData pD1 = puzzle.GetPieceData(p1.id);
        PieceData pD2 = puzzle.GetPieceData(p2.id);

        Vector3 relativeOffset = p2.transform.position - p1.transform.position;
        //make correction for current rotation
        relativeOffset = Quaternion.AngleAxis(-p2.transform.eulerAngles.z, new Vector3(0,0,1)) * relativeOffset;        
        Vector3 correctRelativeOffset = pD2.position - pD1.position; 
        
        return relativeOffset - correctRelativeOffset;
    }

    void EnableNetworkTransform(GameObject obj){
        //server has smooth motion, no need to enable or disable network transform
        if(isServer) return;
        NetworkTransform netTrans = obj.GetComponent<NetworkTransform>();
        netTrans.transformSyncMode = NetworkTransform.TransformSyncMode.SyncTransform;
    }

    void DisableNetworkTransform(GameObject obj){
        //server has smooth motion, no need to enable or disable network transform
        if(isServer) return;
        NetworkTransform netTrans = obj.GetComponent<NetworkTransform>();
        netTrans.transformSyncMode = NetworkTransform.TransformSyncMode.SyncNone;
    }

    struct SnapInfo {
        NetworkPiece piece;
        Vector3 position;
        Vector3 rotation;
    }
}

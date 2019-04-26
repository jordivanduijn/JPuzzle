using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
    private GameObject draggingPiece = null;
    private Piece lastPiece = null;
    private Vector3 mousePos;

    public override void OnStartLocalPlayer(){
        
    }

    void Update()
    {
        if(!isLocalPlayer) return;        
        
        //every frame, update this player's position to the mouse position
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x, mousePos.y, 0f);

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
        if(Input.GetMouseButtonDown(0)){
            GrabPiece();
        }
    }

    void ReleasePiece(){
        //tell the server to unparent this piece from this player
        CmdReleasePiece(draggingPiece.GetComponent<NetworkIdentity>().netId);

        //re-enable the network transform of this piece so it get's updated when other players move it
        EnableNetworkTransform(draggingPiece);

        //locally unparent this piece
        draggingPiece.transform.SetParent(null);

        //forget that this player is dragging a piece
        draggingPiece = null;
    }

    void GrabPiece(){
        //create a raycast to check if the player clicks on a piece
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);            
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider != null) {
            //keep track of the piece that this player is dragging
            draggingPiece = hit.collider.gameObject;

            //disable the network transform for this piece (for this player only) to prevent updates from the server while dragging
            DisableNetworkTransform(draggingPiece);

            //locally parent this piece to the player
            draggingPiece.transform.SetParent(transform);

            //tell the server to parent this piece to the player so the other players will know
            CmdGrabPiece(draggingPiece.GetComponent<NetworkIdentity>().netId, netId);
        }
    }

    [Command]
    void CmdGrabPiece(NetworkInstanceId pieceId, NetworkInstanceId parentId){
        GameObject piece = NetworkServer.FindLocalObject(pieceId);
        GameObject parent = NetworkServer.FindLocalObject(parentId);
        piece.transform.SetParent(parent.transform);
    }

    [Command]
    void CmdReleasePiece(NetworkInstanceId pieceId){  
        GameObject piece = NetworkServer.FindLocalObject(pieceId);
        piece.transform.SetParent(null);
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
}

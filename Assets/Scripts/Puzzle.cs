using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    public Texture2D image;
    public int numPieces = 25;
    public float scale = 10.0f; //a puzzle size of 10 means the diagonal (of the bounding box) will be 10 units
    public Material material;

    private Vector2 pSize;
    private List<PieceData> piecesData;

    void Awake(){
        float factor = Mathf.Sqrt(image.width*image.width + image.height*image.height) / scale;
        pSize = new Vector2(image.width / factor, image.height / factor);
        material.SetTexture("_MainTex", image);
        GeneratePiecesData();
    }

    public PieceData GetPieceData(int pieceID){
        foreach(PieceData data in piecesData){
            if(data.id == pieceID) return data;
        }
        return new PieceData();
    }

    public List<PieceData> GeneratePiecesData(){
        piecesData = new List<PieceData>();

        float aspect = image.width / image.height;
        
        //rough estimation for now, definitely not final
        int numCols = (int) Mathf.Floor(Mathf.Sqrt(aspect) * Mathf.Sqrt(numPieces));
        int numRows = (int) Mathf.Ceil((float)numPieces / numCols);
        
        float pieceWidth = pSize.x / numCols;
        float pieceHeight = pSize.y / numRows;
        
        int row = 0;
        int col = 0;
        for(int i = 0; i < numPieces; i++){

            Vector3 position = new Vector3(col * pieceWidth - pSize.x*0.5f, row * pieceHeight - pSize.y*0.5f, 0);
            List<int> neighbours = new List<int>();

            //horizontal neighbours
            if(col > 0) neighbours.Add(i-1);
            if(col < (numCols-1) && i < (numPieces-1)) neighbours.Add(i+1);

            //vertical neighbours
            if(row > 0) neighbours.Add(i-numCols);
            if(row < (numRows-1) && (i+numCols) < numPieces) neighbours.Add(i+numCols);

            Vector2 baseUV = new Vector2((float)col/numCols, (float)row/numRows);
            Mesh mesh = GeneratePieceMesh(new Vector2(pieceWidth, pieceHeight), baseUV);
        
            PieceData newPiece = new PieceData(i,position,neighbours,mesh);
            piecesData.Add(newPiece);

            col++;
            if(col >= numCols){
                col = 0;
                row++;
            }
        }

        return piecesData;
    }

    private Mesh GeneratePieceMesh(Vector2 pieceSize, Vector2 baseUV){
        Mesh mesh = new Mesh();

        Vector2 halfSize = pieceSize * 0.5f;
        Vector2 uvPieceSize = pieceSize / pSize;

        Vector3[] vertices = new Vector3[]{
            new Vector3(-halfSize.x,-halfSize.y, 0),
            new Vector3(-halfSize.x, halfSize.y, 0),
            new Vector3( halfSize.x,-halfSize.y, 0),
            new Vector3( halfSize.x, halfSize.y, 0),
        };

        int[] triangles = new int[]{0,1,2,2,1,3};

        Vector2[] uv = new Vector2[]{
            baseUV,
            new Vector2(baseUV.x, baseUV.y + uvPieceSize.y),
            new Vector2(baseUV.x + uvPieceSize.x, baseUV.y),
            new Vector2(baseUV.x + uvPieceSize.x, baseUV.y + uvPieceSize.y),
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }
}

public struct PieceData
{
    public int id;
    public Vector3 position;
    public List<int> neighbours;
    public Mesh mesh;

    public PieceData(int newId, Vector3 newPos, List<int> newNeighbours, Mesh newMesh)
    {
        id = newId;
        position = newPos;
        neighbours = newNeighbours;
        mesh = newMesh;
    }

    public override string ToString(){
        string s = "id: "+id+" ";
        s += "pos: ("+position.x+","+position.y+","+position.z+")\r\n";
        s += "neighbours: ["+string.Join( ",", neighbours.ToArray() )+"]";
        return s;
    }
}
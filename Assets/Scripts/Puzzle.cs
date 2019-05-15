using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle : MonoBehaviour
{
    public Texture2D image;
    public Texture2D noise;
    public int numPieces = 25;
    public float scale = 10.0f; //a puzzle size of 10 means the diagonal (of the bounding box) will be 10 units
    public Material material;
    public Material backsideMaterial;

    public float rotationSnapThreshold = 7f;
    public float positionSnapThreshold = 0.2f;

    [Range(0.01f, 0.5f)]
    public float thickness = 0.1f;
    public int meshDetail = 40;
    public int colliderDetail = 10;
    
    private float width, height;
    private List<PieceData> piecesData;
    private List<NetworkPiece> networkPieces;

    void Awake(){
        float factor = Mathf.Sqrt(image.width*image.width + image.height*image.height) / scale;
        width = image.width / factor;
        height = image.height / factor;
        material.SetTexture("_MainTex", image);
        GeneratePiecesData();
    }

    public PieceData GetPieceData(int pieceID){
        foreach(PieceData p in piecesData){
            if(p.id == pieceID) return p;
        }
        return null;
    }

    public NetworkPiece GetNetworkPiece(int pieceID){
        foreach(NetworkPiece p in networkPieces){
            if(p.id == pieceID) return p;
        }
        return null;
    }

    public void addNetworkPiece(NetworkPiece p){
        networkPieces.Add(p);
    }
    
    public List<Cluster> possibleFits(int droppedPieceId){

        List<Cluster> clustersThatFit = new List<Cluster>();
        NetworkPiece droppedPiece = GetNetworkPiece(droppedPieceId);
        
        // go through all neighbours of pieces that have to be checked and check if anything fits
        foreach(NetworkPiece pieceToCheck in droppedPiece.cluster.pieces){
            foreach(Neighbour neighbourOfPieceToCheck in GetPieceData(pieceToCheck.id).neighbours){
                //continue if this 'neighbour' is the outer edge of the puzzle or if it is already connected
                if(neighbourOfPieceToCheck.id == -1 || neighbourOfPieceToCheck.connected) continue;

                NetworkPiece neighbourPiece = GetNetworkPiece(neighbourOfPieceToCheck.id);

                //skip this piece if it's part of cluster that is already being merged or part of the same cluster
                if(clustersThatFit.Contains(neighbourPiece.cluster) || neighbourPiece.cluster == droppedPiece.cluster) continue;                
                
                if(Fit(pieceToCheck, neighbourPiece, neighbourOfPieceToCheck.correctRelativePosition)){                  
                    
                    //create mutual connection in neighbours (so connected neighbours won't be checked again)
                    Neighbour pieceToCheckAsNeighbour = GetPieceData(neighbourPiece.id).GetNeigbourWithID(pieceToCheck.id);
                    pieceToCheckAsNeighbour.connected = true;
                    neighbourOfPieceToCheck.connected = true;

                    clustersThatFit.Add(neighbourPiece.cluster);
                }
            }
        }
        return clustersThatFit;
    }

    public bool Fit(NetworkPiece p1, NetworkPiece p2, Vector2 correctRelativePositionP1toP2){
        
        //first check relative rotation
        float zRot1 = p1.transform.rotation.eulerAngles.z;
        float zRot2 = p2.transform.rotation.eulerAngles.z;
        float angleOffset = Mathf.Abs(zRot1 - zRot2);
        if(angleOffset > rotationSnapThreshold) return false;

        //then check distance
        float distance = Mathf.Abs((p2.transform.position - p1.transform.position).magnitude - correctRelativePositionP1toP2.magnitude);
        if(distance > positionSnapThreshold) return false;

        //then check relative direction
        Vector2 relativeDir = Quaternion.AngleAxis(-zRot1, new Vector3(0,0,1)) * (p2.transform.position - p1.transform.position);
        if(Vector2.Angle(relativeDir, correctRelativePositionP1toP2) > rotationSnapThreshold) return false;
        
        return true;
    }

    public List<PieceData> GeneratePiecesData(){

        if(piecesData == null) piecesData = piecesData = new List<PieceData>();
        //don't re-generate
        if(piecesData.Count > 0) return piecesData;
        
        networkPieces = new List<NetworkPiece>();

        float aspect = image.width / image.height;
        
        //rough estimation for now, definitely not final
        int numCols = (int) Mathf.Floor(Mathf.Sqrt(aspect) * Mathf.Sqrt(numPieces));
        int numRows = (int) Mathf.Ceil((float)numPieces / numCols);
        
        float pieceWidth = width / numCols;
        float pieceHeight = height / numRows;

        Vector2[] baseVertices = GenerateRectangularBase(pieceWidth, pieceHeight);
        
        int row = 0;
        int col = 0;
        for(int i = 0; i < numPieces; i++){

            Vector3 position = new Vector3( (col + 0.5f) * pieceWidth - width * 0.5f, (row + 0.5f) * pieceHeight - height * 0.5f, 0);
            List<Neighbour> neighbours = new List<Neighbour>();

            //left neighbour
            if(col > 0) neighbours.Add(  new Neighbour(i-1, new Vector2(-pieceWidth, 0), 0, 1)  );
            else neighbours.Add(  new Neighbour(-1, Vector2.zero, 0, 1)  );

            //top neighbour
            if(row < (numRows-1) && (i+numCols) < numPieces) neighbours.Add(  new Neighbour(i+numCols, new Vector2(0, pieceHeight), 1, 2)  );
            else neighbours.Add(  new Neighbour(-1, Vector2.zero, 1, 2)  );

            //right neighbour
            if(col < (numCols-1) && i < (numPieces-1)) neighbours.Add(  new Neighbour(i+1, new Vector2(pieceWidth, 0), 2, 3)  );
            else neighbours.Add(  new Neighbour(-1, Vector2.zero, 2, 3)  );

            //bottom neighbour
            if(row > 0) neighbours.Add(  new Neighbour(i-numCols, new Vector2(0, -pieceHeight), 3, 0)  );
            else neighbours.Add(  new Neighbour(-1, Vector2.zero, 3, 0)  );
        
            PieceData newPiece = new PieceData(i, position, neighbours, baseVertices);
            piecesData.Add(newPiece);

            col++;
            if(col >= numCols){
                col = 0;
                row++;
            }
        }

        //generate mesh afterwards when all neighbours are known
        piecesData.ForEach(p => GeneratePieceMesh(p));

        return piecesData;
    }

    private Vector2[] GenerateRectangularBase(float pieceWidth, float pieceHeight){
        float halfWidth = pieceWidth * 0.5f;
        float halfHeight = pieceHeight * 0.5f;

        Vector2[] baseVertices = new Vector2[]{
            new Vector2(-halfWidth,-halfHeight),
            new Vector2(-halfWidth, halfHeight),
            new Vector2( halfWidth, halfHeight),
            new Vector2( halfWidth,-halfHeight),
        };

        return baseVertices;
    }

    private void GeneratePieceMesh(PieceData p){

        //use HashSet to prevent double vertices
        HashSet<Vector2> vertices2DSet = new HashSet<Vector2>();
        HashSet<Vector2> vertices2DColliderSet = new HashSet<Vector2>();
        
        foreach(Neighbour n in p.neighbours){
            
            //if this 'neighbour' is the boundary, set edgeType to straight
            if(n.id == -1){
                n.edgeType = PEdge.EdgeType.Straight;
            }

            //else if the edgeType wasn't set to something yet, set it here 
            //and also set the corresponding neighbours edgeType to the opposite type
            else if(n.edgeType == PEdge.EdgeType.Nothing){
                PieceData neighbourPiece = GetPieceData(n.id);
                Neighbour meAsNeighbour = neighbourPiece.GetNeigbourWithID(p.id);

                if(randomBool(p.id)){
                    n.edgeType = PEdge.EdgeType.Shaped;
                    meAsNeighbour.edgeType = PEdge.EdgeType.ShapedInverse;
                } else {
                    n.edgeType = PEdge.EdgeType.ShapedInverse;
                    meAsNeighbour.edgeType = PEdge.EdgeType.Shaped;
                }
            }
            
            PEdge edge = new PEdge(p.baseVertices[n.v1], p.baseVertices[n.v2], n.edgeType, meshDetail);
            PEdge edgeLowDetail = new PEdge(p.baseVertices[n.v1], p.baseVertices[n.v2], n.edgeType, colliderDetail);

            edge.points.ForEach(point => vertices2DSet.Add(point));
            edgeLowDetail.points.ForEach(point => vertices2DColliderSet.Add(point));
        }

        List<Vector2> vertices2D = vertices2DSet.ToList();
        List<Vector2> vertices2DCollider = vertices2DColliderSet.ToList();
        
        //create final visual mesh
        Triangulator triangulator = new Triangulator(vertices2D);
        int[] triangles = triangulator.Triangulate();
        Vector3[] vertices = vertices2D.ConvertAll(v2 => new Vector3(v2.x, v2.y, 0f)).ToArray();
        p.mesh = MeshFunctions.ExtrudeFlatPiece(vertices, triangles, thickness);
        ProjectUV(p.mesh, p.position);
        
        //create final collider mesh
        triangulator = new Triangulator(vertices2DCollider);
        int[] colliderTriangles = triangulator.Triangulate();
        Vector3[] colliderVertices = vertices2DCollider.ConvertAll(v2 => new Vector3(v2.x, v2.y, 0f)).ToArray();
        p.colliderMesh = MeshFunctions.ExtrudeFlatPieceNoSplit(colliderVertices, colliderTriangles, thickness);
    }

    private void ProjectUV(Mesh mesh, Vector3 piecePosition){
        List<Vector2> uv = new List<Vector2>();
        Vector2 halfVector = new Vector2(.5f,.5f);
        foreach(Vector3 vertex in mesh.vertices){
            Vector2 vertexPuzzlePos = (Vector2) (vertex + piecePosition);
            Vector2 newUV = (vertexPuzzlePos / new Vector2(width, height)) + halfVector;
            uv.Add(newUV);
        }
        mesh.uv = uv.ToArray();
    }

    private bool randomBool(int n){
        
        n = n % (noise.width * noise.height);

        int x = n % noise.width;
        int y = n / noise.height;
        
        Color c = noise.GetPixel(x, y);

        return c.r >= 0.5f;
    }
}

public class PieceData
{
    public int id;
    public Vector3 position;
    public List<Neighbour> neighbours;
    public Vector2[] baseVertices;
    public Mesh mesh;
    public Mesh colliderMesh;
    public MeshCollider collider;

    public PieceData(int id, Vector3 position, List<Neighbour> neighbours, Vector2[] baseVertices)
    {
        this.id = id;
        this.position = position;
        this.neighbours = neighbours;
        this.baseVertices = baseVertices;
    }

    public override string ToString(){
        string s = "id: "+id+" ";
        s += "pos: ("+position.x+","+position.y+","+position.z+")\r\n";
        s += "neighbours: ["+string.Join<Neighbour>( ",", neighbours.ToArray() )+"]";
        return s;
    }

    public Neighbour GetNeigbourWithID(int neighbourID){
        foreach(Neighbour n in neighbours){
            if(n.id == neighbourID) return n;
        }
        return null;
    }
}

public class Neighbour
{
    public int id;
    public Vector2 correctRelativePosition;
    public int v1, v2;
    public PEdge.EdgeType edgeType;
    public bool connected;

    public Neighbour(int id, Vector2 correctRelativePosition, int v1, int v2){
        this.id = id;
        this.correctRelativePosition = correctRelativePosition;
        this.v1 = v1;
        this.v2 = v2;
        this.edgeType = PEdge.EdgeType.Nothing;
    }

    public override string ToString(){
        return id+"";
    }
}
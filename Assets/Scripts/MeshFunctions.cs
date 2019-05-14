using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshFunctions {

    //assumption 1: only 'outer' vertices exist
    //assumption 2: all vertex indices are ordered in clockwise order
    //this function extrudes a mesh and splits the rim from the top and bottom (by duplicating vertices)
    public static Mesh ExtrudeFlatPiece(Vector3[] vertices, int[] triangles, float extrudeDist){

        Mesh mesh = new Mesh();

        int nV = vertices.Length;
        int nT = triangles.Length;
        Vector3[] newVerts = new Vector3[nV * 4];
        List<int> newTris = new List<int>();

         //add bottom triangles in reverse order (to flip normals)
        for(int i = 0; i < nT; i+=3){  
            newTris.Add(triangles[i+2]);
            newTris.Add(triangles[i+1]);
            newTris.Add(triangles[i]);
        }
                
        //extrude up
        for(int i = 0; i < nV; i++){
            Vector3 v = vertices[i];

            //extrude original vertex up
            Vector3 extrudedVertex = new Vector3(v.x, v.y, v.z - extrudeDist);

            newVerts[i] = v;
            newVerts[i+nV] = v;
            newVerts[i+nV*2] = extrudedVertex;
            newVerts[i+nV*3] = extrudedVertex;

            //add triangles for the rim (two per vertex)
            newTris.Add(i + nV);
            newTris.Add(i == nV-1 ? 2*nV : i + 2*nV + 1);
            newTris.Add(i + nV*2);

            newTris.Add(i + nV);
            newTris.Add(i == nV-1 ? nV : i + 1 + nV);
            newTris.Add(i == nV-1 ? 2*nV : i + 2*nV + 1);
        }

        //add top triangles
        for(int i = 0; i < nT; i+=3){  
            newTris.Add(triangles[i]   + nV*3);
            newTris.Add(triangles[i+1] + nV*3);
            newTris.Add(triangles[i+2] + nV*3);
        }

        mesh.vertices = newVerts;
        mesh.triangles = newTris.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    //assumption 1: only 'outer' vertices exist
    //assumption 2: all vertex indices are ordered in clockwise order
    public static Mesh ExtrudeFlatPieceNoSplit(Vector3[] vertices, int[] triangles, float extrudeDist){

        Mesh mesh = new Mesh();

        int nV = vertices.Length;
        int nT = triangles.Length;
        Vector3[] newVerts = new Vector3[nV * 2];
        List<int> newTris = new List<int>();

         //add bottom triangles in reverse order (to flip normals)
        for(int i = 0; i < nT; i+=3){  
            newTris.Add(triangles[i+2]);
            newTris.Add(triangles[i+1]);
            newTris.Add(triangles[i]);
        }
                
        //extrude up
        for(int i = 0; i < nV; i++){
            Vector3 v = vertices[i];

            //extrude original vertex up
            Vector3 extrudedVertex = new Vector3(v.x, v.y, v.z - extrudeDist);
            
            //add new vertices
            newVerts[i] = v;
            newVerts[i+nV] = extrudedVertex;

            //add triangles for the rim (two per vertex)
            newTris.Add(i);
            newTris.Add(i == nV-1 ? nV : i + nV + 1);
            newTris.Add(i + nV);

            newTris.Add(i);
            newTris.Add(i == nV-1 ? 0 : i + 1);
            newTris.Add(i == nV-1 ? nV : i + nV + 1);
        }

        //add top triangles
        for(int i = 0; i < nT; i+=3){  
            newTris.Add(triangles[i]   + nV);
            newTris.Add(triangles[i+1] + nV);
            newTris.Add(triangles[i+2] + nV);
        }

        mesh.vertices = newVerts;
        mesh.triangles = newTris.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }
}
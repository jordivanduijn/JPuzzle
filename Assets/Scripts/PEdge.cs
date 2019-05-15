using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This class represents an edge in a cycle of length @c.
 * The edge is defined by the s and t, respectively the
 * source and target vertex of the edge.
 * The edge is in clockwise order if t = s + 1 mod c,
 * otherwise it is considered counter-clockwise.
 * 
 * The shape of the edge is a uniform cubic spline defined by
 * a set of control points @points, describing a curve from the
 * euclidean point (0,0) to (0,1).
 * 
 * An edge can be given an embedding: giving the source and target
 * of the edge planar coordinates.
 */
public class PEdge // : IComparer
{
	public Vector2 source;
	public Vector2 target;
	public List<Vector2> points;
  
  public enum EdgeType {Nothing, Straight, Shaped, ShapedInverse};

	public PEdge (Vector2 source, Vector2 target, EdgeType type, int precision)
	{
    switch(type){
      case EdgeType.Straight:
        points = new List<Vector2>{source, target};
        break;
      case EdgeType.Shaped:
        this.source = source;
        this.target = target;   
        points = BuildEdge(DefaultEdge(), precision);
        break;
      case EdgeType.ShapedInverse:
        this.source = target;
        this.target = source;   
        points = BuildEdge(DefaultEdge(), precision);
        points.Reverse();
        break;
    }
	}

	public void AddControlPoint (Vector2 a)
	{
		points.Add(a);
	}

	public List<Vector2> BuildEdge (List<Vector2> controlPoints, int precision)
	{
		var result = new List<Vector2> ();

		var approx = Splines.Approximate(controlPoints, precision);	
		
		approx.ForEach(p => result.Add(Splines.ComplexMultiply (p, target - source) + source));
		result.Add(target);
    
		return result;
	}

	// int IComparer.Compare (object a, object b)
	// {
	// 	return 0;
	// }

	private List<Vector2> DefaultEdge()
	{
		List<Vector2> cPoints = new List<Vector2> ();
		float norm = 1 / 12.0f;
		cPoints.Add( new Vector2 (0, 0) * norm);
		cPoints.Add( new Vector2 (2, 0) * norm);
		cPoints.Add( new Vector2 (6, -1) * norm);
		cPoints.Add( new Vector2 (4, 1) * norm);
		cPoints.Add( new Vector2 (4, 3) * norm);
		cPoints.Add( new Vector2 (6, 4) * norm);
		cPoints.Add( new Vector2 (8, 3) * norm);
		cPoints.Add( new Vector2 (8, 1) * norm);
		cPoints.Add( new Vector2 (6, -1) * norm);
		cPoints.Add( new Vector2 (10, 0) * norm);
		cPoints.Add( new Vector2 (12, 0) * norm);

		return cPoints;
	}
}
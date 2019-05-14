using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Splines {

	public static List<Vector2> Approximate(List<Vector2> points, int precision)
	{
		if (points.Count < 3)
			return new List<Vector2> ();

		var curvePoints = new List<Vector2> (precision - 1);

		points.Insert (0, points [0]);
		points.Insert (0, points [0]);
		points.Add( points [points.Count - 1]);
		points.Add( points [points.Count - 1]);

		for (var i = 1; i < precision; i++) {
			var progress = (points.Count - 4) * i / ((float)precision); 
			var current = Mathf.FloorToInt (progress);
			Matrix4x4 m = VectorMatrix (points [current], points [current + 1], points [current + 2], points [current + 3]);
			curvePoints.Add(m * BSplineWeights (progress - current));
		}

		return curvePoints;
	}

	public static Matrix4x4 VectorMatrix (Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		var m = new Matrix4x4 ();
		m.SetColumn (0, (Vector4) a);
		m.SetColumn (1, (Vector4) b);
		m.SetColumn (2, (Vector4) c);
		m.SetColumn (3, (Vector4) d);
		return m;
	}

	public static Vector4 BSplineWeights (float t)
	{
		var t2 = Mathf.Pow (t, 2.0f);
		var t3 = Mathf.Pow (t, 3.0f);
		var b0 = Mathf.Pow (1.0f - t, 3.0f) / 6.0f;
		var b1 = (3.0f * t3 - 6.0f * t2 + 4.0f) / 6.0f;
		var b2 = (-3.0f * t3 + 3.0f * t2 + 3.0f * t + 1.0f) / 6.0f;
		var b3 = t3 / 6.0f;
		return new Vector4 (b0, b1, b2, b3);
	}

	public static Vector2 ComplexMultiply(Vector2 a, Vector2 b)
	{
		return new Vector2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
	}
}

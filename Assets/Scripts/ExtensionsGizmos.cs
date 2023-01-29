//#define DEBUG_FRUSTUM_PLANES
//#define DEBUG_FRUSTUM_LINES

#define DEBUG_FRUSTUM_PROJECTION

using System;
using UnityEngine;

#if UNITY_EDITOR

#endif

public static class ExtensionsGizmos
{
	private const float DIM = 1.0f;
	private const float AXIS_GAP = 0.7f;
	private const float DEFAULT_SIZE = .1f;

	public static CxPivotMeta ToMeta(this CxPivot source)
	{
		return new()
		{
			Source = source,
			Size = DEFAULT_SIZE,
			Color = Color.yellow,
			Type = CxPivotMeta.DrawingType.Cross,
		};
	}

	public static void DrawPoint(this Vector3 position, Color color, float size = DEFAULT_SIZE, float duration = 0f)
	{
		var oneOpposite = new Vector3(-1f, 1f);
		Debug.DrawLine(position + Vector3.one * size, position - Vector3.one * size, color, duration);
		Debug.DrawLine(position + oneOpposite * size, position - oneOpposite * size, color, duration);
	}

	private static void DrawCross(this Vector3 position, Quaternion rotation, Color color, float size, float duration)
	{
		Debug.DrawLine(position + rotation * Vector3.up * size * AXIS_GAP, position + rotation * Vector3.up * size, Color.green * DIM, duration);
		Debug.DrawLine(position, position + rotation * Vector3.up * size * AXIS_GAP, color * DIM, duration);
		Debug.DrawLine(position, position - rotation * Vector3.up * size, color * DIM, duration);

		Debug.DrawLine(position + rotation * Vector3.right * size * AXIS_GAP, position + rotation * Vector3.right * size, Color.red * DIM, duration);
		Debug.DrawLine(position, position + rotation * Vector3.right * size * AXIS_GAP, color * DIM, duration);
		Debug.DrawLine(position, position - rotation * Vector3.right * size, color * DIM, duration);

		Debug.DrawLine(position + rotation * Vector3.forward * size * AXIS_GAP, position + rotation * Vector3.forward * size, Color.blue * DIM, duration);
		Debug.DrawLine(position, position + rotation * Vector3.forward * size * AXIS_GAP, color * DIM, duration);
		Debug.DrawLine(position, position - rotation * Vector3.forward * size, color * DIM, duration);
	}

	public static void DrawArrow(this Vector3 source, Vector3 pointTo, Color color, Color colorArrow, float duration = 0)
	{
		var dir = pointTo - source;
		Debug.DrawLine(source, source + dir * .8f, color, duration);
		Debug.DrawLine(source + dir * .8f, source + dir, colorArrow, duration);
	}

	public static void DrawSector(this Vector3[] vectors, Vector3 center, Color color, bool isGradient, float duration = 0f)
	{
		if(vectors.Length == 0)
		{
			return;
		}

		Debug.DrawLine(center, vectors[0], color, duration);
		for(var index = 1; index < vectors.Length; index++)
		{
			var colorTemp = isGradient
				? Color.Lerp(color, color * .3f, (float)index / (vectors.Length - 1))
				: color;
			Debug.DrawLine(center, vectors[index], colorTemp);
			Debug.DrawLine(vectors[index - 1], vectors[index], colorTemp);
		}
	}

	public static void DrawPolyClosed(this Vector3[] source, Color color)
	{
		for(var index = 0; index < source.Length; index++)
		{
			var next = (index + 1) % source.Length;
			Debug.DrawLine(source[index], source[next], color);
		}
	}

	private static void DrawCircle(this CxPivot cxPivot, float radius, Color color, float duration = 0f)
	{
		const int HALF_PRECISION = 12;
		var vector = Vector3.up * radius;
		var step = Quaternion.AngleAxis(Mathf.Rad2Deg * Mathf.PI / HALF_PRECISION, Vector3.forward);
		for(var index = 0; index < HALF_PRECISION * 2; index++)
		{
			var next = step * vector;
			Debug.DrawLine(
				cxPivot.ConvertSpaceOf(vector),
				cxPivot.ConvertSpaceOf(next),
				color,
				duration);
			vector = next;
		}
	}

	public static void Draw(this Plane source, Color color)
	{
		var origin = source.normal * -source.distance;
		const float STEP = Mathf.PI / 12f;
		var unit = Quaternion.FromToRotation(Vector3.up, source.normal) * Vector3.left;
		for(var delta = 0f; delta < 2f * Mathf.PI; delta += Mathf.PI / 12f)
		{
			Debug.DrawLine(
				origin + Quaternion.AngleAxis(delta * Mathf.Rad2Deg, source.normal) * unit,
				origin + Quaternion.AngleAxis((delta + STEP) * Mathf.Rad2Deg, source.normal) * unit,
				color);
		}

		Debug.DrawLine(origin, origin + source.normal * 1.2f, Color.cyan);
		//origin.DrawCross(Quaternion.LookRotation(source.normal), color);
		//origin.DrawCross(Quaternion.identity, color);
	}

	public static void Draw(this Ray source, Color color)
	{
		Debug.DrawLine(source.origin, source.origin + source.direction, color);
	}

	public static void Draw(this CxPivotMeta source)
	{
		if(source.Type == CxPivotMeta.DrawingType.Cross)
		{
			source.Source.Origin.DrawCross(source.Source.Rotation, source.Color, source.Size, source.Duration);
		}
		else if(source.Type == CxPivotMeta.DrawingType.Circle)
		{
			source.Source.DrawCircle(source.Size, source.Color, source.Duration);
		}
	}

	public static Vector3 ConvertSpaceOf(this CxPivot source, Vector3 target)
	{
		return source.Rotation * target + source.Origin;
	}

	private const float SCALE_GLOBAL_F = .03f;

	public static void DrawVectorAt(this Vector2 vector, Vector2 position, float scale = 1f)
	{
		using(new WithColor(Color.blue))
		{
			Gizmos.DrawLine(position, position + vector.normalized * (scale * SCALE_GLOBAL_F));
		}
	}

	public static void DrawAsPoint(this Vector3 position, float scale = 1f)
	{
		DrawAsPoint((Vector2)position, Quaternion.identity, scale);
	}

	public static void DrawAsPoint(this Vector3 position, Quaternion orient, float scale = 1f)
	{
		DrawAsPoint((Vector2)position, orient, scale);
	}

	public static void DrawAsPoint(this Vector2 position, float scale = 1f)
	{
		DrawAsPoint(position, Quaternion.identity, scale);
	}

	public static void DrawAsPoint(this Vector2 position, Quaternion orient, float scale = 1f, float duration = .5f)
	{
		if(Application.isPlaying)
		{
			scale *= SCALE_GLOBAL_F;
			var up = (Vector2)(orient * Vector3.up);
			var down = (Vector2)(orient * Vector3.down);
			var left = (Vector2)(orient * Vector3.left);
			var right = (Vector2)(orient * Vector3.right);
			Debug.DrawLine(position, position + down * scale, Color.black, duration);
			Debug.DrawLine(position, position + left * scale, Color.black, duration);
			Debug.DrawLine(position, position + right * scale, Color.red, duration);
			Debug.DrawLine(position, position + up * scale, Color.green, duration);
		}
		else
		{
			using(new WithColor(Color.black))
			{
				scale *= SCALE_GLOBAL_F;
				var up = (Vector2)(orient * Vector3.up);
				var down = (Vector2)(orient * Vector3.down);
				var left = (Vector2)(orient * Vector3.left);
				var right = (Vector2)(orient * Vector3.right);
				Gizmos.DrawLine(position, position + down * scale);
				Gizmos.DrawLine(position, position + left * scale);
				Gizmos.color = Color.red;
				Gizmos.DrawLine(position, position + right * scale);
				Gizmos.color = Color.green;
				Gizmos.DrawLine(position, position + up * scale);
			}
		}
	}

	public static void DrawAsKnot(this Vector3 position, float scale = 1f)
	{
		DrawAsKnot((Vector2)position, Quaternion.identity, scale);
	}

	public static void DrawAsKnot(this Vector3 position, Quaternion orient, float scale = 1f)
	{
		DrawAsKnot((Vector2)position, orient, scale);
	}

	public static void DrawAsKnot(this Vector2 position, float scale = 1f)
	{
		DrawAsKnot(position, Quaternion.identity, scale);
	}

	public static void DrawAsKnot(this Vector2 position, Quaternion orient, float scale = 1f)
	{
		using(new WithColor(Color.gray))
		{
			scale *= SCALE_GLOBAL_F * .5f;
			var p0 = (Vector2)(orient * new Vector3(1f, 1f) * scale) + position;
			var p1 = (Vector2)(orient * new Vector3(1f, -1f) * scale) + position;
			var p2 = (Vector2)(orient * new Vector3(-1f, -1f) * scale) + position;
			var p3 = (Vector2)(orient * new Vector3(-1f, 1f) * scale) + position;
			Gizmos.DrawLine(p0, p1);
			Gizmos.DrawLine(p1, p2);
			Gizmos.DrawLine(p2, p3);
			Gizmos.DrawLine(p3, p0);
		}
	}
}

public struct WithColor : IDisposable
{
	private readonly Color _backup;
	private readonly bool _hadChanged;

	public WithColor(Color color)
	{
		_backup = Gizmos.color;
		Gizmos.color = color;
		_hadChanged = true;
	}

	public void Dispose()
	{
		if(_hadChanged)
		{
			Gizmos.color = _backup;
		}
	}
}

public struct CxPivotMeta
{
	public enum DrawingType
	{
		Point,
		Cross,
		Arrow,
		Circle,
	}

	public CxPivot Source;
	public DrawingType Type;
	public Color Color;
	public float Size;
	public float Duration;
}

public struct CxPivot
{
	public Vector3 Origin;
	public Quaternion Rotation;

	public override string ToString()
	{
		return $"{Origin}";
	}

	//TODO: implement convert to matrix
}

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Utility
{
	public static class ExtensionsMath
	{
		public const float EPSILON = 0.000001f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AreEquals(this Rect source, Rect other)
		{
			var @is =
				Mathf.Approximately(source.x, other.x) &&
				Mathf.Approximately(source.y, other.y) &&
				Mathf.Approximately(source.height, other.height) &&
				Mathf.Approximately(source.width, other.width);
			return @is;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Abs(this float source)
		{
			return source > 0 ? source : -source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Approx(this float source, float value)
		{
			return (source > value ? source - value : value - source) < EPSILON;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Approx(this Vector2 source, Vector2 value)
		{
			return (value - source).magnitude.Approx(0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Approx(this Vector3 source, Vector3 value)
		{
			return (value - source).magnitude.Approx(0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp(this int source, int min, int max)
		{
			source = source < min ? min : source;
			source = source > max ? max : source;
			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp(this float source, float min, float max)
		{
			source = source < min ? min : source;
			source = source > max ? max : source;
			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double Clamp(this double source, double min, double max)
		{
			source = source < min ? min : source;
			source = source > max ? max : source;
			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Clamp(this TimeSpan source, TimeSpan min, TimeSpan max)
		{
			source = source < min ? min : source;
			source = source > max ? max : source;
			return source;
		}

		/// <summary> Fisher Yates Algorithm </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Shuffle<T>(this T[] target)
		{
			for(var index = target.Length - 1; index > -1; index--)
			{
				var indexRandom = UnityEngine.Random.Range(0, index);
				target.Swap(index, indexRandom);
			}

			return target;
		}

		/// <summary> Fisher Yates Algorithm </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IList<T> Shuffle<T>(this IList<T> target)
		{
			for(var index = target.Count - 1; index > -1; index--)
			{
				var indexRandom = UnityEngine.Random.Range(0, index);
				target.Swap(index, indexRandom);
			}
			return target;
		}
	}
}

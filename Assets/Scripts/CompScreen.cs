using System;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CompScreen : MonoBehaviour
{
	protected void Awake()
	{
		var canvas = GetComponent<Canvas>();
		if(canvas.isRootCanvas && canvas.worldCamera == null)
		{
			var current = transform;
			while(current.parent != null)
			{
				current = current.parent;
			}
			canvas.worldCamera =
				current.GetComponentInChildren<Camera>() ??
				throw new Exception("can't find UI camera");
		}
	}
}

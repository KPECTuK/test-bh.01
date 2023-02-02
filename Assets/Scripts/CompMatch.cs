using UnityEngine;

public class CompMatch : MonoBehaviour
{
	private void Awake()
	{
		var app = FindObjectOfType<CompApp>();
		app.ShowScreen<CompScreenMatch>();
	}
}

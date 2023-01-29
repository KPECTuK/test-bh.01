using kcp2k;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(KcpTransport))]
[RequireComponent(typeof(NetworkManagerHUD))]
public class CompApp : MonoBehaviour
{
	public GameObject UiRoot;
	public CompScreen ScreenDebug;

	private void Awake()
	{
		var ui = Instantiate(UiRoot);
		Instantiate(ScreenDebug.gameObject, ui.transform);
	}
}
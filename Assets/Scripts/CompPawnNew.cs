using System;
using Mirror;
using UnityEngine;
using Utility;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class CompPawnNew : NetworkBehaviour
{
	private const double ULTIMATE_PERFORM_INTERVAL_D = .6f;
	private const double ULTIMATE_AFFECT_INTERVAL_D = 3f;
	private const float SPAWN_DELAY_INTERVAL_SEC_F = 1f;
	private const float ULTIMATE_DISTANCE_F = 3f;

	public double Velocity = .1f;
	public double MaxSpeed = 1f;

	public float MinAngleCamera = -60f;
	public float MaxAngleCamera = 60f;

	public float UltimateDistance = ULTIMATE_DISTANCE_F;
	public double UltimateAffectInterval = ULTIMATE_AFFECT_INTERVAL_D;
	public double UltimatePerformInterval = ULTIMATE_PERFORM_INTERVAL_D;
	public double SpawnDelayInterval = SPAWN_DELAY_INTERVAL_SEC_F;

	public int MaxScore = 3;

	public Transform OriginCamera;

	private Vector3 _cameraHomePosition;
	private Quaternion _cameraHomeOrientation;

	private IPawnAspect[] _client;
	private IPawnAspect[] _server;
	private IPawnAspect[] _everyone;

	// ReSharper disable MemberCanBePrivate.Global
	[NonSerialized] public CompApp Controller;
	[NonSerialized] public NetworkTransform NetTransform;
	[NonSerialized] public NetworkIdentity NetIdentity;
	[NonSerialized] public Camera Camera;
	[NonSerialized] public CapsuleCollider Collider;
	[NonSerialized] public MeshRenderer Renderer;
	[NonSerialized] public StatePawn State;
	// ReSharper restore MemberCanBePrivate.Global

	[Command]
	public void CmdUltimate()
	{
		if(!State.IsUltimate())
		{
			// set here for the server
			State.SetUltimateEnabled(this);

			RpcUltimate(State.UltimateTargetTimestamp, State.UltimateTargetPosition);
		}
	}

	[ClientRpc(includeOwner = true)]
	public void RpcUltimate(double targetTime, Vector3 targetPosition)
	{
		// set here for the rest of the clients
		State.SetUltimateEnabled(this, targetTime, targetPosition);

		DataShared.I.Log($"[client] receive ultimate at {NetIdentity.netId} ({State.UltimateTargetPosition}, {State.UltimateTargetTimestamp:F3})");
	}

	// must be authority
	[Server]
	public void OnAffected()
	{
		DataShared.I.Log($"[server] sending affected to: {NetIdentity.netId}");

		RpcAffected(State.AffectedTargetTimestamp);
	}

	[ClientRpc(includeOwner = true)]
	public void RpcAffected(double targetTime)
	{
		DataShared.I.Log($"[client] receive affected at: {NetIdentity.netId}");

		State.SetAffectedEnabled(targetTime);
	}

	// must be authority
	[Server]
	public void OnComplete()
	{
		DataShared.I.Log($"[server] sending complete to: {NetIdentity.netId}");

		RpcComplete(State.Score, name);

		Invoke(nameof(OnRestart), 5f);
	}

	[ClientRpc(includeOwner = true)]
	public void RpcComplete(int score, string name)
	{
		DataShared.I.Log($"[client] receive complete at: {NetIdentity.netId}");

		DataShared.I.NameWinner = name;

		if(isLocalPlayer)
		{
			State.Score = score;
			DataShared.I.ScoreCurrent = State.Score;
		}

		if(this.IsWinner(isLocalPlayer))
		{
			Controller.ShowScreen<CompScreenWin>();
		}
		else
		{
			Controller.ShowScreen<CompScreenLose>();
		}

		DataShared.I.Log("<color=red>complete</color>");
	}

	// must be authority
	[Server]
	public void OnRestart()
	{
		DataShared.I.Log($"[server] sending restart to: {NetIdentity.netId}");

		Controller.Restart();
	}

	[ClientRpc(includeOwner = true)]
	public void RpcRestart(Vector3 position)
	{
		DataShared.I.Log($"[client] receive restart at: {NetIdentity.netId}");

		State.Score = 0;

		if(isLocalPlayer)
		{
			Camera.InitializeCamera(OriginCamera);
			this.InitializePawn(position);

			DataShared.I.ScoreCurrent = State.Score;
			Controller.ShowScreen<CompScreenMatch>();

			DataShared.I.Log("<color=green>restart</color>");
		}
	}

	// must be authority
	[Server]
	public void OnScore()
	{
		DataShared.I.Log($"[server] sending score to: {NetIdentity.netId}");

		RpcScore(State.Score);
	}

	[ClientRpc(includeOwner = true)]
	public void RpcScore(int score)
	{
		DataShared.I.Log($"[client] receive score at: {NetIdentity.netId}");

		State.Score = score;
		if(isLocalPlayer)
		{
			DataShared.I.ScoreCurrent = State.Score;
		}
	}

	public void SetColor(Color color)
	{
		Renderer.material.color = color;
	}

	private void Start()
	{
		Controller = FindObjectOfType<CompApp>();

		NetIdentity = GetComponent<NetworkIdentity>();
		NetTransform = GetComponent<NetworkTransform>();

		Collider = GetComponent<CapsuleCollider>();
		Renderer = GetComponentInChildren<MeshRenderer>();

		UltimateAffectInterval = UltimateAffectInterval.Clamp(
			ULTIMATE_AFFECT_INTERVAL_D,
			double.MaxValue);
		UltimatePerformInterval = UltimatePerformInterval.Clamp(
			0.0,
			double.MaxValue);
		UltimateDistance = UltimateDistance.Clamp(
			ULTIMATE_DISTANCE_F,
			float.MaxValue);

		Controller.Register(this);

		if(isLocalPlayer)
		{
			Camera = Camera.main;

			_cameraHomePosition = Camera.transform.position;
			_cameraHomeOrientation = Camera.transform.rotation;

			Camera.InitializeCamera(OriginCamera);
			this.InitializePawn(transform.position);
			Cursor.lockState = CursorLockMode.Locked;

			DataShared.I.Log($"(      local): [ {(authority ? "" : "no ")}authority; {(isServer ? "server" : "client")} ]");
		}
		else
		{
			DataShared.I.Log($"(not a local): [ {(authority ? "" : "no ")}authority; {(isServer ? "server" : "client")} ]");
		}
	}

	private void OnDestroy()
	{
		Controller.Unregister(this);

		if(isLocalPlayer)
		{
			var camTransform = Camera.transform;
			camTransform.SetParent(null);
			camTransform.position = _cameraHomePosition;
			camTransform.rotation = _cameraHomeOrientation;

			Cursor.lockState = CursorLockMode.None;
		}
	}

	private void Update()
	{
		// first value will be passed
		State.TimeDelta = NetworkTime.time - State.TimeAbs;
		State.TimeAbs = NetworkTime.time;

		// must use (authority) - broken
		if(isServer && !Controller.IsAnyScoreMax())
		{
			// skip first frame
			if(_server == null)
			{
				_server = new IPawnAspect[]
				{
					new PawnAspectHit(Controller.GetComponent<NetworkManager>().maxConnections),
				};
			}
			else
			{
				for(var index = 0; index < _server.Length; index++)
				{
					if(_server[index].IsActive)
					{
						_server[index].Update(ref State, this);
					}
				}
			}
		}

		//if(isClient)
		//{
		//	private static int _count;
		//	! not the same as the manual says: client and server are not separate on the same host
		//	DataShared.I.Log($"[client]: call update {NetIdentity.netId} ({_count})");
		//}

		if(isLocalPlayer && !Controller.IsAnyScoreMax())
		{
			if(_client == null)
			{
				// skip first frame
				var router = new Router();
				_client = new IPawnAspect[]
				{
					new PawnAspectInput(this),
					router,
					//
					new PawnAspectUltimate(router),
					new PawnAspectMovement(router),
				};
			}
			else
			{
				for(var index = 0; index < _client.Length; index++)
				{
					if(_client[index].IsActive)
					{
						_client[index].Update(ref State, this);
					}
				}
			}
		}

		if(_everyone == null)
		{
			// skip first frame
			_everyone = new IPawnAspect[]
			{
				new PawnAspectLookUnaffected(),
				new PawnAspectLookAffected(),
			};
		}
		else
		{
			for(var index = 0; index < _everyone.Length; index++)
			{
				if(_everyone[index].IsActive)
				{
					_everyone[index].Update(ref State, this);
				}
			}
		}
	}
}

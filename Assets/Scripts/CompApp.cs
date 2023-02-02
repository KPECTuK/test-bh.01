using System;
using System.Collections.Generic;
using kcp2k;
using Mirror;
using UnityEditor;
using UnityEngine;
using Utility;

[RequireComponent(typeof(KcpTransport))]
[RequireComponent(typeof(NetworkManagerHUD))]
public class CompApp : NetworkRoomManager
{
	public GameObject UiRoot;
	public ScreenBase ScreenDebug;
	public ScreenBase ScreenMatch;
	public ScreenBase ScreenWin;
	public ScreenBase ScreenLose;

	public Color[] ColorsUnaffected =
	{
		Color.red,
		Color.yellow,
		Color.blue,
		Color.green,
	};

	public Color[] ColorsAffected =
	{
		Color.Lerp(Color.red, Color.black, .7f),
		Color.Lerp(Color.yellow, Color.black, .7f),
		Color.Lerp(Color.blue, Color.black, .7f),
		Color.Lerp(Color.green, Color.black, .7f),
	};

	private CompPawnNew[] _assets; //~ use Mirror's native
	private readonly List<ScreenBase> _screens = new();

	//private Action<NetworkConnectionToClient, MessageCreatePawn> _handlerCreatePawn;

	public bool IsAnyScoreMax()
	{
		for(var index = 0; index < _assets.Length; index++)
		{
			if(ReferenceEquals(_assets[index], null))
			{
				return false;
			}

			var pawn = _assets[index];
			if(pawn.State.Score >= pawn.MaxScore)
			{
				return true;
			}
		}

		return false;
	}

	public void Register(CompPawnNew asset)
	{
		var index = _assets.InsertAtFirstDefault(asset);
		if(index == -1)
		{
			throw new Exception("no room for pawn");
		}

		asset.State.ColorAffected = ColorsAffected[index];
		asset.State.ColorUnaffected = ColorsUnaffected[index];
	}

	public void Unregister(CompPawnNew asset)
	{
		_assets.SetDefault(asset);
		_assets.DeFragment();
	}

	public void ShowScreen<T>() where T : ScreenBase
	{
		for(var index = 0; index < _screens.Count; index++)
		{
			_screens[index].gameObject.SetActive(typeof(T) == _screens[index].GetType());
		}
	}

	public void Restart()
	{
		var spawns = FindObjectsOfType<NetworkStartPosition>();
		spawns.Shuffle();
		for(var index = 0; index < _assets.Length; index++)
		{
			_assets[index].RpcRestart(spawns[index].transform.position);
		}
	}

	//public override void OnStartServer()
	//{
	//	base.OnStartServer();

	//	// ReSharper disable once ConvertClosureToMethodGroup (not a CLR)
	//	_handlerCreatePawn = (connection, message) => { OnCreatePawn(connection, message); };
	//	NetworkServer.RegisterHandler(_handlerCreatePawn);

	//	DataShared.I.Log("server start callback");
	//}

	//public override void OnStopServer()
	//{
	//	base.OnStopServer();

	//	if(!ReferenceEquals(null, _handlerCreatePawn))
	//	{
	//		NetworkServer.UnregisterHandler<MessageCreatePawn>();
	//	}

	//	DataShared.I.Log("server stop callback");
	//}

	//public override void OnClientConnect()
	//{
	//	base.OnClientConnect();

	//	NetworkClient.Send(new MessageCreatePawn(playerPrefab));
	//}

	//private static void OnCreatePawn(NetworkConnectionToClient conn, MessageCreatePawn message)
	//{
	//	var instance = Instantiate(message.Proto);
	//	NetworkServer.AddPlayerForConnection(conn, instance);
	//}

	public override void Start()
	{
		base.Start();

		var ui = Instantiate(UiRoot);
		Instantiate(ScreenDebug.gameObject, ui.transform).SetActive(true);
		Instantiate(ScreenMatch.gameObject, ui.transform).AppendTo(_screens).SetActive(false);
		Instantiate(ScreenLose.gameObject, ui.transform).AppendTo(_screens).SetActive(false);
		Instantiate(ScreenWin.gameObject, ui.transform).AppendTo(_screens).SetActive(false);
		DontDestroyOnLoad(ui.gameObject);

		_assets = new CompPawnNew[maxConnections];
	}

	public override void Update()
	{
		base.Update();

		if(Input.GetKey(KeyCode.Escape))
		{
			#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
			#else
			Application.Quit();
			#endif
		}
	}
}

/// <summary> interface.. </summary>
public readonly struct MessageCreatePawn : NetworkMessage
{
	public readonly GameObject Proto;

	public MessageCreatePawn(GameObject proto)
	{
		Proto = proto;
	}
}

/// <summary> modify but not reassign issue </summary>
public struct StatePawn
{
	public double TimeAbs;
	public double TimeDelta;
	public Vector3 Current;
	public Vector3 Target;
	public Quaternion RotationPawn;
	public Quaternion RotationCamera;
	public double Speed;
	//
	public Vector3 UltimateSourcePosition;
	public double UltimateSourceTimestamp;
	public Vector3 UltimateTargetPosition;
	public double UltimateTargetTimestamp;
	//
	public double AffectedSourceTimestamp;
	public double AffectedTargetTimestamp;
	//
	public Color ColorUnaffected;
	public Color ColorAffected;
	//
	public int Score;
}

public interface IPawnAspect
{
	bool IsActive { get; }
	void Update(ref StatePawn state, CompPawnNew pawn);
}

public interface IRouter
{
	bool IsActive<T>() where T : IPawnAspect;
}

public class Router : IPawnAspect, IRouter
{
	private StatePawn _copy;

	public bool IsActive => true;

	public void Update(ref StatePawn state, CompPawnNew pawn)
	{
		_copy = state;
	}

	bool IRouter.IsActive<T>()
	{
		if(typeof(T) == typeof(PawnAspectMovement))
		{
			//return !_copy.IsUltimate();
			return true;
		}

		if(typeof(T) == typeof(PawnAspectUltimate))
		{
			return _copy.IsUltimate();
		}

		return false;
	}
}

public sealed class PawnAspectInput : IPawnAspect
{
	private double _spawnDelay;

	public bool IsActive => !Input.GetKey(KeyCode.LeftControl);

	public PawnAspectInput(CompPawnNew pawn)
	{
		_spawnDelay = pawn.SpawnDelayInterval;
	}

	public void Update(ref StatePawn state, CompPawnNew pawn)
	{
		_spawnDelay -= Time.deltaTime;
		_spawnDelay = _spawnDelay.Clamp(0.0, double.MaxValue);

		pawn.Camera.ReadUserInput(pawn.MinAngleCamera, pawn.MaxAngleCamera, ref state);
		// don't affects movement, no meter when to apply
		pawn.Camera.transform.localRotation = state.RotationCamera;
		
		pawn.ReadUserInput(ref state);

		if(Input.GetMouseButtonDown(0))
		{
			pawn.CmdUltimate();
		}
	}
}

public sealed class PawnAspectUltimate : IPawnAspect
{
	private readonly IRouter _router;

	public PawnAspectUltimate(IRouter router)
	{
		_router = router;
	}

	public bool IsActive => _router.IsActive<PawnAspectUltimate>();

	public void Update(ref StatePawn state, CompPawnNew pawn)
	{
		var v1 = state.UltimateTargetTimestamp - NetworkTime.time;
		var v2 = state.UltimateTargetTimestamp - state.UltimateSourceTimestamp;
		var value = (float)(v1 / v2);
		value = Mathf.Sin(value * Mathf.PI / 2f);

		state.Current = pawn.transform.position;
		state.Target = Vector3.Lerp(
			state.UltimateTargetPosition,
			state.UltimateSourcePosition,
			Mathf.Clamp(value, 0f, float.MaxValue));
	}
}

public sealed class PawnAspectMovement : IPawnAspect
{
	private readonly IRouter _router;

	public PawnAspectMovement(IRouter router)
	{
		_router = router;
	}

	public bool IsActive => _router.IsActive<PawnAspectMovement>();

	public void Update(ref StatePawn state, CompPawnNew pawn)
	{
		state.LimitByObstacleAround();
		state.LimitByObstacleBelow(Vector3.down);

		var trans = pawn.transform;
		trans.localRotation = state.RotationPawn;
		trans.position = state.Target;
	}
}

public sealed class PawnAspectHit : IPawnAspect
{
	private readonly Collider[] _buffer;

	public PawnAspectHit(int connectionNumber)
	{
		_buffer = new Collider[connectionNumber];

		DataShared.I.Log($"[server] buffer size: {_buffer.Length}");
	}

	public bool IsActive => true;

	public void Update(ref StatePawn state, CompPawnNew pawn)
	{
		if(pawn.DetectPawnAffection(_buffer, out var countHits))
		{
			for(var index = 0; index < countHits; index++)
			{
				var affected = _buffer[index].GetComponent<CompPawnNew>();

				if(ReferenceEquals(null, affected) || ReferenceEquals(pawn, affected))
				{
					continue;
				}

				if(!affected.State.IsAffected())
				{
					affected.State.SetAffectedEnabled(affected);
					affected.OnAffected();

					state.Score++;
					if(state.Score == pawn.MaxScore)
					{
						pawn.OnComplete();
					}
					else
					{
						pawn.OnScore();
					}
				}
			}
		}
	}
}

public sealed class PawnAspectLookUnaffected : IPawnAspect
{
	private bool _isActive;

	public bool IsActive => true;

	public void Update(ref StatePawn state, CompPawnNew pawn)
	{
		//~ can be at router
		var currentActive = !state.IsAffected();
		if(_isActive != currentActive)
		{
			_isActive = currentActive;
			if(_isActive)
			{
				pawn.SetColor(state.ColorUnaffected);
			}

			//DataShared.I.Log($"look switch unaffected: {currentActive}");
		}
	}
}

public sealed class PawnAspectLookAffected : IPawnAspect
{
	private bool _isActive;

	public bool IsActive => true;

	public void Update(ref StatePawn state, CompPawnNew pawn)
	{
		//~ can be at router
		var currentActive = state.IsAffected();
		if(_isActive != currentActive)
		{
			_isActive = currentActive;
			if(_isActive)
			{
				pawn.SetColor(state.ColorAffected);
			}

			//DataShared.I.Log($"look switch affected: {currentActive}");
		}
	}
}

public static class Extensions
{
	public static GameObject AppendTo(this GameObject item, List<ScreenBase> screens)
	{
		if(ReferenceEquals(null, item))
		{
			throw new Exception("no item to append");
		}

		var screen = item.GetComponent<ScreenBase>();
		
		if(ReferenceEquals(null, screens))
		{
			throw new Exception($"no screen to append in: {item.name}");
		}

		// do not use LINQ intently
		for(var index = 0; index < screens.Count; index++)
		{
			if(screens[index].GetType() == screen.GetType())
			{
				throw new Exception($"appending the same screen as: {item.name}");
			}
		}

		screens.Add(screen);
		return screen.gameObject;
	}

	public static void InitializeCamera(this Camera target, Transform cameraAnchor)
	{
		var camTransform = target.transform;
		camTransform.SetParent(cameraAnchor, false);
		camTransform.localPosition = Vector3.zero;
		camTransform.localRotation = Quaternion.identity;
	}

	public static void InitializePawn(this CompPawnNew target, Vector3 position)
	{
		var pawnTransform = target.transform;
		pawnTransform.position = position;
		pawnTransform.localRotation = Quaternion.LookRotation(
			Vector3.zero - pawnTransform.position,
			Vector3.up);
	}

	public static float AngleNormalize(this float source)
	{
		source = Mathf.DeltaAngle(0f, source);
		source = source > 180f ? 360f - source : source;
		return source;
	}

	public static void ReadUserInput(this Camera source, float minAngle, float maxAngle, ref StatePawn state)
	{
		var rotation = source.transform.localRotation;
		var mouseYAxis = Input.GetAxis("Mouse Y") * Time.deltaTime;
		var result = (rotation.eulerAngles.x + mouseYAxis).AngleNormalize();
		var max = maxAngle.Clamp(0f, 170f);
		var min = minAngle.Clamp(-170f, 0f);
		result = result.Clamp(min, max);
		state.RotationCamera = Quaternion.Euler(result, 0f, 0f);
	}

	public static void ReadUserInput(this CompPawnNew source, ref StatePawn state)
	{
		// will not implement inertia (just a test), and will not use GetAxis() (can not control precisely)
		var delta = Vector3.zero;
		var transform = source.transform;
		var forward = transform.forward;
		var right = transform.right;
		delta += right * (Input.GetKey(KeyCode.D) ? 1f : 0f);
		delta += right * (Input.GetKey(KeyCode.A) ? -1f : 0f);
		delta += forward * (Input.GetKey(KeyCode.W) ? 1f : 0f);
		delta += forward * (Input.GetKey(KeyCode.S) ? -1f : 0f);

		if(delta.sqrMagnitude > 0f)
		{
			state.Speed += state.TimeDelta * source.Velocity;
			state.Speed = state.Speed.Clamp(0f, source.MaxSpeed);
		}
		else
		{
			state.Speed = 0.0;
		}

		delta = delta.normalized * (float)state.Speed;

		state.Current = source.transform.position;
		state.Target = state.Current + delta;

		// to implement mouse axis is to complex also
		var mouseXAxis = ((float)(Input.GetAxis("Mouse X") * state.TimeDelta)).AngleNormalize();
		state.RotationPawn = transform.localRotation * Quaternion.Euler(0f, mouseXAxis, 0f);
	}

	public static bool DetectPawnAffection(this CompPawnNew pawn, Collider[] buffer, out int countHits)
	{
		// https://roundwide.com/physics-overlap-capsule/

		countHits = 0;
		if(pawn.State.IsUltimate())
		{
			var collider = pawn.Collider;
			var transform = pawn.transform;
			var direction = new Vector3 { [collider.direction] = 1f };
			var offset = collider.height / 2f - collider.radius;
			var localPoint0 = collider.center - direction * offset;
			var localPoint1 = collider.center + direction * offset;
			var point0 = transform.TransformPoint(localPoint0);
			var point1 = transform.TransformPoint(localPoint1);
			var radius = collider.radius;
			var components = transform.TransformVector(radius, radius, radius);
			radius = 0f;
			for(var index = 0; index < 3; index++)
			{
				var component = index == collider.direction ? 0 : components[index];
				component = Mathf.Abs(component);
				radius = radius > component ? radius : component;
			}
			countHits = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, buffer, LayerMask.GetMask("ph_pawns"));
		}

		return countHits > 0;
	}

	public static bool IsUltimate(ref this StatePawn source)
	{
		return
			source.UltimateSourceTimestamp <= NetworkTime.time &&
			source.UltimateTargetTimestamp >= NetworkTime.time;
	}

	public static bool IsAffected(ref this StatePawn source)
	{
		return
			source.AffectedSourceTimestamp <= NetworkTime.time &&
			source.AffectedTargetTimestamp >= NetworkTime.time;
	}

	public static bool IsWinner(this CompPawnNew pawn, bool isLocalPlayer)
	{
		return isLocalPlayer && pawn.State.Score >= pawn.MaxScore;
	}

	public static void SetUltimateEnabled(ref this StatePawn target, CompPawnNew pawn)
	{
		var transform = pawn.transform;
		target.UltimateSourceTimestamp = NetworkTime.time;
		target.UltimateSourcePosition = transform.position;
		target.UltimateTargetTimestamp = NetworkTime.time + pawn.UltimatePerformInterval;
		target.UltimateTargetPosition = transform.TransformPoint(Vector3.forward * pawn.UltimateDistance);
	}

	public static void SetUltimateEnabled(ref this StatePawn target, CompPawnNew pawn, double targetTime, Vector3 targetPosition)
	{
		var transform = pawn.transform;
		target.UltimateSourceTimestamp = NetworkTime.time;
		target.UltimateSourcePosition = transform.position;
		target.UltimateTargetTimestamp = targetTime > NetworkTime.time ? targetTime : NetworkTime.time;
		target.UltimateTargetPosition = targetPosition;
	}

	public static void SetAffectedEnabled(ref this StatePawn target, CompPawnNew pawn)
	{
		target.AffectedSourceTimestamp = NetworkTime.time;
		target.AffectedTargetTimestamp = NetworkTime.time + pawn.UltimateAffectInterval;
	}

	public static void SetAffectedEnabled(ref this StatePawn target, double targetTime)
	{
		target.AffectedSourceTimestamp = NetworkTime.time;
		target.AffectedTargetTimestamp = targetTime > NetworkTime.time ? targetTime : NetworkTime.time;
	}

	public static void LimitByObstacleAround(ref this StatePawn source)
	{
		var direction = (source.Target - source.Current).normalized;
		var isObstacle = Physics.Raycast(
			new Ray(source.Current, direction),
			out var hitInfo,
			.5f,
			LayerMask.GetMask("ph_pawns"));
		if(isObstacle)
		{
			// hold
			source.Target = source.Current;

			var meta = new CxPivot { Origin = source.Target }.ToMeta();
			meta.Color = Color.magenta;
			meta.Duration = 2f;
			meta.Draw();
		}
	}

	public static void LimitByObstacleBelow(ref this StatePawn source, Vector3 probe)
	{
		var mask = LayerMask.GetMask("ph_obstacles");
		var offset = probe * -.5f;
		if(!Physics.Raycast(new Ray(source.Target + offset, probe), mask))
		{
			var start = source.Current;
			var stop = source.Target;
			var iterations = 16;
			while(--iterations > 0)
			{
				var middle = (start + stop) * .5f;
				if(Physics.Raycast(new Ray(middle + offset, probe), mask))
				{
					start = middle;
				}
				else
				{
					stop = middle;
				}
			}

			source.Target = stop;
			if(!Physics.Raycast(new Ray(source.Target + offset, probe), mask))
			{
				source.Target = stop;
				if(!Physics.Raycast(new Ray(source.Target + offset, probe), mask))
				{
					source.Target = source.Current;
				}
			}
		}
	}
}

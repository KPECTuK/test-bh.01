using Mirror;
using UnityEngine;
using Utility;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransform))]
public class CompPawn : NetworkBehaviour
{
	private const double ULTIMATE_INTERVAL_PERFORM_F = .6f;
	private const double ULTIMATE_INTERVAL_AFFECT_F = 3f;
	private const float ULTIMATE_DISTANCE_F = 3f;
	private const float SPAWN_DELAY_SEC_F = 1f;

	private Camera _camera;
	private BehaviorBase _controllerRegular;

	private IAspect _behaviourCurrent;
	private IAspect _colorCurrent;

	private float _rotationCamera;
	private float _rotationPawn;

	[Header("'distance' setting")]
	//
	[SerializeField]
	private float UltimateDistance;
	[Header("'color change duration' setting")]
	//
	[SerializeField]
	private double UltimateIntervalAffect;
	[Header("Camera anchor")]
	//
	[SerializeField]
	private Transform OriginCamera;
	[Header("Debug")]
	//
	[ReadOnlyInInspector]
	public float MouseX;
	[ReadOnlyInInspector] public float MouseY;

	/// <summary> но это уже такое.. мысли на будущее </summary>
	private interface IAspect
	{
		void Update();
	}

	private sealed class ColorRegular : IAspect
	{
		public ColorRegular(CompPawn slave)
		{
			slave.GetComponentInChildren<MeshRenderer>().material.color =
				slave.isLocalPlayer ? Color.red : Color.yellow;
		}

		public void Update() { }
	}

	private sealed class ColorAffected : IAspect
	{
		private readonly double _targetTime;
		private readonly CompPawn _slave;

		public ColorAffected(CompPawn slave)
		{
			_slave = slave;
			_slave.GetComponentInChildren<MeshRenderer>().material.color = Color.black;
			_targetTime = NetworkTime.time + slave.UltimateIntervalAffect;
		}

		public void Update()
		{
			if(_targetTime < NetworkTime.time)
			{
				_slave._colorCurrent = new ColorRegular(_slave);
			}
		}
	}

	private abstract class BehaviorBase : IAspect
	{
		protected readonly CompPawn Slave;

		protected BehaviorBase(CompPawn slave)
		{
			Slave = slave;
		}

		public abstract void Update();

		protected bool AssertHit(Vector3 target, Vector3 source, out NetworkIdentity identity)
		{
			source += Vector3.up * .5f;
			target += Vector3.up * .5f;
			var direction = (target - source).normalized;

			var result = false;
			identity = null;

			if(direction.magnitude > 0f)
			{
				result = Physics.Raycast(
					new Ray(source, direction),
					out var hitInfo,
					.5f,
					LayerMask.GetMask("ph_pawns"));

				var meta = new CxPivot
				{
					Origin = source,
					Rotation = Quaternion.LookRotation(direction, Vector3.up),
				}.ToMeta();
				meta.Color = result ? Color.magenta : Color.blue;
				meta.Draw();

				identity = hitInfo.transform.GetComponent<NetworkIdentity>();

				DataShared.I.Log($"hit: {hitInfo.transform.name}");
			}

			return result;
		}

		protected void LimitByObstacleAround(ref Vector3 target, ref Vector3 source)
		{
			var direction = (target - source).normalized;
			var isObstacle = Physics.Raycast(
				new Ray(source, direction),
				out var hitInfo,
					.5f,
				LayerMask.GetMask("ph_pawns"));
			if(isObstacle)
			{
				//target = source + direction * hitInfo.distance;
				target = source;

				var meta = new CxPivot { Origin = target }.ToMeta();
				meta.Color = Color.magenta;
				meta.Duration = 2f;
				meta.Draw();
			}
		}

		protected void LimitByObstacleBelow(ref Vector3 target, ref Vector3 source, Vector3 probe)
		{
			var mask = LayerMask.GetMask("ph_obstacles");
			var offset = probe * -.5f;
			if(!Physics.Raycast(new Ray(target + offset, probe), mask))
			{
				var start = source;
				var stop = target;
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

				DataShared.I.Log($"iterations left: {iterations}; new: {target}");

				target = stop;
				if(!Physics.Raycast(new Ray(target + offset, probe), mask))
				{
					target = stop;
					if(!Physics.Raycast(new Ray(target + offset, probe), mask))
					{
						target = source;
					}
				}
			}
		}
	}

	private sealed class BehaviorRemoteControl : BehaviorBase
	{
		public BehaviorRemoteControl(CompPawn slave) : base(slave) { }

		public override void Update() { }
	}

	private sealed class BehaviorUserControl : BehaviorBase
	{
		private float _instantiationDelay = SPAWN_DELAY_SEC_F;

		public BehaviorUserControl(CompPawn slave) : base(slave)
		{
			Slave._camera = Camera.main;

			var camTransform = Slave._camera.transform;
			camTransform.SetParent(Slave.OriginCamera, false);
			camTransform.localPosition = Vector3.zero;
			camTransform.localRotation = Quaternion.identity;
			Slave._rotationCamera = 0f;

			var pawnTransform = Slave.transform;
			pawnTransform.localRotation = Quaternion.LookRotation(
				Vector3.zero - pawnTransform.position,
				Vector3.up);
			Slave._rotationPawn = pawnTransform.localRotation.eulerAngles.y;

			Cursor.lockState = CursorLockMode.Locked;
		}

		public override void Update()
		{
			if(_instantiationDelay > 0f)
			{
				_instantiationDelay -= Time.deltaTime;

				return;
			}

			Slave.MouseX = Input.GetAxis("Mouse X") * Time.deltaTime;
			Slave._rotationPawn += Slave.MouseX;
			Slave.transform.localRotation = Quaternion.Euler(0f, Slave._rotationPawn, 0f);

			Slave.MouseY = Input.GetAxis("Mouse Y") * Time.deltaTime;
			Slave._rotationCamera += Slave.MouseY;
			Slave._rotationCamera = Mathf.Clamp(Slave._rotationCamera, -20f, 60f);
			Slave._camera.transform.localRotation = Quaternion.Euler(Slave._rotationCamera, 0f, 0f);

			var positionCurrent = Slave.transform.position;
			var positionNew = positionCurrent;
			positionNew += Slave.transform.right * Input.GetAxis("Horizontal");
			positionNew += Slave.transform.forward * Input.GetAxis("Vertical");
			//? use collider mass center
			LimitByObstacleAround(ref positionNew, ref positionCurrent);
			LimitByObstacleBelow(ref positionNew, ref positionCurrent, Vector3.down);
			Slave.transform.position = positionNew;

			if(Input.GetMouseButtonDown(0))
			{
				Slave._behaviourCurrent = new BehaviorUltimate(
					Slave,
					ULTIMATE_INTERVAL_PERFORM_F,
					Slave.UltimateDistance);

				Slave.CmdUltimate();
			}
		}
	}

	private sealed class BehaviorUltimate : BehaviorBase
	{
		public readonly double TargetTimestamp;
		public readonly double SourceTimestamp;
		public readonly Vector3 TargetPosition;
		public readonly Vector3 SourcePosition;

		public BehaviorUltimate(CompPawn slave, double duration, float distance) : base(slave)
		{
			SourceTimestamp = NetworkTime.time;
			SourcePosition = Slave.transform.position;
			TargetTimestamp = NetworkTime.time + duration;
			TargetPosition = Slave.transform.TransformPoint(Vector3.forward * distance);
		}

		public override void Update()
		{
			var value = (float)((TargetTimestamp - NetworkTime.time) / (TargetTimestamp - SourceTimestamp));
			value = Mathf.Sin(value * Mathf.PI / 2f);

			var positionCurrent = Slave.transform.position;
			var positionNew = Vector3.Lerp(
				TargetPosition,
				SourcePosition,
				Mathf.Clamp(value, 0f, float.MaxValue));
			LimitByObstacleAround(ref positionNew, ref positionCurrent);
			LimitByObstacleBelow(ref positionNew, ref positionCurrent, Vector3.down);
			Slave.transform.position = positionNew;

			if(Slave.isServer)
			{
				if(AssertHit(positionNew, positionCurrent, out var identity))
				{
					Slave.OnReceiveHit(identity.connectionToClient);
				}
			}

			if(value < 0f)
			{
				Slave._behaviourCurrent = Slave._controllerRegular;

				if(Slave.isServer)
				{
					var identity = Slave.GetComponent<NetworkIdentity>();
					//~Slave.OnCompleteUltimate(identity.connectionToClient, positionNew);
					Slave.OnCompleteUltimate(positionNew, identity.netId);
				}

				DataShared.I.StrategyCurrent = Slave._behaviourCurrent.GetType().Name;
			}
		}
	}

	[Command]
	private void CmdUltimate()
	{
		//? set in ultimate state (on server)
		//! take latency into account by network time

		var identity = GetComponent<NetworkIdentity>();
		OnReceiveUltimate();

		DataShared.I.Log($"at local: {transform.position}; netID: {identity.netId}");
		DataShared.I.StrategyCurrent = _behaviourCurrent.GetType().Name;
	}

	[ClientRpc(includeOwner = false)]
	private void OnReceiveUltimate()
	{
		_behaviourCurrent = new BehaviorUltimate(
			this,
			ULTIMATE_INTERVAL_PERFORM_F,
			UltimateDistance);
		DataShared.I.StrategyCurrent = _behaviourCurrent.GetType().Name;
		DataShared.I.Log($"at remote: {transform.position}");
	}

	//~[TargetRpc]
	//~private void OnCompleteUltimate(NetworkConnection connection, Vector3 position)
	[ClientRpc(includeOwner = true)]
	private void OnCompleteUltimate(Vector3 position, uint targetNetId)
	{
		//~ sync echo: jitter

		var identity = GetComponent<NetworkIdentity>();
		if(identity.netId == targetNetId)
		{
			transform.position = position;
			DataShared.I.Log($"receive position fix: {transform.position}");
		}
	}

	[TargetRpc]
	private void OnReceiveHit(NetworkConnection connection)
	{
		if(_colorCurrent is ColorAffected)
		{
			return;
		}

		_colorCurrent = new ColorAffected(this);
	}

	private void Start()
	{
		_controllerRegular = isLocalPlayer
			? new BehaviorUserControl(this)
			: new BehaviorRemoteControl(this);
		_behaviourCurrent = _controllerRegular;
		_colorCurrent = new ColorRegular(this);
		UltimateIntervalAffect = UltimateIntervalAffect > ULTIMATE_INTERVAL_AFFECT_F
			? UltimateIntervalAffect
			: ULTIMATE_INTERVAL_AFFECT_F;
		UltimateDistance = UltimateDistance > ULTIMATE_DISTANCE_F
			? UltimateDistance
			: ULTIMATE_DISTANCE_F;
		DataShared.I.StrategyCurrent = _behaviourCurrent.GetType().Name;
	}

	private void Update()
	{
		_colorCurrent.Update();
		_behaviourCurrent.Update();
	}

	private void OnDestroy()
	{
		Cursor.lockState = CursorLockMode.None;
	}
}

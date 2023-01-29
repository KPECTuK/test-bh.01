using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransform))]
public class CompPawn : NetworkBehaviour
{
	private const double ULTIMATE_INTERVAL_PERFORM_F = .6f;
	private const double ULTIMATE_INTERVAL_AFFECT_F = 3f;
	private const float ULTIMATE_DISTANCE_F = 3f;
	private const float SPAWN_DELAY_SEC_F = 1f;

	private Camera _camera;
	private BehaviorBase _controllerCurrent;
	private BehaviorBase _controllerRegular;
	private Collider _affection;

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

	private abstract class BehaviorBase
	{
		protected readonly CompPawn Slave;

		protected BehaviorBase(CompPawn slave)
		{
			Slave = slave;
		}

		public abstract void Update();

		protected void Limit(ref Vector3 target, ref Vector3 source, Vector3 probe)
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
		public BehaviorRemoteControl(CompPawn slave) : base(slave)
		{
			slave.GetComponentInChildren<MeshRenderer>().material.color = Color.yellow;
		}

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

			Slave._affection = Slave.GetComponent<Collider>();

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

			Limit(ref positionNew, ref positionCurrent, Vector3.down);
			Slave.transform.position = positionNew;

			if(Input.GetMouseButtonDown(0))
			{
				Slave._controllerCurrent = new BehaviorUltimate(
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

			DataShared.I.Line_00 = SourcePosition.ToString();
			DataShared.I.Line_01 = TargetPosition.ToString();
		}

		public override void Update()
		{
			var value = (float)((TargetTimestamp - NetworkTime.time) / (TargetTimestamp - SourceTimestamp));
			value = Mathf.Sin(value * Mathf.PI / 2f);

			if(value < 0f)
			{
				Slave._controllerCurrent = Slave._controllerRegular;
				value = 0f;

				DataShared.I.StrategyCurrent = Slave._controllerCurrent.GetType().Name;
			}

			var positionCurrent = Slave.transform.position;
			var positionNew = Vector3.Lerp(
				TargetPosition,
				SourcePosition,
				value);
			Limit(ref positionNew, ref positionCurrent, Vector3.down);
			Slave.transform.position = positionNew;

			Slave.OnUltimateStep();
		}
	}

	[Server]
	private void OnUltimateStep()
	{
		//_affection.
	}

	[Command]
	private void CmdUltimate()
	{
		OnReceiveUltimate();

		DataShared.I.StrategyCurrent = _controllerCurrent.GetType().Name;
		DataShared.I.Log($"at local: {transform.position}");
	}

	[ClientRpc(includeOwner = false)]
	private void OnReceiveUltimate()
	{
		_controllerCurrent = new BehaviorUltimate(
			this,
			ULTIMATE_INTERVAL_PERFORM_F,
			UltimateDistance);
		DataShared.I.StrategyCurrent = _controllerCurrent.GetType().Name;
		DataShared.I.Log($"at remote: {transform.position}");
	}

	[ClientRpc(includeOwner = false)]
	private void OnReceiveHit()
	{
		//
	}

	private void Start()
	{
		_controllerRegular = isLocalPlayer
			? new BehaviorUserControl(this)
			: new BehaviorRemoteControl(this);
		_controllerCurrent = _controllerRegular;
		UltimateIntervalAffect = UltimateIntervalAffect > ULTIMATE_INTERVAL_AFFECT_F
			? UltimateIntervalAffect
			: ULTIMATE_INTERVAL_AFFECT_F;
		UltimateDistance = UltimateDistance > ULTIMATE_DISTANCE_F
			? UltimateDistance
			: ULTIMATE_DISTANCE_F;
		DataShared.I.StrategyCurrent = _controllerCurrent.GetType().Name;
	}

	private void OnCollisionEnter(Collision other)
	{
		DataShared.I.Log("collision enter with");
	}

	private void OnTriggerEnter(Collider other)
	{
		DataShared.I.Log("trigger enter with");
	}

	private void Update()
	{
		_controllerCurrent.Update();
	}

	private void OnDestroy()
	{
		Cursor.lockState = CursorLockMode.None;
	}
}

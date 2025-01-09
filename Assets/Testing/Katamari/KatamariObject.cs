using System.Linq;
using jKnepel.ProteusNet.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform), typeof(Rigidbody))]
	public class KatamariObject : NetworkBehaviour
	{
		#region attributes

		private Transform _attachedTo;
		private float _maxDistance;

		[SerializeField] private NetworkObject networkObject;
		[SerializeField] private Rigidbody rb;
		[SerializeField] private float gravitationalPull = 3000;

		public bool IsAttached { get; private set; }

		#endregion

		#region lifecycle

		private void Awake()
		{
			if (networkObject == null)
				networkObject = GetComponent<NetworkObject>();
			if (rb == null)
				rb = GetComponent<Rigidbody>();
		}

		private void FixedUpdate()
		{
			if (!IsAttached)
				return;

			var distance = Vector3.Distance(transform.position, _attachedTo.position);
			var strength = Map(distance, _maxDistance, 0, 0, gravitationalPull);
			rb.AddForce(strength * Time.fixedDeltaTime * (_attachedTo.position - transform.position));
		}

		#endregion

		#region public methods

		public void Attach(Transform trf)
		{
			if (IsAttached)
				return;

			if (!IsOwner)
				networkObject.RequestOwnership();
			IsAttached = true;
			_attachedTo = trf;
			_maxDistance = trf.GetComponents<Collider>().First(x => x.isTrigger).bounds.size.x;
		}

		public void Detach()
		{
			if (!IsAttached)
				return;

			if (IsOwner)
				networkObject.ReleaseOwnership();
			IsAttached = false;
			_attachedTo = null;
			_maxDistance = 0;
		}

		#endregion

		#region private methods

		private static float Map(float value, float from1, float from2, float to1, float to2)
		{
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}

		#endregion
	}

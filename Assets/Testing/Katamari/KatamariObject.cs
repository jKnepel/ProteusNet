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
			if (!IsOwner)
				return;

			var distance = Vector3.Distance(transform.position, _attachedTo.position);
			var strength = Map(distance, _maxDistance, 0, 0, gravitationalPull);
			rb.AddForce(strength * Time.fixedDeltaTime * (_attachedTo.position - transform.position));
		}

		#endregion

		#region public methods

		public override void OnOwnershipChanged(uint _)
		{
			if (IsOwner)
			{
				_maxDistance = _attachedTo.GetComponents<Collider>().First(x => x.isTrigger).bounds.size.x;
			}
			else
			{
				_attachedTo = null;
				_maxDistance = 0;
			}
		}

		public void Attach(Transform trf)
		{
			if (OwnerID != 0)
				return;

			_attachedTo = trf;
			networkObject.RequestOwnership();
		}

		public void Detach()
		{
			if (!IsOwner)
				return;

			networkObject.ReleaseOwnership();
		}

		#endregion

		#region private methods

		private static float Map(float value, float from1, float from2, float to1, float to2)
		{
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}

		#endregion
	}

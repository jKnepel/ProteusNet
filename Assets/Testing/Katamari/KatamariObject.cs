using jKnepel.ProteusNet.Components;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform), typeof(Rigidbody))]
	public class KatamariObject : NetworkBehaviour
	{
		#region attributes

		private Transform _attachedTo;
		private float _maxDistance;

		[SerializeField] private NetworkObject networkObject;
		[SerializeField] private Rigidbody rb;
		[SerializeField] private new MeshRenderer renderer;
		[SerializeField] private float gravitationalPull = 3000;

		private Material _material;
		private static readonly int Color = Shader.PropertyToID("_Color");

		#endregion

		#region lifecycle

		private void Awake()
		{
			if (networkObject == null)
				networkObject = GetComponent<NetworkObject>();
			if (rb == null)
				rb = GetComponent<Rigidbody>();
			if (renderer == null)
				renderer = GetComponent<MeshRenderer>();

			_material = renderer.material = new(renderer.material);
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

		public override void OnNetworkSpawned()
		{
			UpdateColor();
		}

		public override void OnAuthorityChanged(uint _)
		{
			UpdateColor();
		}

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

		private void OnCollisionEnter(Collision other)
		{
			if (IsAuthor && other.gameObject.TryGetComponent<KatamariObject>(out var obj))
			{
				if (!obj.IsAuthor && obj.OwnerID == 0)
					obj.RequestAuthority();
			}
		}

		private void UpdateColor()
		{
			if (AuthorID == 0)
			{
				_material.SetColor(Color, UnityEngine.Color.white);
				return;
			}
			
			if (IsAuthor)
			{
				_material.SetColor(Color, NetworkManager.Client.UserColour);			
			}
			else
			{
				var client = IsServer
					? NetworkManager.Server.ConnectedClients[AuthorID]
					: NetworkManager.Client.ConnectedClients[AuthorID];
				_material.SetColor(Color, client.UserColour);
			}
		}

		private static float Map(float value, float from1, float from2, float to1, float to2)
		{
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}

		#endregion
	}

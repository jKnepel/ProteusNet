using jKnepel.ProteusNet.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform), typeof(Rigidbody))]
public class KatamariPlayer : NetworkBehaviour
{
	#region attributes

	[SerializeField] private Rigidbody rb;
	[SerializeField] private float forceMult = 100;

	#endregion

	#region lifecycle

	private void Awake()
	{
		if (rb == null)
			rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		if (!IsAuthor)
			return;
			
		Vector2 dir = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		Vector3 delta = new(dir.x, 0, dir.y);
		rb.AddForce(forceMult * Time.fixedDeltaTime * delta);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!IsAuthor || !other.TryGetComponent<KatamariObject>(out var att))
			return;

		att.Attach(transform);
	}

	private void OnTriggerExit(Collider other)
	{
		if (!IsAuthor || !other.TryGetComponent<KatamariObject>(out var att))
			return;

		att.Detach();
	}

	#endregion
}

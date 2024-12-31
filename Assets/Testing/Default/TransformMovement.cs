using UnityEngine;

public class TransformMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    
    private void Update()
    {
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");

        var movement = new Vector3(horizontal, 0, vertical);
        if (movement.magnitude > 1f)
            movement.Normalize();

        transform.Translate(movement * (movementSpeed * Time.deltaTime), Space.World);
        
        if (Input.GetKey(KeyCode.R))
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}

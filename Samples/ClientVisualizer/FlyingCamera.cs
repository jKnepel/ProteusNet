using UnityEngine;

namespace jKnepel.ProteusNet.Samples
{
    public class FlyingCamera : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private float sprintMultiplier = 2f;

        private float _yaw;
        private float _pitch;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            HandleMovement();
            HandleMouseLook();
        }

        private void HandleMovement()
        {
            var moveForward = Input.GetAxis("Vertical");
            var moveRight = Input.GetAxis("Horizontal");
            var moveUp = 0f;

            if (Input.GetKey(KeyCode.E)) moveUp += 1f;
            if (Input.GetKey(KeyCode.Q)) moveUp -= 1f;

            var currentSpeed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

            var trf = transform;
            var movement = trf.forward * moveForward +
                           trf.right * moveRight +
                           trf.up * moveUp;

            trf.position += movement * (currentSpeed * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            var mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            var mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            _yaw += mouseX;
            _pitch -= mouseY;

            _pitch = Mathf.Clamp(_pitch, -90f, 90f);
            transform.eulerAngles = new(_pitch, _yaw, 0f);
        }
    }
}

using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float fastMoveMultiplier = 3f;

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public bool requireRightMouseToLook = true;

    float yaw;
    float pitch;

    void Start()
    {
        var e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void Update()
    {
        bool looking = !requireRightMouseToLook || Input.GetMouseButton(1);

        if (looking)
        {
            yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxisRaw("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        if (requireRightMouseToLook)
        {
            Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !looking;
        }

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? fastMoveMultiplier : 1f);

        Vector3 planar = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        int upDown = 0;
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E)) upDown += 1;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q)) upDown -= 1;

        Vector3 moveLocal = (transform.right * planar.x) + (transform.forward * planar.z) + (transform.up * upDown);
        if (moveLocal.sqrMagnitude > 1f) moveLocal.Normalize();
        transform.position += moveLocal * speed * Time.deltaTime;
    }
}


















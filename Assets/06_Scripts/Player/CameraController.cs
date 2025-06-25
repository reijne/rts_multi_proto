using Unity.Netcode;
using UnityEngine;

public class CameraController : NetworkBehaviour
{
    public float moveSpeed = 10f;
    public float zoomSpeed = 1000f;
    public float minY = 1f;
    public float maxY = 100f;
    public float zoomMovementSpeedMultiplier = 0.1f;

    private new Camera camera;

    public void Start()
    {
        if (Game.singleton.isMulti && !IsOwner)
        {
            // Make sure only the owning player uses the camera
            Destroy(gameObject);
            return;
        }

        // Get the camera that is a direct child of the CameraRig.
        camera = GetComponentInChildren<Camera>();
        camera.enabled = true;
        camera.tag = "MainCamera";
    }

    void Update()
    {
        if (Game.singleton.isMulti && !IsOwner || camera == null)
            return;

        move();
        zoom();
    }

    // Movement on the plane of existence.
    void move()
    {
        Vector3 input = new Vector3(
            Input.GetAxis("Horizontal"),
            0,
            Input.GetAxis("Vertical")
        );

        float zoomMult = Mathf.Max(
            1,
            transform.position.y * zoomMovementSpeedMultiplier
        );

        transform.position += zoomMult * moveSpeed * Time.deltaTime * input;
    }

    void zoom()
    {
        // Zoom in and out using the mouse wheel, or touchpad.
        // Note: We cannot perform this in one operation because we need
        // the actual value of the transform.y to be set and clamped,
        // not updated each time with a clamped value.
        float scroll =
            Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * Time.deltaTime;

        float newY = Mathf.Clamp(transform.position.y + scroll, minY, maxY);

        Vector3 pos = transform.position;
        pos.y = newY;
        transform.position = pos;
    }
}

using Unity.Netcode;
using UnityEngine;

public class RTSPlayerCameraController : NetworkBehaviour
{
  public float moveSpeed = 10f;
  public float zoomSpeed = 1000f;
  public float minY = 1f;
  public float maxY = 100f;

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
    camera.tag = "MainCamera";
    camera.enabled = true;
  }

  void Update()
  {
    if (Game.singleton.isMulti && !IsOwner || camera == null)
    {
      return;
    }

    // Movement on the plane of existence.
    Vector3 input = new Vector3(
      Input.GetAxis("Horizontal"),
      0,
      Input.GetAxis("Vertical")
    );
    transform.position += moveSpeed * Time.deltaTime * input;

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

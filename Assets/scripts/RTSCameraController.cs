using UnityEngine;

public class RTSCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 20f;
    public float edgeScrollSpeed = 20f;
    public int edgeSize = 10;

    [Header("Zoom")]
    public float zoomSpeed = 200f;
    public float minY = 8f;
    public float maxY = 60f;

    [Header("Bounds")]
    public float minX = -50f;
    public float maxX = 50f;
    public float minZ = -50f;
    public float maxZ = 50f;

    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        // Freeze camera while the player is in the
        // game start setup (name entry steps).
        // Unfreezes as soon as setup is done and
        // the player is placing their first Town Hall.
        if (GameStartUI.Instance != null &&
            GameStartUI.Instance.IsCameraFrozen())
            return;

        HandleMovement();
        HandleZoom();

        transform.position = Vector3.Lerp(
            transform.position, targetPosition, Time.deltaTime * 10f);
    }

    void HandleMovement()
    {
        Vector3 dir = Vector3.zero;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        if (Input.GetKey(KeyCode.W)) dir += forward;
        if (Input.GetKey(KeyCode.S)) dir -= forward;
        if (Input.GetKey(KeyCode.A)) dir -= right;
        if (Input.GetKey(KeyCode.D)) dir += right;

        if (Input.mousePosition.x >= Screen.width - edgeSize) dir += right;
        if (Input.mousePosition.x <= edgeSize) dir -= right;
        if (Input.mousePosition.y >= Screen.height - edgeSize) dir += forward;
        if (Input.mousePosition.y <= edgeSize) dir -= forward;

        // 🔥 Speed scales with zoom (IMPORTANT)
        float heightFactor = targetPosition.y / maxY;
        float speed = baseMoveSpeed * Mathf.Lerp(0.5f, 2f, heightFactor);

        targetPosition += dir * speed * Time.deltaTime;

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
    }

    void HandleZoom()
    {
        // Disable scroll zoom when policy panel is open
        // so the player can scroll the policy list freely
        if (PolicyPanelUI.Instance != null &&
            PolicyPanelUI.Instance.IsOpen)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // 🔥 PURE vertical zoom (NO forward drift)
        targetPosition.y -= scroll * zoomSpeed * Time.deltaTime;

        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
    }
}
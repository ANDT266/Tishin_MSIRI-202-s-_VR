
using UnityEngine;
using UnityEngine.InputSystem;

public class PCMouseGrab : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Interaction")]
    public float grabDistance = 15f;
    public float grabRadius = 0.6f;

    [Header("Object Movement")]
    public float holdDistance = 1.5f;
    public float minHoldDistance = 0.5f;
    public float maxHoldDistance = 4f;
    public float wheelSensitivity = 0.5f;

    [Header("Machine Interaction")]
    public ViseController viseController;
    public ViseMagnetController magnetController;

    private Rigidbody rb;
    private Collider[] workpieceColliders;

    private bool isHeld;
    private Quaternion heldRotation;
    private float currentDistance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        workpieceColliders = GetComponentsInChildren<Collider>();

        if (rb == null)
        {
            Debug.LogError("PCMouseGrab: Rigidbody missing!");
            enabled = false;
            return;
        }

        if (playerCamera == null)
            Debug.LogError("PCMouseGrab: Player Camera missing!");
    }

    private void Update()
    {
        if (Mouse.current == null || playerCamera == null)
            return;

        if (!isHeld && Mouse.current.leftButton.wasPressedThisFrame)
            TryGrab();

        if (isHeld)
        {
            UpdateHeldPosition();

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                Release();
        }
    }

    private void TryGrab()
    {
        if (viseController != null &&
            (viseController.IsClamped || viseController.IsBusy))
        {
            Debug.LogWarning("PCMouseGrab: Vise is busy or clamped.");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        Physics.SyncTransforms();

        float bestDistance = float.PositiveInfinity;
        bool found = false;

        foreach (Collider col in workpieceColliders)
        {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                continue;

            // Find the closest point on the actual workpiece collider
            // to the mouse ray.
            Vector3 toCenter = col.bounds.center - ray.origin;
            float alongRay = Vector3.Dot(toCenter, ray.direction);

            if (alongRay < 0f || alongRay > grabDistance)
                continue;

            Vector3 rayPoint = ray.GetPoint(alongRay);
            Vector3 closestPoint = col.ClosestPoint(rayPoint);

            float distanceFromRay = Vector3.Distance(
                rayPoint, closestPoint
            );

            if (distanceFromRay <= grabRadius && alongRay < bestDistance)
            {
                bestDistance = alongRay;
                found = true;
            }
        }

        if (!found)
        {
            Debug.LogWarning("PCMouseGrab: Workpiece is outside grab area.");
            return;
        }

        Debug.Log($"PCMouseGrab: Workpiece found at {bestDistance:F2} m.");
        Grab();
    }

    private void Grab()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.useGravity = false;
        rb.isKinematic = true;

        if (magnetController != null)
            magnetController.NotifyPickedUp();

        currentDistance = Mathf.Clamp(
            holdDistance, minHoldDistance, maxHoldDistance
        );

        heldRotation = transform.rotation;
        isHeld = true;

        Debug.Log("PCMouseGrab: WORKPIECE PICKED UP!");
    }

    private void UpdateHeldPosition()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        currentDistance -= scroll * wheelSensitivity * 0.01f;

        currentDistance = Mathf.Clamp(
            currentDistance, minHoldDistance, maxHoldDistance
        );

        Ray ray = playerCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        rb.position = ray.GetPoint(currentDistance);
        rb.rotation = heldRotation;
    }

    private void Release()
    {
        isHeld = false;

        if (magnetController != null)
        {
            magnetController.NotifyDropped();
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("PCMouseGrab: WORKPIECE RELEASED!");
    }
}


using UnityEngine;

public class PCMouseGrab : MonoBehaviour
{
    public Camera playerCamera;
    public Transform holdPoint;
    public float grabDistance = 3f;
    public float throwMultiplier = 1.2f;

    private Rigidbody rb;
    private Transform originalParent;
    private bool isHeld;
    private Vector3 holdVelocity;
    private Vector3 previousHoldPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isHeld)
            TryGrab();

        if (isHeld)
        {
            holdVelocity = (holdPoint.position -
                previousHoldPosition) / Mathf.Max(Time.deltaTime, 0.001f);

            previousHoldPosition = holdPoint.position;

            if (Input.GetMouseButtonUp(0))
                Release();
        }
    }

    void TryGrab()
    {
        Ray ray = playerCamera.ScreenPointToRay(
            Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance))
        {
            if (hit.rigidbody == rb)
                Grab();
        }
    }

    void Grab()
    {
        originalParent = transform.parent;

        rb.isKinematic = true;
        rb.useGravity = false;

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        holdVelocity = Vector3.zero;
        previousHoldPosition = holdPoint.position;
        isHeld = true;
    }

    void Release()
    {
        transform.SetParent(originalParent, true);

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.linearVelocity = holdVelocity * throwMultiplier;

        isHeld = false;
    }
}

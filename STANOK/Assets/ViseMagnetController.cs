
using System.Collections;
using UnityEngine;

public class ViseMagnetController : MonoBehaviour
{
    [Header("Workpiece")]
    public Transform workpiece;
    public Rigidbody workpieceBody;

    [Header("Loading position")]
    public Transform loadingPoint;

    [Header("Magnet settings")]
    [Min(0.01f)]
    public float snapDuration = 0.3f;

    [Min(0.01f)]
    public float detectionRadius = 0.5f;

    private bool isSnapping;
    private bool isSnapped;
    private bool isHeld;

    private Coroutine snapRoutine;

    public bool IsSnapped => isSnapped;
    public bool IsSnapping => isSnapping;

    private void Awake()
    {
        if (workpieceBody == null && workpiece != null)
            workpieceBody = workpiece.GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TrySnap(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySnap(other);
    }

    private void TrySnap(Collider other)
    {
        if (workpiece == null ||
            workpieceBody == null ||
            loadingPoint == null)
            return;

        if (other.attachedRigidbody != workpieceBody)
            return;

        if (isSnapping || isSnapped || isHeld)
            return;

        if (workpieceBody.isKinematic)
            return;

        float distance = Vector3.Distance(
            workpiece.position,
            transform.position
        );

        if (distance > detectionRadius)
            return;

        snapRoutine = StartCoroutine(SnapWorkpiece());
    }

    private IEnumerator SnapWorkpiece()
    {
        isSnapping = true;

        workpieceBody.linearVelocity = Vector3.zero;
        workpieceBody.angularVelocity = Vector3.zero;
        workpieceBody.useGravity = false;
        workpieceBody.isKinematic = true;

        Vector3 startPosition = workpiece.position;
        Quaternion startRotation = workpiece.rotation;

        Vector3 targetPosition = loadingPoint.position;
        Quaternion targetRotation = loadingPoint.rotation;

        float elapsed = 0f;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / snapDuration)
            );

            workpiece.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            workpiece.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        workpiece.position = targetPosition;
        workpiece.rotation = targetRotation;

        workpieceBody.isKinematic = true;
        workpieceBody.useGravity = false;

        isSnapping = false;
        isSnapped = true;
        snapRoutine = null;

        Debug.Log("Vise Magnet: workpiece positioned.");
    }

    public void NotifyPickedUp()
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        isSnapping = false;
        isSnapped = false;
        isHeld = true;

        Debug.Log("Vise Magnet: pickup registered.");
    }

    public void NotifyDropped()
    {
        isHeld = false;
        isSnapping = false;
        isSnapped = false;

        if (workpieceBody != null)
        {
            if (workpieceBody.isKinematic)
            {
                workpieceBody.linearVelocity = Vector3.zero;
                workpieceBody.angularVelocity = Vector3.zero;
            }

            workpieceBody.isKinematic = false;
            workpieceBody.useGravity = true;
        }

        Debug.Log("Vise Magnet: workpiece dropped.");
    }

    public void ReleaseWorkpiece()
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        isSnapping = false;
        isSnapped = false;
        isHeld = false;

        if (workpieceBody != null)
        {
            if (workpieceBody.isKinematic)
            {
                workpieceBody.linearVelocity = Vector3.zero;
                workpieceBody.angularVelocity = Vector3.zero;
            }

            workpieceBody.isKinematic = false;
            workpieceBody.useGravity = true;
        }

        Debug.Log("Vise Magnet: workpiece released.");
    }
}

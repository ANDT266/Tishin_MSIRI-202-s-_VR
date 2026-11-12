
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

    private bool isSnapping;
    private bool isSnapped;
    private bool mustExitBeforeSnap;

    private Transform originalParent;
    private Coroutine snapRoutine;

    public bool IsSnapped => isSnapped;
    public bool IsSnapping => isSnapping;

    private void Awake()
    {
        if (workpiece != null)
            originalParent = workpiece.parent;
    }

    private void OnTriggerStay(Collider other)
    {
        if (workpieceBody == null || loadingPoint == null)
            return;

        if (other.attachedRigidbody != workpieceBody)
            return;

        if (isSnapping || isSnapped || mustExitBeforeSnap)
            return;

        if (workpieceBody.isKinematic)
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

            float t = Mathf.Clamp01(elapsed / snapDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            workpiece.position =
                Vector3.Lerp(startPosition, targetPosition, t);

            workpiece.rotation =
                Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        workpiece.position = targetPosition;
        workpiece.rotation = targetRotation;

        workpieceBody.linearVelocity = Vector3.zero;
        workpieceBody.angularVelocity = Vector3.zero;

        isSnapping = false;
        isSnapped = true;
        snapRoutine = null;

        Debug.Log("Vise Magnet: workpiece positioned.");
    }

    private void Update()
    {
        if (!isSnapped || workpiece == null)
            return;

        // Detect pickup by mouse or VR parenting.
        if (workpiece.parent != transform.parent)
        {
            isSnapped = false;
            mustExitBeforeSnap = true;

            Debug.Log("Vise Magnet: workpiece picked up.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (workpieceBody == null)
            return;

        if (other.attachedRigidbody == workpieceBody)
        {
            mustExitBeforeSnap = false;
        }
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
        mustExitBeforeSnap = true;

        if (workpieceBody != null)
        {
            workpieceBody.isKinematic = false;
            workpieceBody.useGravity = true;
            workpieceBody.linearVelocity = Vector3.zero;
            workpieceBody.angularVelocity = Vector3.zero;
        }

        Debug.Log("Vise Magnet: workpiece released.");
    }
}

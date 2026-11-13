
using UnityEngine;

public class ViseController : MonoBehaviour
{
    [Header("Vise components")]
    public Transform viseRoot;
    public Transform movingJaw;
    public Collider movingJawPlate;
    public Collider fixedJawPlate;

    [Header("Workpiece")]
    public Transform workpiece;
    public Collider workpieceCollider;
    public Rigidbody workpieceBody;
    public ViseMagnetController magnetController;

    [Header("Movement")]
    [Min(0.01f)]
    public float jawSpeed = 0.15f;

    [Min(0f)]
    public float contactOffset = 0.001f;

    [Header("Controls")]
    public bool enableKeyboardTest = false;

    private Vector3 initialJawPosition;
    private float targetJawX;

    private bool isClamping;
    private bool isReleasing;
    private bool isClamped;

    public bool IsClamped => isClamped;
    public bool IsBusy => isClamping || isReleasing;

    private void Start()
    {
        if (viseRoot == null || movingJaw == null ||
            workpiece == null || movingJawPlate == null ||
            fixedJawPlate == null || workpieceCollider == null)
        {
            Debug.LogError("ViseController: Missing Inspector references!");
            enabled = false;
            return;
        }

        if (workpieceBody == null)
            workpieceBody = workpiece.GetComponent<Rigidbody>();

        if (workpieceBody == null)
        {
            Debug.LogError("ViseController: Workpiece Rigidbody missing!");
            enabled = false;
            return;
        }

        initialJawPosition = movingJaw.localPosition;
    }

    private void Update()
    {
        if (enableKeyboardTest)
        {
            if (Input.GetKeyDown(KeyCode.C))
                ClampWorkpiece();

            if (Input.GetKeyDown(KeyCode.R))
                ReleaseWorkpiece();
        }

        if (isClamping)
            MoveJaw();

        if (isReleasing)
            MoveJawBack();
    }

    public void ClampWorkpiece()
    {
        if (IsBusy || isClamped)
            return;

        if (magnetController != null &&
            (!magnetController.IsSnapped || magnetController.IsSnapping))
        {
            Debug.LogWarning("Vise: workpiece is not positioned by magnet.");
            return;
        }

        // Lock the workpiece during clamping.
        workpieceBody.linearVelocity = Vector3.zero;
        workpieceBody.angularVelocity = Vector3.zero;
        workpieceBody.useGravity = false;
        workpieceBody.isKinematic = true;

        Physics.SyncTransforms();

        float fixedFace = GetLocalMinMax(fixedJawPlate.bounds).y;
        Vector2 partBounds = GetLocalMinMax(workpieceCollider.bounds);

        float alignmentOffset = fixedFace - partBounds.x;

        workpiece.position += viseRoot.TransformVector(
            new Vector3(alignmentOffset, 0f, 0f)
        );

        Physics.SyncTransforms();

        partBounds = GetLocalMinMax(workpieceCollider.bounds);
        float movingFace = GetLocalMinMax(movingJawPlate.bounds).x;

        targetJawX = movingJaw.localPosition.x +
            (partBounds.y + contactOffset - movingFace);

        isClamping = true;

        Debug.Log("Vise: clamping started.");
    }

    public void ReleaseWorkpiece()
    {
        if (IsBusy || (!isClamped &&
            (magnetController == null || !magnetController.IsSnapped)))
            return;

        isClamping = false;
        isClamped = false;
        isReleasing = true;

        Debug.Log("Vise: releasing workpiece.");
    }

    private void MoveJaw()
    {
        Vector3 p = movingJaw.localPosition;

        p.x = Mathf.MoveTowards(
            p.x, targetJawX, jawSpeed * Time.deltaTime
        );

        movingJaw.localPosition = p;

        if (Mathf.Abs(p.x - targetJawX) < 0.0001f)
        {
            p.x = targetJawX;
            movingJaw.localPosition = p;

            isClamping = false;
            isClamped = true;

            Debug.Log("Vise: workpiece clamped.");
        }
    }

    private void MoveJawBack()
    {
        Vector3 p = movingJaw.localPosition;

        p.x = Mathf.MoveTowards(
            p.x, initialJawPosition.x, jawSpeed * Time.deltaTime
        );

        movingJaw.localPosition = p;

        if (Mathf.Abs(p.x - initialJawPosition.x) < 0.0001f)
        {
            p.x = initialJawPosition.x;
            movingJaw.localPosition = p;

            // Keep the workpiece in its current world position.
            // Do not change its parent or enable gravity.
            workpieceBody.linearVelocity = Vector3.zero;
            workpieceBody.angularVelocity = Vector3.zero;
            workpieceBody.useGravity = false;
            workpieceBody.isKinematic = true;

            if (magnetController != null)
                magnetController.ReleaseWorkpiece();

            isReleasing = false;

            Debug.Log("Vise: jaw opened, workpiece released and held in place.");
        }
    }

    private Vector2 GetLocalMinMax(Bounds bounds)
    {
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;

        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                (i & 4) == 0 ? bounds.min.z : bounds.max.z
            );

            float localX = viseRoot.InverseTransformPoint(corner).x;

            min = Mathf.Min(min, localX);
            max = Mathf.Max(max, localX);
        }

        return new Vector2(min, max);
    }
}

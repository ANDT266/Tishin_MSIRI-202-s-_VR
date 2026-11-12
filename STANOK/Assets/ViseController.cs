
using UnityEngine;

public class ViseController : MonoBehaviour
{
    [Header("Vise components")]
    public Transform movingJaw;
    public Collider movingJawPlate;
    public Collider fixedJawPlate;

    [Header("Workpiece")]
    public Transform workpiece;
    public Collider workpieceCollider;

    [Header("Movement")]
    [Min(0.01f)]
    public float jawSpeed = 0.15f;

    [Min(0f)]
    public float contactOffset = 0.001f;

    [Header("Controls")]
    public bool enableKeyboardTest = true;

    private Vector3 initialJawPosition;
    private Transform originalWorkpieceParent;

    private bool isClamping;
    private bool isReleasing;
    private bool isClamped;

    private float targetJawX;

    private void Start()
    {
        if (movingJaw != null)
            initialJawPosition = movingJaw.localPosition;

        if (workpiece != null)
            originalWorkpieceParent = workpiece.parent;
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
        if (isClamping || isClamped || isReleasing)
            return;

        if (!ReferencesValid())
            return;

        // Find the fixed jaw inner face.
        float fixedFace = GetLocalMinMax(fixedJawPlate.bounds).y;

        // Find the workpiece bounds along local X.
        Vector2 partBounds = GetLocalMinMax(workpieceCollider.bounds);

        // Move the workpiece against the fixed jaw.
        float alignmentOffset = fixedFace - partBounds.x;

        Vector3 shift = transform.TransformVector(
            new Vector3(alignmentOffset, 0f, 0f));

        workpiece.position += shift;

        // Recalculate workpiece bounds after positioning.
        partBounds = GetLocalMinMax(workpieceCollider.bounds);

        // Find the inner face of the moving jaw.
        float movingFace = GetLocalMinMax(movingJawPlate.bounds).x;

        // Move the jaw toward the workpiece.
        targetJawX = movingJaw.localPosition.x
                   + (partBounds.y + contactOffset - movingFace);

        isClamping = true;

        Debug.Log("Vise: automatic centering and clamping started.");
    }

    public void ReleaseWorkpiece()
    {
        if (!isClamped && !isClamping)
            return;

        isClamping = false;
        isClamped = false;
        isReleasing = true;

        // Detach the workpiece while preserving its world position.
        if (workpiece != null)
            workpiece.SetParent(originalWorkpieceParent, true);

        Debug.Log("Vise: releasing workpiece.");
    }

    private void MoveJaw()
    {
        Vector3 p = movingJaw.localPosition;

        p.x = Mathf.MoveTowards(
            p.x,
            targetJawX,
            jawSpeed * Time.deltaTime);

        movingJaw.localPosition = p;

        if (Mathf.Abs(p.x - targetJawX) < 0.0001f)
        {
            p.x = targetJawX;
            movingJaw.localPosition = p;

            isClamping = false;
            isClamped = true;

            // Attach workpiece to the vise, preserving its world position.
            workpiece.SetParent(transform, true);

            Debug.Log("Vise: workpiece clamped.");
        }
    }

    private void MoveJawBack()
    {
        Vector3 p = movingJaw.localPosition;

        p.x = Mathf.MoveTowards(
            p.x,
            initialJawPosition.x,
            jawSpeed * Time.deltaTime);

        movingJaw.localPosition = p;

        if (Mathf.Abs(p.x - initialJawPosition.x) < 0.0001f)
        {
            p.x = initialJawPosition.x;
            movingJaw.localPosition = p;

            isReleasing = false;

            Debug.Log("Vise: jaw opened.");
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

            float localX = transform.InverseTransformPoint(corner).x;

            min = Mathf.Min(min, localX);
            max = Mathf.Max(max, localX);
        }

        return new Vector2(min, max);
    }

    private bool ReferencesValid()
    {
        if (movingJaw == null ||
            movingJawPlate == null ||
            fixedJawPlate == null ||
            workpiece == null ||
            workpieceCollider == null)
        {
            Debug.LogError(
                "ViseController: assign all references in Inspector.");

            return false;
        }

        return true;
    }
}

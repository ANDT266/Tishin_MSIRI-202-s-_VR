
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class MachineKinematics : MonoBehaviour
{
    [Header("Machine Axes")]
    public Transform headZ;
    public Transform tableX;
    public Transform tableY;

    [Header("Spindle")]
    public SpindleController spindleController;

    [Header("Vise")]
    public ViseController viseController;

    [Header("Movement Speed")]
    public float moveSpeed = 0.5f;

    [Header("Head Z Limits")]
    public float headMin = -0.3f;
    public float headMax = 0.3f;

    [Header("Table X Limits")]
    public float tableXMin = -0.3f;
    public float tableXMax = 0.3f;

    [Header("Table Y Limits")]
    public float tableYMin = -0.3f;
    public float tableYMax = 0.3f;

    [Header("Coordinate Display")]
    public TMP_Text coordinateXText;
    public TMP_Text coordinateYText;
    public TMP_Text coordinateZText;
    public float millimetersPerUnit = 1000f;

    [Header("Machine Status")]
    public Image statusBadgeImage;
    public TMP_Text statusText;

    private readonly Color32 activeBackground =
        new Color32(18, 60, 43, 255);

    private readonly Color32 activeTextColor =
        new Color32(50, 213, 131, 255);

    private readonly Color32 standbyBackground =
        new Color32(255, 255, 255, 255);

    private readonly Color32 standbyTextColor =
        new Color32(16, 26, 43, 255);

    private readonly Color32 stopBackground =
        new Color32(122, 23, 32, 255);

    private readonly Color32 stopTextColor =
        new Color32(255, 255, 255, 255);

    private Vector3 headStart;
    private Vector3 tableXStart;
    private Vector3 tableYStart;

    private bool stopRequested;
    private bool machineEnabled;
    private bool pendingStart;

    private void Start()
    {
        if (headZ == null || tableX == null || tableY == null)
        {
            Debug.LogError("Machine axes are not assigned!");
            enabled = false;
            return;
        }

        headStart = headZ.localPosition;
        tableXStart = tableX.localPosition;
        tableYStart = tableY.localPosition;

        machineEnabled = false;

        if (spindleController != null)
            spindleController.StopSpindle();

        UpdateCoordinateDisplay();
        SetStandby();
    }

    private void Update()
    {
        // Wait until the vise finishes clamping.
        if (pendingStart)
        {
            if (stopRequested)
            {
                pendingStart = false;
            }
            else if (viseController != null &&
                     viseController.IsClamped)
            {
                pendingStart = false;
                machineEnabled = true;

                if (spindleController != null)
                    spindleController.StartSpindle();

                Debug.Log(
                    "Machine: clamping complete, machine started."
                );
            }
            else
            {
                UpdateCoordinateDisplay();
                SetStandby();
                return;
            }
        }

        if (stopRequested || !machineEnabled)
        {
            if (stopRequested)
                SetStopVisual();
            else
                SetStandby();

            UpdateCoordinateDisplay();
            return;
        }

        Keyboard kb = Keyboard.current;

        if (kb == null)
        {
            SetStandby();
            return;
        }

        float speed = moveSpeed * Time.deltaTime;

        Vector3 oldHead = headZ.localPosition;
        Vector3 oldX = tableX.localPosition;
        Vector3 oldY = tableY.localPosition;

        // HEAD Z: I / K
        float z = 0f;

        if (kb.iKey.isPressed) z += 1f;
        if (kb.kKey.isPressed) z -= 1f;

        Vector3 hp = headZ.localPosition;

        hp.y = Mathf.Clamp(
            hp.y + z * speed,
            headStart.y + headMin,
            headStart.y + headMax
        );

        headZ.localPosition = hp;

        // TABLE X: J / L
        float x = 0f;

        if (kb.lKey.isPressed) x += 1f;
        if (kb.jKey.isPressed) x -= 1f;

        Vector3 xp = tableX.localPosition;

        xp.x = Mathf.Clamp(
            xp.x + x * speed,
            tableXStart.x + tableXMin,
            tableXStart.x + tableXMax
        );

        tableX.localPosition = xp;

        // TABLE Y: U / O
        float y = 0f;

        if (kb.uKey.isPressed) y += 1f;
        if (kb.oKey.isPressed) y -= 1f;

        Vector3 yp = tableY.localPosition;

        yp.z = Mathf.Clamp(
            yp.z + y * speed,
            tableYStart.z + tableYMin,
            tableYStart.z + tableYMax
        );

        tableY.localPosition = yp;

        bool isMoving =
            (headZ.localPosition - oldHead).sqrMagnitude > 0.0000001f ||
            (tableX.localPosition - oldX).sqrMagnitude > 0.0000001f ||
            (tableY.localPosition - oldY).sqrMagnitude > 0.0000001f;

        UpdateCoordinateDisplay();

        bool spindleIsRunning =
            spindleController != null &&
            spindleController.spindleRunning;

        if (isMoving || spindleIsRunning)
            SetActive();
        else
            SetStandby();
    }

    private void UpdateCoordinateDisplay()
    {
        float x = (tableX.localPosition.x - tableXStart.x)
                  * millimetersPerUnit;

        float y = (tableY.localPosition.z - tableYStart.z)
                  * millimetersPerUnit;

        float z = (headZ.localPosition.y - headStart.y)
                  * millimetersPerUnit;

        if (coordinateXText != null)
            coordinateXText.text = $"X: {x:F1} мм";

        if (coordinateYText != null)
            coordinateYText.text = $"Y: {y:F1} мм";

        if (coordinateZText != null)
            coordinateZText.text = $"Z: {z:F1} мм";
    }

    private void SetMachineStatus(
        string label,
        Color32 background,
        Color32 foreground)
    {
        if (statusText != null)
        {
            statusText.text = label;
            statusText.color = foreground;
        }

        if (statusBadgeImage != null)
            statusBadgeImage.color = background;
    }

    private void SetActive()
    {
        SetMachineStatus(
            "ACTIVE",
            activeBackground,
            activeTextColor
        );
    }

    private void SetStandby()
    {
        SetMachineStatus(
            "STAND BY",
            standbyBackground,
            standbyTextColor
        );
    }

    private void SetStopVisual()
    {
        SetMachineStatus(
            "STOP",
            stopBackground,
            stopTextColor
        );
    }

    public void ResumeMachine()
    {
        if (stopRequested || pendingStart)
            return;

        if (viseController == null)
        {
            Debug.LogError(
                "Machine: Vise Controller is not assigned!"
            );
            return;
        }

        if (viseController.IsClamped)
        {
            machineEnabled = true;

            if (spindleController != null)
                spindleController.StartSpindle();

            SetActive();
            return;
        }

        if (viseController.magnetController == null ||
            !viseController.magnetController.IsSnapped)
        {
            Debug.LogWarning(
                "Machine: workpiece is not positioned in the vise."
            );
            return;
        }

        machineEnabled = false;

        viseController.ClampWorkpiece();

        if (viseController.IsBusy)
        {
            pendingStart = true;
            SetStandby();
        }
        else if (viseController.IsClamped)
        {
            machineEnabled = true;

            if (spindleController != null)
                spindleController.StartSpindle();
        }
        else
        {
            Debug.LogWarning(
                "Machine: vise could not start clamping."
            );
        }
    }

    public void StopMachine()
    {
        pendingStart = false;
        stopRequested = true;
        machineEnabled = false;

        if (spindleController != null)
            spindleController.StopSpindle();

        // The vise remains clamped.
        SetStopVisual();
    }

    public void ResetMachine()
    {
        pendingStart = false;
        stopRequested = false;
        machineEnabled = false;

        if (spindleController != null)
            spindleController.StopSpindle();

        if (viseController != null)
            viseController.ReleaseWorkpiece();

        SetStandby();

        Debug.Log("Machine: reset, vise release requested.");
    }
}

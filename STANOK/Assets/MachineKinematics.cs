
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

    private bool stopRequested = false;
    private bool machineEnabled = true;

    void Start()
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

        if (spindleController != null)
            spindleController.StopSpindle();

        UpdateCoordinateDisplay();
        SetStandby();
    }

    void Update()
    {
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

    void UpdateCoordinateDisplay()
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

    void SetMachineStatus(
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

    void SetActive()
    {
        SetMachineStatus(
            "ACTIVE",
            activeBackground,
            activeTextColor
        );
    }

    void SetStandby()
    {
        SetMachineStatus(
            "STAND BY",
            standbyBackground,
            standbyTextColor
        );
    }

    void SetStopVisual()
    {
        SetMachineStatus(
            "STOP",
            stopBackground,
            stopTextColor
        );
    }

    public void StopMachine()
    {
        stopRequested = true;
        machineEnabled = false;

        if (spindleController != null)
            spindleController.StopSpindle();

        SetStopVisual();
    }

    public void ResetMachine()
    {
        stopRequested = false;
        machineEnabled = false;

        if (spindleController != null)
            spindleController.StopSpindle();

        SetStandby();
    }

    public void ResumeMachine()
    {
        if (stopRequested)
            return;

        machineEnabled = true;

        if (spindleController != null)
            spindleController.StartSpindle();

        SetStandby();
    }
}

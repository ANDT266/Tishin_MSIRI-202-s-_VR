
using UnityEngine;

public class SpindleController : MonoBehaviour
{
    [Header("Spindle Settings")]
    public float rpm = 1200f;
    public bool spindleRunning = false;

    void Update()
    {
        if (!spindleRunning)
            return;

        float rotationSpeed = rpm * 6f * Time.deltaTime;

        transform.Rotate(
            Vector3.up,
            rotationSpeed,
            Space.Self
        );
    }

    public void StartSpindle()
    {
        spindleRunning = true;
    }

    public void StopSpindle()
    {
        spindleRunning = false;
    }

    public void SetRPM(float newRPM)
    {
        rpm = Mathf.Clamp(newRPM, 0f, 6000f);
    }
}

using UnityEngine;

public class StaffWave : MonoBehaviour 
{
    private LineRenderer lineRenderer;
    
    [Header("Wave Settings")]
    public int pointsCount = 100;    
    public float waveSpeed = 2f;    
    public float waveAmplitude = 0.5f; 
    public float zLength = 50f;     

    void Start() 
    {
        lineRenderer = GetComponent<LineRenderer>();
        // Ensure the line follows the parent object's position
        lineRenderer.useWorldSpace = false; 
    }

    void Update() 
    {
        // Set the number of points for the LineRenderer
        lineRenderer.positionCount = pointsCount;

        for (int i = 0; i < pointsCount; i++) 
        {
            // Calculate the progress along the line (0 to 1)
            float progress = (float)i / (pointsCount - 1);
            // Calculate the Z position based on the total length
            float z = progress * zLength;

            // Basic Sine Wave Logic: 
            // Mathf.Sin(Time.time * waveSpeed) makes the whole line vibrate
            // Adding (z * 0.5f) creates different phases along the Z axis (the wave shape)
            float y = Mathf.Sin(Time.time * waveSpeed + z * 0.5f) * waveAmplitude;

            // Apply the calculated position to the specific point
            lineRenderer.SetPosition(i, new Vector3(0, y, z));
        }
    }
}
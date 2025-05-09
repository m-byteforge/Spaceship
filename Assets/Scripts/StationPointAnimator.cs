using UnityEngine;

public class StationPointAnimator : MonoBehaviour
{
    private int level;
    private float rotationSpeed = 30f; // Slow rotation speed (degrees per second)
    private Transform connectorRotator;
    private PlayerController playerController; // To check if the rocket is active

    public void Initialize(int level)
    {
        this.level = level;

        // Find the ConnectorRotator child for Levels 1 and 2
        if (level == 1 || level == 2)
        {
            connectorRotator = transform.Find("ConnectorRotator") ?? transform.Find("connectorrotator") ?? transform.Find("Connector Rotator");
            if (connectorRotator == null)
            {
                Debug.LogWarning($"ConnectorRotator not found on {gameObject.name}! Rotation will not be applied.");
            }
        }
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
       
        if (playerController != null && playerController.enabled)
            return;

        if (level == 1 || level == 2)
        {
        
            if (connectorRotator != null)
            {
                connectorRotator.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.Self);
            }
        }
        else if (level == 3)
        {

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}

/******************************************************done*/

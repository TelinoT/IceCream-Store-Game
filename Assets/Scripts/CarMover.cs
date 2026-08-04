using UnityEngine;

public class CarMover : MonoBehaviour
{
    [HideInInspector] public float speed = 5f;
    public float lifetime = 15f; // How long before it auto-destroys

    void Start()
    {
        // Safety cleanup: Destroy this specific car after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Always move forward based on the car's local rotation
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
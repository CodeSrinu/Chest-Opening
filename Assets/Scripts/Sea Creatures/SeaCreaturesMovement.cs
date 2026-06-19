using UnityEngine;

public class SeaCreaturesMovement : MonoBehaviour
{
    public float speed = 2f;

    void Update()
    {
        transform.Translate(0, speed, 0);
    }
}

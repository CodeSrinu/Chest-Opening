using UnityEngine;

public class SharkSpawner : MonoBehaviour
{
    public GameObject sharkPrefab;
    public Transform player;

    public float spawnDistance = 50f;

    private float nextSpawnZ;

    void Start()
    {
        nextSpawnZ = player.position.z + spawnDistance;
    }

    void Update()
    {
        if (player.position.z >= nextSpawnZ)
        {
            SpawnShark();

            nextSpawnZ += spawnDistance;
        }
    }

    void SpawnShark()
    {
        Vector3 spawnPos = new Vector3(
            0f,
            2f,
            nextSpawnZ + 30f
        );

        Instantiate(sharkPrefab, spawnPos, Quaternion.identity);
    }
}
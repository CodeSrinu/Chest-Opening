using System.Collections.Generic;

using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] obstaclePrefabs;
    private List<GameObject> Obstaclepool=new List<GameObject>();
    public int poolSize=10;

    [Header("Spawn Settings")]
    public Transform player;
    public float spawnDistanceAhead = 20f;
    public float minLateralOffset = -4f;
    public float maxLateralOffset = 4f;

    [Header("Spawn Timing")]
    public float initialInterval = 4f;
    public float minInterval = 1.5f;
    public float intervalDecreaseRate = 0.02f;

    private float currentInterval;
    private float timer;
    private float gameTime;

    private void Start()
    {
        currentInterval = initialInterval;
        for(int i = 0; i < poolSize; i++)
        {
            GameObject obstacle=Instantiate(obstaclePrefabs[Random.Range(0,obstaclePrefabs.Length)]);
            obstacle.SetActive(false);
            Obstaclepool.Add(obstacle);
        }
    }
   GameObject GetObstacle()
    {
        List<GameObject> available = new List<GameObject>();
        foreach (GameObject obstacle in Obstaclepool)
        {
            if (!obstacle.activeInHierarchy)
            {
                available.Add(obstacle);
            }
        }
        if (available.Count == 0)
        {
            return null;
        }
        return available[Random.Range(0, available.Count)];
    }


    private void Update()
    {
        if (player == null)
            return;

        gameTime += Time.deltaTime;
        currentInterval = Mathf.Max(minInterval, initialInterval - gameTime * intervalDecreaseRate);

        timer += Time.deltaTime;

        if (timer >= currentInterval)
        {
            SpawnObstacle();
            timer = 0f;
        }
        foreach (GameObject obstacle in Obstaclepool)
        {
            if (obstacle.activeInHierarchy)
            {
                if (player.position.z > obstacle.transform.position.z + 10f)
                {
                    obstacle.SetActive(false);
                }
            }
        }
    }

    private void SpawnObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
            return;


        float lateralOffset = Random.Range(minLateralOffset, maxLateralOffset);

        Vector3 spawnPos =
            player.position + player.forward * spawnDistanceAhead + player.right * lateralOffset;

        GameObject Obstacle = GetObstacle();
        if (Obstacle == null)
        {
            return;
        }
        Obstacle.transform.position=spawnPos;
        Obstacle.transform.rotation= Quaternion.identity;
        Obstacle.SetActive(true);
    }
}

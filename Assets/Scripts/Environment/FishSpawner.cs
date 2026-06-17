using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    List<GameObject> Fishes=new List<GameObject>();
    public Transform player;
    public float SpawnDistance;
    void Update()
    {
        SpawnDistance=Random.Range(2f,15f);
        
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class landSpawner : MonoBehaviour
{
    public Transform player;
    public GameObject[] landPrefabs;
    public int tilesCount=10;
    public float spawnDistance=1f;
    public float tileLength=20f;
    private List<GameObject> tiles=new List<GameObject>();
    private float nextspawnZ;
    void Start()
    {
        Debug.Log("Start called");
        for(int i = 0; i < tilesCount; i++)
        {
            GameObject randomPrefab=landPrefabs[Random.Range(0,landPrefabs.Length)];
            GameObject tile=Instantiate(randomPrefab,new Vector3(0,0,i*tileLength),Quaternion.identity);
            tiles.Add(tile);
        }
        nextspawnZ=tilesCount*tileLength;
    }
    void Update()
    {
        GameObject oldestTile = tiles[0];
        if (player.position.z+spawnDistance > oldestTile.transform.position.z+tileLength)
        {
            ReuseTile();
            Debug.Log("Update Running");
        }
        
    }
    void ReuseTile()
    {
        GameObject oldestTile=tiles[0];
        oldestTile.transform.position=new Vector3(0,0,nextspawnZ);
        tiles.RemoveAt(0);
        tiles.Add(oldestTile);
        nextspawnZ+=tileLength;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Phys;

public class ObjectSpawner : MonoBehaviour {

    public List<Transform> ObjectPrefabs = new List<Transform>();

    public float SpawnInterval;
    public float SpawnAmount;
    public Vector2 PositionVariation;
    public Vector2 SizeVariation;
    public Vector2 DensityVariation;
    public Vector2 RotationVariation;

    private int spawnedAmount;

	// Use this for initialization
	void Start () {
        InvokeRepeating("Spawn", 0f, SpawnInterval);
	}
	
	void Spawn()
    {
        if(spawnedAmount < SpawnAmount)
        {
            Vector2 position = transform.position + transform.right * Random.Range(PositionVariation.x, PositionVariation.y);
            float radius = Random.Range(SizeVariation.x, SizeVariation.y);
            float density = Random.Range(DensityVariation.x, DensityVariation.y);
            int index = Random.Range(0, ObjectPrefabs.Count);
            float rot = Random.Range(RotationVariation.x, RotationVariation.y);

            Transform trans = Instantiate(ObjectPrefabs[index]);
            trans.position = position;
            trans.localScale *= radius;
            trans.rotation *= Quaternion.Euler(0f, 0f, rot);

            PhysRigidbody rigid = trans.GetComponent<PhysRigidbody>();
            rigid.Density = density;

            spawnedAmount++;
        }
    }
}

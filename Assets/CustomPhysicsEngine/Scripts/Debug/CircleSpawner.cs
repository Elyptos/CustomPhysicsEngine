using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Phys;

public class CircleSpawner : MonoBehaviour {

    public Transform CirclePrefab;

    public float SpawnInterval;
    public float SpawnAmount;
    public Vector2 PositionVariation;
    public Vector2 SizeVariation;
    public Vector2 MassVariation;

    private int spawnedAmount;

	// Use this for initialization
	void Start () {
        InvokeRepeating("Spawn", 0f, SpawnInterval);
	}
	
	void Spawn()
    {
        if(spawnedAmount <= SpawnAmount)
        {
            Vector2 position = transform.position + transform.right * Random.Range(PositionVariation.x, PositionVariation.y);
            float radius = Random.Range(SizeVariation.x, SizeVariation.y);
            float mass = Random.Range(MassVariation.x, MassVariation.y);

            Transform trans = Instantiate(CirclePrefab);
            trans.position = position;

            PhysSphereCollider2D collider = trans.GetComponent<PhysSphereCollider2D>();
            collider.CollisionSphere.Radius = radius;

            PhysRigidbody rigid = trans.GetComponent<PhysRigidbody>();
            rigid.Mass = mass;

            spawnedAmount++;
        }
    }
}

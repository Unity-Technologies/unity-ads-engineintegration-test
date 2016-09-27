using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class LoadTesting : MonoBehaviour
{
	public float FPS;

	private List<byte[]> memoryConsumingArrays = new List<byte[]> ();
	private long mbAllocated = 0;
	private float deltaTime; // for FPS

	const int mbToAllocate = 100;
	const int bytesToAllocate = mbToAllocate * 1024 * 1024;

	void Update()
	{
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
		this.FPS = 1.0f / deltaTime;
	}

	public void Allocate100MB ()
	{
		mbAllocated += mbToAllocate;

		var memoryConsumingArray = new byte[bytesToAllocate];
		memoryConsumingArrays.Add (memoryConsumingArray);

		UIController.Instance.Log (string.Format("Allocated {0} MB in total", mbAllocated));
	}

	public void ToggleCPULoad ()
	{
		StartCoroutine (StartCpuIntensiveOperation ());
	}

	private IEnumerator StartCpuIntensiveOperation()
	{
		float origFPS = FPS;

		var prefab = Resources.Load ("UnityBall");

		// create gameobjects until we manage to create some load
		while (true)
		{
			if (FPS > (origFPS - 5))
			{
				Instantiate (
					prefab, 
					new Vector2 (UnityEngine.Random.Range (-2f, 2f), UnityEngine.Random.Range (0f, 2f)),
					Quaternion.identity);
			}

			yield return new WaitForSeconds (0.1f);
		}
	}
}

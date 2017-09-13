using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ProceduralPlanet : MonoBehaviour {

	public bool autoUpdate;
	[Range(0,6)]
	public int recursionLevel;
	public float scale;
	[Range(0.001f,1f)]
	public float height;
	[Range(2,10)]
	public int heightLevels;
	[Range(1f,2f)]
	public float waterLevel;
	public float seed;

	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;
	private Texture3D tex;

	void Start () {
		meshRenderer = GetComponent<MeshRenderer> ();
		meshFilter = GetComponent<MeshFilter> ();
		Generate ();

	}

	public void Generate() {
		ProceduralPlanetMesh ppm = new ProceduralPlanetMesh ();
		Mesh mesh = ppm.GenerateMesh (recursionLevel, height, seed, waterLevel, scale, heightLevels);
		meshFilter.mesh = mesh;
		meshRenderer.sharedMaterials [0].SetFloat ("_HeightMax", ppm.GetHeight ());
	}

	void OnValidate() {
		if (scale < 1f) {
			scale = 1f;
		}
	}
}

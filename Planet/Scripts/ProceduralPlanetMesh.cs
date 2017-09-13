using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[System.Serializable]
public class ProceduralPlanetMesh {

	public Mesh mesh;

	private List<Vector3> vertices;
	private List<int> trianglesLand;
	private List<int> trianglesWater;
	private List<Vector2> UV;
	private float height = 0f;

	public Mesh GenerateMesh(int recursionLevel, float distortion, float seed, float waterLevel, float scale, int heightLevels) {
		Mesh mesh = new Mesh ();
		mesh.name = "Planet mesh";

		vertices = new List<Vector3> ();
		trianglesLand = new List<int> ();
		trianglesWater = new List<int> ();
		UV = new List<Vector2> ();

		vertices = Create (recursionLevel, distortion, seed, waterLevel, scale, heightLevels);
		mesh.subMeshCount = 2;
		mesh.vertices = vertices.ToArray ();
		mesh.SetTriangles (trianglesLand, 0);
		//mesh.SetTriangles (trianglesWater, 1);
		mesh.uv = UV.ToArray ();
		mesh.RecalculateNormals ();

		return mesh;
	}
		
	private struct TriangleIndices {
		public int v1;
		public int v2;
		public int v3;

		public TriangleIndices(int v1, int v2, int v3) {
			this.v1 = v1;
			this.v2 = v2;
			this.v3 = v3;
		}

	}

	private List<Vector3> geometry;
	private int index;
	private Dictionary<long, int> middlePointIndexCache;

	// add vertex to mesh, fix position to be on unit sphere, return index
	private int addVertex(Vector3 p) {
		float length = Mathf.Sqrt (p.x * p.x + p.y * p.y + p.z * p.z);
		geometry.Add (new Vector3 (p.x / length, p.y / length, p.z / length));
		return index++;
	}

	// return index of point in the middle of p1 and p2
	private int getMiddlePoint(int p1, int p2) {
		// first check if we have it already
		bool firstIsSmaller = p1 < p2;
		long smallerIndex = firstIsSmaller ? p1 : p2;
		long greaterIndex = firstIsSmaller ? p2 : p1;
		long key = (smallerIndex << 32) + greaterIndex;

		int ret;
		if (this.middlePointIndexCache.TryGetValue (key, out ret)) {
			return ret;
		}

		// not in cache, calculate it
		Vector3 point1 = this.geometry [p1];
		Vector3 point2 = this.geometry [p2];
		Vector3 middle = new Vector3 (
			                  (point1.x + point2.x) / 2.0f, 
			                  (point1.y + point2.y) / 2.0f, 
			                  (point1.z + point2.z) / 2.0f);

		// add vertex makes sure point is on unit sphere
		int i = addVertex (middle); 

		// store it, return index
		this.middlePointIndexCache.Add (key, i);
		return i;
	}

	public List<Vector3> Create(int recursionLevel, float distortion, float seed, float waterLevel, float scale, int heightLevels) {
		this.geometry = new List<Vector3> ();
		this.middlePointIndexCache = new Dictionary<long, int> ();
		this.index = 0;

		// create 12 vertices of a icosahedron
		var t = (1.0f + Mathf.Sqrt (5.0f)) / 2.0f;

		addVertex (new Vector3 (-1, t, 0));
		addVertex (new Vector3 (1, t, 0));
		addVertex (new Vector3 (-1, -t, 0));
		addVertex (new Vector3 (1, -t, 0));

		addVertex (new Vector3 (0, -1, t));
		addVertex (new Vector3 (0, 1, t));
		addVertex (new Vector3 (0, -1, -t));
		addVertex (new Vector3 (0, 1, -t));

		addVertex (new Vector3 (t, 0, -1));
		addVertex (new Vector3 (t, 0, 1));
		addVertex (new Vector3 (-t, 0, -1));
		addVertex (new Vector3 (-t, 0, 1));


		// create 20 triangles of the icosahedron
		var faces = new List<TriangleIndices> ();

		// 5 faces around point 0
		faces.Add (new TriangleIndices (0, 11, 5));
		faces.Add (new TriangleIndices (0, 5, 1));
		faces.Add (new TriangleIndices (0, 1, 7));
		faces.Add (new TriangleIndices (0, 7, 10));
		faces.Add (new TriangleIndices (0, 10, 11));

		// 5 adjacent faces 
		faces.Add (new TriangleIndices (1, 5, 9));
		faces.Add (new TriangleIndices (5, 11, 4));
		faces.Add (new TriangleIndices (11, 10, 2));
		faces.Add (new TriangleIndices (10, 7, 6));
		faces.Add (new TriangleIndices (7, 1, 8));

		// 5 faces around point 3
		faces.Add (new TriangleIndices (3, 9, 4));
		faces.Add (new TriangleIndices (3, 4, 2));
		faces.Add (new TriangleIndices (3, 2, 6));
		faces.Add (new TriangleIndices (3, 6, 8));
		faces.Add (new TriangleIndices (3, 8, 9));

		// 5 adjacent faces 
		faces.Add (new TriangleIndices (4, 9, 5));
		faces.Add (new TriangleIndices (2, 4, 11));
		faces.Add (new TriangleIndices (6, 2, 10));
		faces.Add (new TriangleIndices (8, 6, 7));
		faces.Add (new TriangleIndices (9, 8, 1));

		// refine triangles
		for (int i = 0; i < recursionLevel; i++) {
			var faces2 = new List<TriangleIndices> ();
			foreach (var tri in faces) {
				// replace triangle by 4 triangles
				int a = getMiddlePoint (tri.v1, tri.v2);
				int b = getMiddlePoint (tri.v2, tri.v3);
				int c = getMiddlePoint (tri.v3, tri.v1);

				faces2.Add (new TriangleIndices (tri.v1, a, c));
				faces2.Add (new TriangleIndices (tri.v2, b, a));
				faces2.Add (new TriangleIndices (tri.v3, c, b));
				faces2.Add (new TriangleIndices (a, b, c));
			}
			faces = faces2;
		}
			
		// Set UV coordinates
		for (int i = 0; i < geometry.Count; i++) {
			Vector3 d = Vector3.Normalize(geometry [i]);
			UV.Add (new Vector2 (Mathf.Atan2 (d.z, d.x) / (2f * Mathf.PI) + 0.5f, d.y * 0.5f + 0.5f));
		}

		// Fix UV wrapping
		int[] wrapped = DetectWrappedUVCoordinates (faces);
		FixWrappedUV (wrapped, faces);


		//List<int> waterIndex = new List<int> ();

		SimplexNoise.SetPerm ();
		float offset = seed; //Random.Range (0f, 200f);
		float currentHeight = 0f;
		for (int i = 0; i < geometry.Count; i++) {
			geometry [i] *= 1f + distortion * StepLandscape (SimplexNoise.GenerateNoise (geometry [i].x, geometry [i].y, geometry [i].z, scale, offset), heightLevels);
			//geometry [i] *= 1f + distortion * SimplexNoise.GenerateNoise (geometry [i].x, geometry [i].y, geometry [i].z, scale, offset);
			geometry [i] *= 1f + 0.6f * distortion * SimplexNoise.GenerateNoise (geometry [i].x, geometry [i].y, geometry [i].z, scale*10f, offset) * distortion * SimplexNoise.GenerateNoise (geometry [i].x, geometry [i].y, geometry [i].z, scale, offset);
			currentHeight = geometry [i].magnitude;
			if (currentHeight > height) {
				height = currentHeight;
			}
		}
		/*var facesWater = new List<TriangleIndices> ();
		for (int i = faces.Count-1; i > -1; i--) {
			if (waterIndex.Contains (faces [i].v1)) {
				if (waterIndex.Contains (faces [i].v2)) {
					if (waterIndex.Contains (faces [i].v3)) {
						facesWater.Add (faces [i]);
						faces.RemoveAt (i);
					}
				}
			}
		}*/

		// Add Triangles
		foreach (var tri in faces) {
			this.trianglesLand.Add (tri.v1);
			this.trianglesLand.Add (tri.v2);
			this.trianglesLand.Add (tri.v3);
		}
		/*foreach (var tri in facesWater) {
			this.trianglesWater.Add (tri.v1);
			this.trianglesWater.Add (tri.v2);
			this.trianglesWater.Add (tri.v3);
		}*/
		//Debug.Log ("Simplex noise: " + System.DateTime.Now.Subtract(ti).TotalSeconds);
		return this.geometry;
	}

	public float GetHeight() {
		return height;
	}

	private int[] DetectWrappedUVCoordinates(List<TriangleIndices> triangleIndices) {
		List<int> indices = new List<int>();
		for (int i = 0; i < triangleIndices.Count; i++) {
			int a = triangleIndices[i].v1;
			int b = triangleIndices[i].v2;
			int c = triangleIndices[i].v3;
			Vector3 texA = new Vector3 (UV [a].x, UV [a].y, 0f);
			Vector3 texB = new Vector3 (UV [b].x, UV [b].y, 0f);
			Vector3 texC = new Vector3 (UV [c].x, UV [c].y, 0f);
			Vector3 texNormal = Vector3.Cross(texB - texA, texC - texA);
			if (texNormal.z > 0)
				indices.Add(i);
		}
		return indices.ToArray();
	}

	private void FixWrappedUV(int[] wrapped, List<TriangleIndices> triangleIndices)
	{
		int verticeIndex = geometry.Count - 1;
		Dictionary<int, int> visited = new Dictionary<int, int>();

		foreach (int i in wrapped)
		{
			int a = triangleIndices[i].v1;
			int b = triangleIndices[i].v2;
			int c = triangleIndices[i].v3;
			Vector3 A = geometry[a];
			Vector3 B = geometry[b];
			Vector3 C = geometry[c];
			if (UV[a].x < 0.25f) {
				int tempA = a;
				if (!visited.TryGetValue(a, out tempA)) {
					geometry.Add(A);
					UV.Add (new Vector2 (UV [a].x + 1f, UV [a].y));
					verticeIndex++;
					visited[a] = verticeIndex;
					tempA = verticeIndex;
				}
				a = tempA;
			}
			if (UV[b].x < 0.25f) {
				int tempB = b;
				if (!visited.TryGetValue(b, out tempB)) {
					geometry.Add(B);
					UV.Add (new Vector2 (UV [b].x + 1f, UV [b].y));
					verticeIndex++;
					visited[b] = verticeIndex;
					tempB = verticeIndex;
				}
				b = tempB;
			}
			if (UV[c].x < 0.25f) {
				int tempC = c;
				if (!visited.TryGetValue(c, out tempC)) {
					geometry.Add(C);
					UV.Add (new Vector2 (UV [c].x + 1f, UV [c].y));
					verticeIndex++;
					visited[c] = verticeIndex;
					tempC = verticeIndex;
				}
				c = tempC;
			}
			triangleIndices.Add (new TriangleIndices (a, b, c));
		}
		/*int k;
		for (int i = wrapped.Length-1; i > -1; i--) {
			k = wrapped [i];
			triangleIndices.RemoveAt (k);
		}*/
	}

	private float StepLandscape(float value, int levels) {
		return Mathf.Round (value * (float)(levels-1))/(float)levels;
	}

	/*private void FixSharedPoleVertices(List<TriangleIndices> triangleIndices)
	{
		Vector3 north = geometry.Find (v => v.y == 1);
		Vector3 south = geometry.Find (v => v.y == -1);
		int northIndex = geometry.FindIndex (v => v == north);
		int southIndex = geometry.FindIndex (v => v == south);
		int verticeIndex = geometry.Count - 1;
		int triangleCount = triangleIndices.Count;
		for (int i = 0; i < triangleCount; i++) {
			if (triangleIndices[i].v1 == northIndex) {
				Debug.Log ("north: " + i);
				Vector3 newNorth = north;
				verticeIndex++;
				geometry.Add(newNorth);
				UV.Add (new Vector2 ((UV [triangleIndices [i].v2].x + UV [triangleIndices [i].v3].x) / 2f, 0f));
				triangleIndices[i] = new TriangleIndices(verticeIndex, triangleIndices[i].v2, triangleIndices[i].v3);
			}
			if (triangleIndices [i].v1 == southIndex) {
				Debug.Log ("south: " + i);
				Vector3 newSouth = south;
				verticeIndex++;
				geometry.Add (newSouth);
				UV.Add (new Vector2 ((UV [triangleIndices [i].v2].x + UV [triangleIndices [i].v3].x) / 2f, 0f));
				triangleIndices [i] = new TriangleIndices(verticeIndex, triangleIndices[i].v2, triangleIndices[i].v3);
			}
		}
	}*/



}

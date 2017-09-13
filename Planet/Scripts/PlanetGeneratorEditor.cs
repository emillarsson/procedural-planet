using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects, CustomEditor (typeof (ProceduralPlanet))]
public class PlanetGeneratorEditor : Editor {

	public override void OnInspectorGUI() {
		ProceduralPlanet procPlanet = (ProceduralPlanet)target;

		if (DrawDefaultInspector ()) {
			if (procPlanet.autoUpdate) {
				procPlanet.Generate ();
			}
		}

		if (GUILayout.Button ("Generate")) {
			procPlanet.Generate ();
		}
	}
}
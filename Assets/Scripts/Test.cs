using UnityEngine;

using System;
using System.Collections.Generic;

public class Test : MonoBehaviour {

	private List<Vector2> vectors;

	/// <summary>
	/// Start is called on the frame when a script is enabled just before
	/// any of the Update methods is called the first time.
	/// </summary>
	void Start()
	{
		Vector2[] vs = {
			Vector2.one,
			Vector2.one,
			Vector2.one,
			Vector2.one,
			Vector2.one,
		};

		vectors = new List<Vector2>(vs);

		// int j = 0;
		// vectors.ForEach((Vector2 vector) => {
		// 	Debug.Log(vector);
		// 	Debug.Log(j++);
		// });

		Each((Vector2 vector, int i) => {
			Debug.Log(i + ": " + vector.ToString());
		});
	}

	public void Each(Action<Vector2, int> action) {
		int i = 0;
		vectors.ForEach((Vector2 vector) => {
			action(vector, i++);
		});
	}

}

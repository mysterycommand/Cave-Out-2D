using UnityEngine;

using System;
using System.Collections.Generic;

public class Test : MonoBehaviour {

	private List<Vector2> vectors;
	private Action<int, int> noop = (int row, int col) => {};

	/// <summary>
	/// Start is called on the frame when a script is enabled just before
	/// any of the Update methods is called the first time.
	/// </summary>
	void Start()
	{
		// vectors = new List<Vector2>(new Vector2[] {
		// 	Vector2.one,
		// 	Vector2.one,
		// 	Vector2.one,
		// 	Vector2.one,
		// 	Vector2.one,
		// });

		// int j = 0;
		// vectors.ForEach((Vector2 vector) => {
		// 	Debug.Log(j + ": " + vector.ToString());
		// });

		vectors = new List<Vector2>();

		EachCell(0, 2, 0, 2, (int col, int row) => {
			vectors.Add(new Vector2(col, row));
		});

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

	public void EachCell(
		int fromCol = 0, int toCol = 0,
		int fromRow = 0, int toRow = 0,
		Action<int, int> action = null)
	{
		if (action == null) {
			action = noop;
		}

		for (int col = fromCol; col < toCol; ++col)
		{
			for (int row = fromRow; row < toRow; ++row)
			{
				action(col, row);
			}
		}
	}

}

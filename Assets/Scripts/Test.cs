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

		// vectors = new List<Vector2>();

		// EachCell(0, 2, 0, 2, (int col, int row) => {
		// 	vectors.Add(new Vector2(col, row));
		// });

		// Each((Vector2 vector, int i) => {
		// 	Debug.Log(i + ": " + vector.ToString());
		// });

		MapTest<Vector2> map = new MapTest<Vector2>(16, 9);
		map.EachCell((int x, int y) => {
			map[x, y] = new Vector2(x, y);
		});

		for (int x = 0; x < 16; ++x)
		{
			for (int y = 0; y < 9; ++y)
			{
				Debug.Log(x + ":" + y + " " + map[x, y]);
			}
		}
	}

	public class MapTest<T> where T : new() {
		private List<T> cells = new List<T>();

		private int width;
		private int height;

		public MapTest(int width, int height)
		{
			this.width = width;
			this.height = height;

			EachCell((int x, int y) => {
				cells.Add(new T());
			});
		}

		public T this[int x, int y]
		{
			get
			{
				return cells[x * height + y];
			}

			set
			{
				cells[x * height + y] = value;
			}
		}

		public void EachCell(Action<int, int> action = null)
		{
			if (action == null)
			{
				// if you don't pass in a function it's a noop
				action = (int x, int y) => {};
			}

			for (int x = 0; x < width; ++x)
			{
				for (int y = 0; y < height; ++y)
				{
					action(x, y);
				}
			}
		}
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

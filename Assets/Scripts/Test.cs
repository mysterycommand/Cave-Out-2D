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

		MapTest<int> intMap = new MapTest<int>(16, 9);

		MapTest<Vector2> vectorMap = new MapTest<Vector2>(16, 9);
		vectorMap.EachCell((int x, int y) => {
			vectorMap[x, y] = new Vector2(x, y);
		});

		for (int x = 0; x < 16; ++x)
		{
			for (int y = 0; y < 9; ++y)
			{
				Debug.Log(x + ":" + y + " " + vectorMap[x, y]);
			}
		}
	}

	public class MapTest<T> where T : new() {
		private List<T> cells = new List<T>();

		public int width { get; private set; }
		public int height { get; private set; }

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

		// public static MapTest<T> operator +(MapTest<T> a, MapTest<T> b) {
		// 	if (a.width != b.width || a.height != b.height) {
		// 		string msg = "Cannot add two maps of different sizes. Attempting to add " + a.ToString() + " and " + b.ToString() + ".";
		// 		throw new InvalidOperationException(msg);
		// 	}

		// 	MapTest<T> map = new MapTest<T>(a.width, a.height);
		// 	map.EachCell((int x, int y) => {
		// 		T axy = a[x, y];
		// 		T bxy = b[x, y];
		// 		map[x, y] = axy + bxy;
		// 	});

		// 	return map;
		// }

		// override public string ToString()
		// {
		// 	return "MapTest<" + typeof(T) + ">(" + width + "," + height + ")";
		// }

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

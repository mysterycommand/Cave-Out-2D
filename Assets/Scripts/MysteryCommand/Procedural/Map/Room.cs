using System;
using System.Collections.Generic;

using UnityEngine;

namespace MysteryCommand.Procedural.Map
{
	public class Room : IComparable<Room>
	{
		public List<Vector2> tiles;
		public List<Vector2> edgeTiles;
		public List<Room> connectedRooms;
		public int roomSize;
		public bool isAccessibleFromMainRoom;
		public bool isMainRoom;

		public Room() {}

		public Room(List<Vector2> roomTiles, Texture2D map)
		{
			tiles = roomTiles;
			roomSize = tiles.Count;
			connectedRooms = new List<Room>();

			edgeTiles = new List<Vector2>();
			foreach (Vector2 tile in tiles)
			{
				for (int x = (int) tile.x - 1; x <= tile.x + 1; x++)
				{
					for (int y = (int) tile.y - 1; y <= tile.y + 1; y++)
					{
						if (x == tile.x || y == tile.y)
						{
							if (map.GetPixel(x, y) == Color.white)
							{
								edgeTiles.Add(tile);
							}
						}
					}
				}
			}
		}

		public void SetAccessibleFromMainRoom()
		{
			if (!isAccessibleFromMainRoom)
			{
				isAccessibleFromMainRoom = true;
				foreach (Room connectedRoom in connectedRooms)
				{
					connectedRoom.SetAccessibleFromMainRoom();
				}
			}
		}

		public static void ConnectRooms(Room roomA, Room roomB)
		{
			if (roomA.isAccessibleFromMainRoom)
			{
				roomB.SetAccessibleFromMainRoom ();
			}
			else if (roomB.isAccessibleFromMainRoom)
			{
				roomA.SetAccessibleFromMainRoom();
			}

			roomA.connectedRooms.Add (roomB);
			roomB.connectedRooms.Add (roomA);
		}

		public bool IsConnected(Room otherRoom)
		{
			return connectedRooms.Contains(otherRoom);
		}

		public int CompareTo(Room otherRoom)
		{
			return otherRoom.roomSize.CompareTo (roomSize);
		}
	}
}

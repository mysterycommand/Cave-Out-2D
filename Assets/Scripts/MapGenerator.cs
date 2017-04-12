using System;
using System.Collections.Generic;

using UnityEngine;

using MysteryCommand.Procedural.Map;

public class MapGenerator : MonoBehaviour
{

    public int width;
    public int height;

    public string seed;
    public bool useRandomSeed;

    [Range(0,100)]
    public int randomFillPercent;

    Texture2D map;

	private Action<int, int> noop = (int row, int col) => {};

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        map = new Texture2D(width, height);
        RandomFillMap();

        for (int i = 0; i < 5; ++i)
        {
            SmoothMap();
        }

        ProcessMap();

        int borderSize = 1;
        Texture2D borderedMap = new Texture2D(
            width + borderSize * 2,
            height + borderSize * 2
        );

        int tx = borderedMap.width,
            ty = borderedMap.height;

        EachCell(0, tx, 0, ty, (int col, int row) => {
            bool isMapCol = col >= borderSize && col < width + borderSize;
            bool isMapRow = row >= borderSize && row < height + borderSize;

            borderedMap.SetPixel(col, row, (isMapCol && isMapRow) ?
                map.GetPixel(col - borderSize, row - borderSize) :
                Color.white);
        });

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
    }

    void ProcessMap()
    {
        List<List<Vector2>> wallRegions = GetRegions(Color.white);
        int wallThresholdSize = 50;

        foreach (List<Vector2> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Vector2 tile in wallRegion)
                {
                    map.SetPixel((int) tile.x, (int) tile.y, Color.black);
                }
            }
        }

        List<List<Vector2>> roomRegions = GetRegions(Color.black);
        int roomThresholdSize = 50;
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Vector2> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Vector2 tile in roomRegion)
                {
                    map.SetPixel((int) tile.x, (int) tile.y, Color.white);
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }

        survivingRooms.Sort();
        survivingRooms[0].isMainRoom = true;
        survivingRooms[0].isAccessibleFromMainRoom = true;

        ConnectClosestRooms (survivingRooms);
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAccessibilityFromMainRoom)
        {
            foreach (Room room in allRooms)
            {
                if (room.isAccessibleFromMainRoom)
                {
                    roomListB.Add (room);
                }
                else
                {
                    roomListA.Add (room);
                }
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Vector2 bestTileA = new Vector2();
        Vector2 bestTileB = new Vector2();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                {
                    continue;
                }
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB)) continue;

                int tx = roomA.edgeTiles.Count,
                    ty = roomB.edgeTiles.Count;

                EachCell(0, tx, 0, ty, (int col, int row) => {
                    Vector2 tileA = roomA.edgeTiles[col];
                    Vector2 tileB = roomB.edgeTiles[row];

                    int distanceBetweenRooms = (int) (Mathf.Pow(tileA.x - tileB.x, 2) + Mathf.Pow(tileA.y - tileB.y, 2));

                    if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                    {
                        bestDistance = distanceBetweenRooms;
                        possibleConnectionFound = true;
                        bestTileA = tileA;
                        bestTileB = tileB;
                        bestRoomA = roomA;
                        bestRoomB = roomB;
                    }
                });
            }

            if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (!forceAccessibilityFromMainRoom)
        {
            ConnectClosestRooms(allRooms, true);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Vector2 tileA, Vector2 tileB)
    {
        Room.ConnectRooms (roomA, roomB);
        List<Vector2> line = GetLine (tileA, tileB);
        foreach (Vector2 c in line)
        {
            DrawCircle(c, 5);
        }
    }

    void DrawCircle(Vector2 c, int r)
    {
        int f = -r,
            t = r + 1;

        EachCell(f, t, f, t, (int col, int row) => {
            if (col * col + row * row > r * r)  return;

            int drawX = (int) c.x + col;
            int drawY = (int) c.y + row;

            if (IsInMapRange(drawX, drawY)) {
                map.SetPixel(drawX, drawY, Color.black);
            }
        });
    }

    List<Vector2> GetLine(Vector2 from, Vector2 to)
    {
        List<Vector2> line = new List<Vector2>();

        int x = (int) from.x;
        int y = (int) from.y;

        int dx = (int) to.x - (int) from.x;
        int dy = (int) to.y - (int) from.y;

        bool inverted = false;
        int step = Math.Sign (dx);
        int gradientStep = Math.Sign (dy);

        int longest = Mathf.Abs (dx);
        int shortest = Mathf.Abs (dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign (dy);
            gradientStep = Math.Sign (dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; ++i)
        {
            line.Add(new Vector2(x,y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }

        return line;
    }

    Vector3 Vector2ToWorldPoint(Vector2 tile)
    {
        float x = -width / 2 + 0.5f + tile.x,
            y = -height / 2 + .5f + tile.y;

        return new Vector3(x, 2, y);
    }

    List<List<Vector2>> GetRegions(Color tileType)
    {
        List<List<Vector2>> regions = new List<List<Vector2>>();
        int[,] mapFlags = new int[width,height];

        EachCell(0, width, 0, height, (int col, int row) => {
            if (!(mapFlags[col, row] == 0 && map.GetPixel(col, row) == tileType)) return;

            List<Vector2> newRegion = GetRegionTiles(col, row);
            regions.Add(newRegion);

            foreach (Vector2 tile in newRegion)
            {
                mapFlags[(int) tile.x, (int) tile.y] = 1;
            }
        });

        return regions;
    }

    List<Vector2> GetRegionTiles(int startX, int startY)
    {
        List<Vector2> tiles = new List<Vector2>();
        int[,] mapFlags = new int[width,height];
        Color tileType = map.GetPixel(startX, startY);

        Queue<Vector2> queue = new Queue<Vector2>();
        queue.Enqueue (new Vector2(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Vector2 tile = queue.Dequeue();
            tiles.Add(tile);

            int fx = (int) tile.x - 1,
                tx = (int) tile.x + 2,
                fy = (int) tile.y - 1,
                ty = (int) tile.y + 2;

            EachCell(fx, tx, fy, ty, (int col, int row) => {
                if (!IsInMapRange(col, row)) return;
                if (!(row == tile.y || col == tile.x)) return;
                if (!(mapFlags[col, row] == 0 && map.GetPixel(col, row) == tileType)) return;

                mapFlags[col, row] = 1;
                queue.Enqueue(new Vector2(col, row));
            });
        }

        return tiles;
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        System.Random rando = new System.Random(seed.GetHashCode());

        EachCell(0, width, 0, height, (int col, int row) => {
            bool isWall = rando.Next(0, 100) < randomFillPercent;
            bool isEdge = col == 0 || col == width - 1 || row == 0 || row == height - 1;
            map.SetPixel(col, row, (isWall || isEdge) ? Color.white : Color.black);
        });
    }

    void SmoothMap()
    {
        EachCell(0, width, 0, height, (int col, int row) => {
            int surroundingWallCount = GetSurroundingWallCount(col, row);

            if (surroundingWallCount > 4) map.SetPixel(col, row, Color.white);
            else if (surroundingWallCount < 4) map.SetPixel(col, row, Color.black);
        });
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;

        int fx = gridX - 1,
            tx = gridX + 2,
            fy = gridY - 1,
            ty = gridY + 2;

        EachCell(fx, tx, fy, ty, (int col, int row) => {
            if (IsInMapRange(col, row))
            {
                if (col != gridX || row != gridY)
                {
                    wallCount += map.GetPixel(col, row) == Color.white ? 1 : 0;
                }
            }
            else
            {
                wallCount++;
            }
        });

        return wallCount;
    }

	public void EachCell(
		int fromCol = 0, int toCol = 0,
		int fromRow = 0, int toRow = 0,
		Action<int, int> action = null)
	{
		if (action == null)
        {
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
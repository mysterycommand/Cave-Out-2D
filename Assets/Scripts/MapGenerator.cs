﻿using System;
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

    int[,] map;

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
        map = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < 5; ++i)
        {
            SmoothMap();
        }

        ProcessMap();

        int borderSize = 1;
        int[,] borderedMap = new int[
            width + borderSize * 2,
            height + borderSize * 2
        ];

        int tx = borderedMap.GetLength(0),
            ty = borderedMap.GetLength(1);

        EachCell(0, tx, 0, ty, (int col, int row) => {
            bool isColBorder = col >= borderSize && col < width + borderSize;
            bool isRowBorder = row >= borderSize && row < height + borderSize;

            borderedMap[col, row] = (isColBorder && isRowBorder) ?
                map[col - borderSize, row - borderSize] :
                1;
        });

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 1);
    }

    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);
        int wallThresholdSize = 50;

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallThresholdSize)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions (0);
        int roomThresholdSize = 50;
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;
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
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
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
                    Coord tileA = roomA.edgeTiles[col];
                    Coord tileB = roomB.edgeTiles[row];

                    int distanceBetweenRooms = (int) (Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

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

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms (roomA, roomB);
        List<Coord> line = GetLine (tileA, tileB);
        foreach (Coord c in line)
        {
            DrawCircle(c, 5);
        }
    }

    void DrawCircle(Coord c, int r)
    {
        int f = -r,
            t = r + 1;

        EachCell(f, t, f, t, (int col, int row) => {
            if (col * col + row * row > r * r)  return;

            int drawX = c.tileX + col;
            int drawY = c.tileY + row;

            if (IsInMapRange(drawX, drawY)) {
                map[drawX,drawY] = 0;
            }
        });
    }

    List<Coord> GetLine(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

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
            line.Add(new Coord(x,y));

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

    Vector3 CoordToWorldPoint(Coord tile)
    {
        float x = -width / 2 + 0.5f + tile.tileX,
            y = -height / 2 + .5f + tile.tileY;

        return new Vector3(x, 2, y);
    }

    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width,height];

        EachCell(0, width, 0, height, (int col, int row) => {
            if (!(mapFlags[col, row] == 0 && map[col, row] == tileType)) return;

            List<Coord> newRegion = GetRegionTiles(col, row);
            regions.Add(newRegion);

            foreach (Coord tile in newRegion)
            {
                mapFlags[tile.tileX, tile.tileY] = 1;
            }
        });

        return regions;
    }

    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width,height];
        int tileType = map [startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue (new Coord (startX, startY));
        mapFlags [startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            int fx = tile.tileX - 1,
                tx = tile.tileX + 2,
                fy = tile.tileY - 1,
                ty = tile.tileY + 2;

            EachCell(fx, tx, fy, ty, (int col, int row) => {
                if (!IsInMapRange(col, row)) return;
                if (!(row == tile.tileY || col == tile.tileX)) return;
                if (!(mapFlags[col, row] == 0 && map[col, row] == tileType)) return;

                mapFlags[col, row] = 1;
                queue.Enqueue(new Coord(col, row));
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
            map[col, row] = (isWall || isEdge) ? 1 : 0;
        });
    }

    void SmoothMap()
    {
        EachCell(0, width, 0, height, (int col, int row) => {
            int surroundingWallCount = GetSurroundingWallCount(col, row);

            if (surroundingWallCount > 4) map[col, row] = 1;
            else if (surroundingWallCount < 4) map[col, row] = 0;
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
                    wallCount += map[col, row];
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
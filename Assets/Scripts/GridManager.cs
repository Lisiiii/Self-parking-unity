/*  GridManager.cs
*   栅格地图 生成和更新
*   2024/12/29  by Lisiyao
*/
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{

    public int[,] gridMap; // 栅格地图  0-5:未知  >5:障碍物  -1:目标点  -2:A*路径点  -3:车辆本体
    private float[,] occupancyGrid; // 栅格地图的占据概率
    public Vector2Int start;
    public Vector2Int end;
    public int scanDistance = 20;

    private GameObject Car;
    private int GridWidth = 20;
    private int GridHeight = 20;
    private GameObject gridContainer;
    private List<List<Image>> gridCellImages;
    private GameObject gridCellPrefab;
    private Vector3 TargetPosition;

    public void InitializeGrid(int scanDistance, GameObject Car, int GridWidth, int GridHeight, GameObject gridContainer, GameObject gridCellPrefab, Vector3 TargetPosition)
    {
        this.scanDistance = scanDistance;
        this.Car = Car;
        this.GridWidth = GridWidth;
        this.GridHeight = GridHeight;
        this.gridContainer = gridContainer;
        this.gridCellPrefab = gridCellPrefab;
        this.TargetPosition = TargetPosition;

        occupancyGrid = new float[GridWidth, GridHeight];

        // 初始化概率为未知状态 (P = 0.5, L = 0)
        for (int i = 0; i < GridWidth; i++)
        {
            for (int j = 0; j < GridHeight; j++)
            {
                occupancyGrid[i, j] = 0.1f;
            }
        }

        gridMap = new int[GridWidth, GridHeight];
        Array.Clear(gridMap, 0, gridMap.Length);

        if (gridContainer.transform.childCount != 0)
            DestroyGridUI();
        CreateGridUI();
    }

    public void UpdateGridMap(Vector3 hitPoint)
    {
        Vector2Int hitGrid = WorldToGrid(hitPoint.x, hitPoint.z);
        Vector2Int targetGrid = WorldToGrid(TargetPosition.x, TargetPosition.z);

        start = new Vector2Int(GridWidth / 2, GridHeight / 2);
        end = targetGrid;

        List<Vector2Int> line = GetLine(start, hitGrid);
        float p;
        foreach (Vector2Int grid in line)
        {
            p = Mathf.Clamp(occupancyGrid[grid.x, grid.y], 0.1f, 0.9f);
            occupancyGrid[grid.x, grid.y] = UpdateFreeProbability(p);
        }
        p = Mathf.Clamp(occupancyGrid[hitGrid.x, hitGrid.y], 0.1f, 0.9f);
        occupancyGrid[hitGrid.x, hitGrid.y] = UpdateOccupiedProbability(p);

        // 占据概率转换为栅格地图
        for (int i = 0; i < GridWidth; i++)
        {
            for (int j = 0; j < GridHeight; j++)
            {
                gridMap[i, j] = Mathf.FloorToInt(occupancyGrid[i, j] * 45);
            }
        }

        // 形态学闭操作，填充不连续的障碍物
        CloseBarrier();

        // 画目标点和车辆本体
        for (int x = start.x - 1; x <= start.x + 1; x++)
        {
            for (int y = start.y - 2; y <= start.y + 2; y++)
            {
                gridMap[x, y] = -3;
            }
        }
        gridMap[end.x, end.y] = -1;
    }

    private void CloseBarrier()
    {
        int[,] structuringElement = {
        { 1, 1, 1 },
        { 1, 1, 1 },
        { 1, 1, 1 }
    };

        int[,] dilatedMap = Dilate(gridMap, structuringElement);
        gridMap = Erode(dilatedMap, structuringElement);

        // 再膨胀一次防止碰撞障碍物
        gridMap = Dilate(gridMap, structuringElement);
    }

    // 膨胀
    private int[,] Dilate(int[,] map, int[,] structuringElement)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        int[,] result = new int[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (map[i, j] > 5)
                {
                    for (int k = 0; k < structuringElement.GetLength(0); k++)
                    {
                        for (int l = 0; l < structuringElement.GetLength(1); l++)
                        {
                            int x = i + k - 1;
                            int y = j + l - 1;
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                result[x, y] = 6;
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // 腐蚀
    private int[,] Erode(int[,] map, int[,] structuringElement)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        int[,] result = new int[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                bool erode = true;
                for (int k = 0; k < structuringElement.GetLength(0); k++)
                {
                    for (int l = 0; l < structuringElement.GetLength(1); l++)
                    {
                        int x = i + k - 1;
                        int y = j + l - 1;
                        if (x < 0 || x >= width || y < 0 || y >= height || map[x, y] < 5)
                        {
                            erode = false;
                            break;
                        }
                    }
                    if (!erode) break;
                }
                if (erode)
                {
                    result[i, j] = 6;
                }
            }
        }

        return result;
    }

    // 更新观测到自由的概率
    private float UpdateFreeProbability(float prior)
    {
        float pFreeGivenObs = 0.3f;
        return pFreeGivenObs * prior /
               (pFreeGivenObs * prior + (1 - pFreeGivenObs) * (1 - prior));
    }

    // 更新观测到占据的概率
    private float UpdateOccupiedProbability(float prior)
    {
        float pOccGivenObs = 0.7f;
        return pOccGivenObs * prior /
               (pOccGivenObs * prior + (1 - pOccGivenObs) * (1 - prior));
    }

    // 世界坐标转栅格坐标
    private Vector2Int WorldToGrid(float x, float y)
    {
        Vector3 localPosition = Car.transform.InverseTransformPoint(new Vector3(x, 0, y)) / scanDistance * (GridWidth / 2);
        int gridX = Mathf.Clamp(Mathf.FloorToInt(localPosition.x + gridMap.GetLength(0) / 2), 0, gridMap.GetLength(0) - 1);
        int gridY = GridHeight - 1 - Mathf.Clamp(Mathf.FloorToInt(localPosition.z + gridMap.GetLength(1) / 2), 0, gridMap.GetLength(1) - 1);
        return new Vector2Int(gridX, gridY);
    }

    // Bresenham算法获取直线上的栅格
    private List<Vector2Int> GetLine(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> line = new List<Vector2Int>();
        int x = start.x;
        int y = start.y;
        int dx = Math.Abs(end.x - start.x);
        int dy = Math.Abs(end.y - start.y);
        int sx = start.x < end.x ? 1 : -1;
        int sy = start.y < end.y ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            if (x < 0 || x >= gridMap.GetLength(0) || y < 0 || y >= gridMap.GetLength(1))
                break;
            line.Add(new Vector2Int(x, y));

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
        return line;
    }
    private void CreateGridUI()
    {
        gridCellImages = new List<List<Image>>();
        for (float x = 0; x < gridMap.GetLength(0); x++)
        {
            gridCellImages.Add(new List<Image>());
            for (float y = 0; y < gridMap.GetLength(1); y++)
            {
                float cellSize = 20f / (float)GridWidth;
                GameObject cell = Instantiate(gridCellPrefab, gridContainer.transform);

                cell.transform.localPosition = new Vector3(x * cellSize, y * cellSize, gridContainer.transform.position.z);
                Image cellImage = cell.transform.GetComponent<Image>();
                cellImage.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                cell.name = $"Cell_{x}_{y}";

                gridCellImages[(int)x].Add(cellImage);
            }
        }
    }
    public void UpdateGridUI()
    {
        for (int x = 0; x < gridMap.GetLength(0); x++)
        {
            for (int y = 0; y < gridMap.GetLength(1); y++)
            {
                Image cellImage = gridCellImages[x][y];
                if (gridMap[x, y] > 5)
                    cellImage.color = Color.red;
                else if (gridMap[x, y] == -1)
                    cellImage.color = Color.blue;
                else if (gridMap[x, y] == -2)
                    cellImage.color = Color.green;
                else if (gridMap[x, y] == -3)
                    cellImage.color = Color.yellow;
                else
                    cellImage.color = Color.white * (1 - gridMap[x, y] / 5f);

            }
        }
    }

    // 析构
    private void DestroyGridUI()
    {
        foreach (Transform child in gridContainer.transform)
        {
            Destroy(child.gameObject);
        }
        gridCellImages.Clear();
    }

}

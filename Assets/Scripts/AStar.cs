/*  AStar.cs
*   A* 寻路算法
*   2024/12/29  by Lisiyao
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
public class AStar : MonoBehaviour
{
    private int[,] gridMap = new int[20, 20];
    private Dictionary<Vector2Int, Node> openList = new Dictionary<Vector2Int, Node>();
    private Dictionary<Vector2Int, Node> closeList = new Dictionary<Vector2Int, Node>();
    private Node startNode;
    private Node endNode;
    private List<Vector2Int> path = new List<Vector2Int>();
    struct Node
    {
        public Vector2Int position;
        public Vector2Int parent;
        public int g;
        public int h;
        public int f;
        public Node(Vector2Int position, Vector2Int parent, int g, int h)
        {
            this.position = position;
            this.parent = parent;
            this.g = g;
            this.h = h;
            this.f = g + h;
        }
    }
    public void updateGrid(int[,] gridMap)
    {
        this.gridMap = gridMap;
    }

    public int[,] getGridMap(Vector2Int start, Vector2Int end)
    {
        path = findPath(start, end);
        foreach (Vector2Int position in path)
        {
            if (position != end)
                gridMap[position.x, position.y] = -2;
        }
        return gridMap;
    }
    private List<Vector2Int> findPath(Vector2Int start, Vector2Int end)
    {
        int cycleCount = 0;
        openList.Clear();
        closeList.Clear();

        startNode = new Node(start, start, 0, getDistance(start, end));
        endNode = new Node(end, end, 0, 0);
        openList.Add(start, startNode);
        while (openList.Count > 0)
        {
            if (cycleCount > 100)
            {
                return path;
            }

            Node currentNode = openList.First().Value;
            foreach (var node in openList)
            {
                if (node.Value.f < currentNode.f || node.Value.f == currentNode.f && node.Value.h < currentNode.h)
                {
                    currentNode = node.Value;
                }
            }
            openList.Remove(currentNode.position);
            closeList.Add(currentNode.position, currentNode);
            if (currentNode.position == endNode.position)
            {
                return generatePath(currentNode);
            }
            List<Node> neighbors = getNeighbors(currentNode);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Node neighbor = neighbors[i];
                if (closeList.ContainsKey(neighbor.position))
                {
                    continue;
                }
                int newG = currentNode.g + getDistance(currentNode.position, neighbor.position);
                if (newG < neighbor.g || !openList.ContainsKey(neighbor.position))
                {
                    neighbor.g = newG;
                    neighbor.h = getDistance(neighbor.position, endNode.position);
                    neighbor.f = neighbor.g + neighbor.h;
                    neighbor.parent = currentNode.position;
                    if (!openList.ContainsKey(neighbor.position))
                    {
                        openList.Add(neighbor.position, neighbor);
                    }
                }
            }
            cycleCount++;
        }
        return new List<Vector2Int>();
    }

    private List<Vector2Int> generatePath(Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;
        while (currentNode.position != startNode.position)
        {
            path.Add(currentNode.position);
            currentNode = closeList[currentNode.parent];
        }
        path.Add(startNode.position);
        path.Reverse();
        return path;
    }

    private List<Node> getNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        Vector2Int[] directions = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };
        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighborPosition = node.position + direction;
            if (neighborPosition.x < 0 || neighborPosition.x >= gridMap.GetLength(0) || neighborPosition.y < 0 || neighborPosition.y >= gridMap.GetLength(1) || gridMap[neighborPosition.x, neighborPosition.y] >= 5)
            {
                continue;
            }
            neighbors.Add(new Node(neighborPosition, node.position, 0, 0));
        }
        return neighbors;
    }

    private int getDistance(Vector2Int point1, Vector2Int point2)
    {
        float dx = Mathf.Abs(point1.x - point2.x);
        float dy = Mathf.Abs(point1.y - point2.y);
        return (int)(10 * Mathf.Sqrt(dx * dx + dy * dy));
    }
}


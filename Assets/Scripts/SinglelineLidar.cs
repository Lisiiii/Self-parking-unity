/*  SinglelineLidar.cs 
*  单线激光雷达仿真
*   2024/12/27  by Lisiyao
*/
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SinglelineLidar : MonoBehaviour
{
    [Tooltip("激光雷达对象")]
    public GameObject Lidar;
    [Range(1f, 2000f)]
    [Tooltip("设置扫描速度：这个速度量最后会乘上Time.deltaTime成为雷达的每帧旋转角度")]
    public float scanSpeed = 1;
    [Range(0, 60)]
    [Tooltip("设置扫描距离")]
    public int scanDistance = 20;
    [Tooltip("车辆对象")]
    public GameObject Car;
    [Tooltip("停车位对象")]
    public GameObject target;
    [Tooltip("栅格容器")]
    public GameObject gridContainer;
    [Tooltip("栅格单元预制体")]
    public GameObject gridCellPrefab;
    [Range(1, 20)]
    [Tooltip("360°内的扫描线数量。由于Unity的单位时间限制，单线雷达无法达到想要的精度,所以设置n线雷达以模拟nHZ的单线雷达。")]
    public int scanLineCount = 1;
    [Range(1, 5)]
    [Tooltip("设置分辨率，代表每米的栅格数。")]
    public int GridDefinition = 1;
    private int lastDefinition = 1;
    private AStar aStar;
    private int GridWidth = 20;
    private int GridHeight = 20;
    private GridManager gridManager;
    private Vector3 TargetPosition;
    private List<Vector3> scanDirections;

    // Start is called before the first frame update
    void Start()
    {
        TargetPosition = new Vector3(target.transform.position.x, 0, target.transform.position.z);
        GridWidth = GridDefinition * 20;
        GridHeight = GridWidth;
        gridManager = new GridManager();
        gridManager.InitializeGrid(scanDistance, Car, GridWidth, GridHeight, gridContainer, gridCellPrefab, TargetPosition);
        aStar = new AStar();
    }

    public void SetGridDefinition(int definition)
    {
        GridDefinition = definition;
    }
    public void SetScanSpeed(float Speed)
    {
        scanSpeed = Speed;
    }
    public int[,] getGridMap()
    {
        return gridManager.gridMap;
    }
    // Update is called once per frame
    void Update()
    {
        Scan();
    }

    private void Scan()
    {
        List<Vector3> scanDirections = new List<Vector3>();
        for (int i = 0; i < scanLineCount; i++)
        {
            if (scanLineCount == 1)
                scanDirections.Add(transform.forward);
            else
                scanDirections.Add(Quaternion.Euler(0, -360 + 360 / (scanLineCount - 1) * i, 0) * transform.forward);
        }
        for (int i = 0; i < scanLineCount; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, scanDirections[i], out hit) && GetDistance(hit.point, transform.position) < scanDistance)
            {
                Debug.DrawLine(transform.position, hit.point, Color.white);
                UpdateGrid(hit.point);
            }
            else
            {
                Debug.DrawLine(transform.position, transform.position + scanDirections[i] * scanDistance, Color.white);
                UpdateGrid(transform.position + scanDirections[i] * scanDistance * 10);
            }
        }
        Lidar.transform.RotateAround(transform.position, transform.up, Time.deltaTime * scanSpeed);
    }

    // 更新栅格地图
    private void UpdateGrid(Vector3 hitPoint)
    {
        gridManager.scanDistance = scanDistance;
        gridManager.UpdateGridMap(hitPoint);

        aStar.updateGrid(gridManager.gridMap);
        gridManager.gridMap = aStar.getGridMap(gridManager.start, gridManager.end);

        gridManager.UpdateGridUI();

        Vector3 hitUp = new Vector3(hitPoint.x, hitPoint.y + 0.2f, hitPoint.z);
        Debug.DrawLine(hitPoint, hitUp, Color.green, 0.5f);

        if (lastDefinition != GridDefinition)
        {
            lastDefinition = GridDefinition;
            GridWidth = GridDefinition * 20;
            GridHeight = GridWidth;
            gridManager.InitializeGrid(scanDistance, Car, GridWidth, GridHeight, gridContainer, gridCellPrefab, TargetPosition);
        }

    }
    private float GetDistance(Vector3 point1, Vector3 point2)
    {
        return Mathf.Sqrt(Mathf.Pow(point1.x - point2.x, 2) + Mathf.Pow(point1.y - point2.y, 2) + Mathf.Pow(point1.z - point2.z, 2));
    }

}

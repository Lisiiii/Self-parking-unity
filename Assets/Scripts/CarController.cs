/*  CarController.cs
*   车辆控制器
*   2024/12/27  by Lisiyao
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public GameObject Car;
    public SinglelineLidar singlelineLidar;
    public WheelCollider leftFrontWheel;
    public WheelCollider rightFrontWheel;
    public WheelCollider leftRearWheel;
    public WheelCollider rightRearWheel;
    private float maxSteerAngle = 20;
    private float motorForce = 1000;
    private int[,] gridMap;
    private Vector2Int start;

    struct Move
    {
        public int direction;
        public int motor;
    }
    private Move moveNext;

    // Start is called before the first frame update
    void Start()
    {
        leftFrontWheel = Car.transform.GetChild(0).Find("LeftFront").GetComponent<WheelCollider>();
        rightFrontWheel = Car.transform.GetChild(0).Find("RightFront").GetComponent<WheelCollider>();
        leftRearWheel = Car.transform.GetChild(0).Find("LeftBack").GetComponent<WheelCollider>();
        rightRearWheel = Car.transform.GetChild(0).Find("RightBack").GetComponent<WheelCollider>();
        moveNext = new Move { direction = 0, motor = 0 };
    }

    public void updateGridMap(int[,] outGridMap)
    {
        gridMap = outGridMap;
        start = new Vector2Int(gridMap.GetLength(0) / 2, gridMap.GetLength(1) / 2);
    }


    private void FixedUpdate()
    {
        resetSpeed();
        if (Input.GetMouseButtonUp(0))
        {
            //get the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                //move the game object to the mouse position
                Car.transform.position = new Vector3(hit.point.x, hit.point.y + 0.5f, hit.point.z);
            }
        }

        if (Input.GetKey(KeyCode.W))
        {
            accelerate(1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            accelerate(-1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            steer(-1);
        }
        if (Input.GetKey(KeyCode.D))
        {
            steer(1);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            brake();
        }

        updateGridMap(singlelineLidar.getGridMap());
        if (gridMap != null)
        {
            moveNext.direction = 0;
            moveNext.motor = 0;
            int width = gridMap.GetLength(0);
            int height = gridMap.GetLength(1);
            int carX = width / 2;
            int carY = height / 2;

            checkArrive(carX, carY);

            int searchRange = 3;
            // 搜索路径并决定下一步
            bool ifFound = decideMove(searchRange, carX, carY);
            while (!ifFound)
            {
                searchRange -= 1;
                ifFound = searchRange < 1 ? true : decideMove(searchRange, carX, carY);
            }
        }

        if (moveNext.direction != 0)
        {
            steer(moveNext.direction);
        }
        if (moveNext.motor != 0)
        {
            accelerate(moveNext.motor);
        }
        else
        {
            brake();
        }

    }
    bool checkArrive(int carX, int carY)
    {
        for (int i = carX - 1; i <= carX + 1; i++)
        {
            for (int j = carY - 1; j <= carY + 1; j++)
            {
                if (gridMap[i, j] == -1)
                {
                    // 目标在范围内，停车
                    moveNext.direction = 0;
                    moveNext.motor = 0;
                    return true;
                }
            }
        }
        return false;
    }

    bool decideMove(int searchRange, int carX, int carY)
    {
        bool ifFound = false;
        for (int i = carX - searchRange; i <= carX + searchRange; i++)
        {
            for (int j = carY - searchRange; j <= carY + searchRange;)
            {
                if (gridMap[i, j] == -2)
                {
                    ifFound = true;
                    // 根据位置设置方向和电机
                    if (i < carX)
                    {
                        moveNext.direction = -1; // 左转
                    }
                    else if (i > carX)
                    {
                        moveNext.direction = 1; // 右转
                    }
                    else
                    {
                        moveNext.direction = 0; // 直行
                    }

                    moveNext.direction = moveNext.direction * Mathf.Abs(i - carX);

                    if (j < carY)
                    {
                        moveNext.motor = 1; // 倒车
                    }
                    else if (j > carY)
                    {
                        moveNext.motor = -1; // 前进
                    }
                }
                if (i == carX - 3 || i == carX + 3)
                {
                    j++;
                }
                else
                {
                    j += searchRange * 2;
                }
            }
        }
        return ifFound;
    }

    void brake()
    {
        leftFrontWheel.brakeTorque = motorForce;
        rightFrontWheel.brakeTorque = motorForce;
        leftRearWheel.brakeTorque = motorForce;
        rightRearWheel.brakeTorque = motorForce;
    }

    void accelerate(int direction)
    {
        if (Mathf.Abs(leftFrontWheel.rpm) < 30 && Mathf.Abs(rightFrontWheel.rpm) < 30)
        {
            leftFrontWheel.motorTorque = motorForce * direction;
            rightFrontWheel.motorTorque = motorForce * direction;
            leftRearWheel.motorTorque = motorForce * direction;
            rightRearWheel.motorTorque = motorForce * direction;
        }
    }

    void steer(int direction)
    {
        leftFrontWheel.steerAngle = maxSteerAngle * direction;
        rightFrontWheel.steerAngle = maxSteerAngle * direction;
    }

    void resetSpeed()
    {
        leftFrontWheel.brakeTorque = 0;
        rightFrontWheel.brakeTorque = 0;
        leftRearWheel.brakeTorque = 0;
        rightRearWheel.brakeTorque = 0;

        leftFrontWheel.steerAngle = 0;
        rightFrontWheel.steerAngle = 0;
        leftFrontWheel.motorTorque = 0;
        rightFrontWheel.motorTorque = 0;
        leftRearWheel.motorTorque = 0;
        rightRearWheel.motorTorque = 0;
    }
}

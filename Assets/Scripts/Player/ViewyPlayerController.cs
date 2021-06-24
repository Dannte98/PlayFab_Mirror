using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;
public class ViewyPlayerController : MonoBehaviour
{
    [SerializeField] NetworkIdentity identity = null;
    [SerializeField] private float speed = 2f;
    private float xAxis;
    private float yAxis;
    private float zAxis;

    // Update is called once per frame
    void Update()
    {
        if (identity.hasAuthority)
        {
            GetInput();
            Move();
        }
        
    }

    private void GetInput()
    {
        xAxis = Input.GetAxis("Horizontal");
        zAxis = Input.GetAxis("Vertical");
        if (Input.GetKey(KeyCode.Space))
        {
            yAxis = 1;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            yAxis = -1;
        }
    }

    private void Move()
    {
        transform.position += new Vector3(xAxis,yAxis,zAxis)*speed*Time.deltaTime;
    }
}

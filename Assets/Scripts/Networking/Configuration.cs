using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Configuration : MonoBehaviour
{
    [Header("Build Type",order =0)]
    //Indicates if its a server build or client build; Build ID (Client build only)
    public BuildType buildType = BuildType.REMOTE_SERVER;
    public string buildID = null;

    [Header("Server properties",order =1)]
    //Build ID, IP address and port to connect to
    public string ipAddress = null;
    public ushort port = 0;

    public enum BuildType
    {
        REMOTE_SERVER,
        REMOTE_CLIENT,
        LOCAL_SERVER
    }
}

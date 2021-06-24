using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewyNetworkManager : NetworkManager
{
    [Header("Viewy Add-Ons",order=5)]
    //Reference the configuration file
    [SerializeField] Configuration configuration = null;

    //Reference the PlayFab manager
    [SerializeField] private ViewyPlayFabManager playFabManager=null;

    //Reference the Login Game Object
    [SerializeField] private GameObject loginCanvas = null;

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        //Deactivate the login canvas
        loginCanvas.SetActive(false);
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        base.OnServerAddPlayer(conn);

        //Get the connection's identity
        ViewyNetworkPlayer player = conn.identity.GetComponent<ViewyNetworkPlayer>();

        player.RpcSetMyName();

        Color newColor = new Color(
            Random.Range(0, 1f),
            Random.Range(0, 1f),
            Random.Range(0, 1f));
        player.SetDisplayColor(newColor);

    }

}

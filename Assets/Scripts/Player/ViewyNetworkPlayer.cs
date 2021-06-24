using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;

using TMPro;

public class ViewyNetworkPlayer : NetworkBehaviour
{
    [SerializeField] private TMP_Text displayNameText=null;
    [SerializeField] private Renderer displayColorRenderer = null;

    [SyncVar(hook =nameof(HandleDisplayNameUpdated))]
    [SerializeField] 
    private string displayName="Missing Name";

    [SyncVar(hook =nameof(HandleDisplayColorUpdated))]
    [SerializeField] 
    private Color displayColor = Color.black;

    #region Server
    [Server]
    public void SetDisplayName(string newDisplayName)
    {
        displayName = newDisplayName;
    }

    [Server]
    public void SetDisplayColor(Color newDisplayColor)
    {
        displayColor = newDisplayColor;
    }

    [Command]
    private void CmdSetDisplayName(string newDisplayName)
    {
        SetDisplayName(newDisplayName);
    }

    #endregion

    #region Client
    private void HandleDisplayNameUpdated(string oldName,string newName)
    {
        displayNameText.text = newName;
    }

    private void HandleDisplayColorUpdated(Color oldColor, Color newColor)
    {
        displayColorRenderer.material.color=newColor;
    }

    [ClientRpc]
    public void RpcSetMyName()
    {
        CmdSetDisplayName(PlayerPrefs.GetString("Username"));
    }
    
    #endregion
}

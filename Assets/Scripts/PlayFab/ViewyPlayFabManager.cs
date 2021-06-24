using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using PlayFab;
using PlayFab.ClientModels;
using PlayFab.ServerModels;
using PlayFab.MultiplayerModels;

using Mirror;
using Mirror.SimpleWeb;


public class ViewyPlayFabManager : MonoBehaviour
{
    //Reference the configuration file
    [SerializeField] Configuration configuration = null;

    //Reference the viewy network Manager and the web transport
    [SerializeField] ViewyNetworkManager networkManager = null;
    //[SerializeField] SimpleWebTransport simpleWebTransport = null;
    [SerializeField] TelepathyTransport telepathyTransport = null;

    //Reference the text elements
    [SerializeField] InputField usernameField=null;
    [SerializeField] InputField emailField = null;
    [SerializeField] InputField passwordField = null;
    [SerializeField] Text errorMessage = null;

    private const string USERNAME_ERROR = "Username length must be between 3-20";
    private const string PASSWORD_ERROR = "Password length must be between 6-100";

    private string username = null;
    private string email = null;

    //PlayFab Settings variables
    private string titleID = null;

    private GetAccountInfoResult accountInfo = null;

    void Start()
    {
        usernameField.onValueChanged.AddListener(delegate { UsernameChangeCheck(); });
        passwordField.onValueChanged.AddListener(delegate { PasswordChangeCheck(); });
        emailField.onValueChanged.AddListener(delegate { EmailChangeCheck(); });

        if (configuration.buildType == Configuration.BuildType.REMOTE_SERVER)
        {
            //Initialize and deploy PlayFab server
            //For documentation on build regions life cycle go to:
            //https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/multiplayer-build-region-lifecycle
            StartRemoteServer();
            //networkManager.StartServer();
        }
        else if(configuration.buildType == Configuration.BuildType.REMOTE_CLIENT)
        {
            //Get the title ID from the editor extension settings
            titleID = PlayFabSettings.TitleId;

            #region Check Player Prefs
            //If there is info stored, show it, so user doesn't have to type everytime
            if (PlayerPrefs.HasKey("Username"))
            {
                username = PlayerPrefs.GetString("Username");
                usernameField.text = username;
            }

            if (PlayerPrefs.HasKey("Email"))
            {
                email = PlayerPrefs.GetString("Email");
                emailField.text = email;
            }
            #endregion
        }
        else
        {
            networkManager.StartServer();
        }
    }

    #region PlayFab Server
    private void StartRemoteServer()
    {
        //Record the configuration for the region
        PlayFabMultiplayerAgentAPI.Start();

        //A server build will become active once a client sends a request
        //For documentation on multiplayer request go to:
        //https://docs.microsoft.com/en-us/gaming/playfab/features/multiplayer/servers/using-playfab-servers-to-host-games#5-request-game-servers
        //Once its active we need to start the mirror server.
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += networkManager.StartServer;

        //Start the Ready for Players API call
        StartCoroutine(ReadyForPlayers());
        networkManager.StartServer();
    }

    private IEnumerator ReadyForPlayers()
    {
        yield return new WaitForSeconds(.5f);

        //Report a StandBy status so the build can be deployed
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
    }
    #endregion

    #region PlayFab Client Login

    public void LogInClient()
    {
        //Create the register PlayFab user request and use the API call
        //For more info on register PlayFab user go to:
        //https://docs.microsoft.com/en-us/rest/api/playfab/client/authentication/register-playfab-user?view=playfab-rest
        var registerRequest = new RegisterPlayFabUserRequest
        {
            Username = usernameField.text,
            Email = emailField.text,
            Password = passwordField.text,
            TitleId = titleID
        };
        PlayFabClientAPI.RegisterPlayFabUser(registerRequest, RegisterPlayFabUserSuccess, RegisterPlayFabUserFailure);
    }

    #region Registration Methods
    private void RegisterPlayFabUserSuccess(RegisterPlayFabUserResult obj)
    {
        PlayerPrefs.SetString("Username", usernameField.text);
        PlayerPrefs.SetString("Email", emailField.text);
        Debug.Log("Registration Successfull");
        RequestMultiplayerServer();
    }
    private void RegisterPlayFabUserFailure(PlayFabError obj)
    {
        //If registration fails check to see if there is already an email registered
        if (obj.Error == PlayFabErrorCode.EmailAddressNotAvailable)
        {
            //If email is already taken try to login with email instead
            //For more info on login with email go to:
            //https://docs.microsoft.com/en-us/rest/api/playfab/client/authentication/login-with-email-address?view=playfab-rest
            var emailRequest = new LoginWithEmailAddressRequest
            {
                Email = emailField.text,
                Password = passwordField.text,
                TitleId = titleID
            };
            PlayFabClientAPI.LoginWithEmailAddress(emailRequest, LoginWithEmailSuccess, LoginWithEmailFailure);
        }
        else
        {
            errorMessage.text = obj.ErrorMessage;
        }
    }
    #endregion

    #region Login With Email Methods

    private void LoginWithEmailSuccess(LoginResult obj)
    {
        //If login succeeds, request multiplayer server
        Debug.Log("Login successfull");
        if (configuration.ipAddress == "")
        {   //We need to grab an IP and Port from a server based on the buildId. Copy this and add it to your Configuration.
            RequestMultiplayerServer();
        }
        else
        {
            ConnectRemoteClient();
        }
    }

    private void LoginWithEmailFailure(PlayFabError obj)
    {
        //If login fails, show error
        errorMessage.text = obj.ErrorMessage;
    }

    #endregion

    #region Request Multiplayer Methods

    private void RequestMultiplayerServer()
    {
        //Create the multiplayer server request and use the API call
        //For more info on multiplayer server request go to:
        //https://docs.microsoft.com/en-us/rest/api/playfab/multiplayer/multiplayer-server/request-multiplayer-server?view=playfab-rest

        RequestMultiplayerServerRequest requestData = new RequestMultiplayerServerRequest
        {
            BuildId = configuration.buildID,
            SessionId = Guid.NewGuid().ToString(),
            PreferredRegions = new List<string>() { "EastUs" }
        };
        PlayFabMultiplayerAPI.RequestMultiplayerServer(requestData, OnRequestMultiplayerServer, OnRequestMultiplayerServerError);
    }

    private void OnRequestMultiplayerServer(RequestMultiplayerServerResponse response)
    {
        //If the request succeeds, get the server info and connect the client
        ConnectRemoteClient(response);
    }

    private void OnRequestMultiplayerServerError(PlayFabError error)
    {
        Debug.Log(error.Error);
        //If the request fails, maybe it's because there are no more servers on StandBy
        //So we try to connect to an active server
        ConnectRemoteClient();
    }

    #endregion

    #region Connect Client

    private void ConnectRemoteClient(RequestMultiplayerServerResponse response = null)
    {
        if (response == null)
        {
            //If there is no response (called method with no parameter)
            //Get the ip and port from configuration file
            networkManager.networkAddress = configuration.ipAddress;
            //simpleWebTransport.port = configuration.port;
            telepathyTransport.port = configuration.port;
        }
        else
        {
            //Get the ip and port from server response
            Debug.Log("**** ADD THIS TO YOUR CONFIGURATION **** -- IP: " + response.IPV4Address + " Port: " + (ushort)response.Ports[0].Num);
            networkManager.networkAddress = response.IPV4Address;
            //simpleWebTransport.port = (ushort)response.Ports[0].Num;
            telepathyTransport.port = (ushort)response.Ports[0].Num;
        }

        //Connect the client
        networkManager.StartClient();
    }

    #endregion

    #endregion

    #region Input Field Listeners

    public void UsernameChangeCheck()
    {
        if(usernameField.text.Length<3 || usernameField.text.Length > 20)
        {
            errorMessage.text = USERNAME_ERROR;
            return;
        }
        errorMessage.text = null;
    }

    public void PasswordChangeCheck()
    {
        if (passwordField.text.Length < 6 || passwordField.text.Length > 100)
        {
            errorMessage.text = PASSWORD_ERROR;
            return;
        }
        errorMessage.text = null;
    }

    public void EmailChangeCheck()
    {
        if (!string.IsNullOrEmpty(errorMessage.text))
        {
            errorMessage.text = null;
        }
        
    }
    #endregion
}

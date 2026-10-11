using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class RelayManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private TextMeshProUGUI statusText;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        if (PlayerPrefs.HasKey("MenuStatus"))
        {
            if (statusText) statusText.text = PlayerPrefs.GetString("MenuStatus");
            PlayerPrefs.DeleteKey("MenuStatus");
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    public async void CreateRoom()
    {
        try
        {
            if (statusText) statusText.text = "CREATING ROOM...";
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (joinCodeText) joinCodeText.text = "CODE: " + joinCode;
            if (statusText) statusText.text = "WAITING FOR FRIEND TO JOIN...";

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            if (statusText) statusText.text = "ERROR CREATING ROOM!";
            Debug.LogError(e);
        }
    }

    public async void JoinRoom()
    {
        try
        {
            string code = codeInputField ? codeInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(code)) return;

            if (statusText) statusText.text = "CONNECTING...";
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();

            if (statusText) statusText.text = "CONNECTED! PLEASE WAIT FOR HOST TO START GAME...";
        }
        catch (RelayServiceException e)
        {
            if (statusText) statusText.text = "INVALID CODE OR ROOM NOT FOUND!";
            Debug.LogError(e);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost && clientId != NetworkManager.Singleton.LocalClientId)
        {
            if (statusText) statusText.text = "PLAYER 2 JOINED! TAP PLAY TO START RACE!";
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (statusText) statusText.text = "PLAYER DISCONNECTED!";
    }
}
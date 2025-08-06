using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LobbyManager : MonoBehaviour
{
    public const int MAX_PLAYER_COUNT = 4;
    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";

    public static LobbyManager Instance { get; private set; }
    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnJoinFailed;

    private float _heartbeatTimer = 0f;
    private Lobby _joinedLobby;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    private void Update()
    {
        HandleHeartbeat();
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void HandleHeartbeat()
    {
        if (IsLobbyHost())
        {
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer <= 0f)
            {
                float heartbeatTimerMax = 15f;
                _heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
            }
        }
    }

    public bool IsLobbyHost()
    {
        return _joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName,
                MAX_PLAYER_COUNT,
                new CreateLobbyOptions { IsPrivate = isPrivate }
            );

            Allocation allocation = await AllocationRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    {
                        KEY_RELAY_JOIN_CODE,
                        new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                    }
                }
            });

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            NetworkManager.Singleton.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void JoinLobby(string lobbyCode)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            string relayJoinCode = _joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }
    public async void DeleteLobby() {
        if(_joinedLobby != null) {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);

                _joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public Lobby GetLobby() {
        return _joinedLobby;
    }
    
    private async Task<Allocation> AllocationRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYER_COUNT - 1);

            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to allocate relay: {e.Message}");
            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation) {
        try {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            return relayJoinCode;
        } catch (RelayServiceException e) {
            Debug.LogError($"Failed to get relay join code: {e.Message}");
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode) {
        try {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            return joinAllocation;
        } catch (RelayServiceException e) {
            Debug.LogError($"Failed to join relay: {e.Message}");
            return default;
        }
    }
}

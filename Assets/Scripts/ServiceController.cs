#if UNITY_EDITOR
using ParrelSync;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServiceController : MonoSingleton<ServiceController>
{
    public static bool IsInitialized = false;

    public static RelayHostData RelayHostData;
    public static RelayJoinData RelayJoinData;

    public const string LobbySceneName = "Lobby";
    public const string GameplaySceneName = "Gameplay";

    public const string LobbyPlayingDataKey = "LobbyPlaying";
    public const string JoinCodeDataKey = "JoinCode";
    public const string PlayerNameDataKey = "PlayerName";
    public const string PlayerColorDataKey = "PlayerColor";

    public static string PlayerId;
    public static string PlayerName;
    public static string PlayerColor;
    public static Lobby joinedLobby;

    public Action onPlayerDataUpdated;
    public Action<Lobby> onLobbyCreated;
    public Action<Lobby> onJoinedLobby;
    public Action onLeaveJoinedLobby;
    public Action<Lobby> onLobbyUpdated;

    protected override void Awake()
    {
        base.Awake();
        PlayerName = "Player" + UnityEngine.Random.Range(10, 100);
        PlayerColor = "FF0000";
    }

    private async void Start()
    {

#if UNITY_EDITOR
        var options = new InitializationOptions();
        options.SetProfile(ClonesManager.GetCurrentProject().name);
        await UnityServices.InitializeAsync(options);
#else
        var options = new InitializationOptions();
        options.SetProfile(PlayerName);
        await UnityServices.InitializeAsync(options);
#endif

#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            Debug.Log("This is a clone project.");
            string customArgument = ParrelSync.ClonesManager.GetArgument();
            AuthenticationService.Instance.SwitchProfile($"Clone_{customArgument}_Profile");
        }
#endif

        // đăng nhập ẩn danh và lấy ra player id
        AuthenticationService.Instance.SignedIn += () =>
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        IsInitialized = true;

        onLobbyUpdated += (_) => CheckJoinedLobbyPlayingAndStartGameplay();
    }

    float heartBeatTimer = 0f;
    float lobbyUpdateTimer = 0f;
    private void Update()
    {
        if (joinedLobby != null)
        {
            try
            {
                HandleLobbyHeartBeat();
                HandleLobbyPollForUpdate();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    private async void HandleLobbyHeartBeat()
    {
        if (IsPlayerHostOfJoinedLobby)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer <= 0)
            {
                heartBeatTimer = 15f;

                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }
    private async void HandleLobbyPollForUpdate()
    {
        lobbyUpdateTimer -= Time.deltaTime;
        if (lobbyUpdateTimer <= 0)
        {
            lobbyUpdateTimer = 1.1f;

            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            if (joinedLobby != null)    // nếu chưa rời lobby thì mới update
            {
                joinedLobby = lobby;
                onLobbyUpdated?.Invoke(lobby);
            }
        }
    }
    public bool IsPlayerHostOfJoinedLobby => joinedLobby.HostId == PlayerId;

    public void UpdatePlayerData(string playerName, string playerColor)
    {
        PlayerName = playerName;
        PlayerColor = playerColor;

        onPlayerDataUpdated?.Invoke();
    }

    public async Task<List<Lobby>> GetLobbiesListFromService()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            return queryResponse.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    Player CreatePlayerDataForLobby()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { PlayerNameDataKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerName) },
                { PlayerColorDataKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerColor) }
            }
        };
    }

    public async Task CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
    {
        try
        {
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(4);
            RelayHostData = new RelayHostData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };
            RelayHostData.JoinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = CreatePlayerDataForLobby(),
                Data = new Dictionary<string, DataObject>
                {
                    { JoinCodeDataKey, new DataObject (DataObject.VisibilityOptions.Member, RelayHostData.JoinCode) },
                    { LobbyPlayingDataKey, new DataObject(DataObject.VisibilityOptions.Public, "0") }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            joinedLobby = lobby;
            onLobbyCreated?.Invoke(lobby);
            onJoinedLobby?.Invoke(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinLobby(Lobby lobby)
    {
        try
        {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
            {
                Player = CreatePlayerDataForLobby()
            };
            joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id, joinLobbyByIdOptions);

            string joinCode = joinedLobby.Data[JoinCodeDataKey].Value;
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);
            RelayJoinData = new RelayJoinData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                HostConnectionData = allocation.HostConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };

            onJoinedLobby?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByCode(string code)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = CreatePlayerDataForLobby()
            };
            joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(code, joinLobbyByCodeOptions);

            onJoinedLobby?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions quickJoinLobbyOptions = new QuickJoinLobbyOptions
            {
                Player = CreatePlayerDataForLobby()
            };
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinLobbyOptions);

            onJoinedLobby?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async Task UpdateJoinedLobbyData(string name, int maxPlayers, bool isPrivate)
    {
        if (!IsPlayerHostOfJoinedLobby)
            return;
        try
        {
            joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Name = name,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate
            });

            onLobbyUpdated?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void StartGame()
    {
        joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { LobbyPlayingDataKey, new DataObject(DataObject.VisibilityOptions.Public, "1") }
            }
        });

        if (SceneManager.GetActiveScene().name == LobbySceneName)
            SceneManager.LoadScene(GameplaySceneName);
    }

    private void CheckJoinedLobbyPlayingAndStartGameplay()
    {
        if (joinedLobby.Data[LobbyPlayingDataKey].Value == "1")
        {
            if (SceneManager.GetActiveScene().name == LobbySceneName)
                SceneManager.LoadScene(GameplaySceneName);
        }
    }

    public async Task LeaveJoinedLobby()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, PlayerId);
            joinedLobby = null;

            onLeaveJoinedLobby?.Invoke();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void OnApplicationQuit()
    {
        if (joinedLobby != null)
        {
            await LeaveJoinedLobby();
        }
    }
}

public struct RelayHostData
{
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] Key;
}
public struct RelayJoinData
{
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] HostConnectionData;
    public byte[] Key;
}
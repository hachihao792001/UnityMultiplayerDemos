﻿#if UNITY_EDITOR
using ParrelSync;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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

    public const string LobbyStartingDataKey = "LobbyStarting"; //để báo hiện màn hình Starting
    public const string LobbyStartedDataKey = "LobbyStarted";   //để báo chuyển qua scene gameplay
    public const string LobbyPlayingDataKey = "LobbyPlaying";   //để báo lobby đang chơi và không vào được
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
    public Action<Lobby> onLobbyStarting;

    public static bool IsBackFromGameplay;

    protected override void SingletonAwake()
    {
        PlayerName = "Player" + UnityEngine.Random.Range(10, 100);
        PlayerColor = "FF0000";

        IsBackFromGameplay = false;
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

        onLobbyUpdated += (_) =>
        {
            CheckJoinedLobbyStarting();
            CheckJoinedLobbyStartedAndStartGameplay();
        };
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
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    //N1 là LobbyPlayingDataKey, xem hàm CreateLobby
                    //tìm những lobby đang không chơi
                    new QueryFilter(QueryFilter.FieldOptions.N1, "0", QueryFilter.OpOptions.EQ)
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
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = CreatePlayerDataForLobby(),
                Data = new Dictionary<string, DataObject>
                {
                    { JoinCodeDataKey, new DataObject (DataObject.VisibilityOptions.Member, "") },
                    { LobbyStartedDataKey, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                    { LobbyStartingDataKey, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                    { LobbyPlayingDataKey, new DataObject(DataObject.VisibilityOptions.Public, "0", DataObject.IndexOptions.N1) }
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

            onJoinedLobby?.Invoke(joinedLobby);
        }
        catch (Exception e)
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
        if (SceneManager.GetActiveScene().name == LobbySceneName)
        {
            // nói cho các player khác biết là nút start mới được bấm và đang vào game (để hiện màn hình Starting...)
            joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { LobbyStartingDataKey, new DataObject (DataObject.VisibilityOptions.Member, "1") },
                }
            });

            // tạo allocation cho relay
            Debug.Log("Creating allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            RelayHostData = new RelayHostData
            {
                Key = allocation.Key,
                Port = (ushort)allocation.RelayServer.Port,
                AllocationID = allocation.AllocationId,
                AllocationIDBytes = allocation.AllocationIdBytes,
                ConnectionData = allocation.ConnectionData,
                IPv4Address = allocation.RelayServer.IpV4
            };
            RelayHostData.JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Created allocation with join code " + RelayHostData.JoinCode);

            // nói cho các player khác biết là lobby đã vào game
            joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { JoinCodeDataKey, new DataObject (DataObject.VisibilityOptions.Member, RelayHostData.JoinCode) },
                    { LobbyStartedDataKey, new DataObject(DataObject.VisibilityOptions.Member, "1") },
                    { LobbyStartingDataKey, new DataObject (DataObject.VisibilityOptions.Member, "0") },
                    { LobbyPlayingDataKey, new DataObject (DataObject.VisibilityOptions.Public, "1", DataObject.IndexOptions.N1) },
                }
            });

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                RelayHostData.IPv4Address,
                RelayHostData.Port,
                RelayHostData.AllocationIDBytes,
                RelayHostData.Key,
                RelayHostData.ConnectionData);

            SceneManager.LoadScene(GameplaySceneName);

            await Task.Delay(1500);
            // set lại started thành 0 vì để khi player trở lại lobby từ gameplay thì ko bị ngay lập tức đi vào lại
            joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { LobbyStartedDataKey, new DataObject(DataObject.VisibilityOptions.Member, "0") },
                }
            });
        }
    }

    private async void CheckJoinedLobbyStartedAndStartGameplay()
    {
        if (!IsBackFromGameplay && joinedLobby.Data[LobbyStartedDataKey].Value == "1")
        {
            if (SceneManager.GetActiveScene().name == LobbySceneName)
            {
                string joinCode = joinedLobby.Data[JoinCodeDataKey].Value;
                Debug.Log("Joining allocation with join code " + joinCode + "...");
                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log("Join allocation with join code " + joinCode + " successfully");
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

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                    RelayJoinData.IPv4Address,
                    RelayJoinData.Port,
                    RelayJoinData.AllocationIDBytes,
                    RelayJoinData.Key,
                    RelayJoinData.ConnectionData,
                    RelayJoinData.HostConnectionData);

                SceneManager.LoadScene(GameplaySceneName);
            }
        }
    }

    public async Task JoinLobbyStopPlaying()
    {
        joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { LobbyPlayingDataKey, new DataObject(DataObject.VisibilityOptions.Public, "0", DataObject.IndexOptions.N1) },
            }
        });
    }

    private void CheckJoinedLobbyStarting()
    {
        if (joinedLobby.Data[LobbyStartingDataKey].Value == "1")
        {
            if (SceneManager.GetActiveScene().name == LobbySceneName)
            {
                onLobbyStarting?.Invoke(joinedLobby);
            }
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
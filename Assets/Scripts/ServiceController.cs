using ParrelSync;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class ServiceController : MonoSingleton<ServiceController>
{
    public static bool IsInitialized = false;

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
        await UnityServices.InitializeAsync();
#endif

        // đăng nhập ẩn danh và lấy ra player id
        AuthenticationService.Instance.SignedIn += () =>
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        IsInitialized = true;
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
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = CreatePlayerDataForLobby()
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
            Lobby joinedLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id, joinLobbyByIdOptions);
            ServiceController.joinedLobby = joinedLobby;

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

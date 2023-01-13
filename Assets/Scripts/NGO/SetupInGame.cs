using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class SetupInGame : MonoBehaviour
{
    [SerializeField] private GameObject ingameRunnerPrefab;
    [SerializeField] private GameObject[] disableWhileInGame;
    
    private InGameRunner _inGameRunner;
    
    private bool _doesNeedCleanup;
    private bool _hasConnectedViaNGO;

    private LocalLobby _localLobby;
    
    private void SetMenuVisibility(bool areVisible)
    {
        foreach (var go in disableWhileInGame)
        {
            go.SetActive(areVisible);
        }
    }
    
    private async Task CreateNetworkManager(LocalLobby localLobby, LocalPlayer localPlayer)
    {
        _localLobby = localLobby;
        _inGameRunner = Instantiate(ingameRunnerPrefab).GetComponentInChildren<InGameRunner>();
        _inGameRunner.Initialize(OnConnectionVerified, _localLobby.PlayerCount, OnGameBegin, OnGameEnd, localPlayer);
        
        if (localPlayer.IsHost.Value)
        {
            await SetRelayHostData();
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            await AwaitRelayCode(localLobby);
            await SetRelayClientData();
            NetworkManager.Singleton.StartClient();
        }
    }
    
    private async Task AwaitRelayCode(LocalLobby lobby)
    {
        var relayCode = lobby.RelayCode.Value;
        lobby.RelayCode.OnChanged += (code) => relayCode = code;
        
        while (string.IsNullOrEmpty(relayCode))
        {
            await Task.Delay(100);
        }
    }
    
    private async Task SetRelayHostData()
    {
        var transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();

        var allocation = await Relay.Instance.CreateAllocationAsync(_localLobby.MaxPlayerCount.Value);
        var joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
        GameManager.Instance.HostSetRelayCode(joinCode);

        var endpoint = GetEndpointForAllocation(allocation.ServerEndpoints,
            allocation.RelayServer.IpV4, allocation.RelayServer.Port, out var isSecure);

        transport.SetHostRelayData(AddressFromEndpoint(endpoint), endpoint.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, isSecure);
    }

    private async Task SetRelayClientData()
    {
        var transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();

        var joinAllocation = await Relay.Instance.JoinAllocationAsync(_localLobby.RelayCode.Value);
        var endpoint = GetEndpointForAllocation(joinAllocation.ServerEndpoints,
            joinAllocation.RelayServer.IpV4, joinAllocation.RelayServer.Port, out var isSecure);

        transport.SetClientRelayData(AddressFromEndpoint(endpoint), endpoint.Port,
            joinAllocation.AllocationIdBytes, joinAllocation.Key,
            joinAllocation.ConnectionData, joinAllocation.HostConnectionData, isSecure);
    }
    
    /// <summary>
    /// Determine the server endpoint for connecting to the Relay server, for either an Allocation or a JoinAllocation.
    /// If DTLS encryption is available, and there's a secure server endpoint available, use that as a secure connection. Otherwise, just connect to the Relay IP unsecured.
    /// </summary>
    private NetworkEndPoint GetEndpointForAllocation(
        List<RelayServerEndpoint> endpoints,
        string ip,
        int port,
        out bool isSecure)
    {
#if ENABLE_MANAGED_UNITYTLS
        foreach (var endpoint in endpoints)
        {
            if (endpoint.Secure && endpoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
            {
                isSecure = true;
                return NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);
            }
        }
#endif
        isSecure = false;
        return NetworkEndPoint.Parse(ip, (ushort)port);
    }

    private string AddressFromEndpoint(NetworkEndPoint endpoint)
    {
        return endpoint.Address.Split(':')[0];
    }

    private void OnConnectionVerified()
    {
        _hasConnectedViaNGO = true;
    }
    
    public void StartNetworkedGame(LocalLobby localLobby, LocalPlayer localPlayer)
    {
        _doesNeedCleanup = true;
        SetMenuVisibility(false);
#pragma warning disable 4014
        CreateNetworkManager(localLobby, localPlayer);
#pragma warning restore 4014
    }
    
    public void OnGameBegin()
    {
        if (!_hasConnectedViaNGO)
        {
            // If this localPlayer hasn't successfully connected via NGO, forcibly exit the minigame.
            // LogHandlerSettings.Instance.SpawnErrorPopup("Failed to join the game."); // todo
            OnGameEnd();
        }
    }
    
    public void OnGameEnd()
    {
        if (_doesNeedCleanup)
        {
            NetworkManager.Singleton.Shutdown(true);
            Destroy(_inGameRunner
                .transform.parent
                .gameObject); // Since this destroys the NetworkManager, that will kick off cleaning up networked objects.
            SetMenuVisibility(true);
            _localLobby.RelayCode.Value = "";
            GameManager.Instance.EndGame();
            _doesNeedCleanup = false;
        }
    }
}
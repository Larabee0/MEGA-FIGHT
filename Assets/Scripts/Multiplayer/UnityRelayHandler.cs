using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using MultiplayerRunTime;

[Serializable]
public struct RelayHostData
{
    public static RelayHostData allocatedData;
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] Key;
}

[Serializable]
public struct RelayJoinData
{
    public static RelayJoinData allocatedData;
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] HostConnectionData;
    public byte[] Key;
}

public static class UnityRelayHandler
{
    public static async Task<RelayHostData> HostGame(int maxConnections)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Allocation allocation = await Unity.Services.Relay.Relay.Instance.CreateAllocationAsync(maxConnections);
        RelayHostData data = new()
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };
        data.JoinCode = await Unity.Services.Relay.Relay.Instance.GetJoinCodeAsync(data.AllocationID);
        return data;
    }

    public static async Task<RelayJoinData> JoinGame(string joinCode)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        JoinAllocation allocation = await Unity.Services.Relay.Relay.Instance.JoinAllocationAsync(joinCode);

        RelayJoinData data = new()
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            HostConnectionData = allocation.HostConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };
        data.JoinCode = joinCode;
        return data;
    }

    public static async Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] key, string joinCode)> 
        AllocateRelayServerAndGetJoinCode(int maxConnections)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Allocation allocation;
        string createJoinCode;
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation request failed {e.Message}");
            throw;
        }

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        try
        {
            createJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.Key, createJoinCode);
    }

    public static async Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] hostConnectionData, byte[] key)> JoinRelayServerFromJoinCode(string joinCode)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        JoinAllocation allocation;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.HostConnectionData, allocation.Key);
    }

}


public class RelayUTPHandler
{
    public NetworkDriver HostDriver;
    public NetworkDriver PlayerDriver;
    public string joinCode;

    private NetworkConnection clientConnection;
    public bool isRelayServerConnected = false;

    private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
    {
        unsafe
        {
            fixed (byte* ptr = allocationIdBytes)
            {
                return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
            }
        }
    }

    private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
    {
        unsafe
        {
            fixed (byte* ptr = connectionData)
            {
                return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
            }
        }
    }

    private static RelayHMACKey ConvertFromHMAC(byte[] hmac)
    {
        unsafe
        {
            fixed (byte* ptr = hmac)
            {
                return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
            }
        }
    }
    private static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
    {
        foreach (var endpoint in endpoints)
        {
            if (endpoint.ConnectionType == connectionType)
            {
                return endpoint;
            }
        }

        return null;
    }
    public static RelayServerData HostRelayData(Allocation allocation, string connectionType = "dtls")
    {
        // Select endpoint based on desired connectionType
        var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
        if (endpoint == null)
        {
            throw new Exception($"endpoint for connectionType {connectionType} not found");
        }

        // Prepare the server endpoint using the Relay server IP and port
        var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);

        // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
        var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
        var connectionData = ConvertConnectionData(allocation.ConnectionData);
        var key = ConvertFromHMAC(allocation.Key);

        // Prepare the Relay server data and compute the nonce value
        // The host passes its connectionData twice into this function
        var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
            ref connectionData, ref key, connectionType == "dtls");
        relayServerData.ComputeNewNonce();

        return relayServerData;
    }

    public static RelayServerData PlayerRelayData(JoinAllocation allocation, string connectionType = "dtls")
    {
        // Select endpoint based on desired connectionType
        var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
        if (endpoint == null)
        {
            throw new Exception($"endpoint for connectionType {connectionType} not found");
        }

        // Prepare the server endpoint using the Relay server IP and port
        var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);

        // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
        var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
        var connectionData = ConvertConnectionData(allocation.ConnectionData);
        var hostConnectionData = ConvertConnectionData(allocation.HostConnectionData);
        var key = ConvertFromHMAC(allocation.Key);

        // Prepare the Relay server data and compute the nonce values
        // A player joining the host passes its own connectionData as well as the host's
        var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
            ref hostConnectionData, ref key, connectionType == "dtls");
        relayServerData.ComputeNewNonce();

        return relayServerData;
    }

    public static async Task<bool> AutehnticatePlayerAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            var playerID = AuthenticationService.Instance.PlayerId;
            return true;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return false;
        }
    }

    public void RelayHandlerUpdate()
    {
        if (HostDriver.IsCreated && isRelayServerConnected)
        {
            HostDriver.ScheduleUpdate().Complete();
            while (HostDriver.Accept() != default)
            {
                Debug.Log("Accepted an incoming connection.");
            }
        }

        if(PlayerDriver.IsCreated && clientConnection.IsCreated)
        {
            PlayerDriver.ScheduleUpdate().Complete();
            Unity.Networking.Transport.NetworkEvent.Type eventType;
            while((eventType = clientConnection.PopEvent(PlayerDriver, out _))!= Unity.Networking.Transport.NetworkEvent.Type.Empty)
            {
                if(eventType == Unity.Networking.Transport.NetworkEvent.Type.Connect)
                {
                    Debug.Log("Client connected to the server");
                }
                else if(eventType == Unity.Networking.Transport.NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from the server");
                    clientConnection = default;
                }
            }
        }
    }

    public IEnumerator StartRelayServer(int relayMaxConnections)
    {
        var auth = AutehnticatePlayerAsync();
        while (!auth.IsCompleted) yield return null;
        if (auth.IsFaulted)
        {
            Debug.Log("Authentication failed");
            yield break;
        }

        var authenticated = auth.Result;
        if (!authenticated)
        {
            Debug.Log("Authentication failed");
            yield break;
        }

        var regionTask = RelayService.Instance.ListRegionsAsync();
        while (!regionTask.IsCompleted)
        {
            yield return null;
        }
        if (regionTask.IsFaulted)
        {
            Debug.LogError("List regions request failed");
            yield break;
        }

        var regionList = regionTask.Result;
        //for (int i = 0; i < regionList.Count; i++)
        //{
        //    Debug.Log(regionList[i].Id);
        //}

        var targetRegion = regionList[1].Id;

        var allocationTask = RelayService.Instance.CreateAllocationAsync(relayMaxConnections, targetRegion);

        while (!allocationTask.IsCompleted)
        {
            yield return null;
        }

        if (allocationTask.IsFaulted)
        {
            Debug.LogError("Create allocation request failed");
            yield break;
        }

        var allocation = allocationTask.Result;

        var joinCodeTask = RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        while (!joinCodeTask.IsCompleted)
        {
            yield return null;
        }

        if (joinCodeTask.IsFaulted)
        {
            Debug.LogError("Create join code request failed");
            yield break;
        }

        joinCode = joinCodeTask.Result;

        var relayServerData = HostRelayData(allocation, "dtls");

        RelayHostData hostData = new()
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };
        yield return ServerBindAndListen(hostData, relayServerData);
    }

    private IEnumerator ServerBindAndListen(RelayHostData hostData, RelayServerData relayServerData)
    {
        var settings = new NetworkSettings();
        settings.WithRelayParameters(serverData: ref relayServerData);
        HostDriver = NetworkDriver.Create(settings);

        if(HostDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
        {
            Debug.Log("Server failed to bind");
        }
        else
        {
            while (!HostDriver.Bound)
            {
                HostDriver.ScheduleUpdate().Complete();
                yield return null;
            }
            Debug.Log("Server bound!");
            if(HostDriver.Listen()!= 0)
            {
                Debug.LogError("Server failed to listen");
            }
            else
            {
                isRelayServerConnected = true;
            }
        }
        NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetHostRelayData(hostData.IPv4Address, hostData.Port, hostData.AllocationIDBytes, hostData.Key, hostData.ConnectionData, false);

        NetworkManager.Singleton.StartHost();
        PasswordLobbyMP.Singleton.menu.spawnPopUp.DisplayJoinCode = joinCode;
    }

    public IEnumerator StartClient(string relayJoinCode)
    {
        var auth = AutehnticatePlayerAsync();
        while (!auth.IsCompleted) yield return null;
        if (auth.IsFaulted)
        {
            Debug.Log("Authentication failed");
            yield break;
        }
        Debug.LogFormat("using client joincode: {0}", relayJoinCode);
        var joinTask = RelayService.Instance.JoinAllocationAsync(relayJoinCode);

        while(!joinTask.IsCompleted)yield return null;

        if (joinTask.IsFaulted)
        {
            Debug.LogError("Join Relay request failed: "+ joinTask.Exception.Message);
            yield break;
        }

        var allocation = joinTask.Result;

        var relayServerData = PlayerRelayData(allocation);
        RelayJoinData clientData = new()
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            HostConnectionData = allocation.HostConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };
        yield return ClientBindAndConnect(clientData, relayServerData);
    }

    private IEnumerator ClientBindAndConnect(RelayJoinData clientData ,RelayServerData relayServerData)
    {
        var settings = new NetworkSettings();
        settings.WithRelayParameters(serverData: ref relayServerData);
        PlayerDriver = NetworkDriver.Create(settings);

        if(PlayerDriver.Bind(NetworkEndPoint.AnyIpv4)!= 0)
        {
            Debug.LogError("Client failed to bind");
        }
        else
        {
            while (!PlayerDriver.Bound)
            {
                PlayerDriver.ScheduleUpdate().Complete();
                yield return null;
            }
            clientConnection = PlayerDriver.Connect(relayServerData.Endpoint);
        }


        NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetClientRelayData(clientData.IPv4Address, clientData.Port, clientData.AllocationIDBytes, clientData.Key, clientData.ConnectionData, clientData.HostConnectionData, false);


        NetworkManager.Singleton.StartClient();
        PasswordLobbyMP.Singleton.menu.spawnPopUp.DisplayJoinCode = joinCode;
    }
}
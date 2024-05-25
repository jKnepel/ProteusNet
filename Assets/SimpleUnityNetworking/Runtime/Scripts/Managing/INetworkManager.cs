using jKnepel.SimpleUnityNetworking.Logging;
using jKnepel.SimpleUnityNetworking.Managing;
using jKnepel.SimpleUnityNetworking.Networking;
using jKnepel.SimpleUnityNetworking.Networking.Transporting;
using jKnepel.SimpleUnityNetworking.Serialising;
using System;
using System.Collections.Concurrent;
using UnityEngine;

using Logger = jKnepel.SimpleUnityNetworking.Logging.Logger;

namespace jKnepel.SimpleUnityNetworking
{
    public interface INetworkManager
    {
        #region fields
        
        /// <summary>
        /// The transport instance, which will be used for sending and receiving data
        /// and managing internal connections
        /// </summary>
        Transport Transport { get; }
        /// <summary>
        /// The configuration that will create the instance of the <see cref="Transport"/>
        /// </summary>
        TransportConfiguration TransportConfiguration { get; set; }
        
        /// <summary>
        /// The configuration for the serialiser used by the network manager
        /// </summary>
        SerialiserConfiguration SerialiserConfiguration { get; set; }
        
        /// <summary>
        /// The logger instance, which will be used for saving and displaying messages
        /// </summary>
        Logger Logger { get; }
        /// <summary>
        /// The configuration that will create the instance of the <see cref="Logger"/>
        /// </summary>
        LoggerConfiguration LoggerConfiguration { get; set; }
        
        /// <summary>
        /// Whether a local server is started
        /// </summary>
        bool IsServer { get; }
        /// <summary>
        /// Whether a local client is started and authenticated
        /// </summary>
        bool IsClient { get; }
        /// <summary>
        /// Whether a local server is started or local client is authenticated
        /// </summary>
        bool IsOnline { get; }
        /// <summary>
        /// Whether a local server is started and local client is authenticated
        /// </summary>
        bool IsHost { get; }
        
        /// <summary>
        /// Information about the local or connected remote server
        /// </summary>
        ServerInformation ServerInformation { get; }
        /// <summary>
        /// The current connection state of the local server
        /// </summary>
        ELocalServerConnectionState Server_LocalState { get; }
        /// <summary>
        /// The clients that are connected to the local server
        /// </summary>
        ConcurrentDictionary<uint, ClientInformation> Server_ConnectedClients { get; }
        /// <summary>
        /// Information about the authenticated local client
        /// </summary>
        ClientInformation ClientInformation { get; }
        /// <summary>
        /// The current connection state of the local client
        /// </summary>
        ELocalClientConnectionState Client_LocalState { get; }
        /// <summary>
        /// The remote clients that are connected to the same server
        /// </summary>
        ConcurrentDictionary<uint, ClientInformation> Client_ConnectedClients { get; }
        
        #endregion
        
        #region events

        /// <summary>
        /// Called when the local server's connection state has been updated
        /// </summary>
        event Action<ELocalServerConnectionState> Server_OnLocalStateUpdated;
        /// <summary>
        /// Called by the local server when a new remote client has been authenticated
        /// </summary>
        event Action<uint> Server_OnRemoteClientConnected;
        /// <summary>
        /// Called by the local server when a remote client disconnected
        /// </summary>
        event Action<uint> Server_OnRemoteClientDisconnected;
        /// <summary>
        /// Called by the local server when a remote client updated its information
        /// </summary>
        event Action<uint> Server_OnRemoteClientUpdated;
        
        /// <summary>
        /// Called when the local client's connection state has been updated
        /// </summary>
        event Action<ELocalClientConnectionState> Client_OnLocalStateUpdated;
        /// <summary>
        /// Called by the local client when a new remote client has been authenticated
        /// </summary>
        event Action<uint> Client_OnRemoteClientConnected;
        /// <summary>
        /// Called by the local client when a remote client disconnected
        /// </summary>
        event Action<uint> Client_OnRemoteClientDisconnected;
        /// <summary>
        /// Called by the local client when a remote client updated its information
        /// </summary>
        event Action<uint> Client_OnRemoteClientUpdated;

        /// <summary>
        /// Called when a tick was started
        /// </summary>
        event Action OnTickStarted;
        /// <summary>
        /// Called when a tick was completed
        /// </summary>
        event Action OnTickCompleted;
        
        #endregion
        
        #region methods

        /// <summary>
        /// This method calls the transport's internal tick method, updating connections and
        /// incoming and outgoing packets.
        /// </summary>
        /// <remarks>
        /// Calling this method will disable automatic ticks in the transport settings.
        /// Only use this method if ticks are to be handled manually.
        /// </remarks>
        void Tick();

        /// <summary>
        /// Method to start a local server with the given parameters
        /// </summary>
        /// <param name="servername"></param>
        void StartServer(string servername);
        /// <summary>
        /// Method to stop the local server
        /// </summary>
        void StopServer();

        /// <summary>
        /// Method to start a local client with the given parameters
        /// </summary>
        /// <param name="username"></param>
        /// <param name="userColour"></param>
        void StartClient(string username, Color32 userColour);
        /// <summary>
        /// Method to stop the local client 
        /// </summary>
        void StopClient();

        /// <summary>
        /// Method to stop both the local client and server
        /// </summary>
        void StopNetwork();

        /// <summary>
        /// Registers a callback for a sent byte array with the defined id
        /// </summary>
        /// <param name="byteID">Id of the data that should invoke the callback</param>
        /// <param name="callback">
        ///     Callback which will be invoked after byte data with the given id has been received
        ///     <param name="callback arg1">The ID of the sender. The ID will be 0 if the struct data was sent by the server</param>
        ///     <param name="callback arg2">The received byte data</param>
        /// </param>
        void Client_RegisterByteData(string byteID, Action<uint, byte[]> callback);
        /// <summary>
        /// Unregisters a callback for a sent byte array with the defined id
        /// </summary>
        /// <param name="byteID">Id of the data that should invoke the callback</param>
        /// <param name="callback">
        ///     Callback which will be invoked after byte data with the given id has been received
        ///     <param name="callback arg1">The ID of the sender. The ID will be 0 if the struct data was sent by the server</param>
        ///     <param name="callback arg2">The received byte data</param>
        /// </param>
        void Client_UnregisterByteData(string byteID, Action<uint, byte[]> callback);
        /// <summary>
        /// Sends byte data with a given id from the local client to the server.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        void Client_SendByteDataToServer(string byteID, byte[] byteData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        /// <summary>
        /// Sends byte data with a given id from the local client to a given remote client.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        void Client_SendByteDataToClient(uint clientID, string byteID, byte[] byteData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        /// <summary>
        /// Sends byte data with a given id from the local client to all other remote clients.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        void Client_SendByteDataToAll(string byteID, byte[] byteData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        /// <summary>
        /// Sends byte data with a given id from the local client to a list of remote clients.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientIDs"></param>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        void Client_SendByteDataToClients(uint[] clientIDs, string byteID, byte[] byteData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);

        /// <summary>
        /// Registers a callback for a sent struct of type <see cref="IStructData"/>
        /// </summary>
        /// <param name="callback">
        ///     Callback which will be invoked after a struct of the same type has been received
        ///     <param name="callback arg1">The ID of the sender. The ID will be 0 if the struct data was sent by the server</param>
        ///     <param name="callback arg2">The received struct data</param>
        /// </param>
        void Client_RegisterStructData<T>(Action<uint, T> callback) where T : struct, IStructData;
        /// <summary>
        /// Unregisters a callback for a sent struct of type <see cref="IStructData"/>
        /// </summary>
        /// <param name="callback">
        ///     Callback which will be invoked after a struct of the same type has been received
        ///     <param name="callback arg1">The ID of the sender. The ID will be 0 if the struct data was sent by the server</param>
        ///     <param name="callback arg2">The received struct data</param>
        /// </param>
        void Client_UnregisterStructData<T>(Action<uint, T> callback) where T : struct, IStructData;
        /// <summary>
        /// Sends a struct of type <see cref="IStructData"/> from the local client to the server.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
        void Client_SendStructDataToServer<T>(T structData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct, IStructData;
        /// <summary>
        /// Sends a struct of type <see cref="IStructData"/> from the local client to a given remote client.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
        void Client_SendStructDataToClient<T>(uint clientID, T structData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct, IStructData;
        /// <summary>
        /// Sends a struct of type <see cref="IStructData"/> from the local client to all other remote client.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
        void Client_SendStructDataToAll<T>(T structData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct, IStructData;
        /// <summary>
        /// Sends a struct of type <see cref="IStructData"/> from the local client to a list of remote client.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientIDs"></param>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
        void Client_SendStructDataToClients<T>(uint[] clientIDs, T structData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct, IStructData;
        
        /// <summary>
        /// Registers a callback for a sent byte array with the defined id
        /// </summary>
        /// <param name="byteID">Id of the data that should invoke the callback</param>
        /// <param name="callback">
        ///     Callback which will be invoked after byte data with the given id has been received
        ///     <param name="callback arg1">The ID of the sender</param>
        ///     <param name="callback arg2">The received byte data</param>
        /// </param>
        void Server_RegisterByteData(string byteID, Action<uint, byte[]> callback);
        /// <summary>
        /// Unregisters a callback for a sent byte array with the defined id
        /// </summary>
        /// <param name="byteID">Id of the data that should invoke the callback</param>
        /// <param name="callback">
        ///     Callback which will be invoked after byte data with the given id has been received
        ///     <param name="callback arg1">The ID of the sender</param>
        ///     <param name="callback arg2">The received byte data</param>
        /// </param>
        void Server_UnregisterByteData(string byteID, Action<uint, byte[]> callback);
        /// <summary>
        /// Sends byte data with a given id from the local client to a given remote client.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        void Server_SendByteDataToClient(uint clientID, string byteID, byte[] byteData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        /// <summary>
        /// Sends byte data with a given id from the local client to all other remote clients.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        void Server_SendByteDataToAll(string byteID, byte[] byteData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);
        /// <summary>
        /// Sends byte data with a given id from the local client to a list of remote clients.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientIDs"></param>
        /// <param name="byteID"></param>
        /// <param name="byteData"></param>
        /// <param name="channel"></param>
        void Server_SendByteDataToClients(uint[] clientIDs, string byteID, byte[] byteData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered);

        /// <summary>
        /// Registers a callback for a sent struct of type <see cref="IStructData"/>
        /// </summary>
        /// <param name="callback">
        ///     Callback which will be invoked after a struct of the same type has been received
        ///     <param name="callback arg1">The ID of the sender</param>
        ///     <param name="callback arg2">The received struct data</param>
        /// </param>
        void Server_RegisterStructData<T>(Action<uint, T> callback) where T : struct, IStructData;
        /// <summary>
        /// Unregisters a callback for a sent struct of type <see cref="IStructData"/>
        /// </summary>
        /// <param name="callback">
        ///     Callback which will be invoked after a struct of the same type has been received
        ///     <param name="callback arg1">The ID of the sender</param>
        ///     <param name="callback arg2">The received struct data</param>
        /// </param>
        void Server_UnregisterStructData<T>(Action<uint, T> callback) where T : struct, IStructData;
        /// <summary>
        /// Sends a struct of type <see cref="IStructData"/> from the local client to a given remote client.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
        void Server_SendStructDataToClient<T>(uint clientID, T structData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct, IStructData;
        /// <summary>
        /// Sends a struct of type <see cref="IStructData"/> from the local client to all other remote client.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
        void Server_SendStructDataToAll<T>(T structData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct, IStructData;
        /// <summary>
        /// Sends a struct of type <see cref="IStructData"/> from the local client to a list of remote client.
        /// Can only be called after the local client has been authenticated
        /// </summary>
        /// <param name="clientIDs"></param>
        /// <param name="structData"></param>
        /// <param name="channel"></param>
        void Server_SendStructDataToClients<T>(uint[] clientIDs, T structData,
            ENetworkChannel channel = ENetworkChannel.UnreliableUnordered) where T : struct, IStructData;
        
        #endregion
    }
}

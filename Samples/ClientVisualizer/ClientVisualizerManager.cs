using jKnepel.ProteusNet.Components;
using System.Collections.Generic;
using UnityEngine;

namespace jKnepel.ProteusNet.Samples
{
    public class ClientVisualizerManager : NetworkBehaviour
    {
        [SerializeField] private ClientVisualizer visualizerPrefab;
        [SerializeField] private Transform visualizerParent;

        private readonly Dictionary<uint, ClientVisualizer> _visualisers = new();

		#region lifecycle

		public override void OnServerSpawned()
        {
            NetworkManager.Server.OnRemoteClientConnected += SpawnClientVisualizer;
            NetworkManager.Server.OnRemoteClientDisconnected += DespawnClientVisualizer;
        }
        
        public override void OnServerDespawned()
        {
            NetworkManager.Server.OnRemoteClientConnected -= SpawnClientVisualizer;
            NetworkManager.Server.OnRemoteClientDisconnected -= DespawnClientVisualizer;
        }

        #endregion

		#region private methods

        private void SpawnClientVisualizer(uint clientID)
        {
            var visualizer = Instantiate(visualizerPrefab, visualizerParent);
            visualizer.Spawn(clientID);
            _visualisers.Add(clientID, visualizer);
        }
        
        private void DespawnClientVisualizer(uint clientID)
        {
            if (_visualisers.Remove(clientID, out var visualizer))
                Destroy(visualizer.gameObject);
        }

        #endregion
    }
}

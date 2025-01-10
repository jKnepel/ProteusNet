using jKnepel.ProteusNet.Components;
using System;
using UnityEngine;

namespace jKnepel.ProteusNet.Samples
{
    [Serializable]
    public class ClientVisualizer : NetworkBehaviour
    {
        [SerializeField] private GameObject visualizer;
        [SerializeField] private new Renderer renderer;
        [SerializeField] private TMPro.TMP_Text usernameObject;
        [SerializeField] private Material material;
        
        private static readonly int Color = Shader.PropertyToID("_Color");

        public override void OnNetworkSpawned()
        {
            if (IsAuthor)
            {
                visualizer.SetActive(false);
                return;
            }

            UpdateVisualizer();
            if (IsServer)
                NetworkManager.Server.OnRemoteClientUpdated += ClientUpdated;
            else
                NetworkManager.Client.OnRemoteClientUpdated += ClientUpdated;
        }

        public override void OnNetworkDespawned()
        {
            if (IsServer)
                NetworkManager.Server.OnRemoteClientUpdated -= ClientUpdated;
            else
                NetworkManager.Client.OnRemoteClientUpdated -= ClientUpdated;
        }

        private void Update()
        {
            if (!IsAuthor)
                return;

            var cam = Camera.main;
            if (cam && cam.transform.hasChanged)
            {
                var trf = cam.transform;
                transform.SetPositionAndRotation(trf.position, trf.rotation);
                trf.hasChanged = false;
            }
        }

        private void ClientUpdated(uint clientID)
        {
            if (clientID == AuthorID)
                UpdateVisualizer();
        }

        private void UpdateVisualizer()
        {
            var author = IsServer 
                ? NetworkManager.Server.ConnectedClients[AuthorID] 
                : NetworkManager.Client.ConnectedClients[AuthorID];
            
            name = $"{author.ID}#{author.Username}";
            usernameObject.text = author.Username;
            usernameObject.color = author.UserColour;
            if (material)
                renderer.material = Instantiate(material);
            renderer.material.SetColor(Color, author.UserColour);
        }
    }
}

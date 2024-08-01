using UnityEngine;

namespace jKnepel.ProteusNet.Networking.Transporting
{
    [CreateAssetMenu(fileName = "UnityTransportConfiguration", menuName = "ProteusNet/UnityTransportConfiguration")]
    public class UnityTransportConfiguration : TransportConfiguration
    {
        public UnityTransportConfiguration()
        {
            Settings = new();
        }
        
        public override string TransportName => "UnityTransport";
        public override Transport GetTransport()
        {
            return new UnityTransport(Settings);
        }
    }
}

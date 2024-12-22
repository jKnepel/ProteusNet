using jKnepel.ProteusNet.Networking;
using jKnepel.ProteusNet.Networking.Packets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace jKnepel.ProteusNet.Components
{
    public enum ETransformType
    {
        Transform,
        Rigidbody
    }
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("ProteusNet/Network Transform")]
    public class NetworkTransform : MonoBehaviour
    {
        private struct TransformSnapshot
        {
            public uint Tick;
            public DateTime Timestamp;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;
        }

        private class TargetTransform
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;
        }
        
        #region fields and properties
        
        [SerializeField] private ENetworkChannel networkChannel = ENetworkChannel.UnreliableOrdered;
        
        [SerializeField] private ETransformType type;
        public ETransformType Type
        {
            get => type;
            set
            {
                if (type == value || NetworkObject.IsSpawned) return;
                type = value;
                // TODO : synchronize type across network ?
                // TODO : add component type configuration (CharacterController)
            }
        }
        
        [SerializeField] private bool synchronizePosition = true;
        [SerializeField] private bool synchronizeRotation = true;
        [SerializeField] private bool synchronizeScale = true;
        
        [SerializeField] private bool snapPosition = true;
        [SerializeField] private float positionSnapThreshold = 1;
        [SerializeField] private bool snapRotation = true;
        [SerializeField] private float rotationSnapThreshold = 90;
        [SerializeField] private bool snapScale = true;
        [SerializeField] private float scaleSnapThreshold = 1;
        
        [SerializeField] private bool useInterpolation = true;
        [SerializeField] private float interpolationInterval = .05f;

        [SerializeField] private bool useExtrapolation = true;
        [SerializeField] private float extrapolationInterval = .2f;
        
        private float moveMult = 30; // TODO : calculate this
        // TODO : add hermite interpolation for rigidbodies
        
        private NetworkObject _networkObject;
        private Rigidbody _rigidbody;
        
        private readonly List<TransformSnapshot> _receivedSnapshots = new();
        // TODO : cleanup unused snapshots

        public NetworkObject NetworkObject => _networkObject;
        public MonoNetworkManager NetworkManager => _networkObject.NetworkManager;
        
        #endregion
        
        #region lifecycle

        private void Awake()
        {
            _networkObject = GetComponent<NetworkObject>();
            NetworkIDUpdated();
            
            switch (Type)
            {
                case ETransformType.Transform:
                    _rigidbody = null;
                    break;
                case ETransformType.Rigidbody:
                    if (!gameObject.TryGetComponent(out _rigidbody))
                        Type = ETransformType.Transform;
                    break;
            }
        }
        
        private void Reset()
        {
            if (transform.TryGetComponent(out _rigidbody))
                type = ETransformType.Rigidbody;
            else
                type = ETransformType.Transform;
            
            // TODO : reset other values
        }

        private void Update()
        {
            if (!_networkObject.IsSpawned || _receivedSnapshots.Count == 0)
                return;
            
            UpdateTransform();
        }

        #endregion
        
        #region private methods

        private void NetworkIDUpdated()
        {
            // TODO : update whenever network manager changes
            NetworkManager.OnTickStarted += SendTransformUpdate;
        }
        
        private void SendTransformUpdate(uint _)
        {
            if (!_networkObject.IsSpawned || !NetworkManager.IsServer) 
                return;

            var packet = new TransformPacket.Builder(NetworkObject.ObjectIdentifier);
            
            var trf = transform;
            if (synchronizePosition)
                packet.WithPosition(trf.localPosition);
            if (synchronizeRotation)
                packet.WithRotation(trf.localRotation);
            if (synchronizeScale)
                packet.WithScale(trf.localScale);
            if (Type == ETransformType.Rigidbody)
                packet.WithLinearVelocity(_rigidbody.velocity).WithAngularVelocity(_rigidbody.angularVelocity);

            var build = packet.Build();
            if (build.NumberOfValues > 0)
                NetworkManager.Server.SendTransformUpdate(this, build);
        }

        internal void ReceiveTransformUpdate(TransformPacket packet, uint tick, DateTime timestamp)
        {
            var trf = transform;
            _receivedSnapshots.Add(new()
            {
                Tick = tick,
                Timestamp = timestamp,
                Position = packet.Position ?? trf.localPosition,
                Rotation = packet.Rotation ?? trf.localRotation,
                Scale = packet.Scale ?? trf.localScale,
                LinearVelocity = packet.LinearVelocity ?? Vector3.zero,
                AngularVelocity = packet.AngularVelocity ?? Vector3.zero
            });
        }

        private void UpdateTransform()
        {
            var target = GetTargetTransform();
            Debug.Log(target);
            
            var trf = transform;
            if (synchronizePosition)
            {
                if (snapPosition && Vector3.Distance(trf.localPosition, target.Position) >= positionSnapThreshold)
                    trf.localPosition = target.Position;
                else
                    trf.localPosition = Vector3.MoveTowards(trf.localPosition, target.Position, Time.deltaTime * moveMult);
            }

            if (synchronizeRotation)
            {
                if (snapRotation && Quaternion.Angle(trf.localRotation, target.Rotation) >= rotationSnapThreshold)
                    trf.localRotation = target.Rotation;
                else
                    trf.localRotation = Quaternion.RotateTowards(trf.localRotation, target.Rotation, Time.deltaTime * moveMult);
            }

            if (synchronizeScale)
            {
                if (snapScale && Vector3.Distance(trf.localScale, target.Scale) >= scaleSnapThreshold)
                    trf.localScale = target.Scale;
                else
                    trf.localScale = Vector3.MoveTowards(trf.localScale, target.Scale, Time.deltaTime * moveMult);
            }

            if (Type == ETransformType.Rigidbody)
            {
                _rigidbody.velocity = target.LinearVelocity;
                _rigidbody.angularVelocity = target.AngularVelocity;
            }
        }

        private TargetTransform GetTargetTransform()
        {
            var renderingTime = DateTime.Now;

            if (_receivedSnapshots.Count >= 2)
            {
                if (useInterpolation)
                {   // use interpolation if enough snapshots
                    renderingTime = DateTime.Now.AddSeconds(-interpolationInterval);
                    // TODO : add rate multiplier depending on length of interpolation queue

                    if (_receivedSnapshots[^1].Timestamp >= renderingTime)
                    {
                        TargetTransform target = default;
                        for (var i = 2; i <= _receivedSnapshots.Count; i++)
                        {
                            var nextSnapshot = _receivedSnapshots[^i];
                            if (nextSnapshot.Timestamp > renderingTime) continue;
                            target = LinearInterpolateSnapshots(nextSnapshot, _receivedSnapshots[^(i - 1)], renderingTime);
                            break;
                        }
                        return target;
                    }
                }

                if (useExtrapolation && (renderingTime - _receivedSnapshots[^1].Timestamp).TotalSeconds <= extrapolationInterval)
                {   // use extrapolation if enough snapshots and within interval
                    return LinearExtrapolateSnapshots(_receivedSnapshots[^2], _receivedSnapshots[^1], renderingTime);
                }
            }
            
            // if no interpolation or extrapolation available
            var snapshot = _receivedSnapshots[^1];
            return new()
            {
                Position = snapshot.Position,
                Rotation = snapshot.Rotation,
                Scale = snapshot.Scale
            };
        }
        
        private static TargetTransform LinearInterpolateSnapshots(TransformSnapshot left, TransformSnapshot right, DateTime time)
        {
            var t = (float)((time - left.Timestamp) / (right.Timestamp - left.Timestamp));
            t = Mathf.Clamp01(t);
            
            return new()
            {
                Position = Vector3.Lerp(left.Position, right.Position, t),
                Rotation = Quaternion.Lerp(left.Rotation, right.Rotation, t),
                Scale = Vector3.Lerp(left.Scale, right.Scale, t),
                LinearVelocity = Vector3.Lerp(left.LinearVelocity, right.LinearVelocity, t),
                AngularVelocity = Vector3.Lerp(left.AngularVelocity, right.AngularVelocity, t)
            };
        }

        private static TargetTransform LinearExtrapolateSnapshots(TransformSnapshot left, TransformSnapshot right, DateTime time)
        {
            var deltaTime = (float)(right.Timestamp - left.Timestamp).TotalSeconds;
            
            /*
            if (deltaTime == 0)
            {   // TODO : temporary fix for doubly received packets on focus
                return new()
                {
                    Position = right.Position,
                    Rotation = right.Rotation,
                    Scale = right.Scale,
                    LinearVelocity = right.LinearVelocity,
                    AngularVelocity = right.AngularVelocity
                };
            }
            */
            
            var extrapolateTime = (float)(time - right.Timestamp).TotalSeconds;
            
            var deltaRot = right.Rotation * Quaternion.Inverse(left.Rotation);
            var targetRot = right.Rotation * Quaternion.Slerp(Quaternion.identity, deltaRot, extrapolateTime / deltaTime);
            
            var targetPos = LinearExtrapolateVector3(left.Position, right.Position, deltaTime, extrapolateTime);
            var targetScale = LinearExtrapolateVector3(left.Scale, right.Scale, deltaTime, extrapolateTime);
            var targetLinVel = LinearExtrapolateVector3(left.LinearVelocity, right.LinearVelocity, deltaTime, extrapolateTime);
            var targetAngVel = LinearExtrapolateVector3(left.AngularVelocity, right.AngularVelocity, deltaTime, extrapolateTime);
            
            return new()
            {
                Position = targetPos,
                Rotation = targetRot,
                Scale = targetScale,
                LinearVelocity = targetLinVel,
                AngularVelocity = targetAngVel
            };
        }

        private static Vector3 LinearExtrapolateVector3(Vector3 left, Vector3 right, float deltaTime, float extrapolateTime)
        {
            var deltaVector = (right - left) / deltaTime;
            var targetVector = right + deltaVector * extrapolateTime;
            return targetVector;
        }

        private static bool IsVector3NaN(Vector3 vector)
        {
            return float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z);
        }
        
        #endregion
    }
}
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
        private class TransformSnapshot
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
            
            public TargetTransform() {}
            public TargetTransform(TransformSnapshot snapshot)
            {
                Position = snapshot.Position;
                Rotation = snapshot.Rotation;
                Scale = snapshot.Scale;
                LinearVelocity = snapshot.LinearVelocity;
                AngularVelocity = snapshot.AngularVelocity;
            }
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

        private const float MOVE_MULT = 30;
        private const float ROTATE_MULT = 90;

        private NetworkObject _networkObject;
        private Rigidbody _rigidbody;

        private Vector3 _lastPosition = Vector3.zero;
        private Quaternion _lastRotation = Quaternion.identity;
        private Vector3 _lastScale = Vector3.zero;
        private readonly List<TransformSnapshot> _receivedSnapshots = new();

        public NetworkObject NetworkObject => _networkObject;
        public MonoNetworkManager NetworkManager => _networkObject.NetworkManager;
        
        // TODO : add hermite interpolation
        // TODO : extra/interpolate based on multiple snapshots
        // TODO : cleanup unused snapshots
        
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
            var localPosition = trf.localPosition;
            var localRotation = trf.localRotation;
            var localScale = trf.localScale;
            
            if (synchronizePosition && localPosition != _lastPosition)
            {
                packet.WithPosition(localPosition);
                _lastPosition = localPosition;
            }
            if (synchronizeRotation && localRotation != _lastRotation)
            {
                packet.WithRotation(localRotation);
                _lastRotation = localRotation;
            }
            if (synchronizeScale && localScale != _lastScale)
            {
                packet.WithScale(localScale);
                _lastScale = localScale;
            }
            if (Type == ETransformType.Rigidbody)
                packet.WithLinearVelocity(_rigidbody.velocity).WithAngularVelocity(_rigidbody.angularVelocity);

            if (packet.NumberOfValues > 0)
                NetworkManager.Server.SendTransformUpdate(this, packet.Build());
        }

        internal void ReceiveTransformUpdate(TransformPacket packet, uint tick, DateTime timestamp)
        {
            var lastSnapshot = _receivedSnapshots.Count > 0 ? _receivedSnapshots[^1] : null;
            var trf = transform;
            _receivedSnapshots.Add(new()
            {
                Tick = tick,
                Timestamp = timestamp,
                Position = packet.Position ?? lastSnapshot?.Position ?? trf.localPosition,
                Rotation = packet.Rotation ?? lastSnapshot?.Rotation ?? trf.localRotation,
                Scale = packet.Scale ?? lastSnapshot?.Scale ?? trf.localScale,
                LinearVelocity = packet.LinearVelocity ?? Vector3.zero,
                AngularVelocity = packet.AngularVelocity ?? Vector3.zero
            });
        }

        private void UpdateTransform()
        {
            var target = GetTargetTransform();
            if (target == null)
                return;
            
            var trf = transform;
            if (synchronizePosition)
            {
                if (snapPosition && Vector3.Distance(trf.localPosition, target.Position) >= positionSnapThreshold)
                    trf.localPosition = target.Position;
                else
                    trf.localPosition = Vector3.MoveTowards(trf.localPosition, target.Position, Time.deltaTime * MOVE_MULT);
            }

            if (synchronizeRotation)
            {
                if (snapRotation && Quaternion.Angle(trf.localRotation, target.Rotation) >= rotationSnapThreshold)
                    trf.localRotation = target.Rotation;
                else
                    trf.localRotation = Quaternion.RotateTowards(trf.localRotation, target.Rotation, Time.deltaTime * ROTATE_MULT);
            }

            if (synchronizeScale)
            {
                if (snapScale && Vector3.Distance(trf.localScale, target.Scale) >= scaleSnapThreshold)
                    trf.localScale = target.Scale;
                else
                    trf.localScale = Vector3.MoveTowards(trf.localScale, target.Scale, Time.deltaTime * MOVE_MULT);
            }

            if (Type == ETransformType.Rigidbody)
            {
                _rigidbody.velocity = target.LinearVelocity;
                _rigidbody.angularVelocity = target.AngularVelocity;
            }
        }

        private TargetTransform GetTargetTransform()
        {
            if (useInterpolation)
            {
                var renderingTime = DateTime.Now.AddSeconds(-interpolationInterval);
                var (left, right) = FindAdjacentSnapshots(renderingTime);
                
                if (left == null && right == null)
                {   // packets are still newer than rendering time, dont render yet
                    return null;
                }
                if (right == null)
                {   // too many packets have been dropped, return newest one
                    return new(_receivedSnapshots[^1]);
                }
                if (useExtrapolation)
                {   // extrapolate from rendering time
                    return LinearExtrapolate(left, right, renderingTime.AddSeconds(extrapolationInterval));
                }
                
                // interpolate at rendering time
                return LinearInterpolate(left, right, renderingTime);
            }

            if (useExtrapolation && _receivedSnapshots.Count >= 2)
            {   // use extrapolation on newest snapshots without interpolation
                return LinearExtrapolate(_receivedSnapshots[^2], _receivedSnapshots[^1], DateTime.Now.AddSeconds(extrapolationInterval));
            }
            
            // return newest packet if no extra-/interpolation
            return new(_receivedSnapshots[^1]);
        }

        private (TransformSnapshot, TransformSnapshot) FindAdjacentSnapshots(DateTime timestamp)
        {
            TransformSnapshot left = null;
            TransformSnapshot right = null;

            for (var i = _receivedSnapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = _receivedSnapshots[i];
                if (snapshot.Timestamp > timestamp) continue;
                left = snapshot;
                right = i + 1 < _receivedSnapshots.Count ? _receivedSnapshots[i + 1] : null;
                break;
            }

            return (left, right);
        }
        
        private static TargetTransform LinearInterpolate(TransformSnapshot left, TransformSnapshot right, DateTime time)
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

        private static TargetTransform LinearExtrapolate(TransformSnapshot left, TransformSnapshot right, DateTime time)
        {
            var extrapolateTime = (float)(time - right.Timestamp).TotalSeconds;
            var deltaTime = (float)(right.Timestamp - left.Timestamp).TotalSeconds;
            deltaTime = Mathf.Max(deltaTime, 0.001f); // prevents NaN when snapshots were received in the same tick
            
            var targetPos = LinearExtrapolate(left.Position, right.Position, deltaTime, extrapolateTime);
            var targetScale = LinearExtrapolate(left.Scale, right.Scale, deltaTime, extrapolateTime);
            var targetRot = LinearExtrapolate(left.Rotation, right.Rotation, deltaTime, extrapolateTime);
            var targetLinVel = LinearExtrapolate(left.LinearVelocity, right.LinearVelocity, deltaTime, extrapolateTime);
            var targetAngVel = LinearExtrapolate(left.AngularVelocity, right.AngularVelocity, deltaTime, extrapolateTime);
            //Debug.Log($"{deltaTime} {left.Tick} {right.Tick}");
            //Debug.Log($"{deltaTime} {extrapolateTime} {left.Position} {right.Position} {targetPos}");
            
            return new()
            {
                Position = targetPos,
                Rotation = targetRot,
                Scale = targetScale,
                LinearVelocity = targetLinVel,
                AngularVelocity = targetAngVel
            };
        }

        private static Vector3 LinearExtrapolate(Vector3 left, Vector3 right, float deltaTime, float extrapolateTime)
        {
            var deltaVector = (right - left) / deltaTime;
            var targetVector = right + deltaVector * extrapolateTime;
            return targetVector;
        }
        
        private static Quaternion LinearExtrapolate(Quaternion left, Quaternion right, float deltaTime, float extrapolateTime)
        {
            var t = 1 + extrapolateTime / deltaTime;
            return Quaternion.Slerp(left, right, t);
        }
        
        #endregion
    }
}
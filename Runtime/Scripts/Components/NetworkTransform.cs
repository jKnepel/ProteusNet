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

    public enum ETransformValues
    {
        Nothing = 0,
        PositionX = 1,
        PositionY = 2,
        PositionZ = 4,
        PositionAll = PositionX | PositionY | PositionZ,
        RotationX = 8,
        RotationY = 16,
        RotationZ = 32,
        RotationAll = RotationX | RotationY | RotationZ,
        ScaleX = 64,
        ScaleY = 128,
        ScaleZ = 256,
        ScaleAll = ScaleX | ScaleY | ScaleZ,
        All = PositionAll | RotationAll | ScaleAll
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

        [SerializeField] private ETransformValues synchronizeValues = ETransformValues.All;
        
        [SerializeField] private bool snapPosition = true;
        [SerializeField] private float snapPositionThreshold = 1;
        [SerializeField] private bool snapRotation = true;
        [SerializeField] private float snapRotationThreshold = 90;
        [SerializeField] private bool snapScale = true;
        [SerializeField] private float snapScaleThreshold = 1;
        
        [SerializeField] private float moveMultiplier = 30;
        [SerializeField] private float rotateMultiplier = 90;
        [SerializeField] private bool useInterpolation = true;
        [SerializeField] private float interpolationInterval = .05f;
        [SerializeField] private bool useExtrapolation = true;
        [SerializeField] private float extrapolationInterval = .2f;

        private const float SYNCHRONIZE_TOLERANCE = 0.001f;

        private NetworkObject _networkObject;
        private Rigidbody _rigidbody;

        private (float, float, float) _lastPosition;
        private (float, float, float) _lastRotation;
        private (float, float, float) _lastScale;

        private readonly List<TransformSnapshot> _receivedSnapshots = new();

        public MonoNetworkManager NetworkManager => _networkObject.NetworkManager;
        public NetworkObject NetworkObject => _networkObject;
        
        // TODO : add hermite interpolation
        // TODO : extra-/interpolate based on multiple snapshots
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
            if (!_networkObject.IsSpawned || !NetworkManager.IsServer || synchronizeValues == ETransformValues.Nothing) 
                return;

            var packet = new TransformPacket.Builder(NetworkObject.ObjectIdentifier);
            
            var trf = transform;
            var localPosition = trf.localPosition;
            var localRotation = trf.localEulerAngles;
            var localScale = trf.localScale;

            if (synchronizeValues.HasFlag(ETransformValues.PositionX) && Math.Abs(localPosition.x - _lastPosition.Item1) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithPositionX(localPosition.x);
                _lastPosition.Item1 = localPosition.x;
            }
            if (synchronizeValues.HasFlag(ETransformValues.PositionY) && Math.Abs(localPosition.y - _lastPosition.Item2) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithPositionY(localPosition.y);
                _lastPosition.Item2 = localPosition.y;
            }
            if (synchronizeValues.HasFlag(ETransformValues.PositionZ) && Math.Abs(localPosition.z - _lastPosition.Item3) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithPositionZ(localPosition.z);
                _lastPosition.Item3 = localPosition.z;
            }
            
            if (synchronizeValues.HasFlag(ETransformValues.RotationX) && Math.Abs(localRotation.x - _lastRotation.Item1) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithRotationX(localRotation.x);
                _lastRotation.Item1 = localRotation.x;
            }
            if (synchronizeValues.HasFlag(ETransformValues.RotationY) && Math.Abs(localRotation.y - _lastRotation.Item2) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithRotationY(localRotation.y);
                _lastRotation.Item2 = localRotation.y;
            }
            if (synchronizeValues.HasFlag(ETransformValues.RotationZ) && Math.Abs(localRotation.z - _lastRotation.Item3) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithRotationZ(localRotation.z);
                _lastRotation.Item3 = localRotation.z;
            }
            
            if (synchronizeValues.HasFlag(ETransformValues.ScaleX) && Math.Abs(localScale.x - _lastScale.Item1) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithScaleX(localScale.x);
                _lastScale.Item1 = localScale.x;
            }
            if (synchronizeValues.HasFlag(ETransformValues.ScaleY) && Math.Abs(localScale.y - _lastScale.Item2) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithScaleY(localScale.y);
                _lastScale.Item2 = localScale.y;
            }
            if (synchronizeValues.HasFlag(ETransformValues.ScaleZ) && Math.Abs(localScale.z - _lastScale.Item3) > SYNCHRONIZE_TOLERANCE)
            {
                packet.WithScaleZ(localScale.z);
                _lastScale.Item3 = localScale.z;
            }

            if (Type == ETransformType.Rigidbody)
                packet.WithRigidbody(_rigidbody.velocity, _rigidbody.angularVelocity);

            var build = packet.Build();
            if (build.Flags != TransformPacket.ETransformPacketFlag.Nothing)
                NetworkManager.Server.SendTransformUpdate(this, build, networkChannel);
        }

        internal void ReceiveTransformUpdate(TransformPacket packet, uint tick, DateTime timestamp)
        {
            var lastSnapshot = _receivedSnapshots.Count > 0 ? _receivedSnapshots[^1] : null;
            var trf = transform;
            var localPosition = trf.localPosition;
            var localRotation = trf.localEulerAngles;
            var localScale = trf.localScale;

            var position = new Vector3(
                packet.PositionX ?? lastSnapshot?.Position.x ?? localPosition.x,
                packet.PositionY ?? lastSnapshot?.Position.y ?? localPosition.y,
                packet.PositionZ ?? lastSnapshot?.Position.z ?? localPosition.z
            );
            var rotation = new Vector3(
                packet.RotationX ?? lastSnapshot?.Rotation.x ?? localRotation.x,
                packet.RotationY ?? lastSnapshot?.Rotation.y ?? localRotation.y,
                packet.RotationZ ?? lastSnapshot?.Rotation.z ?? localRotation.z
            );
            var scale = new Vector3(
                packet.ScaleX ?? lastSnapshot?.Scale.x ?? localScale.x,
                packet.ScaleY ?? lastSnapshot?.Scale.y ?? localScale.y,
                packet.ScaleZ ?? lastSnapshot?.Scale.z ?? localScale.z
            );
            
            _receivedSnapshots.Add(new()
            {
                Tick = tick,
                Timestamp = timestamp,
                Position = position,
                Rotation = Quaternion.Euler(rotation),
                Scale = scale,
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
            if (snapPosition && Vector3.Distance(trf.localPosition, target.Position) >= snapPositionThreshold)
                trf.localPosition = target.Position;
            else
                trf.localPosition = Vector3.MoveTowards(trf.localPosition, target.Position, Time.deltaTime * moveMultiplier);

            if (snapRotation && Quaternion.Angle(trf.localRotation, target.Rotation) >= snapRotationThreshold)
                trf.localRotation = target.Rotation;
            else
                trf.localRotation = Quaternion.RotateTowards(trf.localRotation, target.Rotation, Time.deltaTime * rotateMultiplier);

            if (snapScale && Vector3.Distance(trf.localScale, target.Scale) >= snapScaleThreshold)
                trf.localScale = target.Scale;
            else
                trf.localScale = Vector3.MoveTowards(trf.localScale, target.Scale, Time.deltaTime * moveMultiplier);

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

            for (var i = 0; i < _receivedSnapshots.Count; i++)
            {
                var snapshot = _receivedSnapshots[^(i + 1)];
                if (snapshot.Timestamp > timestamp) continue;
                left = snapshot;
                right = i > 0 ? _receivedSnapshots[^i] : null;
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

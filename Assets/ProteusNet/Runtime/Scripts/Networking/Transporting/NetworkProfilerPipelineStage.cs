using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;

namespace jKnepel.ProteusNet.Networking.Transporting
{
    public struct NetworkProfilerContext
    {
        public uint PacketSentCount;
        public uint PacketSentSize;
        public uint PacketReceivedCount;
        public uint PacketReceivedSize;
    }
    
    [BurstCompile]
    internal unsafe struct NetworkProfilerPipelineStage : INetworkPipelineStage
    {
        private static readonly TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunction = new(Receive);
        private static readonly TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunction = new(Send);
        private static readonly TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunction = new(InitializeConnection);

        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer,
            int staticInstanceBufferLength,
            NetworkSettings settings)
        {
            return new(
                ReceiveFunction,
                SendFunction,
                InitializeConnectionFunction,
                ReceiveCapacity: 0,
                SendCapacity: 0,
                HeaderCapacity: 0,
                SharedStateCapacity: UnsafeUtility.SizeOf<NetworkProfilerContext>());
        }

        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext networkPipelineContext,
            ref InboundRecvBuffer inboundReceiveBuffer,
            ref NetworkPipelineStage.Requests requests,
            int systemHeaderSize)
        {
            var sharedContext = (NetworkProfilerContext*)networkPipelineContext.internalSharedProcessBuffer;
            sharedContext->PacketReceivedCount++;
            sharedContext->PacketReceivedSize += (uint)(inboundReceiveBuffer.bufferLength + systemHeaderSize);
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext networkPipelineContext,
            ref InboundSendBuffer inboundSendBuffer,
            ref NetworkPipelineStage.Requests requests,
            int systemHeaderSize)
        {
            var sharedContext = (NetworkProfilerContext*)networkPipelineContext.internalSharedProcessBuffer;
            sharedContext->PacketSentCount++;
            sharedContext->PacketSentSize += (uint)(inboundSendBuffer.bufferLength + systemHeaderSize);
            return 0;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.InitializeConnectionDelegate))]
        private static void InitializeConnection(byte* staticInstanceBuffer, int staticInstanceBufferLength,
            byte* sendProcessBuffer, int sendProcessBufferLength, byte* receiveProcessBuffer, int receiveProcessBufferLength,
            byte* sharedProcessBuffer, int sharedProcessBufferLength)
        {
            var sharedContext = (NetworkProfilerContext*)sharedProcessBuffer;
            sharedContext->PacketSentCount = 0;
            sharedContext->PacketSentSize = 0;
            sharedContext->PacketReceivedCount = 0;
            sharedContext->PacketReceivedSize = 0;
        }
    }
}

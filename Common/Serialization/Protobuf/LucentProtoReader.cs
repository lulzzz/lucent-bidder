using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lucent.Common.Protobuf;

namespace Lucent.Common.Serialization.Protobuf
{
    /// <summary>
    /// Protobuf reader
    /// </summary>
    public class LucentProtoReader : ILucentReader
    {
        readonly ProtobufReader protobufReader;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="target"></param>
        /// <param name="leaveOpen"></param>
        public LucentProtoReader(Stream target, bool leaveOpen)
            => protobufReader = new ProtobufReader(target, leaveOpen);

        /// <inheritdoc/>
        public SerializationFormat Format { get => SerializationFormat.PROTOBUF; }

        /// <inheritdoc/>
        public Task<ILucentArrayReader> GetArrayReader() => 
            Task.FromResult((ILucentArrayReader)new LucentProtoArrayReader(protobufReader.GetNextMessageReader()));

        /// <inheritdoc/>
        public Task<ILucentObjectReader> GetObjectReader() => 
            Task.FromResult((ILucentObjectReader)new LucentProtoObjectReader(protobufReader.GetNextMessageReader()));

        /// <inheritdoc/>
        public async Task<PropertyId> NextAsync() => await protobufReader.ReadAsync()
                ? new PropertyId { Id = protobufReader.FieldNumber } : null;

        /// <inheritdoc/>
        public async Task<bool> NextBoolean() => await protobufReader.ReadBoolAsync();

        /// <inheritdoc/>
        public async Task<double> NextDouble() => await protobufReader.ReadDoubleAsync();

        /// <inheritdoc/>
        public async Task<int> NextInt() => await protobufReader.ReadInt32Async();

        /// <inheritdoc/>
        public async Task<long> NextLong() => await protobufReader.ReadInt64Async();

        /// <inheritdoc/>
        public async Task<float> NextSingle() => await protobufReader.ReadFloatAsync();

        /// <inheritdoc/>
        public async Task<string> NextString() => await protobufReader.ReadStringAsync();

        /// <inheritdoc/>
        public async Task<uint> NextUInt() => await protobufReader.ReadUInt32Async();

        /// <inheritdoc/>
        public async Task<ulong> NextULong() => await protobufReader.ReadUInt64Async();

        /// <inheritdoc/>
        public async Task<Guid> NextGuid() => (await protobufReader.ReadStringAsync()).DecodeGuid();
        /// <inheritdoc/>
        public async Task<DateTime> NextDateTime() => DateTime.FromFileTimeUtc(await protobufReader.ReadInt64Async());

        /// <inheritdoc/>
        public async Task<TEnum> NextEnum<TEnum>() => (TEnum)Enum.ToObject(typeof(TEnum), await protobufReader.ReadInt32Async());
        
        /// <inheritdoc/>
        public async Task<byte[]> NextObjBytes() => await protobufReader.GetNextReaderBytesAsync();

        /// <inheritdoc/>
        public async Task Skip() => await protobufReader.SkipAsync();

        /// <inheritdoc/>
        public void Dispose() => protobufReader.Close();
    }
}
using System.Threading;
using System.Threading.Tasks;
using Lucent.Common.Filters;
using Lucent.Common.OpenRTB;
using Lucent.Common.Serialization;

namespace Lucent.Common.Entities.Serializers
{
    /// <summary>
    /// 
    /// </summary>
    public class LedgerEntrySerializer : IEntitySerializer<LedgerEntry>
    {
        /// <inheritdoc />
        public LedgerEntry Read(ISerializationStreamReader serializationStreamReader)
            => ReadAsync(serializationStreamReader, CancellationToken.None).Result;

        /// <inheritdoc />
        public async Task<LedgerEntry> ReadAsync(ISerializationStreamReader serializationStreamReader, CancellationToken token)
        {
            if (!await serializationStreamReader.StartObjectAsync())
                return null;

            var entry = new LedgerEntry();

            while (await serializationStreamReader.HasMorePropertiesAsync())
            {
                var propId = serializationStreamReader.Id;
                switch (propId.Name)
                {
                    case "":
                        switch (propId.Id)
                        {
                            case 1:
                                entry.Id = await serializationStreamReader.ReadStringAsync();
                                break;
                            case 2:
                                entry.SecondaryId = await serializationStreamReader.ReadGuidAsync();
                                break;
                            case 3:
                                entry.EntryType = await serializationStreamReader.ReadAsAsync<LedgerEntryType>();
                                break;
                            case 4:
                                entry.OriginalAmount = await serializationStreamReader.ReadDoubleAsync();
                                break;
                            case 5:
                                entry.RemainingAmount = await serializationStreamReader.ReadDoubleAsync();
                                break;
                            default:
                                await serializationStreamReader.SkipAsync();
                                break;
                        }
                        break;
                    case "id":
                        entry.Id = await serializationStreamReader.ReadStringAsync();
                        break;
                    case "secondary":
                        entry.SecondaryId = await serializationStreamReader.ReadGuidAsync();
                        break;
                    case "entrytype":
                        entry.EntryType = await serializationStreamReader.ReadAsAsync<LedgerEntryType>();
                        break;
                    case "original":
                        entry.OriginalAmount = await serializationStreamReader.ReadDoubleAsync();
                        break;
                    case "remaining":
                        entry.RemainingAmount = await serializationStreamReader.ReadDoubleAsync();
                        break;
                    default:
                        await serializationStreamReader.SkipAsync();
                        break;
                }
            }

            return entry;
        }

        /// <inheritdoc />
        public void Write(ISerializationStreamWriter serializationStreamWriter, LedgerEntry instance)
            => WriteAsync(serializationStreamWriter, instance, CancellationToken.None).Wait();

        /// <inheritdoc />
        public async Task WriteAsync(ISerializationStreamWriter serializationStreamWriter, LedgerEntry instance, CancellationToken token)
        {
            if (instance != null)
            {
                await serializationStreamWriter.StartObjectAsync();
                await serializationStreamWriter.WriteAsync(new PropertyId { Id = 1, Name = "id" }, instance.Id);
                await serializationStreamWriter.WriteAsync(new PropertyId { Id = 2, Name = "secondary" }, instance.SecondaryId);
                await serializationStreamWriter.WriteAsync(new PropertyId { Id = 3, Name = "entrytype" }, instance.EntryType);
                await serializationStreamWriter.WriteAsync(new PropertyId { Id = 4, Name = "original" }, instance.OriginalAmount);
                await serializationStreamWriter.WriteAsync(new PropertyId { Id = 5, Name = "remaining" }, instance.RemainingAmount);
                await serializationStreamWriter.EndObjectAsync();
                await serializationStreamWriter.FlushAsync();
            }
        }
    }
}
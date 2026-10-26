#nullable enable
using CIE.AspNetCore.Authentication.Saml.IdP;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CIE.AspNetCore.Authentication.Helpers
{
    internal sealed class CieMetadataCache
    {
        private static readonly XmlSerializer Serializer = new(typeof(EntityDescriptorType));
        private readonly SemaphoreSlim _gate = new(1, 1);
        private EntityDescriptorType? _cached;
        private string? _url;
        private DateTimeOffset _expires;

        public async Task<EntityDescriptorType> GetAsync(IHttpClientFactory factory, string url, bool cache, TimeProvider clock, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!cache) return await DownloadAsync(factory, url, cancellationToken).ConfigureAwait(false);
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_cached != null && _url == url && _expires > clock.GetUtcNow()) return _cached;
                var downloaded = await DownloadAsync(factory, url, cancellationToken).ConfigureAwait(false);
                _cached = downloaded;
                _url = url;
                _expires = clock.GetUtcNow().AddHours(1);
                return downloaded;
            }
            finally { _gate.Release(); }
        }

        private static async Task<EntityDescriptorType> DownloadAsync(IHttpClientFactory factory, string url, CancellationToken cancellationToken)
        {
            using var client = factory.CreateClient("cie");
            var xml = await SamlInput.ReadMetadataAsync(client, url, cancellationToken).ConfigureAwait(false);
            using var reader = SamlInput.CreateXmlReader(xml, SamlInput.MaxMetadataBytes);
            return Serializer.Deserialize(reader) as EntityDescriptorType
                ?? throw new InvalidDataException("Identity provider metadata is invalid.");
        }
    }
}

#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace CIE.AspNetCore.Authentication.Helpers
{
    // Bounds apply before logging or deserializing login messages.
    internal static class SamlInput
    {
        internal const int MaxMessageBytes = 1024 * 1024;
        internal const int MaxMetadataBytes = 4 * 1024 * 1024;

        public static string DecodePost(string value) => Decode(value, false);
        public static string DecodeRedirect(string value) => Decode(value, true);

        private static string Decode(string value, bool compressed)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 4 * ((MaxMessageBytes + 2) / 3))
                throw new InvalidDataException("SAML message exceeds the permitted size or is empty.");
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length > MaxMessageBytes)
                throw new InvalidDataException("SAML message exceeds the permitted size.");
            if (compressed)
            {
                using var source = new MemoryStream(bytes);
                using var inflater = new DeflateStream(source, CompressionMode.Decompress);
                using var output = new MemoryStream();
                var buffer = new byte[8192];
                int count;
                while ((count = inflater.Read(buffer, 0, Math.Min(buffer.Length, MaxMessageBytes - (int)output.Length + 1))) > 0)
                {
                    if (output.Length + count > MaxMessageBytes)
                        throw new InvalidDataException("SAML message exceeds the permitted size.");
                    output.Write(buffer, 0, count);
                }
                bytes = output.ToArray();
            }
            var xml = new UTF8Encoding(false, true).GetString(bytes);
            using var reader = CreateXmlReader(xml, MaxMessageBytes);
            while (reader.Read()) { }
            return xml;
        }

        public static XmlReader CreateXmlReader(string xml, int limit) => XmlReader.Create(new StringReader(xml), new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = limit
        });

        public static async Task<string> ReadMetadataAsync(HttpClient client, string url, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException("Identity provider metadata requires an HTTPS URL without user information.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri is Uri finalUri && finalUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("Identity provider metadata requires HTTPS.");
            if (response.Content.Headers.ContentLength > MaxMetadataBytes)
                throw new InvalidDataException("Identity provider metadata exceeds the permitted size.");
            using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, MaxMetadataBytes - (int)output.Length + 1)), timeout.Token).ConfigureAwait(false)) > 0)
            {
                if (output.Length + count > MaxMetadataBytes)
                    throw new InvalidDataException("Identity provider metadata exceeds the permitted size.");
                output.Write(buffer, 0, count);
            }
            // Preserve HttpClient's charset/BOM handling after enforcing the byte limit.
            using var boundedContent = new ByteArrayContent(output.ToArray());
            boundedContent.Headers.ContentType = response.Content.Headers.ContentType;
            return await boundedContent.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        }
    }
}

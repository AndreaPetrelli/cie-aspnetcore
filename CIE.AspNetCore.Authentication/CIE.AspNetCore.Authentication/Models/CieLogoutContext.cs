using CIE.AspNetCore.Authentication.Saml;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace CIE.AspNetCore.Authentication.Models
{
    /// <summary>A server-captured snapshot of the original CIE session, independent of later logins.</summary>
    public sealed class CieLogoutContext
    {
        public const string ParameterName = "Cie.LogoutContext";
        public string SubjectNameIdentifierXml { get; set; }
        public string SessionIndex { get; set; }

        /// <summary>Reads the existing protected CIE cookie before the application signs out.</summary>
        public static CieLogoutContext Capture(HttpRequest request, CieOptions options)
        {
            var properties = new AuthenticationProperties();
            properties.Load(request, options.StateDataFormat);
            var snapshot = new CieLogoutContext
            {
                SubjectNameIdentifierXml = SamlHandler.SerializeMessage(properties.GetSubjectNameIdentifier()),
                SessionIndex = properties.GetSessionIndex()
            };
            snapshot.Apply(new AuthenticationProperties());
            return snapshot;
        }

        internal void Apply(AuthenticationProperties properties)
        {
            var nameId = SamlHandler.DeserializeMessage<NameIDType>(SubjectNameIdentifierXml);
            if (string.IsNullOrWhiteSpace(nameId?.Value) || string.IsNullOrWhiteSpace(SessionIndex))
                throw new AuthenticationFailureException("Invalid CIE logout context.");
            properties.SetSubjectNameIdentifier(nameId);
            properties.SetSessionIndex(SessionIndex);
        }
    }
}

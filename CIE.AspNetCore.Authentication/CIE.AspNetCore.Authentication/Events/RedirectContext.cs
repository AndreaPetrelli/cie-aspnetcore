﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using CIE.AspNetCore.Authentication.Models;

namespace CIE.AspNetCore.Authentication.Events
{
    public class RedirectContext : PropertiesContext<CieOptions>
    {
        /// <summary>
        /// Creates a new context object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="scheme"></param>
        /// <param name="options"></param>
        /// <param name="properties"></param>
        /// <param name="signedProtocolMessage"></param>
        public RedirectContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CieOptions options,
            AuthenticationProperties properties,
            object signedProtocolMessage)
            : base(context, scheme, options, properties) 
        {
            SignedProtocolMessage = signedProtocolMessage;
        }

        /// <summary>
        /// The message used to compose the redirect.
        /// </summary>
        public object SignedProtocolMessage { get; set; }

        /// <summary>
        /// If true, will skip any default logic for this redirect.
        /// </summary>
        public bool Handled { get; private set; }

        /// <summary>
        /// Skips any default logic for this redirect.
        /// </summary>
        public void HandleResponse() => Handled = true;
    }
}

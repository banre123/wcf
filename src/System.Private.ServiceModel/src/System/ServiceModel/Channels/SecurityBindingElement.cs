// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Security;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Xml;

namespace System.ServiceModel.Channels
{
    public abstract class SecurityBindingElement : BindingElement
    {
        internal const bool defaultIncludeTimestamp = true;
        internal const bool defaultAllowInsecureTransport = false;
        internal const bool defaultRequireSignatureConfirmation = false;
        internal const bool defaultEnableUnsecuredResponse = false;
        internal const bool defaultProtectTokens = false;


        private SupportingTokenParameters _endpointSupportingTokenParameters;
        private bool _includeTimestamp;

        private LocalClientSecuritySettings _localClientSettings;

        private MessageSecurityVersion _messageSecurityVersion;
        private SecurityHeaderLayout _securityHeaderLayout;
        private bool _protectTokens = defaultProtectTokens;

        internal SecurityBindingElement()
            : base()
        {
            _messageSecurityVersion = MessageSecurityVersion.Default;
            _includeTimestamp = defaultIncludeTimestamp;
            _localClientSettings = new LocalClientSecuritySettings();
            _endpointSupportingTokenParameters = new SupportingTokenParameters();
            _securityHeaderLayout = SecurityProtocolFactory.defaultSecurityHeaderLayout;
            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement is not supported");
        }

        internal SecurityBindingElement(SecurityBindingElement elementToBeCloned)
            : base(elementToBeCloned)
        {
            if (elementToBeCloned == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("elementToBeCloned");

            _includeTimestamp = elementToBeCloned._includeTimestamp;
            _messageSecurityVersion = elementToBeCloned._messageSecurityVersion;
            _securityHeaderLayout = elementToBeCloned._securityHeaderLayout;
            _endpointSupportingTokenParameters = elementToBeCloned._endpointSupportingTokenParameters.Clone();
            _localClientSettings = elementToBeCloned._localClientSettings.Clone();
            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement cloning not supported.");
        }

        public SupportingTokenParameters EndpointSupportingTokenParameters
        {
            get
            {
                return _endpointSupportingTokenParameters;
            }
        }

        public SecurityHeaderLayout SecurityHeaderLayout
        {
            get
            {
                return _securityHeaderLayout;
            }
            set
            {
                if (!SecurityHeaderLayoutHelper.IsDefined(value))
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));

                _securityHeaderLayout = value;
            }
        }

        public MessageSecurityVersion MessageSecurityVersion
        {
            get
            {
                return _messageSecurityVersion;
            }
            set
            {
                if (value == null)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
                _messageSecurityVersion = value;
            }
        }

        public bool IncludeTimestamp
        {
            get
            {
                return _includeTimestamp;
            }
            set
            {
                _includeTimestamp = value;
            }
        }

        public LocalClientSecuritySettings LocalClientSettings
        {
            get
            {
                return _localClientSettings;
            }
        }

        internal virtual bool SessionMode
        {
            get { return false; }
        }

        internal virtual bool SupportsDuplex
        {
            get { return false; }
        }

        internal virtual bool SupportsRequestReply
        {
            get { return false; }
        }

        private void GetSupportingTokensCapabilities(ICollection<SecurityTokenParameters> parameters, out bool supportsClientAuth, out bool supportsWindowsIdentity)
        {
            supportsClientAuth = false;
            supportsWindowsIdentity = false;
            foreach (SecurityTokenParameters p in parameters)
            {
                if (p.SupportsClientAuthentication)
                    supportsClientAuth = true;
                if (p.SupportsClientWindowsIdentity)
                    supportsWindowsIdentity = true;
            }
        }

        private void GetSupportingTokensCapabilities(SupportingTokenParameters requirements, out bool supportsClientAuth, out bool supportsWindowsIdentity)
        {
            supportsClientAuth = false;
            supportsWindowsIdentity = false;
            bool tmpSupportsClientAuth;
            bool tmpSupportsWindowsIdentity;
            this.GetSupportingTokensCapabilities(requirements.Endorsing, out tmpSupportsClientAuth, out tmpSupportsWindowsIdentity);
            supportsClientAuth = supportsClientAuth || tmpSupportsClientAuth;
            supportsWindowsIdentity = supportsWindowsIdentity || tmpSupportsWindowsIdentity;

            this.GetSupportingTokensCapabilities(requirements.SignedEndorsing, out tmpSupportsClientAuth, out tmpSupportsWindowsIdentity);
            supportsClientAuth = supportsClientAuth || tmpSupportsClientAuth;
            supportsWindowsIdentity = supportsWindowsIdentity || tmpSupportsWindowsIdentity;

            this.GetSupportingTokensCapabilities(requirements.SignedEncrypted, out tmpSupportsClientAuth, out tmpSupportsWindowsIdentity);
            supportsClientAuth = supportsClientAuth || tmpSupportsClientAuth;
            supportsWindowsIdentity = supportsWindowsIdentity || tmpSupportsWindowsIdentity;
        }

        internal void GetSupportingTokensCapabilities(out bool supportsClientAuth, out bool supportsWindowsIdentity)
        {
            this.GetSupportingTokensCapabilities(this.EndpointSupportingTokenParameters, out supportsClientAuth, out supportsWindowsIdentity);
        }

        protected static void SetIssuerBindingContextIfRequired(SecurityTokenParameters parameters, BindingContext issuerBindingContext)
        {
            throw ExceptionHelper.PlatformNotSupported("SetIssuerBindingContextIfRequired is not supported.");
        }

        internal bool RequiresChannelDemuxer(SecurityTokenParameters parameters)
        {
            throw ExceptionHelper.PlatformNotSupported("RequiresChannelDemuxer is not supported.");
        }

        internal virtual bool RequiresChannelDemuxer()
        {
            foreach (SecurityTokenParameters parameters in EndpointSupportingTokenParameters.Endorsing)
            {
                if (RequiresChannelDemuxer(parameters))
                {
                    return true;
                }
            }
            foreach (SecurityTokenParameters parameters in EndpointSupportingTokenParameters.SignedEndorsing)
            {
                if (RequiresChannelDemuxer(parameters))
                {
                    return true;
                }
            }

            return false;
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");

            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement.BuildChannelFactory is not supported.");
        }

        protected abstract IChannelFactory<TChannel> BuildChannelFactoryCore<TChannel>(BindingContext context);

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");

            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement.CanBuildChannelFactory is not supported.");
        }

        private bool CanBuildSessionChannelFactory<TChannel>(BindingContext context)
        {
            if (!(context.CanBuildInnerChannelFactory<IRequestChannel>()
                || context.CanBuildInnerChannelFactory<IRequestSessionChannel>()
                || context.CanBuildInnerChannelFactory<IDuplexChannel>()
                || context.CanBuildInnerChannelFactory<IDuplexSessionChannel>()))
            {
                return false;
            }

            if (typeof(TChannel) == typeof(IRequestSessionChannel))
            {
                return (context.CanBuildInnerChannelFactory<IRequestChannel>() || context.CanBuildInnerChannelFactory<IRequestSessionChannel>());
            }
            else if (typeof(TChannel) == typeof(IDuplexSessionChannel))
            {
                return (context.CanBuildInnerChannelFactory<IDuplexChannel>() || context.CanBuildInnerChannelFactory<IDuplexSessionChannel>());
            }
            else
            {
                return false;
            }
        }

        public virtual void SetKeyDerivation(bool requireDerivedKeys)
        {
            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement.SetKeyDerivation is not supported.");
        }

        internal virtual bool IsSetKeyDerivation(bool requireDerivedKeys)
        {
            if (!this.EndpointSupportingTokenParameters.IsSetKeyDerivation(requireDerivedKeys))
                return false;

            return true;
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (context == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
            }
            if (typeof(T) == typeof(ISecurityCapabilities))
            {
                return (T)(object)GetSecurityCapabilities(context);
            }
            else if (typeof(T) == typeof(IdentityVerifier))
            {
                return (T)(object)_localClientSettings.IdentityVerifier;
            }
            else
            {
                return context.GetInnerProperty<T>();
            }
        }

        internal abstract ISecurityCapabilities GetIndividualISecurityCapabilities();

        private ISecurityCapabilities GetSecurityCapabilities(BindingContext context)
        {
            ISecurityCapabilities thisSecurityCapability = this.GetIndividualISecurityCapabilities();
            ISecurityCapabilities lowerSecurityCapability = context.GetInnerProperty<ISecurityCapabilities>();
            if (lowerSecurityCapability == null)
            {
                return thisSecurityCapability;
            }
            else
            {
                bool supportsClientAuth = thisSecurityCapability.SupportsClientAuthentication;
                bool supportsClientWindowsIdentity = thisSecurityCapability.SupportsClientWindowsIdentity;
                bool supportsServerAuth = thisSecurityCapability.SupportsServerAuthentication || lowerSecurityCapability.SupportsServerAuthentication;
                ProtectionLevel requestProtectionLevel = ProtectionLevelHelper.Max(thisSecurityCapability.SupportedRequestProtectionLevel, lowerSecurityCapability.SupportedRequestProtectionLevel);
                ProtectionLevel responseProtectionLevel = ProtectionLevelHelper.Max(thisSecurityCapability.SupportedResponseProtectionLevel, lowerSecurityCapability.SupportedResponseProtectionLevel);
                return new SecurityCapabilities(supportsClientAuth, supportsServerAuth, supportsClientWindowsIdentity, requestProtectionLevel, responseProtectionLevel);
            }
        }

        // If any changes are made to this method, please make sure that they are
        // reflected in the corresponding IsMutualCertificateBinding() method.
        static public SecurityBindingElement CreateMutualCertificateBindingElement()
        {
            return CreateMutualCertificateBindingElement(MessageSecurityVersion.WSSecurity11WSTrustFebruary2005WSSecureConversationFebruary2005WSSecurityPolicy11);
        }

        // this method reverses CreateMutualCertificateBindingElement() logic
        internal static bool IsMutualCertificateBinding(SecurityBindingElement sbe)
        {
            return IsMutualCertificateBinding(sbe, false);
        }

        // If any changes are made to this method, please make sure that they are
        // reflected in the corresponding IsMutualCertificateBinding() method.
        static public SecurityBindingElement CreateMutualCertificateBindingElement(MessageSecurityVersion version)
        {
            return CreateMutualCertificateBindingElement(version, false);
        }

        // If any changes are made to this method, please make sure that they are
        // reflected in the corresponding IsMutualCertificateBinding() method.
        static public SecurityBindingElement CreateMutualCertificateBindingElement(MessageSecurityVersion version, bool allowSerializedSigningTokenOnReply)
        {
            if (version == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("version");
            }

            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement.CreateMutualCertificateBindingElement is not supported.");
        }


        // this method reverses CreateMutualCertificateBindingElement() logic
        internal static bool IsMutualCertificateBinding(SecurityBindingElement sbe, bool allowSerializedSigningTokenOnReply)
        {
            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement.IsMutualCertificateBinding is not supported.");
        }

        // If any changes are made to this method, please make sure that they are
        // reflected in the corresponding IsUserNameOverTransportBinding() method.
        static public TransportSecurityBindingElement CreateUserNameOverTransportBindingElement()
        {
            TransportSecurityBindingElement result = new TransportSecurityBindingElement();
            result.EndpointSupportingTokenParameters.SignedEncrypted.Add(
                new UserNameSecurityTokenParameters());
            result.IncludeTimestamp = true;

            return result;
        }

        // this method reverses CreateMutualCertificateBindingElement() logic
        internal static bool IsUserNameOverTransportBinding(SecurityBindingElement sbe)
        {
            // do not check local settings: sbe.LocalServiceSettings and sbe.LocalClientSettings
            if (!sbe.IncludeTimestamp)
                return false;

            if (!(sbe is TransportSecurityBindingElement))
                return false;

            SupportingTokenParameters parameters = sbe.EndpointSupportingTokenParameters;
            if (parameters.Signed.Count != 0 || parameters.SignedEncrypted.Count != 1 || parameters.Endorsing.Count != 0 || parameters.SignedEndorsing.Count != 0)
                return false;

            UserNameSecurityTokenParameters userNameParameters = parameters.SignedEncrypted[0] as UserNameSecurityTokenParameters;
            if (userNameParameters == null)
                return false;

            return true;
        }

        // If any changes are made to this method, please make sure that they are
        // reflected in the corresponding IsCertificateOverTransportBinding() method.
        static public TransportSecurityBindingElement CreateCertificateOverTransportBindingElement(MessageSecurityVersion version)
        {
            if (version == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("version");
            }

            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement.CreateCertificateOverTransportBindingElement is not supported.");
        }

        // this method reverses CreateMutualCertificateBindingElement() logic
        internal static bool IsCertificateOverTransportBinding(SecurityBindingElement sbe)
        {
            // do not check local settings: sbe.LocalServiceSettings and sbe.LocalClientSettings
            if (!sbe.IncludeTimestamp)
                return false;

            if (!(sbe is TransportSecurityBindingElement))
                return false;

            SupportingTokenParameters parameters = sbe.EndpointSupportingTokenParameters;
            if (parameters.Signed.Count != 0 || parameters.SignedEncrypted.Count != 0 || parameters.Endorsing.Count != 1 || parameters.SignedEndorsing.Count != 0)
                return false;

            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement.IsCertificateOverTransportBinding is not supported.");
        }

        // If any changes are made to this method, please make sure that they are
        // reflected in the corresponding IsSecureConversationBinding() method.
        static public SecurityBindingElement CreateSecureConversationBindingElement(SecurityBindingElement bootstrapSecurity)
        {
            throw ExceptionHelper.PlatformNotSupported("SecurityBindingElement.CreateSecureConversatationBindingElement is not supported.");
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0}:", this.GetType().ToString()));
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "IncludeTimestamp: {0}", _includeTimestamp.ToString()));
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "MessageSecurityVersion: {0}", this.MessageSecurityVersion.ToString()));
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "SecurityHeaderLayout: {0}", _securityHeaderLayout.ToString()));
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "ProtectTokens: {0}", _protectTokens.ToString()));
            sb.AppendLine("EndpointSupportingTokenParameters:");
            sb.AppendLine("  " + this.EndpointSupportingTokenParameters.ToString().Trim().Replace("\n", "\n  "));

            return sb.ToString().Trim();
        }
    }
}

﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ServiceReference1
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="https://api.1pcom.net/", ConfigurationName="ServiceReference1.DIDManagementSoap")]
    public interface DIDManagementSoap
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDWebserviceHeartbeat", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<string> DIDWebserviceHeartbeatAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDAPIVersion", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDAPIVersionAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDOrder", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDOrderAsync(ServiceReference1.Credentials Auth, string DID, bool SMSEnable);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDOrderBulkPattern", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDOrderBulkPatternAsync(ServiceReference1.Credentials Auth, ServiceReference1.DIDOrderQuery DIDPattern, bool SMSEnable, int amount);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDSMSEnable", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDSMSEnableAsync(ServiceReference1.Credentials Auth, string DID);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDInventorySearch", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.DIDOrderInfoArray> DIDInventorySearchAsync(ServiceReference1.Credentials Auth, ServiceReference1.DIDOrderQuery DIDSearch, int ReturnAmount);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDInventoryGetAvailableCities", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.DIDInventoryGetAvailableCitiesResponseDIDInventoryGetAvailableCitiesResult> DIDInventoryGetAvailableCitiesAsync(ServiceReference1.Credentials Auth, string state);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDInventoryGetAvailableStates", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.DIDInventoryGetAvailableStatesResponseDIDInventoryGetAvailableStatesResult> DIDInventoryGetAvailableStatesAsync(ServiceReference1.Credentials Auth, string country);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDInventoryGetAvailableNumbersByCity", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.DIDInventoryGetAvailableNumbersByCityResponseDIDInventoryGetAvailableNumbersByCityResult> DIDInventoryGetAvailableNumbersByCityAsync(ServiceReference1.Credentials Auth, string city);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDInventoryGetAvailableCountries", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<string[]> DIDInventoryGetAvailableCountriesAsync(ServiceReference1.Credentials Auth);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/LongCodeSearchInAccount", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.LongcodeInfoArray> LongCodeSearchInAccountAsync(ServiceReference1.Credentials Auth, ServiceReference1.DIDOrderQuery DIDSearch, int ReturnAmount);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDSearchInAccount", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.DIDOrderInfoArray> DIDSearchInAccountAsync(ServiceReference1.Credentials Auth, ServiceReference1.DIDOrderQuery DIDSearch, int ReturnAmount);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDDeleteFromAccount", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDDeleteFromAccountAsync(ServiceReference1.Credentials Auth, string did);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDRouteVoiceToGatewayBasic", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteVoiceToGatewayBasicAsync(ServiceReference1.Credentials Auth, string DID, string GatewayIP);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDRouteVoiceToRCFBasic", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteVoiceToRCFBasicAsync(ServiceReference1.Credentials Auth, string did, string RouteToNumber);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/LongCodeShowRouting", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.SMSLongcodeRoute> LongCodeShowRoutingAsync(ServiceReference1.Credentials Auth, string LongCode);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDRouteSMSToEPIDBasic", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToEPIDBasicAsync(ServiceReference1.Credentials Auth, string DID, int EPID);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDUnrouteSMS", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDUnrouteSMSAsync(ServiceReference1.Credentials Auth, string DID);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDRouteSMSToEmail", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToEmailAsync(ServiceReference1.Credentials Auth, string DID, string emailaddress);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDRouteSMSToXMPP", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToXMPPAsync(ServiceReference1.Credentials Auth, string DID, string XMPPUser);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDRouteSMSToPUSHAPI", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToPUSHAPIAsync(ServiceReference1.Credentials Auth, string DID, string URL);
        
        [System.ServiceModel.OperationContractAttribute(Action="https://api.1pcom.net/DIDRouteSMSToPullAPI", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToPullAPIAsync(ServiceReference1.Credentials Auth, string DID);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="https://api.1pcom.net/")]
    public partial class QueryResult
    {
        
        private int codeField;
        
        private string textField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public int code
        {
            get
            {
                return this.codeField;
            }
            set
            {
                this.codeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="https://api.1pcom.net/")]
    public partial class SMSLongcodeRoute
    {
        
        private string routeField;
        
        private int epidField;
        
        private string epnameField;
        
        private string eptypeField;
        
        private string additionalField;
        
        private QueryResult queryResultField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string route
        {
            get
            {
                return this.routeField;
            }
            set
            {
                this.routeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public int epid
        {
            get
            {
                return this.epidField;
            }
            set
            {
                this.epidField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string epname
        {
            get
            {
                return this.epnameField;
            }
            set
            {
                this.epnameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public string eptype
        {
            get
            {
                return this.eptypeField;
            }
            set
            {
                this.eptypeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public string additional
        {
            get
            {
                return this.additionalField;
            }
            set
            {
                this.additionalField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=5)]
        public QueryResult QueryResult
        {
            get
            {
                return this.queryResultField;
            }
            set
            {
                this.queryResultField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="https://api.1pcom.net/")]
    public partial class LongcodeInfo
    {
        
        private string longcodeField;
        
        private string rateCenterField;
        
        private string nPAField;
        
        private string nXXField;
        
        private bool inInventoryField;
        
        private bool inAccountField;
        
        private QueryResult queryResultField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string Longcode
        {
            get
            {
                return this.longcodeField;
            }
            set
            {
                this.longcodeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string RateCenter
        {
            get
            {
                return this.rateCenterField;
            }
            set
            {
                this.rateCenterField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string NPA
        {
            get
            {
                return this.nPAField;
            }
            set
            {
                this.nPAField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public string NXX
        {
            get
            {
                return this.nXXField;
            }
            set
            {
                this.nXXField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public bool InInventory
        {
            get
            {
                return this.inInventoryField;
            }
            set
            {
                this.inInventoryField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=5)]
        public bool InAccount
        {
            get
            {
                return this.inAccountField;
            }
            set
            {
                this.inAccountField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=6)]
        public QueryResult QueryResult
        {
            get
            {
                return this.queryResultField;
            }
            set
            {
                this.queryResultField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="https://api.1pcom.net/")]
    public partial class LongcodeInfoArray
    {
        
        private LongcodeInfo[] longcodeInfoField;
        
        private QueryResult queryresultField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order=0)]
        public LongcodeInfo[] LongcodeInfo
        {
            get
            {
                return this.longcodeInfoField;
            }
            set
            {
                this.longcodeInfoField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public QueryResult queryresult
        {
            get
            {
                return this.queryresultField;
            }
            set
            {
                this.queryresultField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="https://api.1pcom.net/")]
    public partial class DIDOrderInfo
    {
        
        private string dIDField;
        
        private string rateCenterField;
        
        private string nPAField;
        
        private string nXXField;
        
        private bool inInventoryField;
        
        private string planNameField;
        
        private string requiresChannelTypeField;
        
        private QueryResult queryResultField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string DID
        {
            get
            {
                return this.dIDField;
            }
            set
            {
                this.dIDField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string RateCenter
        {
            get
            {
                return this.rateCenterField;
            }
            set
            {
                this.rateCenterField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string NPA
        {
            get
            {
                return this.nPAField;
            }
            set
            {
                this.nPAField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public string NXX
        {
            get
            {
                return this.nXXField;
            }
            set
            {
                this.nXXField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public bool InInventory
        {
            get
            {
                return this.inInventoryField;
            }
            set
            {
                this.inInventoryField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=5)]
        public string PlanName
        {
            get
            {
                return this.planNameField;
            }
            set
            {
                this.planNameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=6)]
        public string RequiresChannelType
        {
            get
            {
                return this.requiresChannelTypeField;
            }
            set
            {
                this.requiresChannelTypeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=7)]
        public QueryResult QueryResult
        {
            get
            {
                return this.queryResultField;
            }
            set
            {
                this.queryResultField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="https://api.1pcom.net/")]
    public partial class DIDOrderInfoArray
    {
        
        private DIDOrderInfo[] dIDOrderField;
        
        private QueryResult queryresultField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order=0)]
        public DIDOrderInfo[] DIDOrder
        {
            get
            {
                return this.dIDOrderField;
            }
            set
            {
                this.dIDOrderField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public QueryResult queryresult
        {
            get
            {
                return this.queryresultField;
            }
            set
            {
                this.queryresultField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="https://api.1pcom.net/")]
    public partial class DIDOrderQuery
    {
        
        private string rateCenterField;
        
        private string nPAField;
        
        private string nXXField;
        
        private string dIDField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string RateCenter
        {
            get
            {
                return this.rateCenterField;
            }
            set
            {
                this.rateCenterField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string NPA
        {
            get
            {
                return this.nPAField;
            }
            set
            {
                this.nPAField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string NXX
        {
            get
            {
                return this.nXXField;
            }
            set
            {
                this.nXXField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public string DID
        {
            get
            {
                return this.dIDField;
            }
            set
            {
                this.dIDField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="https://api.1pcom.net/")]
    public partial class Credentials
    {
        
        private string usernameField;
        
        private string passwordField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string Username
        {
            get
            {
                return this.usernameField;
            }
            set
            {
                this.usernameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string Password
        {
            get
            {
                return this.passwordField;
            }
            set
            {
                this.passwordField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="https://api.1pcom.net/")]
    public partial class DIDInventoryGetAvailableCitiesResponseDIDInventoryGetAvailableCitiesResult
    {
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlElement any1Field;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Namespace="http://www.w3.org/2001/XMLSchema", Order=0)]
        public System.Xml.XmlElement[] Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Namespace="urn:schemas-microsoft-com:xml-diffgram-v1", Order=1)]
        public System.Xml.XmlElement Any1
        {
            get
            {
                return this.any1Field;
            }
            set
            {
                this.any1Field = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="https://api.1pcom.net/")]
    public partial class DIDInventoryGetAvailableStatesResponseDIDInventoryGetAvailableStatesResult
    {
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlElement any1Field;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Namespace="http://www.w3.org/2001/XMLSchema", Order=0)]
        public System.Xml.XmlElement[] Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Namespace="urn:schemas-microsoft-com:xml-diffgram-v1", Order=1)]
        public System.Xml.XmlElement Any1
        {
            get
            {
                return this.any1Field;
            }
            set
            {
                this.any1Field = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="https://api.1pcom.net/")]
    public partial class DIDInventoryGetAvailableNumbersByCityResponseDIDInventoryGetAvailableNumbersByCityResult
    {
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlElement any1Field;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Namespace="http://www.w3.org/2001/XMLSchema", Order=0)]
        public System.Xml.XmlElement[] Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Namespace="urn:schemas-microsoft-com:xml-diffgram-v1", Order=1)]
        public System.Xml.XmlElement Any1
        {
            get
            {
                return this.any1Field;
            }
            set
            {
                this.any1Field = value;
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    public interface DIDManagementSoapChannel : ServiceReference1.DIDManagementSoap, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.1.0")]
    public partial class DIDManagementSoapClient : System.ServiceModel.ClientBase<ServiceReference1.DIDManagementSoap>, ServiceReference1.DIDManagementSoap
    {
        
        /// <summary>
        /// Implement this partial method to configure the service endpoint.
        /// </summary>
        /// <param name="serviceEndpoint">The endpoint to configure</param>
        /// <param name="clientCredentials">The client credentials</param>
        static partial void ConfigureEndpoint(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint, System.ServiceModel.Description.ClientCredentials clientCredentials);
        
        public DIDManagementSoapClient(EndpointConfiguration endpointConfiguration) : 
                base(DIDManagementSoapClient.GetBindingForEndpoint(endpointConfiguration), DIDManagementSoapClient.GetEndpointAddress(endpointConfiguration))
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public DIDManagementSoapClient(EndpointConfiguration endpointConfiguration, string remoteAddress) : 
                base(DIDManagementSoapClient.GetBindingForEndpoint(endpointConfiguration), new System.ServiceModel.EndpointAddress(remoteAddress))
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public DIDManagementSoapClient(EndpointConfiguration endpointConfiguration, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(DIDManagementSoapClient.GetBindingForEndpoint(endpointConfiguration), remoteAddress)
        {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public DIDManagementSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        public System.Threading.Tasks.Task<string> DIDWebserviceHeartbeatAsync()
        {
            return base.Channel.DIDWebserviceHeartbeatAsync();
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDAPIVersionAsync()
        {
            return base.Channel.DIDAPIVersionAsync();
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDOrderAsync(ServiceReference1.Credentials Auth, string DID, bool SMSEnable)
        {
            return base.Channel.DIDOrderAsync(Auth, DID, SMSEnable);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDOrderBulkPatternAsync(ServiceReference1.Credentials Auth, ServiceReference1.DIDOrderQuery DIDPattern, bool SMSEnable, int amount)
        {
            return base.Channel.DIDOrderBulkPatternAsync(Auth, DIDPattern, SMSEnable, amount);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDSMSEnableAsync(ServiceReference1.Credentials Auth, string DID)
        {
            return base.Channel.DIDSMSEnableAsync(Auth, DID);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.DIDOrderInfoArray> DIDInventorySearchAsync(ServiceReference1.Credentials Auth, ServiceReference1.DIDOrderQuery DIDSearch, int ReturnAmount)
        {
            return base.Channel.DIDInventorySearchAsync(Auth, DIDSearch, ReturnAmount);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.DIDInventoryGetAvailableCitiesResponseDIDInventoryGetAvailableCitiesResult> DIDInventoryGetAvailableCitiesAsync(ServiceReference1.Credentials Auth, string state)
        {
            return base.Channel.DIDInventoryGetAvailableCitiesAsync(Auth, state);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.DIDInventoryGetAvailableStatesResponseDIDInventoryGetAvailableStatesResult> DIDInventoryGetAvailableStatesAsync(ServiceReference1.Credentials Auth, string country)
        {
            return base.Channel.DIDInventoryGetAvailableStatesAsync(Auth, country);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.DIDInventoryGetAvailableNumbersByCityResponseDIDInventoryGetAvailableNumbersByCityResult> DIDInventoryGetAvailableNumbersByCityAsync(ServiceReference1.Credentials Auth, string city)
        {
            return base.Channel.DIDInventoryGetAvailableNumbersByCityAsync(Auth, city);
        }
        
        public System.Threading.Tasks.Task<string[]> DIDInventoryGetAvailableCountriesAsync(ServiceReference1.Credentials Auth)
        {
            return base.Channel.DIDInventoryGetAvailableCountriesAsync(Auth);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.LongcodeInfoArray> LongCodeSearchInAccountAsync(ServiceReference1.Credentials Auth, ServiceReference1.DIDOrderQuery DIDSearch, int ReturnAmount)
        {
            return base.Channel.LongCodeSearchInAccountAsync(Auth, DIDSearch, ReturnAmount);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.DIDOrderInfoArray> DIDSearchInAccountAsync(ServiceReference1.Credentials Auth, ServiceReference1.DIDOrderQuery DIDSearch, int ReturnAmount)
        {
            return base.Channel.DIDSearchInAccountAsync(Auth, DIDSearch, ReturnAmount);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDDeleteFromAccountAsync(ServiceReference1.Credentials Auth, string did)
        {
            return base.Channel.DIDDeleteFromAccountAsync(Auth, did);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteVoiceToGatewayBasicAsync(ServiceReference1.Credentials Auth, string DID, string GatewayIP)
        {
            return base.Channel.DIDRouteVoiceToGatewayBasicAsync(Auth, DID, GatewayIP);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteVoiceToRCFBasicAsync(ServiceReference1.Credentials Auth, string did, string RouteToNumber)
        {
            return base.Channel.DIDRouteVoiceToRCFBasicAsync(Auth, did, RouteToNumber);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.SMSLongcodeRoute> LongCodeShowRoutingAsync(ServiceReference1.Credentials Auth, string LongCode)
        {
            return base.Channel.LongCodeShowRoutingAsync(Auth, LongCode);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToEPIDBasicAsync(ServiceReference1.Credentials Auth, string DID, int EPID)
        {
            return base.Channel.DIDRouteSMSToEPIDBasicAsync(Auth, DID, EPID);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDUnrouteSMSAsync(ServiceReference1.Credentials Auth, string DID)
        {
            return base.Channel.DIDUnrouteSMSAsync(Auth, DID);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToEmailAsync(ServiceReference1.Credentials Auth, string DID, string emailaddress)
        {
            return base.Channel.DIDRouteSMSToEmailAsync(Auth, DID, emailaddress);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToXMPPAsync(ServiceReference1.Credentials Auth, string DID, string XMPPUser)
        {
            return base.Channel.DIDRouteSMSToXMPPAsync(Auth, DID, XMPPUser);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToPUSHAPIAsync(ServiceReference1.Credentials Auth, string DID, string URL)
        {
            return base.Channel.DIDRouteSMSToPUSHAPIAsync(Auth, DID, URL);
        }
        
        public System.Threading.Tasks.Task<ServiceReference1.QueryResult> DIDRouteSMSToPullAPIAsync(ServiceReference1.Credentials Auth, string DID)
        {
            return base.Channel.DIDRouteSMSToPullAPIAsync(Auth, DID);
        }
        
        public virtual System.Threading.Tasks.Task OpenAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginOpen(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndOpen));
        }
        
        private static System.ServiceModel.Channels.Binding GetBindingForEndpoint(EndpointConfiguration endpointConfiguration)
        {
            if ((endpointConfiguration == EndpointConfiguration.DIDManagementSoap))
            {
                System.ServiceModel.BasicHttpBinding result = new System.ServiceModel.BasicHttpBinding();
                result.MaxBufferSize = int.MaxValue;
                result.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
                result.MaxReceivedMessageSize = int.MaxValue;
                result.AllowCookies = true;
                result.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
                return result;
            }
            if ((endpointConfiguration == EndpointConfiguration.DIDManagementSoap12))
            {
                System.ServiceModel.Channels.CustomBinding result = new System.ServiceModel.Channels.CustomBinding();
                System.ServiceModel.Channels.TextMessageEncodingBindingElement textBindingElement = new System.ServiceModel.Channels.TextMessageEncodingBindingElement();
                textBindingElement.MessageVersion = System.ServiceModel.Channels.MessageVersion.CreateVersion(System.ServiceModel.EnvelopeVersion.Soap12, System.ServiceModel.Channels.AddressingVersion.None);
                result.Elements.Add(textBindingElement);
                System.ServiceModel.Channels.HttpsTransportBindingElement httpsBindingElement = new System.ServiceModel.Channels.HttpsTransportBindingElement();
                httpsBindingElement.AllowCookies = true;
                httpsBindingElement.MaxBufferSize = int.MaxValue;
                httpsBindingElement.MaxReceivedMessageSize = int.MaxValue;
                result.Elements.Add(httpsBindingElement);
                return result;
            }
            throw new System.InvalidOperationException(string.Format("Could not find endpoint with name \'{0}\'.", endpointConfiguration));
        }
        
        private static System.ServiceModel.EndpointAddress GetEndpointAddress(EndpointConfiguration endpointConfiguration)
        {
            if ((endpointConfiguration == EndpointConfiguration.DIDManagementSoap))
            {
                return new System.ServiceModel.EndpointAddress("https://api.1pcom.net/ws2/DIDManagement.asmx");
            }
            if ((endpointConfiguration == EndpointConfiguration.DIDManagementSoap12))
            {
                return new System.ServiceModel.EndpointAddress("https://api.1pcom.net/ws2/DIDManagement.asmx");
            }
            throw new System.InvalidOperationException(string.Format("Could not find endpoint with name \'{0}\'.", endpointConfiguration));
        }
        
        public enum EndpointConfiguration
        {
            
            DIDManagementSoap,
            
            DIDManagementSoap12,
        }
    }
}
﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Manager.Integration.Test.Properties {
    
    
    [CompilerGenerated()]
    [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [ApplicationScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("http://localhost:9000/jobmanager/")]
        public string ManagerLocationUri {
            get {
                return ((string)(this["ManagerLocationUri"]));
            }
        }
        
        [ApplicationScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("../../../Manager.Integration.Tests.Console.Host/bin/")]
        public string ManagerIntegrationConsoleHostLocation {
            get {
                return ((string)(this["ManagerIntegrationConsoleHostLocation"]));
            }
        }
        
        [ApplicationScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("Manager.IntegrationTest.Console.Host.exe")]
        public string ManagerIntegrationConsoleHostAssemblyName {
            get {
                return ((string)(this["ManagerIntegrationConsoleHostAssemblyName"]));
            }
        }
        
        [ApplicationScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("Manager.config")]
        public string ManagerConfigurationFileName {
            get {
                return ((string)(this["ManagerConfigurationFileName"]));
            }
        }
        
        [ApplicationScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("Manager.IntegrationTest.Console.Host.exe.config")]
        public string ManagerIntegrationConsoleHostConfigurationFile {
            get {
                return ((string)(this["ManagerIntegrationConsoleHostConfigurationFile"]));
            }
        }
        
        [ApplicationScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("scripts/CreateLoggingTable.sql")]
        public string CreateLoggingTableSqlScriptLocationAndFileName {
            get {
                return ((string)(this["CreateLoggingTableSqlScriptLocationAndFileName"]));
            }
        }
        
        [ApplicationScopedSetting()]
        [DebuggerNonUserCode()]
        [DefaultSettingValue("http://localhost:9100/managerIntegrationTestController/")]
        public string ManagerIntegrationTestControllerBaseAddress {
            get {
                return ((string)(this["ManagerIntegrationTestControllerBaseAddress"]));
            }
        }
    }
}

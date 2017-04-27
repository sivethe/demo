using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LatencyWorkerRole
{
    internal static class WorkerSettings
    {
        private const string IsWriteRegionText = "IsWriteRegion";
        private const string CurrentRegionText = "CurrentRegion";
        private const string TableNativeConnectionStringText = "TableNativeConnectionString";
        private const string TablePremiumConnectionStringText = "TablePremiumConnectionString";
        private const string LatencyResultStoreConnectionStringText = "LatencyResultStoreConnectionString";

        static WorkerSettings()
        {
            WorkerSettings.IsWriteRegion = ConvertValue<bool>(RoleEnvironment.GetConfigurationSettingValue(IsWriteRegionText));
            WorkerSettings.CurrentRegion = RoleEnvironment.GetConfigurationSettingValue(CurrentRegionText);
            WorkerSettings.TableNativeConnectionString = RoleEnvironment.GetConfigurationSettingValue(TableNativeConnectionStringText);
            WorkerSettings.TablePremiumConnectionString = RoleEnvironment.GetConfigurationSettingValue(TablePremiumConnectionStringText);
            WorkerSettings.LatencyResultStoreConnectionString = RoleEnvironment.GetConfigurationSettingValue(LatencyResultStoreConnectionStringText);
        }

        public static bool IsWriteRegion { get; private set; }

        public static string CurrentRegion { get; private set; }

        public static string TableNativeConnectionString { get; private set; }

        public static string TablePremiumConnectionString { get; private set; }

        public static string LatencyResultStoreConnectionString { get; private set; }

        private static T ConvertValue<T>(string value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
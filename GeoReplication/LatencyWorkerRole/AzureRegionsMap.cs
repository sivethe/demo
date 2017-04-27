using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LatencyWorkerRole
{
    internal sealed class AzureRegionsMap
    {
        private static Dictionary<string, string> regionsMaps 
            = new Dictionary<string, string>(); //region from endpoint -> official Azure region name

        static AzureRegionsMap()
        {
            AzureRegionsMap.PopulateAzureRegions();
        }

        public static string GetRegionName(string endpoint)
        {
            int index_dot = endpoint.IndexOf('.');
            int index_dash = endpoint.IndexOf('-');

            string regionName = string.Empty;
            if (index_dash == -1)
                return regionName;

            regionName = endpoint.Substring(index_dash + 1, index_dot - index_dash - 1);
            string region = string.Empty;
            if (AzureRegionsMap.regionsMaps.TryGetValue(regionName, out region))
            {
                //found region;
            }
            else
            {
                Trace.TraceError("Region not present in Azure regions Map : {0}", regionName);
            }

            return region;
        }

        private static void PopulateAzureRegions()
        {
            AzureRegionsMap.regionsMaps.Add("southcentralus", "South Central US");
            AzureRegionsMap.regionsMaps.Add("eastus", "East US");
            AzureRegionsMap.regionsMaps.Add("eastus2", "East US 2");
            AzureRegionsMap.regionsMaps.Add("westus", "West US");
            AzureRegionsMap.regionsMaps.Add("westus2", "West US 2");
            AzureRegionsMap.regionsMaps.Add("centralus", "Central US");
            AzureRegionsMap.regionsMaps.Add("westcentralus", "West Central US");
            AzureRegionsMap.regionsMaps.Add("northcentralus", "North Central US");

            AzureRegionsMap.regionsMaps.Add("southindia", "South India");
            AzureRegionsMap.regionsMaps.Add("westindia", "West India");
            AzureRegionsMap.regionsMaps.Add("centralindia", "Central India");

            AzureRegionsMap.regionsMaps.Add("westeurope", "West Europe");
            AzureRegionsMap.regionsMaps.Add("northeurope", "North Europe");
            AzureRegionsMap.regionsMaps.Add("ukwest", "UK West");
            AzureRegionsMap.regionsMaps.Add("uksouth", "UK South");

            AzureRegionsMap.regionsMaps.Add("brazilsouth", "Brazil South");

            AzureRegionsMap.regionsMaps.Add("japanwest", "Japan West");
            AzureRegionsMap.regionsMaps.Add("japaneast", "Japan East");

            AzureRegionsMap.regionsMaps.Add("koreacentral", "Korea Central");
            AzureRegionsMap.regionsMaps.Add("koreasouth", "Korea South");

            AzureRegionsMap.regionsMaps.Add("southeastasia", "Southeast Asia");
            AzureRegionsMap.regionsMaps.Add("eastasia", "East Asia");

            AzureRegionsMap.regionsMaps.Add("australiaeast", "Australia East");
            AzureRegionsMap.regionsMaps.Add("australiasoutheast", "Australia SouthEast");

            AzureRegionsMap.regionsMaps.Add("canadacentral", "Canada Central");
            AzureRegionsMap.regionsMaps.Add("canadaeast", "Canada East");
        }
    }
}

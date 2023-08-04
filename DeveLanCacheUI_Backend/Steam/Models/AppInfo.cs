namespace DeveLanCacheUI_Backend.Steam.Models
{
    /// <summary>
    /// Represents an application (game, tool, video, server) that can be downloaded from steam
    ///
    /// Adapted from : https://github.com/tpill90/steam-lancache-prefill/blob/master/SteamPrefill/Models/AppInfo.cs
    /// </summary>
    public sealed class AppInfo
    {
        public uint AppId { get; set; }

        public List<uint> DlcAppIds { get; } = new List<uint>();

        /// <summary>
        /// Includes this app's depots, as well as any depots from its "children" DLC apps
        /// </summary>
        public List<DepotInfo> Depots { get; } = new List<DepotInfo>();

        public string Name { get; set; }
        
        /// <summary>
        /// Specifies the type of app, can be "config", "tool", "game".  This seems to be up to the developer, and isn't 100% consistent.
        /// </summary>
        public AppType Type { get; }

        //TODO might want to make this a field
        public bool IsInvalidApp => Type == null;

        public AppInfo(uint appId, string name)
        {
            AppId = appId;
            Name = name;
        }

        public AppInfo(uint appId, KeyValue rootKeyValue)
        {
            AppId = appId;

            Name = rootKeyValue["common"]["name"].Value;
            Type = rootKeyValue["common"]["type"].AsEnum<AppType>(toLower: true);


            if (rootKeyValue["depots"] != KeyValue.Invalid)
            {
                // Depots should always have a numerical ID for their name. For whatever reason Steam also includes branches + other metadata
                // that we don't care about in here, which will be filtered out as they don't have a numerical ID
                Depots = rootKeyValue["depots"].Children.Where(e => uint.TryParse(e.Name, out _))
                                                       .Select(e => new DepotInfo(e, appId))
                                                       .Where(e => !e.IsInvalidDepot)
                                                       .ToList();
            }

            // Extended Section
            var listOfDlc = rootKeyValue["extended"]["listofdlc"].Value;
            if (listOfDlc != null)
            {
                DlcAppIds = listOfDlc.Split(",")
                                     .Select(e => uint.Parse(e))
                                     // Only including DLC that we own
                                     .ToList();
            }

        }
    }
}
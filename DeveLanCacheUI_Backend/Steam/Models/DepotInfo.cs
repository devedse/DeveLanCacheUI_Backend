namespace DeveLanCacheUI_Backend.Steam.Models
{
    /// <summary>
    /// Adapted from : https://github.com/tpill90/steam-lancache-prefill/blob/master/SteamPrefill/Models/DepotInfo.cs
    /// </summary>
    public sealed class DepotInfo
    {
        public uint DepotId { get; init; }
        public string Name { get; }

        public ulong? ManifestId { get; set; }

        /// <summary>
        /// Determines what app actually owns the depot, by default it is the current app.
        /// However in the case of a linked/DLC app, the depot will need to be downloaded using the referenced app's id
        /// </summary>
        public uint ContainingAppId
        {
            get
            {
                if (DlcAppId != null)
                {
                    return DlcAppId.Value;
                }
                if (DepotFromApp != null)
                {
                    return DepotFromApp.Value;
                }
                return _originalAppId;
            }
        }
        private readonly uint _originalAppId;

        /// <summary>
        /// Determines if a depot is a "linked" depot.  If the current depot is linked, it won't actually have a manifest to download under the current app.
        /// Instead, the depot will need to be downloaded from the linked app.
        /// </summary>
        public uint? DepotFromApp { get; }
        private uint? DlcAppId { get; }

        // If there is no manifest we can't download this depot, and if there is no shared depot then we can't look up a related manifest we could use
        public bool IsInvalidDepot => ManifestId == null && DepotFromApp == null;

        public DepotInfo(KeyValue rootKey, uint appId)
        {
            DepotId = uint.Parse(rootKey.Name);
            Name = rootKey["name"].Value;
            _originalAppId = appId;

            ManifestId = rootKey["manifests"]["public"]["gid"].AsUnsignedLongNullable();
            // Legacy key where the manifest id was previously stored.  Not all depots have migrated to the new "gid" key, so this is still necessary.
            if (ManifestId == null)
            {
                ManifestId = rootKey["manifests"]["public"].AsUnsignedLongNullable();
            }

            DepotFromApp = rootKey["depotfromapp"].AsUnsignedIntNullable();
            DlcAppId = rootKey["dlcappid"].AsUnsignedIntNullable();

        }

        public override string ToString()
        {
            return $"{DepotId} - {Name}";
        }
    }
}
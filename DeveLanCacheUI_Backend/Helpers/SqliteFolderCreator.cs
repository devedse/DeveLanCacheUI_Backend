using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Resources;

namespace DeveLanCacheUI_Backend.Helpers
{
    public static class SqliteFolderCreator
    {
        private const string DataDirectoryMacro = "|DataDirectory|";

        public const int SQLITE_OPEN_READONLY = 0x00000001;  /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_READWRITE = 0x00000002; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_CREATE = 0x00000004; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_DELETEONCLOSE = 0x00000008; /* VFS only */
        public const int SQLITE_OPEN_EXCLUSIVE = 0x00000010; /* VFS only */
        public const int SQLITE_OPEN_AUTOPROXY = 0x00000020; /* VFS only */
        public const int SQLITE_OPEN_URI = 0x00000040; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_MEMORY = 0x00000080; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_MAIN_DB = 0x00000100; /* VFS only */
        public const int SQLITE_OPEN_TEMP_DB = 0x00000200; /* VFS only */
        public const int SQLITE_OPEN_TRANSIENT_DB = 0x00000400; /* VFS only */
        public const int SQLITE_OPEN_MAIN_JOURNAL = 0x00000800; /* VFS only */
        public const int SQLITE_OPEN_TEMP_JOURNAL = 0x00001000; /* VFS only */
        public const int SQLITE_OPEN_SUBJOURNAL = 0x00002000; /* VFS only */
        public const int SQLITE_OPEN_SUPER_JOURNAL = 0x00004000; /* VFS only */
        public const int SQLITE_OPEN_NOMUTEX = 0x00008000; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_FULLMUTEX = 0x00010000; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_SHAREDCACHE = 0x00020000; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_PRIVATECACHE = 0x00040000; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_WAL = 0x00080000; /* VFS only */
        public const int SQLITE_OPEN_NOFOLLOW = 0x01000000; /* Ok for sqlite3_open_v2() */
        public const int SQLITE_OPEN_EXRESCODE = 0x02000000; /* Extended result codes */

        public static string GetFileNameFromSqliteConnectionString(string connectionString)
        {
            var connectionOptions = new SqliteConnectionStringBuilder(connectionString);
            var filename = connectionOptions.DataSource;
            var flags = 0;

            if (filename.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                flags |= SQLITE_OPEN_URI;
            }

            switch (connectionOptions.Mode)
            {
                case SqliteOpenMode.ReadOnly:
                    flags |= SQLITE_OPEN_READONLY;
                    break;

                case SqliteOpenMode.ReadWrite:
                    flags |= SQLITE_OPEN_READWRITE;
                    break;

                case SqliteOpenMode.Memory:
                    flags |= SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_MEMORY;
                    if ((flags & SQLITE_OPEN_URI) == 0)
                    {
                        flags |= SQLITE_OPEN_URI;
                        filename = "file:" + filename;
                    }

                    break;

                default:
                    Debug.Assert(
                        connectionOptions.Mode == SqliteOpenMode.ReadWriteCreate,
                        "connectionOptions.Mode is not ReadWriteCreate");
                    flags |= SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE;
                    break;
            }

            switch (connectionOptions.Cache)
            {
                case SqliteCacheMode.Shared:
                    flags |= SQLITE_OPEN_SHAREDCACHE;
                    break;

                case SqliteCacheMode.Private:
                    flags |= SQLITE_OPEN_PRIVATECACHE;
                    break;

                default:
                    Debug.Assert(
                        connectionOptions.Cache == SqliteCacheMode.Default,
                        "connectionOptions.Cache is not Default.");
                    break;
            }

            var dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
            if (!string.IsNullOrEmpty(dataDirectory)
                && (flags & SQLITE_OPEN_URI) == 0
                && !filename.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                if (filename.StartsWith(DataDirectoryMacro, StringComparison.InvariantCultureIgnoreCase))
                {
                    filename = Path.Combine(dataDirectory, filename.Substring(DataDirectoryMacro.Length));
                }
                else if (!Path.IsPathRooted(filename))
                {
                    filename = Path.Combine(dataDirectory, filename);
                }
            }

            return filename;
        }
    }
}
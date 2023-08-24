namespace DeveLanCacheUI_Backend.Steam
{
    /// <summary>
    /// Taken and adapted from:
    /// https://github.com/tpill90/steam-lancache-prefill/blob/master/SteamPrefill/Handlers/Steam/Steam3Session.cs
    /// </summary>
    public sealed class Steam3Session : IDisposable
    {
        private readonly ILogger<Steam3Session> _logger;

        #region Member fields

        // Steam services
        private readonly SteamClient _steamClient;
        private readonly SteamUser _steamUser;
        public readonly SteamApps SteamAppsApi;
        public readonly SteamConfiguration Configuration;
        public readonly Client CdnClient;
        public readonly CallbackManager CallbackManager;

        public bool LoggedInToSteam { get; private set; }
        
        #endregion

        public Steam3Session(ILogger<Steam3Session> logger)
        {
            _logger = logger;

            _steamClient = new SteamClient(SteamConfiguration.Create(e => e.WithConnectionTimeout(TimeSpan.FromSeconds(120))));
            Configuration = _steamClient.Configuration;
            _steamUser = _steamClient.GetHandler<SteamUser>();
            SteamAppsApi = _steamClient.GetHandler<SteamApps>();

            CallbackManager = new CallbackManager(_steamClient);

            // This callback is triggered when SteamKit2 makes a successful connection
            CallbackManager.Subscribe<SteamClient.ConnectedCallback>(e =>
            {
                _isConnecting = false;
                _disconnected = false;
            });
            // If a connection attempt fails in anyway, SteamKit2 notifies of the failure with a "disconnect"
            CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(e =>
            {
                _isConnecting = false;
                _disconnected = true;
            });

            CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(loggedOn =>
            {
                _loggedOnCallbackResult = loggedOn;
            });

            CdnClient = new Client(_steamClient);
        }
        
        public void LoginToSteam()
        {
            _logger.LogInformation("Starting Steam login!");

            int retryCount = 0;
            bool logonSuccess = false;
            while (!logonSuccess)
            {
                CallbackManager.RunWaitAllCallbacks(timeout: TimeSpan.FromMilliseconds(50));

                _logger.LogInformation("Connecting to Steam...");
                ConnectToSteam();

                var logonResult = AttemptSteamLogin();
                logonSuccess = HandleLogonResult(logonResult);

                retryCount++;
                if (retryCount == 5)
                {
                    throw new SteamLoginException("Unable to login to Steam!  Try again in a few moments...");
                }
            }

            LoggedInToSteam = true;
            _logger.LogInformation("Steam session initialization complete!");
        }
        
        #region  Connecting to Steam

        // Used to busy wait until the connection attempt finishes in either a success or failure
        private bool _isConnecting;

        /// <summary>
        /// Attempts to establish a connection to the Steam network.
        /// Retries if necessary until successful connection is established
        /// </summary>
        /// <exception cref="SteamConnectionException">Throws if unable to connect to Steam</exception>
        private void ConnectToSteam()
        {
            var timeoutAfter = DateTime.Now.AddSeconds(30);

            // Busy waiting until the client has a successful connection established
            while (!_steamClient.IsConnected)
            {
                _isConnecting = true;
                _steamClient.Connect();

                // Busy waiting until SteamKit2 either succeeds/fails the connection attempt
                while (_isConnecting)
                {
                    CallbackManager.RunWaitAllCallbacks(timeout: TimeSpan.FromMilliseconds(50));
                    if (DateTime.Now > timeoutAfter)
                    {
                        throw new SteamConnectionException("Timeout connecting to Steam...  Try again in a few moments");
                    }
                }
            }
            _logger.LogInformation("Connected to Steam!");
        }

        #endregion

        #region Logging into Steam

        private SteamUser.LoggedOnCallback _loggedOnCallbackResult;
        private SteamUser.LoggedOnCallback AttemptSteamLogin()
        {
            var timeoutAfter = DateTime.Now.AddSeconds(30);
            // Need to reset this global result value, as it will be populated once the logon callback completes
            _loggedOnCallbackResult = null;

            _steamUser.LogOnAnonymous();

            // Busy waiting for the callback to complete, then we can return the callback value synchronously
            while (_loggedOnCallbackResult == null)
            {
                CallbackManager.RunWaitAllCallbacks(timeout: TimeSpan.FromMilliseconds(50));
                if (DateTime.Now > timeoutAfter)
                {
                    throw new SteamLoginException("Timeout logging into Steam...  Try again in a few moments");
                }
            }
            return _loggedOnCallbackResult;
        }

        [SuppressMessage("", "VSTHRD002:Synchronously waiting on tasks may cause deadlocks.", Justification = "Its not possible for this callback method to be async, must block synchronously")]
        private bool HandleLogonResult(SteamUser.LoggedOnCallback logonResult)
        {
            var loggedOn = logonResult;

            if (loggedOn.Result == EResult.ServiceUnavailable)
            {
                throw new SteamLoginException($"Unable to login to Steam : Service is unavailable");
            }
            if (loggedOn.Result != EResult.OK)
            {
                throw new SteamLoginException($"Unable to login to Steam.  An unknown error occurred : {loggedOn.Result}");
            }

            _logger.LogInformation($"Logged in anonymously to Steam");

            return true;
        }

        private bool _disconnected = true;
        public void Disconnect()
        {
            if (_disconnected)
            {
                _logger.LogInformation("Already disconnected from Steam");
                return;
            }

            _disconnected = false;
            _steamClient.Disconnect();

            _logger.LogInformation("Disconnecting from Steam..");
            while (!_disconnected)
            {
                CallbackManager.RunWaitAllCallbacks(TimeSpan.FromMilliseconds(100));
            }
            _logger.LogInformation("Disconnected from Steam!");
            LoggedInToSteam = false;
        }

        #endregion
        
        public void Dispose()
        {
            CdnClient.Dispose();
        }
    }
}
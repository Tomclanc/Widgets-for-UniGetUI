using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using System.Timers;
using WidgetsForUniGetUI.Templates;

namespace WidgetsForUniGetUI
{

    internal class WingetUIConnector
    {
        private const double MINIMUM_REQUIRED_HOST_VERSION = 3.119;
        private const string MINIMUM_REQUIRED_HOST_VERSION_STRING = "3.1.2-beta0";
        private const double CACHE_EXPIRATION_INTERVAL = 10 * 60 * 1000;

        public event EventHandler<UpdatesCheckFinishedEventArgs>? UpdateCheckFinished;

        private string SessionToken = "";
        private bool is_connected_to_host = false;
        private bool update_cache_is_valid = false;
        private Package[] cached_updates = new Package[0];

        private System.Timers.Timer CacheExpirationTimer = new();

        public Dictionary<string, string> WidgetSourceReference = new()
        {
            {Widgets.All, ""},
            {Widgets.Winget, "winget"},
            {Widgets.Scoop, "scoop"},
            {Widgets.Chocolatey, "chocolatey"},
            {Widgets.Pip, "pip"},
            {Widgets.Npm, "npm"},
            {Widgets.Dotnet, "dotnet-tool"},
            {Widgets.PowerShell5, "winps"},
            {Widgets.PowerShell7, "pwsh"},
            {Widgets.Cargo, "cargo"},
            {Widgets.Vcpkg, "vcpkg"}
        };

        public WingetUIConnector()
        {
            CacheExpirationTimer.Elapsed += OnCacheExpire;
            CacheExpirationTimer.Interval = CACHE_EXPIRATION_INTERVAL;
        }

        public void ResetConnection()
        {
            is_connected_to_host = false;
            ResetCachedUpdates();
        }

        public void ResetCachedUpdates()
        {
            cached_updates = new Package[0];
            update_cache_is_valid = false;
        }


        public void OnCacheExpire(object? source, ElapsedEventArgs? e)
        {
            ResetCachedUpdates();
            Logger.Log("Updates expired");
            CacheExpirationTimer.Stop();
        }

        async public void GetAvailableUpdates(GenericWidget Widget, bool DeepCheck = false)
        {
            Logger.Log("BEGIN GetAvailableUpdates(). Widget.Name=" + Widget.Name + ", DeepCheck=" + DeepCheck.ToString());
            UpdatesCheckFinishedEventArgs result = new(Widget);
            string AllowedSource = WidgetSourceReference[Widget.Name];
            Package[] found_updates;

            // Connect to UniGetUI if needed

            try
            {
                if (!is_connected_to_host)
                {
                    Logger.Log("GetAvailableUpdates: BEGIN connection to the host");

                    new Template_LoadingPage(Widget).UpdateWidget();

                    UniGetUIEndpoint endpoint = GetEndpoint();
                    SessionToken = endpoint.Token;

                    using HttpClient client = CreateIpcClient(endpoint);

                    HttpResponseMessage task = await client.GetAsync("/uniget/v1/status");
                    if (task.IsSuccessStatusCode)
                    {
                        double host_version;
                        UniGetUIStatus status = ParseStatus(await task.Content.ReadAsStringAsync());

                        host_version = status.BuildNumber;
                        Logger.Log("Found UniGetUI " + status.Version);

                        if (host_version < MINIMUM_REQUIRED_HOST_VERSION)
                        {
                            string minVersion = MINIMUM_REQUIRED_HOST_VERSION.ToString(CultureInfo.InvariantCulture);
                            string minVersionName = MINIMUM_REQUIRED_HOST_VERSION_STRING;

                            Logger.Log("GetAvailableUpdates: ABORTED: MINIMUM_REQUIRED_HOST_VERSION "
                                + $"{minVersion} ({minVersionName}) was not met by the host (host is {host_version})");
                            result.Succeeded = is_connected_to_host = false;
                            result.ErrorReason = "UniGetUI"
                                + $" {minVersion} ({minVersionName}) is required. You are running UniGetUI version {host_version.ToString(CultureInfo.InvariantCulture)})";
                            if (UpdateCheckFinished != null) UpdateCheckFinished(this, result);
                            return;
                        }
                        else
                        {
                            Logger.Log("GetAvailableUpdates: SUCCESS Connected to host successfully.");
                            is_connected_to_host = true;
                        }
                    }
                    else if (task.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Logger.Log("GetAvailableUpdates: ABORTED connection to UniGetUI due to UNAUTHORIZED");
                        result.Succeeded = is_connected_to_host = false;
                        result.ErrorReason = "UNAUTHORIZED";
                        if (UpdateCheckFinished != null)
                            UpdateCheckFinished(this, result);
                        return;
                    }
                    else
                    {
                        Logger.Log("GetAvailableUpdates: ABORTED connection to UniGetUI due to StatusCode=" + task.StatusCode);
                        result.Succeeded = is_connected_to_host = false;
                        result.ErrorReason = "NO_WINGETUI";
                        if (UpdateCheckFinished != null)
                            UpdateCheckFinished(this, result);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("GetAvailableUpdates: ABORTED connection to host: An exception was thrown");
                Logger.Log(ex.Message);
                result.Succeeded = is_connected_to_host = false;
                result.ErrorReason = "NO_WINGETUI";
                if (UpdateCheckFinished != null)
                    UpdateCheckFinished(this, result);
                Logger.Log("END of GetAvailableUpdates()");
                return;
            }

            // Get fresh updates from the host if cached ones are not valid

            if (!update_cache_is_valid || DeepCheck)
            {
                try
                {
                    Logger.Log("GetAvailableUpdates: BEGIN retrieving updates from the host");

                    new Template_LoadingPage(Widget).UpdateWidget();

                    Logger.Log("Fetching updates from server");
                    UniGetUIEndpoint endpoint = GetEndpoint();
                    using HttpClient client = CreateIpcClient(endpoint);

                    HttpResponseMessage task = await client.GetAsync("/uniget/v1/packages/updates?token=" + endpoint.Token);

                    string outputString = await task.Content.ReadAsStringAsync();

                    if (!task.IsSuccessStatusCode)
                    {
                        Logger.Log("GetAvailableUpdates: ABORT checking for updates due to StatusCode=" + task.StatusCode.ToString());
                        throw new Exception("Update fetching failed with code " + task.StatusCode.ToString());
                    }

                    cached_updates = ParsePackages(outputString);
                    update_cache_is_valid = true;
                    CacheExpirationTimer.Stop();
                    CacheExpirationTimer.Start();
                    Logger.Log("GetAvailableUpdates: SUCCESS checking for updates successfully");
                }
                catch (Exception ex)
                {
                    Logger.Log("GetAvailableUpdates: ABORTED checking for updates: An exception was thrown.");
                    result.ErrorReason = "CANNOT_FETCH_UPDATES: " + ex.Message;
                    result.Succeeded = false;
                    if (UpdateCheckFinished != null)
                        UpdateCheckFinished(this, result);
                    Logger.Log("Failed to fetch updates!");
                    Logger.Log(ex);
                    Logger.Log("END of GetAvailableUpdates()");
                    return;
                }
            }

            // Handle the updates and return only the requested ones.

            try
            {
                Logger.Log("GetAvailableUpdates: BEGIN parsing updates");
                found_updates = cached_updates;

                Package[] valid_updates = new Package[found_updates.Length];

                int skippedPackages = 0;

                for (int i = 0; i < found_updates.Length; i++)
                {
                    if ((AllowedSource == "" && found_updates[i].ManagerName != "") || (AllowedSource != "" && AllowedSource == found_updates[i].ManagerName))
                    {
                        Logger.Log($"\"{found_updates[i].ManagerName}\"");
                        valid_updates[i - skippedPackages] = found_updates[i];
                    }
                    else
                        skippedPackages++;
                }

                Package[] updates = new Package[found_updates.Length - skippedPackages];
                for (int i = 0; i < found_updates.Length - skippedPackages; i++)
                    updates[i] = valid_updates[i];

                result.Updates = updates;
                result.Count = found_updates.Length - skippedPackages;
                result.Succeeded = true;
                result.ErrorReason = "";
                if (UpdateCheckFinished != null)
                    UpdateCheckFinished(this, result);
                Logger.Log("GetAvailableUpdates: SUCCESS parsing updates.");
                Logger.Log("END of GetAvailableUpdates()");
                return;

            }
            catch (Exception ex)
            {
                Logger.Log("GetAvailableUpdates: ABORT parsing updates due to an exception thrown.");
                result.ErrorReason = "CANNOT_PROCESS_UPDATES: " + ex.Message;
                result.Succeeded = false;
                if (UpdateCheckFinished != null)
                    UpdateCheckFinished(this, result);
                Logger.Log("Failed to process updates!");
                Logger.Log(ex);
                Logger.Log("END of GetAvailableUpdates()");
                return;
            }
        }

        public void OpenWingetUI()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "unigetui://showUniGetUI",
                    UseShellExecute = true
                });
                /*HttpClient client = new();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                await client.GetAsync("/widgets/v1/open_wingetui?token=" + SessionToken);*/
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                ResetConnection();
            }
        }

        public void ViewOnWingetUI()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "unigetui://showUpdatesPage",
                    UseShellExecute = true
                });
                /*HttpClient client = new();
                client.BaseAddress = new Uri("http://localhost:7058//");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                await client.GetAsync("/widgets/v1/view_on_wingetui?token=" + SessionToken);*/
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                ResetConnection();
            }
        }

        async public void UpdatePackage(Package package)
        {
            try
            {
                UniGetUIEndpoint endpoint = GetEndpoint();
                using HttpClient client = CreateIpcClient(endpoint);
                cached_updates = cached_updates.Where((val, idx) => val != package).ToArray(); // Remove that widget from the current list

                await client.PostAsync(
                    "/uniget/v1/packages/update?token=" + endpoint.Token
                    + "&packageId=" + Uri.EscapeDataString(package.Id)
                    + "&manager=" + Uri.EscapeDataString(package.ManagerName)
                    + "&packageSource=" + Uri.EscapeDataString(package.Source),
                    null);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                ResetConnection();
            }
        }

        async public void UpdateAllPackages()
        {
            try
            {
                UniGetUIEndpoint endpoint = GetEndpoint();
                using HttpClient client = CreateIpcClient(endpoint);

                await client.PostAsync("/uniget/v1/packages/update-all?token=" + endpoint.Token, null);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                ResetConnection();
            }
        }

        async public void UpdateAllPackagesForSource(string source)
        {
            try
            {
                UniGetUIEndpoint endpoint = GetEndpoint();
                using HttpClient client = CreateIpcClient(endpoint);

                await client.PostAsync(
                    "/uniget/v1/packages/update-manager?token=" + endpoint.Token
                    + "&manager=" + Uri.EscapeDataString(source),
                    null);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString());
                ResetConnection();
            }
        }

        private static HttpClient CreateIpcClient(UniGetUIEndpoint endpoint)
        {
            HttpClient client;
            if (CanConnectTcp(endpoint.TcpPort))
            {
                Logger.Log($"Using UniGetUI TCP IPC on port {endpoint.TcpPort}");
                client = new HttpClient
                {
                    BaseAddress = new Uri($"http://localhost:{endpoint.TcpPort}")
                };
            }
            else
            {
                Logger.Log($"Using UniGetUI named pipe IPC: {endpoint.NamedPipeName}");
                SocketsHttpHandler handler = new()
                {
                    ConnectCallback = async (context, cancellationToken) =>
                    {
                        NamedPipeClientStream stream = new(".", endpoint.NamedPipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                        await stream.ConnectAsync(5000, cancellationToken);
                        return stream;
                    }
                };
                client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://localhost")
                };
            }

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private static bool CanConnectTcp(int port)
        {
            if (port <= 0)
            {
                return false;
            }

            try
            {
                using TcpClient tcpClient = new();
                Task connectTask = tcpClient.ConnectAsync("127.0.0.1", port);
                return connectTask.Wait(TimeSpan.FromMilliseconds(300)) && tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static UniGetUIEndpoint GetEndpoint()
        {
            string endpointDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI", "Configuration", "IpcApiEndpoints");
            if (!Directory.Exists(endpointDir))
            {
                throw new FileNotFoundException("UniGetUI IPC endpoint directory was not found");
            }

            HashSet<int> runningPids = Process.GetProcessesByName("UniGetUI").Select(process => process.Id).ToHashSet();
            UniGetUIEndpoint[] endpoints = Directory.GetFiles(endpointDir, "*.json")
                .Select(path =>
                {
                    UniGetUIEndpoint endpoint = ParseEndpoint(File.ReadAllText(path));
                    endpoint.LastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
                    return endpoint;
                })
                .Where(endpoint => endpoint.Token != "" && endpoint.NamedPipeName != "")
                .OrderByDescending(endpoint => runningPids.Contains(endpoint.ProcessId))
                .ThenByDescending(endpoint => endpoint.SessionKind.Equals("headless", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(endpoint => endpoint.LastWriteTimeUtc)
                .ToArray();

            if (endpoints.Length == 0)
            {
                throw new FileNotFoundException("No valid UniGetUI IPC endpoint was found");
            }

            return endpoints[0];
        }

        private static UniGetUIStatus ParseStatus(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            int buildNumber = ReadInt(root, "buildNumber");
            return new UniGetUIStatus
            {
                Version = ReadString(root, "version", buildNumber.ToString(CultureInfo.InvariantCulture)),
                BuildNumber = buildNumber == 0 ? (int)MINIMUM_REQUIRED_HOST_VERSION : buildNumber
            };
        }

        private static Package[] ParsePackages(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return document.RootElement.EnumerateArray().Select(package => new Package
            {
                Name = ReadString(package, "name"),
                Id = ReadString(package, "id"),
                Version = ReadString(package, "version"),
                NewVersion = ReadString(package, "newVersion"),
                Source = ReadString(package, "source"),
                ManagerName = ReadString(package, "manager"),
                Icon = ReadString(package, "icon", "https://marticliment.com/resources/widgets/package_color.png")
            }).ToArray();
        }

        private static UniGetUIEndpoint ParseEndpoint(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            return new UniGetUIEndpoint
            {
                SessionKind = ReadString(root, "sessionKind"),
                Token = ReadString(root, "token"),
                ProcessId = ReadInt(root, "processId"),
                Transport = ReadInt(root, "transport", 1),
                TransportName = ReadString(root, "transportName"),
                TcpPort = ReadInt(root, "tcpPort", 7058),
                NamedPipeName = ReadString(root, "namedPipeName", "UniGetUI.IPC")
            };
        }

        private static string ReadString(JsonElement element, string propertyName, string fallback = "")
        {
            return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? fallback
                : fallback;
        }

        private static int ReadInt(JsonElement element, string propertyName, int fallback = 0)
        {
            return element.TryGetProperty(propertyName, out JsonElement property) && property.TryGetInt32(out int value)
                ? value
                : fallback;
        }
    }

    public class Package
    {
        public string Name { get; set; } = "";
        public string Id { get; set; } = "";
        public string Version { get; set; } = "";
        public string NewVersion { get; set; } = "";
        public string Source { get; set; } = "";
        public string ManagerName { get; set; } = "";
        public string Manager
        {
            get => ManagerName;
            set => ManagerName = value;
        }
        public string Icon { get; set; } = "https://marticliment.com/resources/widgets/package_color.png";
        public bool isValid = true;

        public Package()
        {
        }

        public Package(string packageString)
        {
            try
            {
                string[] packageParts = packageString.Split('|');
                Name = packageParts[0];
                Id = packageParts[1];
                Version = packageParts[2];
                NewVersion = packageParts[3];
                Source = packageParts[4];
                ManagerName = packageParts[5];
                if (packageParts[6] != "")
                    Icon = packageParts[6];
                else
                    Icon = "https://marticliment.com/resources/widgets/package_color.png";
            }
            catch
            {
                isValid = false;
                Name = "";
                Id = "";
                Version = "";
                NewVersion = "";
                Source = "";
                ManagerName = "";
                Icon = "https://marticliment.com/resources/widgets/package_color.png";
                Logger.Log("Can't construct package, given packageString=" + packageString);
            }
        }
    }

    public class UniGetUIEndpoint
    {
        public string SessionKind { get; set; } = "";
        public string Token { get; set; } = "";
        public int ProcessId { get; set; }
        public int Transport { get; set; } = 1;
        public string TransportName { get; set; } = "";
        public int TcpPort { get; set; } = 7058;
        public string NamedPipeName { get; set; } = "UniGetUI.IPC";
        public DateTime LastWriteTimeUtc { get; set; }
    }

    public class UniGetUIStatus
    {
        public string Version { get; set; } = "";
        public int BuildNumber { get; set; }
    }

    public class UpdatesCheckFinishedEventArgs : EventArgs
    {
        public Package[] Updates { get; set; }
        public int Count { get; set; }
        public bool Succeeded { get; set; }
        public GenericWidget widget { get; set; }
        public string ErrorReason { get; set; } = "";

        public UpdatesCheckFinishedEventArgs(GenericWidget widget)
        {
            Updates = new Package[0];
            this.widget = widget;
        }
    }
}

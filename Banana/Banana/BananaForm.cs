using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;

namespace Banana
{
    public partial class Banana : Form
    {
        public Banana()
        {
            InitializeComponent();
        }

        private readonly string baseUrl = "https://raw.githubusercontent.com/ShibaGT/Banana/main/";
        private const string modsListUrl = "https://raw.githubusercontent.com/SteveTheAnimator/Banana_Cleaned/refs/heads/main/Banana/Banana/modInfo.txt";     

        private static readonly string gtaglocation = GetGtPath();
        private readonly string bananaDir = Path.Combine(gtaglocation, "Gorilla Tag_Data", "Banana");
        private readonly string currentVersion = "1.0.2";

        class ModEntry
        {
            public required string CheckboxName { get; set; }
            public required string Source { get; set; }
            public required string FileName { get; set; }
            public required string StatusText { get; set; }
            public bool IsDirectUrl { get; set; }
        }

        private static string GetGtPath()
        {
            using var steamKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var steam = steamKey?.GetValue("SteamPath")?.ToString().Replace("/", "\\");
            if (steam == null) return "Steam not found";

            var vdfPath = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdfPath)) return "Not installed";
            var vdf = File.ReadAllText(vdfPath);
            var libs = Regex.Matches(vdf, "\"path\"\\s*\"(.*?)\"");

            foreach (Match m in libs)
            {
                var libPath = m.Groups[1].Value.Replace("\\\\", "\\");
                var manifestPath = Path.Combine(libPath, "steamapps", "appmanifest_1533390.acf");
                if (File.Exists(manifestPath))
                    return Path.Combine(libPath, "steamapps", "common", "Gorilla Tag");
            }

            var mainManifest = Path.Combine(steam, "steamapps", "appmanifest_1533390.acf");
            if (File.Exists(mainManifest))
                return Path.Combine(steam, "steamapps", "common", "Gorilla Tag");

            return "Not installed";
        }

        private readonly WebClient w = new();

        private string githubDownload;

        private async Task GetDownloadFromGithub(string repo)
        {
            string url = $"https://api.github.com/repos/{repo}/releases/latest";
            using HttpClient client = new();

            client.DefaultRequestHeaders.UserAgent.ParseAdd("CSharpApp");
            string response = await client.GetStringAsync(url).ConfigureAwait(false);
            var release = Newtonsoft.Json.Linq.JObject.Parse(response);
            githubDownload = release["assets"]?[0]?["browser_download_url"]?.ToString() ?? "(no download)";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string versionPath = Path.Combine(bananaDir, "banana_version.txt");
            version.Text = $"Banana Version: {currentVersion}";

            Directory.CreateDirectory(bananaDir);

            File.WriteAllText(versionPath, currentVersion);

            label1.Text = gtaglocation;
            status.Text = "init path";

            DisableEnableUpdate();
        }

        private void game_Click(object sender, EventArgs e)
        {
            string cleanPath = gtaglocation.Replace(@"\\", @"\");
            Process.Start("explorer.exe", cleanPath);
        }

        private void mods_Click(object sender, EventArgs e)
        {
            string cleanPath = gtaglocation.Replace(@"\\", @"\");
            Process.Start("explorer.exe", Path.Combine(cleanPath, "BepInEx", "plugins"));
        }

        public static void BepInExInstall()
        {
            string downloadUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip";
            string downloadPath = Path.Combine(Path.GetTempPath(), "BepInEx_win_x64_5.4.23.2.zip");
            string extractTempPath = Path.Combine(Path.GetTempPath(), "BepInExExtract");
            string targetPath = gtaglocation.Replace(@"\\", @"\");
            try
            {
                using (WebClient client = new())
                {
                    client.DownloadFile(downloadUrl, downloadPath);
                }

                if (Directory.Exists(extractTempPath))
                    Directory.Delete(extractTempPath, true);

                ZipFile.ExtractToDirectory(downloadPath, extractTempPath);

                CopyFilesRecursively(new DirectoryInfo(extractTempPath), new DirectoryInfo(targetPath));

                File.Delete(downloadPath);
                Directory.Delete(extractTempPath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
            Directory.CreateDirectory(Path.Combine(targetPath, "BepInEx", "plugins"));

            File.WriteAllText(Path.Combine(targetPath, "BepInEx", "config", "BepInEx.cfg"),       "[Caching]\r\n\r\n## Enable/disable assembly metadata cache\r\n## Enabling this will speed up discovery of plugins and patchers by caching the metadata of all types BepInEx discovers.\r\n# Setting type: Boolean\r\n# Default value: true\r\nEnableAssemblyCache = true\r\n\r\n[Chainloader]\r\n\r\n## If enabled, hides BepInEx Manager GameObject from Unity.\r\n## This can fix loading issues in some games that attempt to prevent BepInEx from being loaded.\r\n## Use this only if you know what this option means, as it can affect functionality of some older plugins.\r\n## \r\n# Setting type: Boolean\r\n# Default value: false\r\nHideManagerGameObject = true\r\n\r\n[Harmony.Logger]\r\n\r\n## Specifies which Harmony log channels to listen to.\r\n## NOTE: IL channel dumps the whole patch methods, use only when needed!\r\n# Setting type: LogChannel\r\n# Default value: Warn, Error\r\n# Acceptable values: None, Info, IL, Warn, Error, Debug, All\r\n# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)\r\nLogChannels = Warn, Error\r\n\r\n[Logging]\r\n\r\n## Enables showing unity log messages in the BepInEx logging system.\r\n# Setting type: Boolean\r\n# Default value: true\r\nUnityLogListening = true\r\n\r\n## If enabled, writes Standard Output messages to Unity log\r\n## NOTE: By default, Unity does so automatically. Only use this option if no console messages are visible in Unity log\r\n## \r\n# Setting type: Boolean\r\n# Default value: false\r\nLogConsoleToUnityLog = false\r\n\r\n[Logging.Console]\r\n\r\n## Enables showing a console for log output.\r\n# Setting type: Boolean\r\n# Default value: false\r\nEnabled = true\r\n\r\n## If enabled, will prevent closing the console (either by deleting the close button or in other platform-specific way).\r\n# Setting type: Boolean\r\n# Default value: false\r\nPreventClose = false\r\n\r\n## If true, console is set to the Shift-JIS encoding, otherwise UTF-8 encoding.\r\n# Setting type: Boolean\r\n# Default value: false\r\nShiftJisEncoding = false\r\n\r\n## Hints console manager on what handle to assign as StandardOut. Possible values:\r\n## Auto - lets BepInEx decide how to redirect console output\r\n## ConsoleOut - prefer redirecting to console output; if possible, closes original standard output\r\n## StandardOut - prefer redirecting to standard output; if possible, closes console out\r\n## \r\n# Setting type: ConsoleOutRedirectType\r\n# Default value: Auto\r\n# Acceptable values: Auto, ConsoleOut, StandardOut\r\nStandardOutType = Auto\r\n\r\n## Which log levels to show in the console output.\r\n# Setting type: LogLevel\r\n# Default value: Fatal, Error, Warning, Message, Info\r\n# Acceptable values: None, Fatal, Error, Warning, Message, Info, Debug, All\r\n# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)\r\nLogLevels = Fatal, Error, Warning, Message, Info\r\n\r\n[Logging.Disk]\r\n\r\n## Include unity log messages in log file output.\r\n# Setting type: Boolean\r\n# Default value: false\r\nWriteUnityLog = false\r\n\r\n## Appends to the log file instead of overwriting, on game startup.\r\n# Setting type: Boolean\r\n# Default value: false\r\nAppendLog = false\r\n\r\n## Enables writing log messages to disk.\r\n# Setting type: Boolean\r\n# Default value: true\r\nEnabled = true\r\n\r\n## Which log leves are saved to the disk log output.\r\n# Setting type: LogLevel\r\n# Default value: Fatal, Error, Warning, Message, Info\r\n# Acceptable values: None, Fatal, Error, Warning, Message, Info, Debug, All\r\n# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)\r\nLogLevels = Fatal, Error, Warning, Message, Info\r\n\r\n[Preloader]\r\n\r\n## Enables or disables runtime patches.\r\n## This should always be true, unless you cannot start the game due to a Harmony related issue (such as running .NET Standard runtime) or you know what you're doing.\r\n# Setting type: Boolean\r\n# Default value: true\r\nApplyRuntimePatches = true\r\n\r\n## Specifies which MonoMod backend to use for Harmony patches. Auto uses the best available backend.\r\n## This setting should only be used for development purposes (e.g. debugging in dnSpy). Other code might override this setting.\r\n# Setting type: MonoModBackend\r\n# Default value: auto\r\n# Acceptable values: auto, dynamicmethod, methodbuilder, cecil\r\nHarmonyBackend = auto\r\n\r\n## If enabled, BepInEx will save patched assemblies into BepInEx/DumpedAssemblies.\r\n## This can be used by developers to inspect and debug preloader patchers.\r\n# Setting type: Boolean\r\n# Default value: false\r\nDumpAssemblies = false\r\n\r\n## If enabled, BepInEx will load patched assemblies from BepInEx/DumpedAssemblies instead of memory.\r\n## This can be used to be able to load patched assemblies into debuggers like dnSpy.\r\n## If set to true, will override DumpAssemblies.\r\n# Setting type: Boolean\r\n# Default value: false\r\nLoadDumpedAssemblies = false\r\n\r\n## If enabled, BepInEx will call Debugger.Break() once before loading patched assemblies.\r\n## This can be used with debuggers like dnSpy to install breakpoints into patched assemblies before they are loaded.\r\n# Setting type: Boolean\r\n# Default value: false\r\nBreakBeforeLoadAssemblies = false\r\n\r\n[Preloader.Entrypoint]\r\n\r\n## The local filename of the assembly to target.\r\n# Setting type: String\r\n# Default value: UnityEngine.CoreModule.dll\r\nAssembly = UnityEngine.CoreModule.dll\r\n\r\n## The name of the type in the entrypoint assembly to search for the entrypoint method.\r\n# Setting type: String\r\n# Default value: Application\r\nType = Application\r\n\r\n## The name of the method in the specified entrypoint assembly and type to hook and load Chainloader from.\r\n# Setting type: String\r\n# Default value: .cctor\r\nMethod = .cctor\r\n\r\n");
        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var directory in source.GetDirectories())
            {
                var targetSubDir = target.CreateSubdirectory(directory.Name);
                CopyFilesRecursively(directory, targetSubDir);
            }

            foreach (var file in source.GetFiles())
            {
                var targetFilePath = Path.Combine(target.FullName, file.Name);
                file.CopyTo(targetFilePath, true);
            }
        }
        
        // Fetches the mods, who would've known?
        private async Task<List<ModEntry>> FetchModsList()
        {
            using HttpClient client = new();
            var lines = (await client.GetStringAsync(modsListUrl).ConfigureAwait(false))
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var mods = new List<ModEntry>();
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 4) continue;
                var isDirect = parts[1].StartsWith("http", StringComparison.OrdinalIgnoreCase); // Hurts my eyes but hey, it works.
                mods.Add(new ModEntry
                {
                    CheckboxName = parts[0],
                    Source = parts[1],
                    FileName = parts[2],
                    StatusText = parts[3],
                    IsDirectUrl = isDirect
                });
            }
            return mods;
        }
        private async void download_Click(object sender, EventArgs e)
        {
            string pluginsloc = Path.Combine(gtaglocation.Replace(@"\\", @"\"), "BepInEx", "plugins");

            if (bepinex.Checked)
            {
                BepInExInstall();
                status.Text = "bepinex";
            }

            var mods = await FetchModsList().ConfigureAwait(false);

            foreach (var mod in mods)
            {
                var field = GetType().GetField(mod.CheckboxName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field?.GetValue(this) is CheckBox cb && cb.Checked)
                {
                    // Rise and shine, mister freeman. Rise... and... shine...

                    string destPath = Path.Combine(pluginsloc, mod.FileName);
                    if (mod.IsDirectUrl)
                    {
                        w.DownloadFile(mod.Source, destPath);
                    }
                    else
                    {
                        await GetDownloadFromGithub(mod.Source).ConfigureAwait(false);
                        w.DownloadFile(githubDownload, destPath);
                    }
                }
            }

            MessageBox.Show("Finished installing mods!");
        }

        private async void iidk_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void disableenable_Click(object sender, EventArgs e)
        {
            string dllPath = Path.Combine(gtaglocation, "winhttp.dll");
            string dPath = Path.Combine(gtaglocation, "winhttp.d");
            if (disableenable.BackColor == Color.Red)
                File.Move(dllPath, dPath, true);
            else
                File.Move(dPath, dllPath, true);

            DisableEnableUpdate();
        }

        private void DisableEnableUpdate()
        {
            string dllPath = Path.Combine(gtaglocation, "winhttp.dll");
            if (File.Exists(dllPath))
            {
                disableenable.BackColor = Color.Red;
                disableenable.Text = "Disable Mods";
            }
            else
            {
                disableenable.BackColor = Color.Green;
                disableenable.Text = "Enable Mods";
            }
        }

        private void discord_Click(object sender, EventArgs e)
        {
            var ps = new ProcessStartInfo("https://discord.gg/NtgqZkwuPy")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }
    }
}

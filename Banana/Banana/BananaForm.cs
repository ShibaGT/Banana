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

        //vars and shit rahh
        public static string baseUrl = "https://raw.githubusercontent.com/ShibaGT/Banana/main/";

        static string gtaglocation = getgtpath();
        string bananaDir = Path.Combine(gtaglocation, "Gorilla Tag_Data", "Banana");
        string currentVersion = "1.0.4";
        static string getgtpath() //YES this is chatgpt YES im lazy YES the rest is coded by me fuck OFF!
        {
            string steam = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath")?.ToString().Replace("/", "\\");
            if (steam == null) return "Steam not found";

            string vdf = File.ReadAllText(Path.Combine(steam, "steamapps", "libraryfolders.vdf"));
            var libs = Regex.Matches(vdf, "\"path\"\\s*\"(.*?)\"");

            foreach (Match m in libs)
                if (File.Exists(Path.Combine(m.Groups[1].Value.Replace("\\\\", "\\"), "steamapps", "appmanifest_1533390.acf")))
                    return Path.Combine(m.Groups[1].Value, "steamapps", "common", "Gorilla Tag");

            if (File.Exists(Path.Combine(steam, "steamapps", "appmanifest_1533390.acf")))
                return Path.Combine(steam, "steamapps", "common", "Gorilla Tag");

            return "Not installed";
        }

        WebClient w = new WebClient();

        string githubDownload;

        private async Task GetDownloadFromGithub(string repo)
        {
            //iiDk-the-actual/iis.Stupid.Menu example dingus
            string url = $"https://api.github.com/repos/{repo}/releases/latest";
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CSharpApp");
            string response = await client.GetStringAsync(url);
            Newtonsoft.Json.Linq.JObject release = Newtonsoft.Json.Linq.JObject.Parse(response);
            githubDownload = release["assets"][0]["browser_download_url"]?.ToString() ?? "(no download)";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string versionPath = Path.Combine(bananaDir, "banana_version.txt");
            version.Text = "Banana Version: " + currentVersion;
            File.WriteAllText(versionPath, currentVersion);

            label1.Text = gtaglocation;
            status.Text = "init path";

            disableenableupdate();
        }

        private void game_Click(object sender, EventArgs e)
        {
            string cleanPath = gtaglocation.Replace(@"\\", @"\");
            Process.Start("explorer.exe", cleanPath);
        }

        private void mods_Click(object sender, EventArgs e)
        {
            string cleanPath = gtaglocation.Replace(@"\\", @"\");
            Process.Start("explorer.exe", cleanPath + "\\BepInEx\\plugins");
        }

        public static void bepinexshit() //chatgpt bepinex shit yes yes ik wha ta skid fuck you
        {
            string downloadUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip";
            string downloadPath = Path.Combine(Path.GetTempPath(), "BepInEx_win_x64_5.4.23.2.zip");
            string extractTempPath = Path.Combine(Path.GetTempPath(), "BepInExExtract");
            string targetPath = gtaglocation.Replace(@"\\", @"\");
            try
            {
                using (WebClient client = new WebClient())
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
            Directory.CreateDirectory(targetPath + "\\BepInEx\\plugins");

            File.WriteAllText(targetPath + "\\BepInEx\\config\\BepInEx.cfg", "[Caching]\r\n\r\n## Enable/disable assembly metadata cache\r\n## Enabling this will speed up discovery of plugins and patchers by caching the metadata of all types BepInEx discovers.\r\n# Setting type: Boolean\r\n# Default value: true\r\nEnableAssemblyCache = true\r\n\r\n[Chainloader]\r\n\r\n## If enabled, hides BepInEx Manager GameObject from Unity.\r\n## This can fix loading issues in some games that attempt to prevent BepInEx from being loaded.\r\n## Use this only if you know what this option means, as it can affect functionality of some older plugins.\r\n## \r\n# Setting type: Boolean\r\n# Default value: false\r\nHideManagerGameObject = true\r\n\r\n[Harmony.Logger]\r\n\r\n## Specifies which Harmony log channels to listen to.\r\n## NOTE: IL channel dumps the whole patch methods, use only when needed!\r\n# Setting type: LogChannel\r\n# Default value: Warn, Error\r\n# Acceptable values: None, Info, IL, Warn, Error, Debug, All\r\n# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)\r\nLogChannels = Warn, Error\r\n\r\n[Logging]\r\n\r\n## Enables showing unity log messages in the BepInEx logging system.\r\n# Setting type: Boolean\r\n# Default value: true\r\nUnityLogListening = true\r\n\r\n## If enabled, writes Standard Output messages to Unity log\r\n## NOTE: By default, Unity does so automatically. Only use this option if no console messages are visible in Unity log\r\n## \r\n# Setting type: Boolean\r\n# Default value: false\r\nLogConsoleToUnityLog = false\r\n\r\n[Logging.Console]\r\n\r\n## Enables showing a console for log output.\r\n# Setting type: Boolean\r\n# Default value: false\r\nEnabled = true\r\n\r\n## If enabled, will prevent closing the console (either by deleting the close button or in other platform-specific way).\r\n# Setting type: Boolean\r\n# Default value: false\r\nPreventClose = false\r\n\r\n## If true, console is set to the Shift-JIS encoding, otherwise UTF-8 encoding.\r\n# Setting type: Boolean\r\n# Default value: false\r\nShiftJisEncoding = false\r\n\r\n## Hints console manager on what handle to assign as StandardOut. Possible values:\r\n## Auto - lets BepInEx decide how to redirect console output\r\n## ConsoleOut - prefer redirecting to console output; if possible, closes original standard output\r\n## StandardOut - prefer redirecting to standard output; if possible, closes console out\r\n## \r\n# Setting type: ConsoleOutRedirectType\r\n# Default value: Auto\r\n# Acceptable values: Auto, ConsoleOut, StandardOut\r\nStandardOutType = Auto\r\n\r\n## Which log levels to show in the console output.\r\n# Setting type: LogLevel\r\n# Default value: Fatal, Error, Warning, Message, Info\r\n# Acceptable values: None, Fatal, Error, Warning, Message, Info, Debug, All\r\n# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)\r\nLogLevels = Fatal, Error, Warning, Message, Info\r\n\r\n[Logging.Disk]\r\n\r\n## Include unity log messages in log file output.\r\n# Setting type: Boolean\r\n# Default value: false\r\nWriteUnityLog = false\r\n\r\n## Appends to the log file instead of overwriting, on game startup.\r\n# Setting type: Boolean\r\n# Default value: false\r\nAppendLog = false\r\n\r\n## Enables writing log messages to disk.\r\n# Setting type: Boolean\r\n# Default value: true\r\nEnabled = true\r\n\r\n## Which log leves are saved to the disk log output.\r\n# Setting type: LogLevel\r\n# Default value: Fatal, Error, Warning, Message, Info\r\n# Acceptable values: None, Fatal, Error, Warning, Message, Info, Debug, All\r\n# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)\r\nLogLevels = Fatal, Error, Warning, Message, Info\r\n\r\n[Preloader]\r\n\r\n## Enables or disables runtime patches.\r\n## This should always be true, unless you cannot start the game due to a Harmony related issue (such as running .NET Standard runtime) or you know what you're doing.\r\n# Setting type: Boolean\r\n# Default value: true\r\nApplyRuntimePatches = true\r\n\r\n## Specifies which MonoMod backend to use for Harmony patches. Auto uses the best available backend.\r\n## This setting should only be used for development purposes (e.g. debugging in dnSpy). Other code might override this setting.\r\n# Setting type: MonoModBackend\r\n# Default value: auto\r\n# Acceptable values: auto, dynamicmethod, methodbuilder, cecil\r\nHarmonyBackend = auto\r\n\r\n## If enabled, BepInEx will save patched assemblies into BepInEx/DumpedAssemblies.\r\n## This can be used by developers to inspect and debug preloader patchers.\r\n# Setting type: Boolean\r\n# Default value: false\r\nDumpAssemblies = false\r\n\r\n## If enabled, BepInEx will load patched assemblies from BepInEx/DumpedAssemblies instead of memory.\r\n## This can be used to be able to load patched assemblies into debuggers like dnSpy.\r\n## If set to true, will override DumpAssemblies.\r\n# Setting type: Boolean\r\n# Default value: false\r\nLoadDumpedAssemblies = false\r\n\r\n## If enabled, BepInEx will call Debugger.Break() once before loading patched assemblies.\r\n## This can be used with debuggers like dnSpy to install breakpoints into patched assemblies before they are loaded.\r\n# Setting type: Boolean\r\n# Default value: false\r\nBreakBeforeLoadAssemblies = false\r\n\r\n[Preloader.Entrypoint]\r\n\r\n## The local filename of the assembly to target.\r\n# Setting type: String\r\n# Default value: UnityEngine.CoreModule.dll\r\nAssembly = UnityEngine.CoreModule.dll\r\n\r\n## The name of the type in the entrypoint assembly to search for the entrypoint method.\r\n# Setting type: String\r\n# Default value: Application\r\nType = Application\r\n\r\n## The name of the method in the specified entrypoint assembly and type to hook and load Chainloader from.\r\n# Setting type: String\r\n# Default value: .cctor\r\nMethod = .cctor\r\n\r\n");
        }


        public static void ueZip() //chatgpt bepinex shit yes yes ik wha ta skid fuck you
        {
            string downloadUrl = $"{baseUrl}Banana/ModFiles/UnityFixV3_LTS.zip";
            string downloadPath = Path.Combine(Path.GetTempPath(), "UnityFixV3_LTS.zip");
            string extractTempPath = Path.Combine(Path.GetTempPath(), "UEExtract");
            string targetPath = gtaglocation.Replace(@"\\", @"\") + "\\BepInEx\\plugins";
            try
            {
                using (WebClient client = new WebClient())
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
        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) //chatgpt too pal
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

        public static void DownloadFromRepo(string mod, string to)
        {
            new WebClient().DownloadFile($"{baseUrl}Banana/ModFiles/{mod}", to);
        }

        private async void download_Click(object sender, EventArgs e)
        {
            string pluginsloc = Path.Combine(gtaglocation.Replace(@"\\", @"\"), "BepInEx", "plugins\\");

            if (bepinex.Checked)
            {
                bepinexshit();
                status.Text = "bepinex";
            }

            if (ue.Checked)
            {
                ueZip();
                status.Text = "ue";
            }

            if (utilla.Checked)
            {
                await GetDownloadFromGithub("iiDk-the-actual/Utilla-Public");
                w.DownloadFile(githubDownload, pluginsloc + "Utilla.dll");
                status.Text = "utilla";
            }

            if (iidk.Checked)
            {
                await GetDownloadFromGithub("iiDk-the-actual/iis.Stupid.Menu");
                w.DownloadFile(githubDownload, pluginsloc + "iis Stupid Menu.dll");
                status.Text = "iidk menu sigma";
            }
            if (sodium.Checked)
            {
                await GetDownloadFromGithub("TAGMONKE/Sodium");
                w.DownloadFile(githubDownload, pluginsloc + "Sodium.dll");
                status.Text = "sodium";
            }
            if (forpreds.Checked)
            {
                await GetDownloadFromGithub("iiDk-the-actual/ForeverPreds");
                w.DownloadFile(githubDownload, pluginsloc + "ForeverPreds.dll");
                status.Text = "forever preds";
            }
            if (forhz.Checked)
            {
                await GetDownloadFromGithub("iiDk-the-actual/ForeverHz");
                w.DownloadFile(githubDownload, pluginsloc + "ForeverHz.dll");
                status.Text = "hz mod";
            }
            if (cosm.Checked)
            {
                await GetDownloadFromGithub("iiDk-the-actual/ForeverCosmetx");
                w.DownloadFile(githubDownload, pluginsloc + "ForeverCosmetx.dll");
                status.Text = "cosmetx";
            }
            if (media.Checked)
            {
                await GetDownloadFromGithub("iiDk-the-actual/GorillaMedia");
                w.DownloadFile(githubDownload, pluginsloc + "GorillaMedia.dll");
                status.Text = "media";
            }

            if (haste.Checked)
            {
                DownloadFromRepo("Haste.dll", pluginsloc + "Haste.dll");
                status.Text = "haste";
            }

            if (walksim.Checked)
            {
                DownloadFromRepo("WalkSimulator-NonUtilla.dll", pluginsloc + "WalkSimulator-NonUtilla.dll");
                status.Text = "walksim";
            }

            if (unknown.Checked)
            {
                DownloadFromRepo("Unkown'sNameTagMod.dll", pluginsloc + "Unkown'sNameTagMod.dll"); 
                status.Text = "unknowntags";
            }
            if (flick.Checked)
            {
                DownloadFromRepo("unknown's DC Flick Mod.dll", pluginsloc + "unknown's DC Flick Mod.dll");
                status.Text = "flick";
            }

            MessageBox.Show("Finished installing mods!");
        }

        private void disableenable_Click(object sender, EventArgs e)
        {
            if (disableenable.BackColor == Color.Red)
                System.IO.File.Move($"{gtaglocation}\\winhttp.dll", $"{gtaglocation}\\winhttp.d");
            else
                System.IO.File.Move($"{gtaglocation}\\winhttp.d", $"{gtaglocation}\\winhttp.dll");

            disableenableupdate();
        }

        private void disableenableupdate()
        {
            if (File.Exists(gtaglocation + "\\winhttp.dll"))
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

using Microsoft.Win32;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Banana
{
    public partial class Banana : Form
    {
        public static string BaseUrl { get; } = "https://raw.githubusercontent.com/ShibaGT/Banana/main/";

        private string _gtagLocation = DetectGorillaTagPath();
        private string _bananaDir;
        private readonly string _currentVersion = "1.2.0";
        string githubVersion;

        private static readonly HttpClient s_httpClient = new HttpClient();

        public Banana()
        {
            InitializeComponent();

            try
            {
                s_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Banana/1.0");
            }
            catch { }

            if (_gtagLocation is null or "Not installed" or "Steam not found")
            {
                MessageBox.Show("Gorilla Tag installation not found. Please select the Gorilla Tag folder manually.");
                _gtagLocation = "";
            }
            else
            {
                _bananaDir = Path.Combine(_gtagLocation, "Gorilla Tag_Data", "Banana");
                Directory.CreateDirectory(_bananaDir);
            }
        }

        private static string DetectGorillaTagPath()
        {
            try
            {
                var steamKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                var steamPathObj = steamKey?.GetValue("SteamPath");
                if (steamPathObj is not string steamPath) return "Steam not found";

                steamPath = steamPath.Replace("/", "\\");
                var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdfPath))
                {
                    if (File.Exists(Path.Combine(steamPath, "steamapps", "appmanifest_1533390.acf")))
                        return Path.Combine(steamPath, "steamapps", "common", "Gorilla Tag");

                    return "Not installed";
                }

                var vdf = File.ReadAllText(vdfPath);
                var libs = Regex.Matches(vdf, "\"path\"\\s*\"(.*?)\"");
                foreach (Match m in libs)
                {
                    var candidate = m.Groups[1].Value.Replace("\\\\", "\\");
                    var manifest = Path.Combine(candidate, "steamapps", "appmanifest_1533390.acf");
                    if (File.Exists(manifest))
                        return Path.Combine(candidate, "steamapps", "common", "Gorilla Tag");
                }

                if (File.Exists(Path.Combine(steamPath, "steamapps", "appmanifest_1533390.acf")))
                    return Path.Combine(steamPath, "steamapps", "common", "Gorilla Tag");

                return "Not installed";
            }
            catch
            {
                return "Not installed";
            }
        }

        private async Task<string?> GetDownloadFromGithub(string repo)
        {
            try
            {
                var url = $"https://api.github.com/repos/{repo}/releases/latest";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd("BananaApp/1.0");
                using var resp = await s_httpClient.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var body = await resp.Content.ReadAsStringAsync();
                var release = Newtonsoft.Json.Linq.JObject.Parse(body);
                return release["assets"]?[0]?["browser_download_url"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task GetVersionFromGithub(string repo)
        {
            string url = $"https://api.github.com/repos/{repo}/releases/latest";
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CSharpApp");
            string response = await client.GetStringAsync(url);
            Newtonsoft.Json.Linq.JObject release = Newtonsoft.Json.Linq.JObject.Parse(response);
            githubVersion = release["tag_name"]?.ToString() ?? "(no download)";
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                var versionPath = Path.Combine(_bananaDir, "banana_version.txt");
                version.Text = "Banana Version: " + _currentVersion;
                File.WriteAllText(versionPath, _currentVersion);

                label1.Text = _gtagLocation;
                status.Text = "init path";

                UpdateSettings();
                UpdateVersions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization error: {ex.Message}");
            }
        }

        private void game_Click(object sender, EventArgs e)
        {
            try
            {
                var cleanPath = Path.GetFullPath(_gtagLocation);
                var psi = new ProcessStartInfo(cleanPath) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open folder: {ex.Message}");
            }
        }

        private void mods_Click(object sender, EventArgs e)
        {
            try
            {
                var plugins = Path.Combine(_gtagLocation, "BepInEx", "plugins");
                var psi = new ProcessStartInfo(plugins) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open mods folder: {ex.Message}");
            }
        }

        public static async Task InstallBepInEx(string installTarget, string baseUrl)
        {
            const string downloadUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.4/BepInEx_win_x64_5.4.23.4.zip";
            var tempZip = Path.Combine(Path.GetTempPath(), "BepInEx_win_x64_5.4.23.4.zip");
            var extractTemp = Path.Combine(Path.GetTempPath(), "BepInExExtract");

            try
            {
                using var http = new HttpClient();
                using var resp = await http.GetAsync(downloadUrl).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();

                await using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await resp.Content.CopyToAsync(fs).ConfigureAwait(false);
                }

                if (Directory.Exists(extractTemp))
                    Directory.Delete(extractTemp, true);

                ZipFile.ExtractToDirectory(tempZip, extractTemp);

                CopyFilesRecursively(new DirectoryInfo(extractTemp), new DirectoryInfo(installTarget));

                File.Delete(tempZip);
                Directory.Delete(extractTemp, true);

                Directory.CreateDirectory(Path.Combine(installTarget, "BepInEx", "plugins"));
                Directory.CreateDirectory(Path.Combine(installTarget, "BepInEx", "config"));

                using var client2 = new HttpClient();
                var configText = await client2.GetStringAsync($"{baseUrl}config.txt").ConfigureAwait(false);
                File.WriteAllText(Path.Combine(installTarget, "BepInEx", "config", "BepInEx.cfg"), configText);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to install BepInEx", ex);
            }
        }

        public async Task InstallUnityFix()
        {
            status.InvokeIfRequired(() => status.Text = "ue");
            var downloadUrl = $"{BaseUrl}Banana/ModFiles/UnityFixV3_LTS.zip";
            var tempZip = Path.Combine(Path.GetTempPath(), "UnityFixV3_LTS.zip");
            var extractTemp = Path.Combine(Path.GetTempPath(), "UEExtract");
            var targetPath = Path.Combine(_gtagLocation, "BepInEx", "plugins");

            try
            {
                using var resp = await s_httpClient.GetAsync(downloadUrl).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();

                await using (var fs = new FileStream(tempZip, FileMode.Create))
                {
                    await resp.Content.CopyToAsync(fs).ConfigureAwait(false);
                }

                if (Directory.Exists(extractTemp))
                    Directory.Delete(extractTemp, true);

                ZipFile.ExtractToDirectory(tempZip, extractTemp);

                CopyFilesRecursively(new DirectoryInfo(extractTemp), new DirectoryInfo(targetPath));

                File.Delete(tempZip);
                Directory.Delete(extractTemp, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while installing UnityFix: {ex.Message}");
            }
        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

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

        private async Task DownloadFileToPath(string url, string destinationFilePath)
        {
            var destDir = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            using var resp = await s_httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            await using var contentStream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);
        }

        public (CheckBox checkBox, string repo, string outputFile, string statusText, Label versionlabel)[] GithubMods
        {
            get
            {
                return new (CheckBox, string, string, string, Label)[]
                {
                       (utilla, "Seralyth/Utilla", "Utilla.dll", "utilla", utillav),
                       (iidk, "Seralyth/Seralyth-Menu", "Seralyth-Menu.dll", "seralyth menu sigma", iiv),
                       (libre, "iiDk-the-actual/LibrePad", "LibrePad.dll", "libre", librev),
                       (forpreds, "iiDk-the-actual/ForeverPreds", "ForeverPreds.dll", "forever preds", predv),
                       (forhz, "iiDk-the-actual/ForeverHz", "ForeverHz.dll", "hz mod", hzv),
                       (cosm, "iiDk-the-actual/ForeverCosmetx", "ForeverCosmetx.dll", "cosmetx", cosmetxv),
                       (media, "iiDk-the-actual/GorillaMedia", "GorillaMedia.dll", "media", mediav),
                       (pokruk, "iiDk-the-actual/iiCamMod", "iiCamMod.dll", "iicam", pokrukv),
                       (toomuchinfo, "iiDk-the-actual/TooMuchInfo", "TooMuchInfo.dll", "too much info", toomuchinfov),
                       (walksim, "iiDk-the-actual/WalkSim", "WalkSim.dll", "walksim", walksimv),
                       (zlothy, "ZlothY29IQ/Zlothy-Nametag", "ZlothYNametag.dll", "zlothy", zlothyv),
                       (shirts, "developer9998/GorillaShirts", "GorillaShirts.dll", "shirts", shirtsv),
                       (volume, "ZlothY29IQ/GorillaVolumeControls", "GorillaVolumeControls.dll", "volumecontrols", volumev),
                       (infolog, "CheemsPookieAlt/Gorilla-Info-Logger", "Gorilla.Info.Logger.dll", "info logger", infologv),
                       (whodis, "ShibaGT/WhoDis", "WhoDis.dll", "whodis", whodisv),
                       (arss, "AutoReportSystem/ARS-Files", "ARS.dll", "ars", arsv),
                       (noleaves, "helenskeleton/No-Leaves", "NoLeaves.dll", "no leaves", noleavesv),
                       (casting, "hamburbur-org/Casting-Should-Be-Free", "CastingShouldBeFree.dll", "casting", castingv),
                       (ham, "hamburbur-org/hamburbur", "hamburbur.dll", "hamburvur", hamv),
                };
            }
        }

        public (CheckBox checkBox, string fileName, string statusText)[] DiscordMods
        {
            get
            {
                return new (CheckBox, string, string)[]
                {
                    (draw, "Cosmetic Buyer.dll", "cosmetic buyer"),
                };
            }
        }

        public async void UpdateVersions()
        {
            try
            {
                foreach (var (checkBox, repo, outputFile, statusText, versionlabel) in GithubMods)
                {
                    await GetVersionFromGithub(repo);
                    versionlabel.InvokeIfRequired(() => versionlabel.Text = githubVersion);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occurred while fetching mod versions.");
                foreach (var (checkBox, repo, outputFile, statusText, versionlabel) in GithubMods)
                {
                    if (versionlabel.Text == "version")
                        versionlabel.Text = "(n/a)";
                }
            }
        }

        private async void download_Click(object sender, EventArgs e)
        {
            var pluginsLoc = Directory.Exists(Path.Combine(_gtagLocation, "BepInEx"))
                ? Path.Combine(_gtagLocation, "BepInEx", "plugins")
                : _gtagLocation;

            try
            {
                if (bepinex.Checked)
                {
                    await InstallBepInEx(_gtagLocation, BaseUrl).ConfigureAwait(false);
                    status.InvokeIfRequired(() => status.Text = "bepinex");
                    pluginsLoc = Path.Combine(_gtagLocation, "BepInEx", "plugins");
                }

                if (ue.Checked)
                    await InstallUnityFix().ConfigureAwait(false);

                foreach (var (checkBox, repo, outputFile, statusText, versionlabel) in GithubMods)
                {
                    if (!checkBox.Checked) continue;

                    var outputFileFinal = outputFile;
                    if (createFolders)
                    {
                        var modFolder = Path.Combine(pluginsLoc, Path.GetFileNameWithoutExtension(outputFile));
                        Directory.CreateDirectory(modFolder);
                        outputFileFinal = Path.Combine(modFolder, outputFile);
                    }
                    status.InvokeIfRequired(() => status.Text = statusText);

                    var downloadUrl = await GetDownloadFromGithub(repo).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(downloadUrl))
                        throw new InvalidOperationException($"No download URL for {repo}");

                    await DownloadFileToPath(downloadUrl, Path.Combine(pluginsLoc, outputFileFinal)).ConfigureAwait(false);
                }

                foreach (var (checkBox, fileName, statusText) in DiscordMods)
                {
                    if (!checkBox.Checked) continue;

                    var outputFileFinal = fileName;
                    if (createFolders)
                    {
                        var modFolder = Path.Combine(pluginsLoc, Path.GetFileNameWithoutExtension(fileName));
                        Directory.CreateDirectory(modFolder);
                        outputFileFinal = Path.Combine(modFolder, fileName);
                    }

                    status.InvokeIfRequired(() => status.Text = statusText);
                    var remoteUrl = $"{BaseUrl}Banana/ModFiles/{Uri.EscapeUriString(fileName)}";
                    await DownloadFileToPath(remoteUrl, Path.Combine(pluginsLoc, outputFileFinal)).ConfigureAwait(false);
                }

                MessageBox.Show("Finished installing mods!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while installing mods. Please check your Gorilla Tag directory and try again.\n\n" + ex.Message);
            }
            finally
            {
                status.InvokeIfRequired(() => status.Text = "idle");
            }
        }

        private void disableenable_Click(object sender, EventArgs e)
        {
            try
            {
                var dllPath = Path.Combine(_gtagLocation, "winhttp.dll");
                var dPath = Path.Combine(_gtagLocation, "winhttp.d");

                if (File.Exists(dllPath))
                {
                    File.Move(dllPath, dPath, overwrite: true);
                }
                else if (File.Exists(dPath))
                {
                    File.Move(dPath, dllPath, overwrite: true);
                }

                UpdateSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling mod enable state: {ex.Message}");
            }
        }

        private void UpdateSettings()
        {
            try
            {
                var dllPath = Path.Combine(_gtagLocation, "winhttp.dll");
                var dPath = Path.Combine(_gtagLocation, "winhttp.d");

                if (!File.Exists(dPath) && !File.Exists(dllPath))
                {
                    disableenable.Visible = false;
                }
                else
                {
                    disableenable.Visible = true;
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

                var createFoldersFile = Path.Combine(_bananaDir, "create_folders.txt");
                if (File.Exists(createFoldersFile))
                    folders.Checked = bool.TryParse(File.ReadAllText(createFoldersFile), out var val) && val;
                else
                    folders.Checked = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating settings: {ex.Message}");
            }
        }

        private void discord_Click(object sender, EventArgs e)
        {
            try
            {
                var ps = new ProcessStartInfo("https://discord.gg/NtgqZkwuPy")
                {
                    UseShellExecute = true
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open link: {ex.Message}");
            }
        }

        private void changelocation_Click(object sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select your Gorilla Tag directory.",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var selectedPath = folderDialog.SelectedPath;
                    MessageBox.Show("Selected Folder: " + selectedPath);

                    label1.Text = selectedPath;
                    _gtagLocation = selectedPath;
                    _bananaDir = Path.Combine(_gtagLocation, "Gorilla Tag_Data", "Banana");
                    Directory.CreateDirectory(_bananaDir);
                    UpdateSettings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error changing Gorilla Tag location: {ex.Message}");
                }
            }
        }

        private bool createFolders;
        private void folders_CheckedChanged(object sender, EventArgs e)
        {
            createFolders = folders.Checked;
            try
            {
                Directory.CreateDirectory(_bananaDir);
                File.WriteAllText(Path.Combine(_bananaDir, "create_folders.txt"), folders.Checked.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to persist folder setting: {ex.Message}");
            }
        }

        private void infolog_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
    internal static class ControlExtensions
    {
        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (control.IsHandleCreated && control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }
    }
}

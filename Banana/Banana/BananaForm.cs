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
        string baseUrl = "https://raw.githubusercontent.com/ShibaGT/Banana/main/";

        static string gtaglocation = getgtpath();
        string bananaDir = Path.Combine(gtaglocation, "Gorilla Tag_Data", "Banana");
        string currentVersion = "1.0.0";
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

        private string GetReleaseVersion(string repo)
        {
            //iiDk-the-actual/iis.Stupid.Menu example dingus
            string url = $"https://api.github.com/repos/{repo}/releases/latest";

            string response = w.DownloadString(url);
            Newtonsoft.Json.Linq.JObject release = Newtonsoft.Json.Linq.JObject.Parse(response);

            string title = release["name"]?.ToString() ?? "(no title)";
            return title;
        }

        string githubDownload;

        private async Task GetDownloadFromGithub(string repo)
        {
            //iiDk-the-actual/iis.Stupid.Menu example dingus
            string url = $"https://api.github.com/repos/{repo}/releases/latest";
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CSharpApp");
            string response = await client.GetStringAsync(url);
            Newtonsoft.Json.Linq.JObject release = Newtonsoft.Json.Linq.JObject.Parse(response);
            string title = release["assets"][0]["browser_download_url"]?.ToString() ?? "(no download)";
            githubDownload = title;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string versionPath = Path.Combine(bananaDir, "banana_version.txt");
            File.WriteAllText(versionPath, currentVersion);

            label1.Text = gtaglocation;
            status.Text = "init path";

            disableenableupdate();
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

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

        //haste
        private void haste_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void bepinex_CheckedChanged(object sender, EventArgs e)
        {

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

        private async void download_Click(object sender, EventArgs e)
        {
            string pluginsloc = Path.Combine(gtaglocation.Replace(@"\\", @"\"), "BepInEx", "plugins\\");

            if (bepinex.Checked)
            {
                bepinexshit();
                status.Text = "bepinex";
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
                w.DownloadFile("https://cdn.discordapp.com/attachments/1211496069519646770/1388580674994438294/Haste.dll?ex=68618007&is=68602e87&hm=ca98e3eebfb4d9fbaf91e2af910b6b879ffbf3e24a7d9134988580d8eebdef6e&", pluginsloc + "Haste.dll");
                status.Text = "haste";
            }

            if (walksim.Checked)
            {
                w.DownloadFile("https://cdn.discordapp.com/attachments/1211496069519646770/1352716579787247676/WalkSimulator-NonUtilla.dll?ex=68618b7c&is=686039fc&hm=6ed57f75c4b030b11c9a3075757e496285d48eabdd7169f09cc64d415a2f2861&", pluginsloc + "WalkSimulator-NonUtilla.dll");
                status.Text = "walksim";
            }

            if (unknown.Checked)
            {
                w.DownloadFile("https://cdn.discordapp.com/attachments/1211496069519646770/1388028513851936768/UnkownsNameTagMod.dll?ex=6861780a&is=6860268a&hm=9a6908a9aea623f75535620c1d95f9179bcef6ba7a9f786a1330e86ccfc7e4bd&", pluginsloc + "Unkown'sNameTagMod.dll");
                status.Text = "unknowntags";
            }
            if (flick.Checked)
            {
                w.DownloadFile("https://cdn.discordapp.com/attachments/1211496069519646770/1386308690562383892/unknowns_DC_Flick_Mod.dll?ex=68612514&is=685fd394&hm=974b228642d645f6c0d1dd0c1bab3d99ad0f3a9fff4bbbfc09b8bac4740a1517&", pluginsloc + "unknown's DC Flick Mod.dll");
                status.Text = "flick";
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
    }
}

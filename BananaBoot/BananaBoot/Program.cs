using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

string baseUrl = "https://raw.githubusercontent.com/ShibaGT/Banana/main/";
string gtaglocation = GetGtPath();
string bananaDir = Path.Combine(gtaglocation, "Gorilla Tag_Data", "Banana");
string versionPath = Path.Combine(bananaDir, "banana_version.txt");

string GetGtPath()
{
    using var steamKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
    string? steam = steamKey?.GetValue("SteamPath")?.ToString()?.Replace("/", "\\");
    if (string.IsNullOrEmpty(steam)) return "Steam not found";

    string vdfPath = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
    if (!File.Exists(vdfPath)) return "Not installed";

    string vdf = File.ReadAllText(vdfPath);
    var libs = Regex.Matches(vdf, "\"path\"\\s*\"(.*?)\"");

    foreach (Match m in libs)
    {
        string libPath = m.Groups[1].Value.Replace("\\\\", "\\");
        string manifest = Path.Combine(libPath, "steamapps", "appmanifest_1533390.acf");
        if (File.Exists(manifest))
            return Path.Combine(libPath, "steamapps", "common", "Gorilla Tag");
    }

    string mainManifest = Path.Combine(steam, "steamapps", "appmanifest_1533390.acf");
    if (File.Exists(mainManifest))
        return Path.Combine(steam, "steamapps", "common", "Gorilla Tag");

    return "Not installed";
}

void DownloadMostRecent()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Downloading Banana...");

    string downloadUrl = $"{baseUrl}Banana/Banana.exe";
    string savePath = Path.Combine(bananaDir, "banana.exe");

    Console.WriteLine($"Downloading from: {downloadUrl}");
    Console.WriteLine($"To: {savePath}");

    Directory.CreateDirectory(bananaDir);

    using (var w = new WebClient())
    {
        w.DownloadFile(downloadUrl, savePath);
    }

    Console.WriteLine("Downloaded Banana!");
    RunBanana();
}

void RunBanana()
{
    Process.Start(new ProcessStartInfo
    {
        FileName = Path.Combine(gtaglocation, "Gorilla Tag_Data", "Banana", "banana.exe"),
        UseShellExecute = true
    });
    Environment.Exit(0);
}

using (var w = new WebClient())
{
    Console.WriteLine("Checking for updates...");
    if (gtaglocation == "Not installed")
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Gorilla Tag installation not found.");
        Console.ReadLine();
        return;
    }

    string current = w.DownloadString($"{baseUrl}version.txt").Trim();
    Console.WriteLine($"Current version : {current}");
    Console.WriteLine($"Gorilla Tag installed at : {gtaglocation}");
    Console.WriteLine("Finding installed version");
    Thread.Sleep(500);

    if (File.Exists(versionPath))
    {
        string installed = File.ReadAllText(versionPath).Trim();
        Console.WriteLine("Found installed version : " + installed);
        if (installed == current)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Banana is up to date!");
            RunBanana();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Banana is NOT up to date!");
            Thread.Sleep(1500);
            DownloadMostRecent();
        }
    }
    else
    {
        Console.WriteLine("Banana installed version not found..");
        Thread.Sleep(1000);
        DownloadMostRecent();
    }
}

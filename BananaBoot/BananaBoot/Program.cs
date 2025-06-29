using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

string baseUrl = "https://raw.githubusercontent.com/ShibaGT/Banana/main/";
string gtaglocation = getgtpath();
string bananaDir = Path.Combine(gtaglocation, "Gorilla Tag_Data", "Banana");
string versionPath = Path.Combine(bananaDir, "banana_version.txt");

string getgtpath() //YES this is chatgpt YES im lazy YES the rest is coded by me fuck OFF!
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

void DownloadMostRecent()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Downloading Banana...");
    var w = new WebClient();

    string downloadUrl = $"{baseUrl}Banana/Banana.exe";
    string savePath = Path.Combine(bananaDir, "banana.exe");

    Console.WriteLine($"Downloading from: {downloadUrl}");
    Console.WriteLine($"To: {savePath}");

    Directory.CreateDirectory(bananaDir);
    w.DownloadFile(downloadUrl, savePath);

    Console.WriteLine("Downloaded Banana!");
    RunBanana();
}

void RunBanana()
{
    Process.Start($"{gtaglocation}/Gorilla Tag_Data/Banana/banana.exe");
    Environment.Exit(0);
}

var w = new WebClient();
Console.WriteLine("Checking for updates...");
if (gtaglocation == "Not installed")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Gorilla Tag installation not found.?");
    Console.ReadLine();
    return;
}
string current = w.DownloadString($"{baseUrl}version.txt");
Console.WriteLine($"Current version : {current}");
Console.WriteLine($"Gorilla Tag installed at : {gtaglocation}");
Console.WriteLine($"Finding installed version");
Thread.Sleep(500);
if (File.Exists(versionPath))
{
    Console.WriteLine("Found installed version : " + File.ReadAllText(versionPath));
    string installed = File.ReadAllText(versionPath);
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web.Script.Serialization;
using Microsoft.Win32;

internal static class EasyCmd
{
    private const string CommandProcessorKey = @"Software\Microsoft\Command Processor";
    private const string MacroMarker = "easycmd.exe\" run ping $*";
    private const string BannerMarker = "easycmd.exe\" banner";
    private const string LegacyAcmdMacroMarker = "acmd.exe\" run ping $*";
    private const string LegacyAcmdBannerMarker = "acmd.exe\" banner";
    private const string LegacyMacroMarker = "doskey ping=\"";
    private const string ProjectUrl = "https://github.com/yydylab/easycmd";
    private const string LatestReleaseApi = "https://api.github.com/repos/yydylab/easycmd/releases/latest";
    private const string EasyCmdKey = @"Software\EasyCMD";
    private const string LanguageValue = "Language";
    private const string CompletionFileName = "easycmd_completion.lua";

    private static readonly IDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "p", "ping" },
            { "t", "tracert" },
            { "n", "nslookup" },
            { "a", "arp" },
            { "s", "ssh" },
            { "c", "curl" },
            { "f", "ftp" },
            { "m", "mstsc" },
            { "pa", "pathping" },
            { "te", "telnet" },
            { "i", "ipconfig" },
            { "g", "getmac" },
            { "ne", "netsh" },
            { "r", "route" },
            { "nb", "nbtstat" },
            { "cc", "curl-cip" },
            { "ci", "curl-ipinfo" },
            { "ia", "ipconfig-all" },
            { "if", "ipconfig-flushdns" },
            { "rp", "route-print" },
            { "rp4", "route-print-4" },
            { "rp6", "route-print-6" },
            { "tp", "tcping" }
        };

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "-v":
            case "--version":
            case "banner":
                PrintBanner();
                return 0;
            case "update":
                return Update();
            case "cn":
                return SetLanguage("cn");
            case "en":
                return SetLanguage("en");
            case "help":
            case "?":
                PrintHelp();
                return 0;
            case "install":
                return Install();
            case "uninstall":
                return Uninstall();
            case "run":
                return Run(args.Skip(1).ToArray());
            case "normalize":
                return Normalize(args.Skip(1).ToArray());
            default:
                PrintUsage();
                return 1;
        }
    }

    private static int Install()
    {
        string executable = Process.GetCurrentProcess().MainModule.FileName;
        string macro = BuildMacro(executable);

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(CommandProcessorKey))
        {
            string current = key.GetValue("AutoRun", string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;
            current = RemoveLegacyMacroGroup(current);
            current = RemoveMacroGroup(current, LegacyAcmdMacroMarker);
            current = RemoveMacroGroup(current, MacroMarker);
            string updated = string.IsNullOrWhiteSpace(current) ? macro : current + " & " + macro;
            key.SetValue("AutoRun", updated, RegistryValueKind.String);
        }

        InstallCompletionScript();
        Console.WriteLine("Installed or updated. Open a new CMD window to use EasyCMD shortcuts.");
        return 0;
    }

    private static int Update()
    {
        try
        {
            Console.WriteLine("Checking for updates...");
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            ReleaseInfo release = GetLatestRelease();
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (release.Version <= currentVersion)
            {
                Console.WriteLine("Installed v{0}; latest GitHub Release is v{1}. No update is required.",
                    currentVersion,
                    release.Version);
                return 0;
            }

            Console.WriteLine("Downloading EasyCMD v{0}...", release.Version);
            string downloadedFile = Path.Combine(Path.GetTempPath(), "easycmd-" + Guid.NewGuid().ToString("N") + ".exe");
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "easycmd-updater";
                client.DownloadFile(release.DownloadUrl, downloadedFile);
            }

            Version downloadedVersion = AssemblyName.GetAssemblyName(downloadedFile).Version;
            if (downloadedVersion != release.Version)
            {
                File.Delete(downloadedFile);
                throw new InvalidOperationException("The downloaded EasyCMD version does not match the GitHub Release.");
            }

            StartUpdater(downloadedFile, Process.GetCurrentProcess().MainModule.FileName);
            Console.WriteLine("Update scheduled. Approve the UAC prompt to complete the upgrade.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Update failed: {0}", error.Message);
            return 1;
        }
    }

    private static ReleaseInfo GetLatestRelease()
    {
        string json;
        using (var client = new WebClient())
        {
            client.Headers[HttpRequestHeader.UserAgent] = "easycmd-updater";
            json = client.DownloadString(LatestReleaseApi);
        }

        var serializer = new JavaScriptSerializer();
        var release = serializer.DeserializeObject(json) as Dictionary<string, object>;
        if (release == null || !release.ContainsKey("tag_name") || !release.ContainsKey("assets"))
            throw new InvalidOperationException("GitHub returned an invalid release response.");

        string downloadUrl = null;
        foreach (object item in (IEnumerable)release["assets"])
        {
            var asset = item as Dictionary<string, object>;
            if (asset != null
                && string.Equals(asset["name"] as string, "easycmd.exe", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = asset["browser_download_url"] as string;
                break;
            }
        }

        if (string.IsNullOrEmpty(downloadUrl))
            throw new InvalidOperationException("The latest GitHub Release does not include easycmd.exe.");

        return new ReleaseInfo
        {
            Version = ParseReleaseVersion(release["tag_name"] as string),
            DownloadUrl = downloadUrl
        };
    }

    private static Version ParseReleaseVersion(string tagName)
    {
        string[] parts = (tagName ?? string.Empty).Trim().TrimStart('v', 'V').Split('.');
        if (parts.Length < 1 || parts.Length > 4 || parts.Any(part => string.IsNullOrEmpty(part)))
            throw new InvalidOperationException("The latest GitHub Release tag is not a version number.");

        while (parts.Length < 4)
            parts = parts.Concat(new[] { "0" }).ToArray();

        Version version;
        if (!Version.TryParse(string.Join(".", parts), out version))
            throw new InvalidOperationException("The latest GitHub Release tag is not a version number.");
        return version;
    }

    private static void StartUpdater(string downloadedFile, string targetFile)
    {
        string script = Path.Combine(Path.GetTempPath(), "easycmd-update-" + Guid.NewGuid().ToString("N") + ".cmd");
        File.WriteAllLines(script, new[]
        {
            "@echo off",
            "ping 127.0.0.1 -n 3 > nul",
            "move /y " + QuoteForCmd(downloadedFile) + " " + QuoteForCmd(targetFile) + " > nul",
            QuoteForCmd(targetFile) + " install > nul",
            "start \"\" \"%ComSpec%\" /k",
            "del \"%~f0\""
        });

        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec"),
            Arguments = "/c " + QuoteForProcess(script),
            UseShellExecute = true,
            Verb = "runas"
        });
    }

    private static int Uninstall()
    {
        bool installed = false;
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(CommandProcessorKey))
        {
            string current = key.GetValue("AutoRun", string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;
            int markerIndex = current.IndexOf(MacroMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                Console.WriteLine("EasyCMD macros are not installed for the current user.");
            }
            else
            {
                string updated = RemoveMacroGroup(current, MacroMarker);

                if (string.IsNullOrEmpty(updated))
                    key.DeleteValue("AutoRun", false);
                else
                    key.SetValue("AutoRun", updated, RegistryValueKind.String);
                installed = true;
            }
        }

        RemoveCompletionScript();
        Console.WriteLine(installed
            ? "Uninstalled. New CMD windows will no longer load EasyCMD macros."
            : "Removed the EasyCMD Clink help script, if it was installed.");
        return 0;
    }

    private static int SetLanguage(string language)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(EasyCmdKey))
        {
            key.SetValue(LanguageValue, language, RegistryValueKind.String);
        }

        Console.WriteLine(language == "cn"
            ? "EasyCMD help language set to Chinese. Open a new CMD window to refresh completion descriptions."
            : "EasyCMD help language set to English. Open a new CMD window to refresh completion descriptions.");
        return 0;
    }

    private static void InstallCompletionScript()
    {
        string profile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "clink");
        if (!Directory.Exists(profile))
        {
            Console.WriteLine("Clink was not found. EasyCMD shortcuts are ready; install Clink to enable Tab and ? help.");
            return;
        }

        try
        {
            string destination = Path.Combine(profile, CompletionFileName);
            using (Stream source = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("EasyCMD.Completion.lua"))
            {
                if (source == null)
                    throw new InvalidOperationException("The embedded Clink completion script is missing.");

                using (FileStream target = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    source.CopyTo(target);
                }
            }
            Console.WriteLine("Installed Clink Tab and ? help: {0}", destination);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("EasyCMD shortcuts were installed, but Clink help could not be installed: {0}", error.Message);
        }
    }

    private static void RemoveCompletionScript()
    {
        try
        {
            string completion = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "clink",
                CompletionFileName);
            if (File.Exists(completion))
                File.Delete(completion);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Unable to remove EasyCMD Clink help: {0}", error.Message);
        }
    }

    private static string BuildMacro(string executable)
    {
        string quotedExecutable = QuoteForCmd(executable);
        return quotedExecutable + " banner & " + string.Join(" & ", Aliases.Select(alias =>
            "doskey " + alias.Key + "=" + quotedExecutable + " run " + alias.Value + " $*"));
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0 || !IsSupportedCommand(args[0]))
        {
            Console.Error.WriteLine("EasyCMD only runs supported Windows network commands.");
            return 1;
        }

        string command = args[0].ToLowerInvariant();
        string nativeCommand = GetNativeCommand(command);
        string[] normalized = TransformArguments(command, args.Skip(1).ToArray());
        string commandPath = FindCommandPath(nativeCommand);

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = commandPath,
                Arguments = string.Join(" ", normalized.Select(QuoteForProcess)),
                UseShellExecute = false
            };

            using (Process child = Process.Start(startInfo))
            {
                child.WaitForExit();
                return child.ExitCode;
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Unable to start {0}: {1}", command, error.Message);
            return 1;
        }
    }

    private static int Normalize(string[] args)
    {
        if (args.Length == 0 || !IsSupportedCommand(args[0]))
        {
            Console.Error.WriteLine("Usage: easycmd.exe normalize <command> [arguments]");
            return 1;
        }

        string command = args[0].ToLowerInvariant();
        Console.WriteLine(GetNativeCommand(command) + " " +
            string.Join(" ", TransformArguments(command, args.Skip(1).ToArray()).Select(QuoteForProcess)));
        return 0;
    }

    private static bool IsSupportedCommand(string command)
    {
        return Aliases.Values.Contains(command, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetNativeCommand(string command)
    {
        if (command.StartsWith("curl-", StringComparison.OrdinalIgnoreCase))
            return "curl";

        if (command.StartsWith("ipconfig-", StringComparison.OrdinalIgnoreCase))
            return "ipconfig";

        return command.StartsWith("route-print", StringComparison.OrdinalIgnoreCase) ? "route" : command;
    }

    private static string[] TransformArguments(string command, string[] arguments)
    {
        if (string.Equals(command, "curl-cip", StringComparison.OrdinalIgnoreCase))
            return BuildIpLookupArguments("cip.cc", arguments);

        if (string.Equals(command, "curl-ipinfo", StringComparison.OrdinalIgnoreCase))
            return BuildIpLookupArguments("ipinfo.io", arguments);

        if (string.Equals(command, "ipconfig-all", StringComparison.OrdinalIgnoreCase))
            return new[] { "/all" }.Concat(arguments).ToArray();

        if (string.Equals(command, "ipconfig-flushdns", StringComparison.OrdinalIgnoreCase))
            return new[] { "/flushdns" }.Concat(arguments).ToArray();

        if (string.Equals(command, "route-print", StringComparison.OrdinalIgnoreCase))
            return new[] { "print" }.Concat(arguments).ToArray();

        if (string.Equals(command, "route-print-4", StringComparison.OrdinalIgnoreCase))
            return new[] { "print", "-4" }.Concat(arguments).ToArray();

        if (string.Equals(command, "route-print-6", StringComparison.OrdinalIgnoreCase))
            return new[] { "print", "-6" }.Concat(arguments).ToArray();

        if (string.Equals(command, "tcping", StringComparison.OrdinalIgnoreCase) && arguments.Length == 1)
            return arguments.Concat(new[] { "22" }).ToArray();

        if (string.Equals(command, "ipconfig", StringComparison.OrdinalIgnoreCase) && arguments.Length > 0)
        {
            if (string.Equals(arguments[0], "a", StringComparison.OrdinalIgnoreCase))
                return new[] { "/all" }.Concat(arguments.Skip(1)).ToArray();

            if (string.Equals(arguments[0], "f", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "/flushdns" }.Concat(arguments.Skip(1)).ToArray();
            }
        }

        if (string.Equals(command, "ping", StringComparison.OrdinalIgnoreCase)
            && arguments.Length > 0
            && string.Equals(arguments[0], "t", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { "-t" }.Concat(arguments.Skip(1).Select(NormalizeArgument)).ToArray();
        }

        if (string.Equals(command, "tracert", StringComparison.OrdinalIgnoreCase)
            && arguments.Length > 0)
        {
            if (string.Equals(arguments[0], "dw", StringComparison.OrdinalIgnoreCase))
                return new[] { "-d", "-w", "1" }.Concat(arguments.Skip(1).Select(NormalizeArgument)).ToArray();

            if (string.Equals(arguments[0], "wd", StringComparison.OrdinalIgnoreCase))
                return new[] { "-w", "1", "-d" }.Concat(arguments.Skip(1).Select(NormalizeArgument)).ToArray();
        }

        if (string.Equals(command, "curl", StringComparison.OrdinalIgnoreCase) && arguments.Length > 0)
        {
            if (string.Equals(arguments[0], "c", StringComparison.OrdinalIgnoreCase))
                return new[] { "cip.cc" }.Concat(arguments.Skip(1)).ToArray();

            if (string.Equals(arguments[0], "i", StringComparison.OrdinalIgnoreCase))
                return new[] { "ipinfo.io" }.Concat(arguments.Skip(1)).ToArray();
        }

        if (string.Equals(command, "mstsc", StringComparison.OrdinalIgnoreCase)
            && arguments.Length > 0
            && IsIpv4Endpoint(arguments[0]))
        {
            string endpoint = arguments[0].IndexOf(':') < 0 ? arguments[0] + ":3389" : arguments[0];
            return new[] { "/v:" + endpoint }.Concat(arguments.Skip(1)).ToArray();
        }

        if (string.Equals(command, "route", StringComparison.OrdinalIgnoreCase))
        {
            if (arguments.Length >= 1 && string.Equals(arguments[0], "p", StringComparison.OrdinalIgnoreCase))
            {
                if (arguments.Length == 1)
                    return new[] { "print" };

                if (arguments[1] == "4" || arguments[1] == "6")
                    return new[] { "print", "-" + arguments[1] }.Concat(arguments.Skip(2)).ToArray();
            }

            if (arguments.Length >= 4
                && (string.Equals(arguments[0], "a", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(arguments[0], "d", StringComparison.OrdinalIgnoreCase)))
            {
                string mask;
                if (TryGetIpv4Mask(arguments[2], out mask))
                {
                    string action = string.Equals(arguments[0], "a", StringComparison.OrdinalIgnoreCase)
                        ? "add"
                        : "delete";
                    return new[] { action, arguments[1], "mask", mask, arguments[3] }
                        .Concat(arguments.Skip(4))
                        .ToArray();
                }
            }
        }

        if (ShouldNormalizeUrls(command))
            return arguments.Select(NormalizeArgument).ToArray();

        return arguments;
    }

    private static bool IsIpv4Endpoint(string value)
    {
        string host = value;
        int portIndex = value.IndexOf(':');
        if (portIndex >= 0)
        {
            host = value.Substring(0, portIndex);
            int port;
            if (!int.TryParse(value.Substring(portIndex + 1), out port) || port < 1 || port > 65535)
                return false;
        }

        IPAddress address;
        return IPAddress.TryParse(host, out address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }

    private static string[] BuildIpLookupArguments(string serviceHost, string[] arguments)
    {
        IPAddress address;
        if (arguments.Length > 0
            && IPAddress.TryParse(arguments[0], out address)
            && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return new[] { serviceHost + "/" + arguments[0] }.Concat(arguments.Skip(1)).ToArray();
        }

        return new[] { serviceHost }.Concat(arguments).ToArray();
    }

    private static bool TryGetIpv4Mask(string prefixText, out string mask)
    {
        mask = null;
        int prefix;
        if (!int.TryParse(prefixText, out prefix) || prefix < 0 || prefix > 32)
            return false;

        uint value = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
        mask = string.Join(".", new[]
        {
            (value >> 24) & 255,
            (value >> 16) & 255,
            (value >> 8) & 255,
            value & 255
        });
        return true;
    }

    private static bool ShouldNormalizeUrls(string command)
    {
        return string.Equals(command, "ping", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "tracert", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "nslookup", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "pathping", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindCommandPath(string command)
    {
        string filename = command + ".exe";
        string systemPath = Path.Combine(Environment.SystemDirectory, filename);
        if (File.Exists(systemPath))
            return systemPath;

        if (string.Equals(command, "ssh", StringComparison.OrdinalIgnoreCase))
        {
            string openSshPath = Path.Combine(Environment.SystemDirectory, "OpenSSH", filename);
            if (File.Exists(openSshPath))
                return openSshPath;
        }

        return filename;
    }

    private static string RemoveMacroGroup(string current, string marker)
    {
        int markerIndex = current.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return current;

        int segmentStart = current.LastIndexOf(" & ", markerIndex, StringComparison.Ordinal);
        segmentStart = segmentStart < 0 ? 0 : segmentStart + 3;

        if (string.Equals(marker, MacroMarker, StringComparison.OrdinalIgnoreCase)
            || string.Equals(marker, LegacyAcmdMacroMarker, StringComparison.OrdinalIgnoreCase))
        {
            string bannerMarker = string.Equals(marker, MacroMarker, StringComparison.OrdinalIgnoreCase)
                ? BannerMarker
                : LegacyAcmdBannerMarker;
            int bannerIndex = current.LastIndexOf(bannerMarker, markerIndex, StringComparison.OrdinalIgnoreCase);
            if (bannerIndex >= 0)
            {
                segmentStart = current.LastIndexOf(" & ", bannerIndex, StringComparison.Ordinal);
                segmentStart = segmentStart < 0 ? 0 : segmentStart + 3;
            }

            int lastMacro = -1;
            foreach (string alias in Aliases.Keys)
            {
                int macro = current.IndexOf("doskey " + alias + "=", markerIndex, StringComparison.OrdinalIgnoreCase);
                if (macro > lastMacro)
                    lastMacro = macro;
            }
            int segmentEnd = lastMacro < 0 ? -1 : current.IndexOf(" & ", lastMacro);
            if (segmentEnd < 0)
                segmentEnd = current.Length;

            return RemoveMacroSegment(current, segmentStart, segmentEnd);
        }

        int nextSegment = current.IndexOf(" & ", markerIndex);
        int end = nextSegment < 0 ? current.Length : nextSegment;
        return RemoveMacroSegment(current, segmentStart, end);
    }

    private static string RemoveMacroSegment(string current, int start, int end)
    {
        string updated = current.Remove(start, end - start).Trim();
        return updated.EndsWith("&", StringComparison.Ordinal)
            ? updated.Substring(0, updated.Length - 1).TrimEnd()
            : updated;
    }

    private static string RemoveLegacyMacroGroup(string current)
    {
        int start = current.IndexOf(LegacyMacroMarker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return current;

        int lastMacro = current.IndexOf("doskey nslookup=", start, StringComparison.OrdinalIgnoreCase);
        int end = lastMacro < 0 ? -1 : current.IndexOf(" & ", lastMacro);
        if (end < 0)
            end = current.Length;

        int segmentStart = current.LastIndexOf(" & ", start, StringComparison.Ordinal);
        segmentStart = segmentStart < 0 ? 0 : segmentStart + 3;
        string updated = current.Remove(segmentStart, end - segmentStart).Trim();
        return updated.EndsWith("&", StringComparison.Ordinal)
            ? updated.Substring(0, updated.Length - 1).TrimEnd()
            : updated;
    }

    private static string NormalizeArgument(string argument)
    {
        string candidate = argument.Trim().Trim('"');
        Uri uri;
        if (Uri.TryCreate(candidate, UriKind.Absolute, out uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host))
        {
            string host = uri.Host.Trim('[', ']');
            IPAddress address;
            if (uri.HostNameType == UriHostNameType.IPv6 && IPAddress.TryParse(host, out address))
                return "[" + address + "]";
            return host;
        }

        return argument;
    }

    private static string QuoteForCmd(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    // Implements the Windows argv quoting rules used by CreateProcessW.
    private static string QuoteForProcess(string value)
    {
        if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
            return value;

        var result = new System.Text.StringBuilder("\"");
        int slashes = 0;
        foreach (char character in value)
        {
            if (character == '\\')
            {
                slashes++;
            }
            else if (character == '"')
            {
                result.Append('\\', slashes * 2 + 1);
                result.Append('"');
                slashes = 0;
            }
            else
            {
                result.Append('\\', slashes);
                result.Append(character);
                slashes = 0;
            }
        }
        result.Append('\\', slashes * 2);
        result.Append('"');
        return result.ToString();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("EasyCMD - easy CMD network command shortcuts.");
        Console.WriteLine("  easycmd.exe -v");
        Console.WriteLine("  easycmd.exe update");
        Console.WriteLine("  easycmd.exe cn | en");
        Console.WriteLine("  easycmd.exe help");
        Console.WriteLine("  easycmd.exe install");
        Console.WriteLine("  easycmd.exe uninstall");
        Console.WriteLine("  easycmd.exe normalize ping https://example.com/path");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("EasyCMD interactive help");
        Console.WriteLine("  Tab: complete an EasyCMD command or list matching commands.");
        Console.WriteLine("  ?: list all commands, commands matching a prefix, or command parameters.");
        Console.WriteLine("  easycmd cn: use Chinese completion descriptions.");
        Console.WriteLine("  easycmd en: use English completion descriptions.");
        Console.WriteLine("  Requires Clink. Run easycmd install after installing Clink.");
    }

    private static void PrintBanner()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        Console.WriteLine("easycmd v{0}", version);
        Console.WriteLine("Copyright (c) 2026 yydylab");
        Console.WriteLine(ProjectUrl);
    }

    private sealed class ReleaseInfo
    {
        public Version Version { get; set; }
        public string DownloadUrl { get; set; }
    }
}

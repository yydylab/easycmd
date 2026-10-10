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
    private const string ShellMarker = "easycmd.exe\" shell";
    private const string LegacyAcmdMacroMarker = "acmd.exe\" run ping $*";
    private const string LegacyAcmdBannerMarker = "acmd.exe\" banner";
    private const string LegacyMacroMarker = "doskey ping=\"";
    private const string ProjectUrl = "https://github.com/yydylab/easycmd";
    private const string LatestReleaseApi = "https://api.github.com/repos/yydylab/easycmd/releases/latest";
    private const string EasyCmdKey = @"Software\EasyCMD";
    private const string LanguageValue = "Language";
    private const int MaximumHistoryEntries = 500;
    private static bool updateScheduled;

    private static readonly IDictionary<string, CommandHelp> InteractiveCommands =
        new Dictionary<string, CommandHelp>(StringComparer.OrdinalIgnoreCase)
        {
            { "ping", new CommandHelp("Ping 连通性测试", "Ping connectivity", "-t 持续 Ping|-n COUNT 指定请求次数|-w TIMEOUT 超时毫秒数") },
            { "tracert", new CommandHelp("路由跟踪", "Trace network route", "-d 不解析主机名|-h MAX_HOPS 最大跃点数|-w TIMEOUT 超时毫秒数") },
            { "nslookup", new CommandHelp("DNS 查询", "Query DNS records", "HOST 域名、IP 或 URL|SERVER 指定 DNS 服务器") },
            { "pathping", new CommandHelp("路径与丢包测试", "Trace route and packet loss", "-n 不解析主机名|-h MAX_HOPS 最大跃点数|-w TIMEOUT 超时毫秒数") },
            { "route", new CommandHelp("查看或管理路由", "View or manage IP routes", "print 显示路由表|add 添加路由|delete 删除路由|change 修改路由") },
            { "ipconfig", new CommandHelp("查看 IP 配置", "Show IP configuration", "/all 完整 TCP/IP 配置|/flushdns 清理 DNS 缓存|/release 释放 IPv4 地址|/renew 更新 IPv4 地址") },
            { "getmac", new CommandHelp("查看 MAC 地址", "Show MAC addresses", "/v 显示详细信息") },
            { "netsh", new CommandHelp("网络配置控制台", "Network configuration console", "interface 网络接口|advfirewall 高级防火墙|wlan 无线网络|winhttp WinHTTP 代理") },
            { "nbtstat", new CommandHelp("NetBIOS 诊断", "NetBIOS diagnostics", "-n 本地 NetBIOS 名称|-a NAME 远程名称表") },
            { "telnet", new CommandHelp("Telnet 客户端", "Telnet client", "HOST 目标主机|PORT 目标端口") },
            { "arp", new CommandHelp("ARP 缓存管理", "Manage ARP cache", "-a 显示 ARP 缓存|-d 删除条目|-s 添加静态条目") },
            { "tcping", new CommandHelp("TCP 端口连通性测试", "Test TCP port connectivity", "HOST 目标主机|PORT TCP 端口；tp 默认 22|-t 持续探测") },
            { "mstsc", new CommandHelp("远程桌面连接", "Remote Desktop Connection", "HOST[:PORT] 目标 RDP 主机|/admin 管理会话|/f 全屏启动") },
            { "ncpa.cpl", new CommandHelp("打开网络适配器", "Open Network Connections", string.Empty) },
            { "ftp", new CommandHelp("FTP 客户端", "FTP client", "HOST FTP 服务器") },
            { "ssh", new CommandHelp("安全 Shell 客户端", "Secure Shell client", "USER@HOST 用户与主机|-p PORT SSH 端口") },
            { "curl", new CommandHelp("HTTP 传输工具", "HTTP transfer tool", "URL 完整 URL|-I 仅获取响应头|-L 跟随重定向|-o FILE 输出文件") },
            { "wget", new CommandHelp("下载工具", "Download tool", "URL 下载地址") },
            { "service.msc", new CommandHelp("打开服务管理", "Open Services console", string.Empty) },
            { "tasklist", new CommandHelp("查看运行进程", "List running processes", "/v 显示详细信息") },
            { "tar", new CommandHelp("归档工具", "Archive utility", "-x 解压归档|-c 创建归档|-f FILE 归档文件") },
            { "optionalfeatures", new CommandHelp("Windows 可选功能", "Open Windows Features", string.Empty) },
            { "firewall.cpl", new CommandHelp("Windows Defender 防火墙", "Open Windows Defender Firewall", string.Empty) },
            { "sysdm.cpl", new CommandHelp("系统属性", "Open System Properties", string.Empty) },
            { "powercfg.cpl", new CommandHelp("电源选项", "Open Power Options", string.Empty) },
            { "msinfo32", new CommandHelp("系统信息", "Open System Information", string.Empty) },
            { "inetcpl.cpl", new CommandHelp("Internet 选项", "Open Internet Options", string.Empty) },
            { "appwiz.cpl", new CommandHelp("程序和功能", "Open Programs and Features", string.Empty) },
            { "msconfig", new CommandHelp("系统配置", "Open System Configuration", string.Empty) },
            { "notepad", new CommandHelp("记事本", "Open Notepad", string.Empty) },
            { "calc", new CommandHelp("计算器", "Open Calculator", string.Empty) },
            { "drivers", new CommandHelp("驱动程序管理", "Driver management", string.Empty) },
            { "control", new CommandHelp("控制面板", "Open Control Panel", string.Empty) },
            { "desk.cpl", new CommandHelp("显示设置", "Open Display Settings", string.Empty) },
            { "winver", new CommandHelp("Windows 版本", "Show Windows version", string.Empty) },
            { "winword", new CommandHelp("Microsoft Word", "Open Microsoft Word", string.Empty) },
            { "excel", new CommandHelp("Microsoft Excel", "Open Microsoft Excel", string.Empty) },
            { "timedate.cpl", new CommandHelp("日期和时间设置", "Open Date and Time", string.Empty) },
            { "intl.cpl", new CommandHelp("区域设置", "Open Region settings", string.Empty) },
            { "regedit", new CommandHelp("注册表编辑器", "Open Registry Editor", string.Empty) },
            { "taskmgr", new CommandHelp("任务管理器", "Open Task Manager", string.Empty) },
            { "hdwwiz", new CommandHelp("添加硬件向导", "Open Add Hardware Wizard", string.Empty) }
        };

    private static readonly IDictionary<string, CommandHelp> EasyCmdCommands =
        new Dictionary<string, CommandHelp>(StringComparer.OrdinalIgnoreCase)
        {
            { "-v", new CommandHelp("显示版本和项目信息", "Show version and project information", string.Empty) },
            { "-h", new CommandHelp("显示帮助", "Show help", string.Empty) },
            { "update", new CommandHelp("检查并升级到最新 Release", "Check for and install the latest Release", string.Empty) },
            { "history", new CommandHelp("查看历史命令", "Show command history", "clear 清空历史命令") },
            { "cn", new CommandHelp("切换中文备注", "Switch descriptions to Chinese", string.Empty) },
            { "en", new CommandHelp("切换英文备注", "Switch descriptions to English", string.Empty) },
            { "help", new CommandHelp("显示帮助", "Show help", string.Empty) },
            { "install", new CommandHelp("安装或刷新 EasyCMD", "Install or refresh EasyCMD", string.Empty) },
            { "uninstall", new CommandHelp("卸载 EasyCMD", "Uninstall EasyCMD", string.Empty) },
            { "shell", new CommandHelp("启动交互模式", "Start interactive mode", string.Empty) }
        };

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
            case "-h":
            case "--help":
                PrintUsage();
                return 0;
            case "update":
                return Update();
            case "shell":
                return Shell();
            case "history":
                return History(args.Skip(1).ToArray());
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

        Console.WriteLine("Installed or updated. Open a new CMD window to use EasyCMD shortcuts, Tab completion, and ? help.");
        return 0;
    }

    private static int Update()
    {
        updateScheduled = false;
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
            updateScheduled = true;
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
            "set attempts=0",
            ":replace",
            "move /y " + QuoteForCmd(downloadedFile) + " " + QuoteForCmd(targetFile) + " > nul",
            "if not errorlevel 1 goto updated",
            "set /a attempts+=1",
            "if %attempts% GEQ 15 goto failed",
            "timeout /t 1 /nobreak > nul",
            "goto replace",
            ":updated",
            QuoteForCmd(targetFile) + " install > nul",
            "start \"\" \"%ComSpec%\" /k",
            "goto cleanup",
            ":failed",
            "echo EasyCMD update failed because the current executable is still in use.",
            ":cleanup",
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

        Console.WriteLine(installed
            ? "Uninstalled. New CMD windows will no longer load EasyCMD macros."
            : "EasyCMD is not installed for the current user.");
        return 0;
    }

    private static int SetLanguage(string language)
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(EasyCmdKey))
        {
            key.SetValue(LanguageValue, language, RegistryValueKind.String);
        }

        Console.WriteLine(language == "cn"
            ? "EasyCMD help language set to Chinese."
            : "EasyCMD help language set to English.");
        return 0;
    }

    private static string BuildMacro(string executable)
    {
        string quotedExecutable = QuoteForCmd(executable);
        return quotedExecutable + " banner & " + string.Join(" & ", Aliases.Select(alias =>
            "doskey " + alias.Key + "=" + quotedExecutable + " run " + alias.Value + " $*"))
            + " & " + quotedExecutable + " shell";
    }

    private static int Shell()
    {
        List<string> history = LoadHistory();
        Console.WriteLine("EasyCMD interactive mode. Press Tab or ? for help; use Up/Down for history; type exit to return to CMD.");
        while (true)
        {
            string line = ReadInteractiveLine(history);
            if (line == null)
                return 0;

            string trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;
            if (string.Equals(trimmed, "exit", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (IsInteractiveUpdateCommand(trimmed))
            {
                AddHistory(history, line);
                int updateExitCode = Update();
                // The updater replaces this executable, so release its file handle immediately.
                if (updateScheduled)
                    return updateExitCode;
                continue;
            }
            if (string.Equals(trimmed, "history", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("history ", StringComparison.OrdinalIgnoreCase))
            {
                History(SplitArguments(trimmed).Skip(1).ToArray());
                continue;
            }
            AddHistory(history, line);
            if (string.Equals(trimmed, "cls", StringComparison.OrdinalIgnoreCase))
            {
                Console.Clear();
                continue;
            }
            if (string.Equals(trimmed, "help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                continue;
            }

            ExecuteInteractiveLine(line);
        }
    }

    private static string ReadInteractiveLine(IList<string> history)
    {
        var buffer = new System.Text.StringBuilder();
        int cursor = 0;
        int historyIndex = history.Count;
        string draft = string.Empty;
        while (true)
        {
            string prompt = Environment.CurrentDirectory + ">";
            RedrawInteractiveLine(prompt, buffer.ToString(), cursor);
            ConsoleKeyInfo key;
            try
            {
                key = Console.ReadKey(true);
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buffer.ToString();
            }
            if (key.Key == ConsoleKey.Backspace && cursor > 0)
            {
                buffer.Remove(--cursor, 1);
                continue;
            }
            if (key.Key == ConsoleKey.Delete && cursor < buffer.Length)
            {
                buffer.Remove(cursor, 1);
                continue;
            }
            if (key.Key == ConsoleKey.LeftArrow && cursor > 0)
            {
                cursor--;
                continue;
            }
            if (key.Key == ConsoleKey.RightArrow && cursor < buffer.Length)
            {
                cursor++;
                continue;
            }
            if (key.Key == ConsoleKey.Home)
            {
                cursor = 0;
                continue;
            }
            if (key.Key == ConsoleKey.End)
            {
                cursor = buffer.Length;
                continue;
            }
            if (key.Key == ConsoleKey.UpArrow)
            {
                if (historyIndex == history.Count)
                    draft = buffer.ToString();
                if (historyIndex > 0)
                {
                    historyIndex--;
                    ReplaceInteractiveBuffer(buffer, history[historyIndex], ref cursor);
                }
                continue;
            }
            if (key.Key == ConsoleKey.DownArrow)
            {
                if (historyIndex < history.Count - 1)
                {
                    historyIndex++;
                    ReplaceInteractiveBuffer(buffer, history[historyIndex], ref cursor);
                }
                else if (historyIndex < history.Count)
                {
                    historyIndex = history.Count;
                    ReplaceInteractiveBuffer(buffer, draft, ref cursor);
                }
                continue;
            }
            if (key.Key == ConsoleKey.Tab)
            {
                CompleteInteractiveLine(buffer, ref cursor);
                continue;
            }
            if (key.KeyChar == '?')
            {
                ShowInteractiveHelp(buffer.ToString(), cursor);
                continue;
            }
            if (!char.IsControl(key.KeyChar))
            {
                buffer.Insert(cursor++, key.KeyChar);
            }
        }
    }

    private static void ReplaceInteractiveBuffer(System.Text.StringBuilder buffer, string value, ref int cursor)
    {
        buffer.Clear();
        buffer.Append(value);
        cursor = buffer.Length;
    }

    private static bool IsInteractiveUpdateCommand(string line)
    {
        string[] parts = SplitArguments(line);
        if (parts.Length != 2 || !string.Equals(parts[1], "update", StringComparison.OrdinalIgnoreCase))
            return false;

        string executable = Path.GetFileName(parts[0]);
        return string.Equals(executable, "easycmd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(executable, "easycmd.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static void RedrawInteractiveLine(string prompt, string line, int cursor)
    {
        Console.Write("\r" + prompt + line + " ");
        Console.Write("\r" + prompt + line.Substring(0, cursor));
    }

    private static void CompleteInteractiveLine(System.Text.StringBuilder buffer, ref int cursor)
    {
        string line = buffer.ToString();
        string firstWord = GetFirstWord(line);
        if (string.Equals(firstWord, "easycmd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(firstWord, "easycmd.exe", StringComparison.OrdinalIgnoreCase))
        {
            CompleteEasyCmdLine(buffer, ref cursor);
            return;
        }
        if (line.IndexOf(' ') >= 0 && InteractiveCommands.ContainsKey(firstWord))
        {
            ShowInteractiveHelp(line, cursor);
            return;
        }

        string prefix = line.Substring(0, cursor).Trim();
        if (prefix.IndexOf(' ') >= 0)
            return;

        string[] matches = InteractiveCommands.Keys
            .Concat(new[] { "easycmd" })
            .Where(command => command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(command => command)
            .ToArray();
        if (matches.Length == 1)
        {
            buffer.Clear();
            buffer.Append(matches[0]).Append(' ');
            cursor = buffer.Length;
            return;
        }
        ShowCommands(matches);
    }

    private static void CompleteEasyCmdLine(System.Text.StringBuilder buffer, ref int cursor)
    {
        string[] parts = SplitArguments(buffer.ToString().Substring(0, cursor));
        if (parts.Length <= 1)
        {
            ShowEasyCmdCommands(EasyCmdCommands.Keys);
            return;
        }
        if (parts.Length == 2)
        {
            string[] matches = EasyCmdCommands.Keys
                .Where(command => command.StartsWith(parts[1], StringComparison.OrdinalIgnoreCase))
                .OrderBy(command => command)
                .ToArray();
            if (matches.Length == 1)
            {
                ReplaceInteractiveBuffer(buffer, "easycmd " + matches[0] + " ", ref cursor);
                return;
            }
            ShowEasyCmdCommands(matches);
            return;
        }
        if (string.Equals(parts[1], "history", StringComparison.OrdinalIgnoreCase))
        {
            string[] matches = new[] { "clear" }
                .Where(command => command.StartsWith(parts[2], StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 1)
                ReplaceInteractiveBuffer(buffer, "easycmd history clear ", ref cursor);
            else
                ShowEasyCmdCommandHelp("history");
        }
    }

    private static void ShowInteractiveHelp(string line, int cursor)
    {
        string command = GetFirstWord(line);
        if (string.Equals(command, "easycmd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "easycmd.exe", StringComparison.OrdinalIgnoreCase))
        {
            ShowEasyCmdHelp(line.Substring(0, Math.Min(cursor, line.Length)));
            return;
        }
        if (line.IndexOf(' ') >= 0 && InteractiveCommands.ContainsKey(command))
        {
            CommandHelp help = InteractiveCommands[command];
            string description = GetLanguage() == "cn" ? help.Chinese : help.English;
            Console.WriteLine();
            Console.WriteLine("  {0}  {1}", command, description);
            foreach (string item in help.Parameters.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                Console.WriteLine("  {0}", item);
            return;
        }

        string prefix = line.Substring(0, Math.Min(cursor, line.Length)).Trim();
        ShowCommands(InteractiveCommands.Keys
            .Concat(new[] { "easycmd" })
            .Where(commandName => commandName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(commandName => commandName)
            .ToArray());
    }

    private static void ShowCommands(IEnumerable<string> names)
    {
        string[] commands = names.ToArray();
        Console.WriteLine();
        if (commands.Length == 0)
        {
            Console.WriteLine("  No matching EasyCMD commands.");
            return;
        }

        bool chinese = GetLanguage() == "cn";
        Console.WriteLine(chinese ? "  EasyCMD 可用命令：" : "  EasyCMD available commands:");
        foreach (string command in commands)
        {
            if (string.Equals(command, "easycmd", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("  {0,-18} {1}", command, chinese ? "EasyCMD 管理命令" : "EasyCMD management commands");
                continue;
            }
            CommandHelp help = InteractiveCommands[command];
            Console.WriteLine("  {0,-18} {1}", command, chinese ? help.Chinese : help.English);
        }
    }

    private static void ShowEasyCmdHelp(string line)
    {
        string[] parts = SplitArguments(line);
        if (parts.Length <= 1)
        {
            ShowEasyCmdCommands(EasyCmdCommands.Keys);
            return;
        }
        ShowEasyCmdCommandHelp(parts[1]);
    }

    private static void ShowEasyCmdCommands(IEnumerable<string> names)
    {
        string[] commands = names.OrderBy(name => name).ToArray();
        bool chinese = GetLanguage() == "cn";
        Console.WriteLine();
        Console.WriteLine(chinese ? "  EasyCMD 管理命令：" : "  EasyCMD management commands:");
        foreach (string command in commands)
        {
            CommandHelp help = EasyCmdCommands[command];
            Console.WriteLine("  {0,-18} {1}", command, chinese ? help.Chinese : help.English);
        }
    }

    private static void ShowEasyCmdCommandHelp(string command)
    {
        CommandHelp help;
        if (!EasyCmdCommands.TryGetValue(command, out help))
        {
            ShowEasyCmdCommands(EasyCmdCommands.Keys
                .Where(name => name.StartsWith(command, StringComparison.OrdinalIgnoreCase)));
            return;
        }
        bool chinese = GetLanguage() == "cn";
        Console.WriteLine();
        Console.WriteLine("  easycmd {0}  {1}", command, chinese ? help.Chinese : help.English);
        foreach (string item in help.Parameters.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            Console.WriteLine("  {0}", item);
    }

    private static string GetFirstWord(string line)
    {
        string trimmed = (line ?? string.Empty).TrimStart();
        int end = trimmed.IndexOfAny(new[] { ' ', '\t' });
        return (end < 0 ? trimmed : trimmed.Substring(0, end)).ToLowerInvariant();
    }

    private static string GetLanguage()
    {
        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(EasyCmdKey))
        {
            return (key.GetValue(LanguageValue, "en") as string ?? "en").ToLowerInvariant();
        }
    }

    private static int History(string[] arguments)
    {
        if (arguments.Length == 1 && string.Equals(arguments[0], "clear", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(GetHistoryPath()))
                    File.Delete(GetHistoryPath());
                Console.WriteLine("EasyCMD command history cleared.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("Unable to clear command history: {0}", error.Message);
                return 1;
            }
        }

        List<string> history = LoadHistory();
        if (history.Count == 0)
        {
            Console.WriteLine("EasyCMD command history is empty.");
            return 0;
        }

        for (int index = 0; index < history.Count; index++)
            Console.WriteLine("{0,4}  {1}", index + 1, history[index]);
        return 0;
    }

    private static List<string> LoadHistory()
    {
        try
        {
            string path = GetHistoryPath();
            if (!File.Exists(path))
                return new List<string>();

            string[] entries = File.ReadAllLines(path)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
            return entries.Skip(Math.Max(0, entries.Length - MaximumHistoryEntries)).ToList();
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Unable to load command history: {0}", error.Message);
            return new List<string>();
        }
    }

    private static void AddHistory(IList<string> history, string line)
    {
        string entry = line.Trim();
        if (entry.Length == 0)
            return;

        try
        {
            string path = GetHistoryPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, entry + Environment.NewLine);
            history.Add(entry);

            if (history.Count > MaximumHistoryEntries)
            {
                while (history.Count > MaximumHistoryEntries)
                    history.RemoveAt(0);
                File.WriteAllLines(path, history);
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Unable to save command history: {0}", error.Message);
        }
    }

    private static string GetHistoryPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EasyCMD",
            "history.txt");
    }

    private static void ExecuteInteractiveLine(string line)
    {
        string[] parts = SplitArguments(line);
        if (parts.Length == 0)
            return;

        string command = parts[0];
        if (string.Equals(command, "cd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(command, "chdir", StringComparison.OrdinalIgnoreCase))
        {
            string path = parts.Length > 1 ? parts[1] : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            try { Environment.CurrentDirectory = Path.GetFullPath(path); }
            catch (Exception error) { Console.Error.WriteLine("The system cannot find the path specified: {0}", error.Message); }
            return;
        }

        string normalizedCommand;
        if (Aliases.TryGetValue(command, out normalizedCommand))
            command = normalizedCommand;

        if (IsSupportedCommand(command))
        {
            Run(new[] { command }.Concat(parts.Skip(1)).ToArray());
            return;
        }

        try
        {
            using (Process child = Process.Start(new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("ComSpec"),
                Arguments = "/c " + line,
                UseShellExecute = false
            }))
            {
                child.WaitForExit();
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Unable to start {0}: {1}", parts[0], error.Message);
        }
    }

    private static string[] SplitArguments(string line)
    {
        var arguments = new List<string>();
        var current = new System.Text.StringBuilder();
        bool quoted = false;
        foreach (char character in line)
        {
            if (character == '"')
            {
                quoted = !quoted;
                continue;
            }
            if (char.IsWhiteSpace(character) && !quoted)
            {
                if (current.Length > 0)
                {
                    arguments.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }
            current.Append(character);
        }
        if (current.Length > 0)
            arguments.Add(current.ToString());
        return arguments.ToArray();
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
            int shellIndex = current.IndexOf(ShellMarker, lastMacro < 0 ? markerIndex : lastMacro,
                StringComparison.OrdinalIgnoreCase);
            if (shellIndex >= 0)
            {
                segmentEnd = current.IndexOf(" & ", shellIndex);
            }
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
        if (GetLanguage() == "cn")
        {
            Console.WriteLine("EasyCMD - 简单的 CMD 网络命令快捷工具。");
            Console.WriteLine("  easycmd.exe -v                 显示版本信息");
            Console.WriteLine("  easycmd.exe -h | help          显示帮助");
            Console.WriteLine("  easycmd.exe update             检查并升级到最新版本");
            Console.WriteLine("  easycmd.exe shell              启动交互模式");
            Console.WriteLine("  easycmd.exe history [clear]    查看或清空历史命令");
            Console.WriteLine("  easycmd.exe cn | en            切换中文或英文备注");
            Console.WriteLine("  easycmd.exe install            安装或刷新 EasyCMD");
            Console.WriteLine("  easycmd.exe uninstall          卸载 EasyCMD");
            Console.WriteLine("  easycmd.exe normalize ping https://example.com/path");
            return;
        }

        Console.WriteLine("EasyCMD - easy CMD network command shortcuts.");
        Console.WriteLine("  easycmd.exe -v");
        Console.WriteLine("  easycmd.exe -h | help");
        Console.WriteLine("  easycmd.exe update");
        Console.WriteLine("  easycmd.exe shell");
        Console.WriteLine("  easycmd.exe history [clear]");
        Console.WriteLine("  easycmd.exe cn | en");
        Console.WriteLine("  easycmd.exe help");
        Console.WriteLine("  easycmd.exe install");
        Console.WriteLine("  easycmd.exe uninstall");
        Console.WriteLine("  easycmd.exe normalize ping https://example.com/path");
    }

    private static void PrintHelp()
    {
        if (GetLanguage() == "cn")
        {
            Console.WriteLine("EasyCMD 交互帮助");
            Console.WriteLine("  Tab：补全 EasyCMD 命令或列出匹配命令。");
            Console.WriteLine("  ?：列出全部命令、前缀匹配命令或当前命令参数。");
            Console.WriteLine("  上/下方向键：浏览历史命令。");
            Console.WriteLine("  交互模式中执行 easycmd update 会退出 Shell 以便替换程序。");
            Console.WriteLine("  easycmd cn：切换为中文备注。");
            Console.WriteLine("  easycmd en：切换为英文备注。");
            Console.WriteLine("  不依赖任何第三方命令行扩展程序。");
            return;
        }

        Console.WriteLine("EasyCMD interactive help");
        Console.WriteLine("  Tab: complete an EasyCMD command or list matching commands.");
        Console.WriteLine("  ?: list all commands, commands matching a prefix, or command parameters.");
        Console.WriteLine("  Up/Down: browse previously entered commands.");
        Console.WriteLine("  In interactive mode, easycmd update exits the shell so the executable can be replaced.");
        Console.WriteLine("  easycmd cn: use Chinese completion descriptions.");
        Console.WriteLine("  easycmd en: use English completion descriptions.");
        Console.WriteLine("  No third-party command-line extension is required.");
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

    private sealed class CommandHelp
    {
        public CommandHelp(string chinese, string english, string parameters)
        {
            Chinese = chinese;
            English = english;
            Parameters = parameters;
        }

        public string Chinese { get; private set; }
        public string English { get; private set; }
        public string Parameters { get; private set; }
    }
}

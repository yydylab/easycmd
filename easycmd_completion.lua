-- EasyCMD completion and contextual help for Clink.
local language = "en"
local query = io.popen('reg query "HKCU\\Software\\EasyCMD" /v Language 2>nul')
if query then
    local output = query:read("*a")
    query:close()
    if output:lower():find("cn", 1, true) then
        language = "cn"
    end
end

local function text(cn, en)
    return language == "cn" and cn or en
end

local commands = {
    { "ping",             "Ping test",                         "Ping connectivity" },
    { "tracert",          "Trace route",                       "Trace network route" },
    { "nslookup",         "DNS query",                         "Query DNS records" },
    { "pathping",         "Path and packet loss test",         "Trace route and packet loss" },
    { "route",            "View or manage routes",             "View or manage IP routes" },
    { "ipconfig",         "View IP configuration",             "Show IP configuration" },
    { "getmac",           "View MAC addresses",                "Show MAC addresses" },
    { "netsh",            "Network configuration console",     "Network configuration console" },
    { "nbtstat",          "NetBIOS diagnostics",               "NetBIOS diagnostics" },
    { "telnet",           "Telnet client",                     "Telnet client" },
    { "arp",              "ARP cache management",              "Manage ARP cache" },
    { "tcping",           "TCP port reachability test",        "Test TCP port connectivity" },
    { "mstsc",            "Remote Desktop connection",         "Remote Desktop Connection" },
    { "ncpa.cpl",         "Open network adapters",             "Open Network Connections" },
    { "ftp",              "FTP client",                        "FTP client" },
    { "ssh",              "Secure Shell client",               "Secure Shell client" },
    { "curl",             "HTTP transfer tool",                "HTTP transfer tool" },
    { "wget",             "Download tool",                     "Download tool" },
    { "service.msc",      "Open Services",                     "Open Services console" },
    { "tasklist",         "View running processes",            "List running processes" },
    { "tar",              "Archive tool",                      "Archive utility" },
    { "optionalfeatures", "Windows optional features",          "Open Windows Features" },
    { "firewall.cpl",     "Windows Defender Firewall",          "Open Windows Defender Firewall" },
    { "sysdm.cpl",        "System properties",                 "Open System Properties" },
    { "powercfg.cpl",     "Power options",                     "Open Power Options" },
    { "msinfo32",         "System information",                "Open System Information" },
    { "inetcpl.cpl",      "Internet options",                  "Open Internet Options" },
    { "appwiz.cpl",       "Programs and Features",             "Open Programs and Features" },
    { "msconfig",         "System configuration",              "Open System Configuration" },
    { "notepad",          "Notepad",                           "Open Notepad" },
    { "calc",             "Calculator",                        "Open Calculator" },
    { "drivers",          "Driver management",                 "Driver management" },
    { "control",          "Control Panel",                     "Open Control Panel" },
    { "desk.cpl",         "Display settings",                  "Open Display Settings" },
    { "winver",           "Windows version",                   "Show Windows version" },
    { "winword",          "Microsoft Word",                    "Open Microsoft Word" },
    { "excel",            "Microsoft Excel",                   "Open Microsoft Excel" },
    { "timedate.cpl",     "Date and time settings",            "Open Date and Time" },
    { "intl.cpl",         "Region settings",                   "Open Region settings" },
    { "regedit",          "Registry Editor",                   "Open Registry Editor" },
    { "taskmgr",          "Task Manager",                      "Open Task Manager" },
    { "hdwwiz",           "Add Hardware Wizard",               "Open Add Hardware Wizard" },
}

local function command_matches()
    local matches = {}
    for _, command in ipairs(commands) do
        table.insert(matches, { match = command[1], type = "word", description = text(command[2], command[3]) })
    end
    return matches
end

local command_generator = clink.generator(20)
function command_generator:generate(line_state, match_builder)
    if line_state:getwordcount() ~= 1 then
        return false
    end
    for _, match in ipairs(command_matches()) do
        match_builder:addmatch(match)
    end
    return false
end

local function describe(matcher, descriptions)
    if matcher.adddescriptions then
        matcher:adddescriptions(descriptions)
    end
    return matcher
end

describe(clink.argmatcher("ping")
    :addflags("-t", "-a", "-n", "-l", "-f", "-i", "-v", "-w", "-4", "-6")
    :addarg({ "HOST", "IP_ADDRESS" }),
{
    ["-t"] = text("持续 Ping，按 Ctrl+C 停止", "Ping continuously; press Ctrl+C to stop"),
    ["-n"] = { " COUNT", text("指定回显请求次数", "Set echo request count") },
    ["-l"] = { " SIZE", text("指定发送缓冲区大小", "Set send buffer size") },
    ["-w"] = { " TIMEOUT", text("指定超时毫秒数", "Set timeout in milliseconds") },
    ["HOST"] = text("主机名、IPv4、IPv6 或 HTTP/HTTPS URL", "Host, IP address, or HTTP/HTTPS URL"),
    ["IP_ADDRESS"] = text("目标 IP 地址", "Target IP address"),
})

describe(clink.argmatcher("tracert")
    :addflags("-d", "-h", "-j", "-w", "-R", "-S", "-4", "-6")
    :addarg({ "HOST", "IP_ADDRESS" }),
{
    ["-d"] = text("不解析地址为主机名", "Do not resolve addresses to host names"),
    ["-h"] = { " MAX_HOPS", text("指定最大跃点数", "Set maximum hops") },
    ["-w"] = { " TIMEOUT", text("指定每次等待毫秒数", "Set wait timeout in milliseconds") },
    ["HOST"] = text("目标主机名或 HTTP/HTTPS URL", "Target host or HTTP/HTTPS URL"),
    ["IP_ADDRESS"] = text("目标 IP 地址", "Target IP address"),
})

describe(clink.argmatcher("nslookup")
    :addarg({ "HOST", "SERVER" }),
{
    ["HOST"] = text("查询的域名、IP 或 HTTP/HTTPS URL", "Name, IP address, or HTTP/HTTPS URL to query"),
    ["SERVER"] = text("指定 DNS 服务器", "DNS server to use"),
})

describe(clink.argmatcher("pathping")
    :addflags("-n", "-h", "-g", "-p", "-q", "-w", "-4", "-6")
    :addarg({ "HOST", "IP_ADDRESS" }),
{
    ["-n"] = text("不解析地址为主机名", "Do not resolve addresses to host names"),
    ["-h"] = { " MAX_HOPS", text("指定最大跃点数", "Set maximum hops") },
    ["HOST"] = text("目标主机名或 HTTP/HTTPS URL", "Target host or HTTP/HTTPS URL"),
    ["IP_ADDRESS"] = text("目标 IP 地址", "Target IP address"),
})

describe(clink.argmatcher("ipconfig")
    :addarg({ "/all", "/release", "/renew", "/flushdns", "/displaydns", "/registerdns" }),
{
    ["/all"] = text("显示完整 TCP/IP 配置", "Show full TCP/IP configuration"),
    ["/release"] = text("释放 IPv4 地址", "Release IPv4 address"),
    ["/renew"] = text("更新 IPv4 地址", "Renew IPv4 address"),
    ["/flushdns"] = text("清理 DNS 解析缓存", "Flush DNS resolver cache"),
    ["/displaydns"] = text("显示 DNS 解析缓存", "Display DNS resolver cache"),
    ["/registerdns"] = text("刷新 DHCP 租约并注册 DNS", "Refresh DHCP lease and register DNS"),
})

describe(clink.argmatcher("route")
    :addarg({ "print", "add", "delete", "change" })
    :addflags("-4", "-6", "/p"),
{
    ["print"] = text("显示路由表", "Display routing table"),
    ["add"] = text("添加路由", "Add a route"),
    ["delete"] = text("删除路由", "Delete a route"),
    ["change"] = text("修改路由", "Change a route"),
    ["/p"] = text("添加永久路由", "Create persistent route"),
})

describe(clink.argmatcher("tcping")
    :addflags("-t", "-n", "-i", "-w", "-h")
    :addarg({ "HOST", "PORT" }),
{
    ["HOST"] = text("目标主机或 IP", "Target host or IP address"),
    ["PORT"] = text("TCP 端口；EasyCMD 简写 tp 未指定时默认 22", "TCP port; EasyCMD tp defaults to 22"),
    ["-t"] = text("持续探测", "Probe continuously"),
    ["-n"] = { " COUNT", text("探测次数", "Probe count") },
})

describe(clink.argmatcher("mstsc")
    :addflags("/v:", "/admin", "/f", "/w:", "/h:", "/multimon", "/public")
    :addarg({ "HOST[:PORT]" }),
{
    ["/v:"] = { " HOST[:PORT]", text("指定远程桌面主机和端口", "Specify RDP host and port") },
    ["/admin"] = text("连接管理会话", "Connect to admin session"),
    ["/f"] = text("全屏启动", "Start full screen"),
    ["HOST[:PORT]"] = text("EasyCMD m 默认使用 3389 端口", "EasyCMD m defaults to port 3389"),
})

describe(clink.argmatcher("curl")
    :addflags("-I", "-L", "-o", "-O", "-v", "-k", "-u", "-H", "-X")
    :addarg({ "URL" }),
{
    ["URL"] = text("完整 URL；EasyCMD 不会裁剪 curl 的 URL", "Full URL; EasyCMD preserves curl URLs"),
    ["-I"] = text("仅获取响应头", "Fetch headers only"),
    ["-L"] = text("跟随重定向", "Follow redirects"),
    ["-o"] = { " FILE", text("指定输出文件", "Write output to file") },
    ["-H"] = { " HEADER", text("添加请求头", "Add request header") },
    ["-X"] = { " METHOD", text("指定 HTTP 方法", "Specify HTTP method") },
})

describe(clink.argmatcher("netsh")
    :addarg({ "interface", "firewall", "advfirewall", "wlan", "winhttp" }),
{
    ["interface"] = text("网络接口配置", "Network interface configuration"),
    ["firewall"] = text("旧版防火墙配置", "Legacy firewall configuration"),
    ["advfirewall"] = text("高级防护墙配置", "Advanced Firewall configuration"),
    ["wlan"] = text("无线网络配置", "Wireless network configuration"),
    ["winhttp"] = text("WinHTTP 代理配置", "WinHTTP proxy configuration"),
})

local function easycmd_help(rl_buffer, line_state)
    if not line_state then
        return
    end
    local words = line_state:getwordcount()
    if words > 1 then
        rl.invokecommand("possible-completions")
        return
    end
    local prefix = line_state:getendword()
    local shown = {}
    for _, command in ipairs(commands) do
        if prefix == "" or command[1]:lower():sub(1, #prefix) == prefix:lower() then
            table.insert(shown, command)
        end
    end
    rl_buffer:beginoutput()
    if #shown == 0 then
        print(text("没有匹配的 EasyCMD 命令。", "No matching EasyCMD commands."))
        return
    end
    print(text("EasyCMD 可用命令：", "EasyCMD available commands:"))
    for _, command in ipairs(shown) do
        print(string.format("  %-18s %s", command[1], text(command[2], command[3])))
    end
end

rl.describemacro([["luafunc:easycmd_help"]], "Show EasyCMD command or argument help")
rl.setbinding([["?"]], [["luafunc:easycmd_help"]])

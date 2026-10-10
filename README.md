# EasyCMD

[中文操作手册](MANUAL.zh-CN.md) | [Download Releases](../../releases)

EasyCMD provides convenient Windows CMD network-command shortcuts. `ping`,
`tracert`, `nslookup`, and `pathping` can accept HTTP/HTTPS URLs; EasyCMD
extracts the host name before running the native Windows command.

## Installation

### One-line PowerShell install

Open PowerShell and run the following command. It downloads the latest GitHub
Release, requests UAC permission, backs up an existing installation, installs
`easycmd.exe` into `C:\Windows\System32`, and refreshes EasyCMD automatically.

```powershell
$s="$env:TEMP\easycmd-install.ps1"; iwr https://raw.githubusercontent.com/yydylab/easycmd/main/install.ps1 -OutFile $s; powershell -ExecutionPolicy Bypass -File $s
```

The same `install.ps1` script is attached to every GitHub Release for users
who prefer to download and run it directly.

### Manual install

1. Download `easycmd.exe` from Releases.
2. Copy it to `C:\Windows\System32\easycmd.exe`.
3. Run:

   ```cmd
   easycmd install
   ```

4. Open a new CMD window:

   ```cmd
   start
   ```

Running `easycmd install` refreshes all EasyCMD macros and startup information.

## Interactive Tab And Help

EasyCMD includes its own interactive CMD input layer. It does not require Clink
or any other third-party terminal extension. After `easycmd install`, every new
CMD window starts the EasyCMD prompt automatically.

Windows commands such as `winver`, `sysdm.cpl`, `control`, and `taskmgr` run
directly from the interactive prompt. EasyCMD management commands, including
`easycmd -h`, `easycmd cn`, `easycmd en`, and `easycmd update`, are handled in
the same prompt without starting a nested CMD session.

| Input | Result |
| --- | --- |
| Press `Tab` at an empty prompt | Lists EasyCMD commands with descriptions. |
| Type a prefix such as `p`, then press `Tab` or `?` | Completes a unique command, or lists matching commands such as `ping`, `pathping`, and `powercfg.cpl`. |
| Type `ping `, then press `Tab` or `?` | Lists supported `ping` arguments with descriptions. |
| Type `ipconfig f`, then press `Tab` | Completes to `ipconfig /flushdns`; parameter aliases do not need `-` or `/`. |
| Type `route p` or `route d`, then press `Tab` | Completes to `route print` or `route delete`. |
| Type `easycmd `, then press `Tab` or `?` | Lists EasyCMD management commands such as `update`, `history`, `cn`, `en`, `install`, and `uninstall`. |
| Type `easycmd history `, then press `Tab` or `?` | Shows the `clear` parameter and its description. |
| Press the Up or Down arrow | Browse commands used in EasyCMD, including commands from earlier CMD windows. |
| `easycmd cn` | Switches completion descriptions to Chinese. |
| `easycmd en` | Switches completion descriptions to English. |
| `easycmd -h` / `easycmd help` | Prints help in the language selected by `easycmd cn` or `easycmd en`. |
| `easycmd history` / `easycmd history clear` | Show or clear the persistent command history. |

History is stored for the current user in `%LOCALAPPDATA%\EasyCMD\history.txt` and retains the latest 500 commands.
The language changes immediately. The completion catalog also includes commonly used Windows tools such as `ncpa.cpl`,
`service.msc`, `taskmgr`, `control`, `regedit`, `winver`, `notepad`, and `calc`.
Network-command parameters can be entered either in their native form or without the
leading `-` or `/`: for example, `ping w 100 host`, `tracert d host`, `ipconfig flushdns`,
`route delete`, `curl head URL`, and `mstsc host admin` are normalized before execution.

## Version And Updates

```cmd
easycmd -v
easycmd update
easycmd cn
easycmd en
easycmd help
```

The one-line installer is the recommended method for a first installation. It
always selects the latest GitHub Release; `easycmd update` handles later
upgrades.

When used inside the EasyCMD interactive prompt, `easycmd update` schedules the
update and exits that prompt so Windows can replace `easycmd.exe`. A new CMD
window opens only after the copied executable reports the expected new version.
If an update cannot replace or verify the executable, review
`%LOCALAPPDATA%\EasyCMD\update.log`.

After `easycmd install`, every newly opened CMD window displays the EasyCMD
version, copyright, and project URL before the prompt. `easycmd update`
compares the local executable with the latest GitHub Release and upgrades it
after UAC approval when needed.

## Command Shortcut Summary

| Shortcut | Actual command | Example | Extension |
| --- | --- | --- | --- |
| `p` | `ping` | `p https://example.com/path` | `p t example.com` -> `ping -t example.com` |
| `t` | `tracert` | `t https://example.com/path` | `t dw example.com` -> `tracert -d -w 1 example.com`<br>`t wd example.com` -> `tracert -w 1 -d example.com` |
| `n` | `nslookup` | `n https://example.com/path` | Extracts the URL host name. |
| `a` | `arp` | `a -a` | Pass native options directly. |
| `s` | `ssh` | `s user@host` | Pass native options directly. |
| `c` | `curl` | `c https://example.com` | `c c` -> `curl cip.cc`<br>`c i` -> `curl ipinfo.io` |
| `cc` / `ci` | `curl cip.cc` / `curl ipinfo.io` | `cc 1.1.1.1` / `ci 1.1.1.1` | Any IPv4 address becomes a lookup path: `cc 1.1.1.1` -> `curl cip.cc/1.1.1.1`<br>`ci 1.1.1.1` -> `curl ipinfo.io/1.1.1.1` |
| `f` | `ftp` | `f ftp.example.com` | Pass native options directly. |
| `m` | `mstsc` | `m 192.168.1.1` | Defaults to port `3389`. |
| `pa` | `pathping` | `pa https://example.com/path` | Extracts the URL host name. |
| `tp` | `tcping` | `tp 192.168.1.200 3389` | Defaults to SSH port `22`; requires [tcping.exe](https://github.com/pouriyajamshidi/tcping) in `PATH` or `System32`. |
| `te` | `telnet` | `te 192.168.1.1 23` | Requires Windows Telnet Client. |
| `i` | `ipconfig` | `i a` / `i f` | `i a` -> `ipconfig /all`<br>`i f` -> `ipconfig /flushdns` |
| `ia` / `if` | `ipconfig /all` / `ipconfig /flushdns` | `ia` / `if` | Direct IP configuration shortcuts. |
| `g` | `getmac` | `g /v` | Pass native options directly. |
| `ne` | `netsh` | `ne interface ip show config` | Pass native contexts and options directly. |
| `r` | `route` | `r p` | `r p 4` -> `route print -4`<br>`r p 6` -> `route print -6`<br>`r a <destination> <CIDR> <gateway>` |
| `rp` / `rp4` / `rp6` | `route print` | `rp4` | `rp` -> `route print`<br>`rp4` -> `route print -4`<br>`rp6` -> `route print -6` |
| `nb` | `nbtstat` | `nb -n` | Pass native options directly. |

## Uninstall

```cmd
easycmd uninstall
```

This removes only EasyCMD's current-user CMD AutoRun configuration.

## Build And Test

```powershell
.\build.ps1
.\test.ps1
```

EasyCMD uses the built-in .NET Framework C# compiler and has no third-party
runtime dependencies.

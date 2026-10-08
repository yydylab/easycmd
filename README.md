# EasyCMD

[中文操作手册](MANUAL.zh-CN.md) | [Download Releases](../../releases)

EasyCMD provides convenient Windows CMD network-command shortcuts. `ping`,
`tracert`, `nslookup`, and `pathping` can accept HTTP/HTTPS URLs; EasyCMD
extracts the host name before running the native Windows command.

## Installation

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
It also removes legacy ACMD macros for the current user.

## Version And Updates

```cmd
easycmd -v
easycmd update
```

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
| `cc` / `ci` | `curl cip.cc` / `curl ipinfo.io` | `cc` / `ci` | Direct IP information shortcuts. |
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

This removes only EasyCMD macros from the current user's CMD AutoRun setting.

## Build And Test

```powershell
.\build.ps1
.\test.ps1
```

EasyCMD uses the built-in .NET Framework C# compiler and has no third-party
runtime dependencies.

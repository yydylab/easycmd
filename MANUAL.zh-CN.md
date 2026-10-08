# EasyCMD 使用操作手册

## 1. 功能说明

EasyCMD 为 Windows CMD 提供常用网络命令简写。`ping`、`tracert`、`nslookup`、
`pathping` 可直接接受 HTTP/HTTPS 网址；按下 Enter 后，EasyCMD 会提取网址主机名，
再调用 Windows 原生命令。

```cmd
p https://github.com/chrisant996/clink
t https://123.com/admin
n https://abc.cn/path
```

实际执行为：

```cmd
ping github.com
tracert 123.com
nslookup abc.cn
```

## 2. 安装

1. 从 Releases 下载 `easycmd.exe`。
2. 以管理员身份打开 CMD 或 PowerShell，并复制程序：

   ```cmd
   copy easycmd.exe C:\Windows\System32\easycmd.exe
   ```

3. 在任意 CMD 中执行：

   ```cmd
   easycmd install
   ```

4. 关闭当前窗口并重新打开 CMD。

`easycmd install` 会为当前用户写入
`HKCU\Software\Microsoft\Command Processor\AutoRun`，刷新 EasyCMD 的全部宏和
启动信息，并自动清理旧版 ACMD 宏。

## 3. 版本、启动信息与更新

```cmd
easycmd -v
easycmd update
```

执行 `easycmd install` 后，每次新开 CMD 窗口都会在提示符前显示 EasyCMD 的版本、
版权和项目地址。`easycmd update` 会检查 GitHub 最新 Release；有新版本时下载并请求
UAC 授权完成替换。

## 4. 命令简写

| 简写 | 实际命令 | 案例 | 拓展 |
| --- | --- | --- | --- |
| `p` | `ping` | `p https://example.com/path` | `p t example.com` -> `ping -t example.com` |
| `t` | `tracert` | `t https://example.com/path` | `t dw example.com` -> `tracert -d -w 1 example.com`<br>`t wd example.com` -> `tracert -w 1 -d example.com` |
| `n` | `nslookup` | `n https://example.com/path` | 自动提取网址主机名。 |
| `a` | `arp` | `a -a` | 原生参数可直接传入。 |
| `s` | `ssh` | `s user@host` | 原生参数可直接传入。 |
| `c` | `curl` | `c https://example.com` | `c c` -> `curl cip.cc`<br>`c i` -> `curl ipinfo.io` |
| `cc` / `ci` | `curl cip.cc` / `curl ipinfo.io` | `cc` / `ci` | 快速查询 IP 信息。 |
| `f` | `ftp` | `f ftp.example.com` | 原生参数可直接传入。 |
| `m` | `mstsc` | `m 192.168.1.1` | 未指定时默认端口 `3389`。 |
| `pa` | `pathping` | `pa https://example.com/path` | 自动提取网址主机名。 |
| `tp` | `tcping` | `tp 192.168.1.200 3389` | 未带端口时默认 SSH `22`；需安装 [tcping.exe](https://github.com/pouriyajamshidi/tcping) 并放入 `PATH` 或 `System32`。 |
| `te` | `telnet` | `te 192.168.1.1 23` | 需启用 Windows Telnet Client。 |
| `i` | `ipconfig` | `i a` / `i f` | `i a` -> `ipconfig /all`<br>`i f` -> `ipconfig /flushdns` |
| `ia` / `if` | `ipconfig /all` / `ipconfig /flushdns` | `ia` / `if` | 直接显示配置或清理 DNS 缓存。 |
| `g` | `getmac` | `g /v` | 原生参数可直接传入。 |
| `ne` | `netsh` | `ne interface ip show config` | 原生上下文与参数可直接传入。 |
| `r` | `route` | `r p` | `r p 4` -> `route print -4`<br>`r p 6` -> `route print -6`<br>`r a <目标> <CIDR> <网关>` |
| `rp` / `rp4` / `rp6` | `route print` | `rp4` | `rp` -> `route print`<br>`rp4` -> `route print -4`<br>`rp6` -> `route print -6` |
| `nb` | `nbtstat` | `nb -n` | 原生参数可直接传入。 |

## 5. 卸载

```cmd
easycmd uninstall
```

该操作仅移除当前用户的 EasyCMD 宏。

## 6. 构建与测试

```powershell
.\build.ps1
.\test.ps1
```

EasyCMD 使用 Windows 自带的 .NET Framework C# 编译器，不依赖第三方运行时组件。

## 7. 注意事项

- 仅转换以 `http://` 或 `https://` 开头的参数。
- `curl` 会保留完整 URL。
- `easycmd.exe` 直接启动原生命令，不使用 `cmd /c`。

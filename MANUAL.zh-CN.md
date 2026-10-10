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
2. 复制程序到`C:\Windows\System32 目录`：
3. 在任意 CMD 中执行：

   ```cmd
   easycmd install
   ```

4. 关闭当前窗口并重新打开 CMD。

   ```cmd
   start
   ```
`easycmd install` 会为当前用户写入
`HKCU\Software\Microsoft\Command Processor\AutoRun`，刷新 EasyCMD 的全部宏和启动信息。

## 3. Tab 补全与 `?` 帮助

EasyCMD 内置交互式 CMD 输入层，不依赖 Clink 或任何第三方终端增强程序。执行
`easycmd install` 后，新开的 CMD 会自动进入 EasyCMD 提示符，提供类似网络设备 CLI
的补全和帮助。

在交互提示符中可直接执行 `winver`、`sysdm.cpl`、`control`、`taskmgr` 等 Windows
命令；`easycmd -h`、`easycmd cn`、`easycmd en`、`easycmd update` 等管理命令也会在
当前提示符内执行，不会再启动嵌套 CMD 或重复显示启动横幅。

| 输入方式 | 结果 |
| --- | --- |
| 在空提示符直接按 `Tab` | 显示全部 EasyCMD 命令及中文备注。 |
| 输入前缀如 `p` 后按 `Tab` 或 `?` | 唯一匹配时自动补全；存在多个匹配时显示 `ping`、`pathping`、`powercfg.cpl` 等候选项。 |
| 输入 `ping ` 后按 `Tab` 或 `?` | 显示 `ping` 支持的参数及说明。 |
| 输入 `ipconfig f` 后按 `Tab` | 自动补全为 `ipconfig /flushdns`；参数可不输入 `-` 或 `/`。 |
| 输入 `route p` 或 `route d` 后按 `Tab` | 自动补全为 `route print` 或 `route delete`。 |
| 输入 `easycmd ` 后按 `Tab` 或 `?` | 显示 `update`、`history`、`cn`、`en`、`install`、`uninstall` 等 EasyCMD 管理命令。 |
| 输入 `easycmd history ` 后按 `Tab` 或 `?` | 显示 `clear` 参数及说明。 |
| 按上、下方向键 | 翻看本次及此前 CMD 窗口中使用过的 EasyCMD 命令。 |
| `easycmd cn` | 切换为中文补全与帮助说明。 |
| `easycmd en` | 切换为英文补全与帮助说明。 |
| `easycmd -h` / `easycmd help` | 按 `easycmd cn` 或 `easycmd en` 当前设置显示帮助。 |
| `easycmd history` / `easycmd history clear` | 查看或清空持久化历史命令。 |

执行 `easycmd cn` 或 `easycmd en` 后，说明语言立即生效。
历史命令保存在当前用户的 `%LOCALAPPDATA%\EasyCMD\history.txt`，默认保留最近 500 条。
补全目录包含常用网络命令及 Windows 工具，例如 `ncpa.cpl`、`service.msc`、`taskmgr`、
`control`、`regedit`、`winver`、`notepad`、`calc` 等。
网络命令参数既可使用原生写法，也可省略开头的 `-` 或 `/`：例如 `ping w 100 host`、
`tracert d host`、`ipconfig flushdns`、`route delete`、`curl head URL`、`mstsc host admin`
都会在执行前转换为对应的原生参数。

## 4. 版本、启动信息与更新

```cmd
easycmd -v
easycmd update
easycmd cn
easycmd en
easycmd help
```

在 EasyCMD 交互提示符中执行 `easycmd update` 后，程序会安排升级并自动退出当前
交互提示符，以便 Windows 替换正在运行的 `easycmd.exe`。仅在替换后的程序验证为预期新版本后，
才会自动打开新的 CMD 窗口；若失败，请查看 `%LOCALAPPDATA%\EasyCMD\update.log`。

执行 `easycmd install` 后，每次新开 CMD 窗口都会在提示符前显示 EasyCMD 的版本、
版权和项目地址。`easycmd update` 会检查 GitHub 最新 Release；有新版本时下载并请求
UAC 授权完成替换。

## 5. 命令简写

| 简写 | 实际命令 | 案例 | 拓展 |
| --- | --- | --- | --- |
| `p` | `ping` | `p https://example.com/path` | `p t example.com` -> `ping -t example.com` |
| `t` | `tracert` | `t https://example.com/path` | `t dw example.com` -> `tracert -d -w 1 example.com`<br>`t wd example.com` -> `tracert -w 1 -d example.com` |
| `n` | `nslookup` | `n https://example.com/path` | 自动提取网址主机名。 |
| `a` | `arp` | `a -a` | 原生参数可直接传入。 |
| `s` | `ssh` | `s user@host` | 原生参数可直接传入。 |
| `c` | `curl` | `c https://example.com` | `c c` -> `curl cip.cc`<br>`c i` -> `curl ipinfo.io` |
| `cc` / `ci` | `curl cip.cc` / `curl ipinfo.io` | `cc 1.1.1.1` / `ci 1.1.1.1` | 任意 IPv4 地址会作为查询路径：`cc 1.1.1.1` -> `curl cip.cc/1.1.1.1`<br>`ci 1.1.1.1` -> `curl ipinfo.io/1.1.1.1` |
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

## 6. 卸载

```cmd
easycmd uninstall
```

该操作仅移除当前用户的 EasyCMD AutoRun 配置。

## 7. 构建与测试

```powershell
.\build.ps1
.\test.ps1
```

EasyCMD 使用 Windows 自带的 .NET Framework C# 编译器，不依赖第三方运行时组件。

## 8. 注意事项

- 仅转换以 `http://` 或 `https://` 开头的参数。
- `curl` 会保留完整 URL。
- `easycmd.exe` 直接启动原生命令，不使用 `cmd /c`。

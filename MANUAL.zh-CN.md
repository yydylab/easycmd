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

| 输入方式 | 结果 |
| --- | --- |
| 在空提示符直接按 `Tab` | 显示全部 EasyCMD 命令及中文备注。 |
| 输入前缀如 `p` 后按 `Tab` 或 `?` | 唯一匹配时自动补全；存在多个匹配时显示 `ping`、`pathping`、`powercfg.cpl` 等候选项。 |
| 输入 `ping ` 后按 `Tab` 或 `?` | 显示 `ping` 支持的参数及说明。 |
| `easycmd cn` | 切换为中文补全与帮助说明。 |
| `easycmd en` | 切换为英文补全与帮助说明。 |
| `easycmd help` | 在普通输出中查看交互帮助速查。 |

执行 `easycmd cn` 或 `easycmd en` 后，说明语言立即生效。
补全目录包含常用网络命令及 Windows 工具，例如 `ncpa.cpl`、`service.msc`、`taskmgr`、
`control`、`regedit`、`winver`、`notepad`、`calc` 等。

## 4. 版本、启动信息与更新

```cmd
easycmd -v
easycmd update
easycmd cn
easycmd en
easycmd help
```

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

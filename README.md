# MX HMI TopMost

一个轻量的 Windows 窗口置顶工具。

用于把普通应用窗口设置为始终置顶，也可以随时取消置顶。适合那些本身没有置顶功能、但需要长时间浮在其他窗口上方的软件。
<img width="1901" height="999" alt="image" src="https://github.com/user-attachments/assets/2c30f6ac-a578-461d-90e8-208d171baf6c" />


## 功能

- 列出当前可见窗口
- 单独切换某个窗口的置顶状态
- 一键取消全部置顶窗口
- 托盘常驻
- 关闭主窗口时最小化到托盘
- 支持开机自启
- 支持自定义快捷键
- 支持鼠标指向窗口后用快捷键切换置顶

## 默认快捷键

| 功能 | 默认快捷键 |
| --- | --- |
| 鼠标指向窗口切换置顶 | `Ctrl + 5` |
| 显示 / 隐藏本工具 | `Ctrl + 6` |
| 取消全部置顶 | `Ctrl + Alt + R` |

快捷键可以在程序的“设置”中修改。

## 使用方式

1. 打开 `MxHmiWindowHost.exe`
2. 在窗口列表中选择目标窗口
3. 点击 `置顶` 或双击列表项
4. 再次点击可取消置顶
5. 关闭主窗口后，程序会留在系统托盘中

也可以把鼠标移动到目标窗口上，然后按快捷键直接切换置顶状态。

## 下载

发布包位于 GitHub Releases。

当前版本文件名：

- `MxHmiTopMost-v0.2.1.zip`

解压后直接运行 `MxHmiWindowHost.exe` 即可。

## 项目结构

```text
.
├─ assets/          # 图标等资源
├─ scripts/         # 实际构建脚本
├─ src/             # C# 源码
├─ build.ps1        # 根目录构建入口
├─ README.md
├─ LICENSE
└─ .gitignore
```

构建产物默认输出到：

- `bin/`
- `release/`

## 构建

项目使用 .NET Framework 编译器构建，不依赖 NuGet。

在项目目录中运行：

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\build.ps1
```

构建结果：

- `bin\MxHmiWindowHost.exe`
- `release\MxHmiTopMost-v0.2.1.zip`

## 注意事项

- 仅支持 Windows。
- 普通窗口可以被置顶，但不能覆盖 UAC、安全桌面、锁屏界面等系统安全界面。
- 如果目标窗口以管理员权限运行，本工具也需要以管理员权限运行才能稳定控制它。
- 多个置顶窗口之间仍然会互相覆盖，这是 Windows 本身的窗口层级规则。
- 未签名的 exe 可能会触发 Windows SmartScreen 或杀毒软件提醒。

## 开机自启

在程序的“设置”中勾选“开机自启”即可。

开机自启使用当前用户的注册表启动项，不需要管理员权限。开机启动时程序会默认进入托盘。

## 许可证

本项目使用 MIT License。

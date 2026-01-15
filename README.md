## EnvVarManager

一个简单、现代的 Windows 环境变量图形管理工具（WPF / .NET 8）。

### 功能特点

- **用户 / 系统变量切换**：通过作用域下拉框快速查看当前用户变量或本机系统变量。
- **增删改查**：
  - 列表查看所有环境变量名称和值。
  - 支持新增变量、编辑选中变量、删除选中变量。
  - 变更后通过 `WM_SETTINGCHANGE` 广播，让新启动的进程获取到最新变量。
- **备份与恢复**：
  - 将当前用户 + 系统环境变量完整导出为 JSON 备份文件。
  - 从 JSON 备份中一键恢复（会覆盖当前的对应作用域环境变量，操作前建议额外做好备份）。
- **现代 UI**：
  - 统一的配色和圆角卡片式布局。
  - 清晰的操作分区和安全提示。

### 本地运行

前置要求：

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/) 已安装

在仓库根目录执行：

```bash
dotnet restore
dotnet run
```

应用启动后即可在主界面中选择作用域、浏览和编辑环境变量。

> 提示：修改系统变量通常需要以管理员身份运行程序，否则部分操作可能失败。

### 发布 / 打包

项目已经配置为使用 .NET 8 WPF：

- 项目文件：`EnvVarManager.csproj`
- 目标框架：`net8.0-windows`

本地打包为自包含单文件（`win-x64`）：

```bash
dotnet publish EnvVarManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out/win-x64
```

打包完成后，在 `out/win-x64` 目录下会生成 `EnvVarManager.exe`，可直接拷贝到其他 Windows 机器使用。

### GitHub Actions 自动构建

仓库中已包含工作流：`.github/workflows/build.yml`，在推送到 `main` / `master` 分支或创建 PR 时会自动：

- 恢复依赖并构建项目；
- 执行自包含发布（`win-x64`）；
- 上传编译输出为构建 Artifact（`EnvVarManager-win-x64`）。

你可以在 GitHub Actions 的对应构建记录中直接下载该 Artifact，用于分发或自测。

### 安全注意事项

- 恢复操作会覆盖当前的环境变量，请确保：
  - 备份文件来自可信来源；
  - 在恢复前对当前环境变量状态有额外备份。
- 删除或修改关键系统变量（例如 `PATH`）可能导致程序无法正常运行，操作时需谨慎。



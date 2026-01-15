using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace EnvVarManager;

public enum EnvScope
{
    User,
    System
}

public class EnvironmentService
{
    private const string EnvironmentSubKey = "Environment";

    public IList<EnvVarEntry> GetVariables(EnvScope scope)
    {
        var list = new List<EnvVarEntry>();
        using var key = OpenScopeKey(scope, writable: false);
        if (key == null)
        {
            return list;
        }

        foreach (var valueName in key.GetValueNames())
        {
            var value = key.GetValue(valueName)?.ToString() ?? string.Empty;
            list.Add(new EnvVarEntry
            {
                Name = valueName,
                Value = value
            });
        }

        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    public void SetVariable(string name, string value, EnvScope scope)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("变量名不能为空。", nameof(name));
        }

        using var key = OpenScopeKey(scope, writable: true)
                        ?? throw new InvalidOperationException("无法打开注册表键。");

        key.SetValue(name, value, RegistryValueKind.ExpandString);
        BroadcastEnvironmentChange();
    }

    public void DeleteVariable(string name, EnvScope scope)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        using var key = OpenScopeKey(scope, writable: true)
                        ?? throw new InvalidOperationException("无法打开注册表键。");

        key.DeleteValue(name, throwOnMissingValue: false);
        BroadcastEnvironmentChange();
    }

    public void BackupAll(string filePath)
    {
        var backup = new EnvBackup
        {
            CreatedAt = DateTimeOffset.Now,
            UserVariables = GetVariables(EnvScope.User),
            SystemVariables = GetVariables(EnvScope.System)
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(backup, options);
        File.WriteAllText(filePath, json);
    }

    public void RestoreFromBackup(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("备份文件不存在。", filePath);
        }

        var json = File.ReadAllText(filePath);
        var backup = JsonSerializer.Deserialize<EnvBackup>(json)
                     ?? throw new InvalidOperationException("备份文件格式不正确。");

        RestoreScope(EnvScope.User, backup.UserVariables);
        RestoreScope(EnvScope.System, backup.SystemVariables);

        BroadcastEnvironmentChange();
    }

    private void RestoreScope(EnvScope scope, IList<EnvVarEntry> items)
    {
        using var key = OpenScopeKey(scope, writable: true)
                        ?? throw new InvalidOperationException("无法打开注册表键。");

        // 清空现有变量
        foreach (var name in key.GetValueNames())
        {
            key.DeleteValue(name, throwOnMissingValue: false);
        }

        // 写入备份内容
        foreach (var entry in items)
        {
            key.SetValue(entry.Name, entry.Value, RegistryValueKind.ExpandString);
        }
    }

    private static RegistryKey? OpenScopeKey(EnvScope scope, bool writable)
    {
        return scope switch
        {
            EnvScope.User => Registry.CurrentUser.OpenSubKey(EnvironmentSubKey, writable),
            EnvScope.System => Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Control\Session Manager\{EnvironmentSubKey}",
                writable),
            _ => null
        };
    }

    private static void BroadcastEnvironmentChange()
    {
        const int HWND_BROADCAST = 0xffff;
        const int WM_SETTINGCHANGE = 0x1A;
        const int SMTO_ABORTIFHUNG = 0x0002;

        SendMessageTimeout(
            new IntPtr(HWND_BROADCAST),
            WM_SETTINGCHANGE,
            IntPtr.Zero,
            "Environment",
            SMTO_ABORTIFHUNG,
            5000,
            out _);
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int Msg,
        IntPtr wParam,
        string? lParam,
        int fuFlags,
        int uTimeout,
        out IntPtr lpdwResult);

    private class EnvBackup
    {
        public DateTimeOffset CreatedAt { get; set; }
        public IList<EnvVarEntry> UserVariables { get; set; } = new List<EnvVarEntry>();
        public IList<EnvVarEntry> SystemVariables { get; set; } = new List<EnvVarEntry>();
    }
}



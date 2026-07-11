# ============================================================
# Uninstall-AutoSleep.ps1 - 无内部提权，直接执行（NSIS已提权）
# ============================================================

$installDir = "C:\ProgramData\AutoSleep"
$taskName = "AutoSleep"
$uninstallRegKey = "HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoSleep"

Write-Host "正在卸载 AutoSleep..." -ForegroundColor Cyan

# 1. 结束所有 AutoSleep 进程
Get-Process -Name powershell -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -like "*AutoSleep.ps1*" -and $_.Id -ne $PID
} | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "已结束 AutoSleep 后台进程" -ForegroundColor Green

# 2. 删除桌面快捷方式
$desktopPath = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopPath "AutoSleep 设置.lnk"
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
    Write-Host "已删除桌面快捷方式" -ForegroundColor Green
}

# 3. 删除计划任务
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($task) {
    Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Write-Host "已删除计划任务" -ForegroundColor Green
}

# 4. 强力删除注册表（强制 64 位视图）
Write-Host "正在删除注册表项..." -ForegroundColor Yellow

# 删除 64 位视图（主视图）
Start-Process -FilePath "reg.exe" -ArgumentList "delete `"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoSleep`" /f /reg:64" -Wait -WindowStyle Hidden

# 也删除 32 位视图（以防有旧版本残留）
Start-Process -FilePath "reg.exe" -ArgumentList "delete `"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoSleep`" /f /reg:32" -Wait -WindowStyle Hidden

# 检查 64 位视图是否还存在
$regPathPs = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoSleep"
if (Test-Path $regPathPs) {
    Write-Host "❌ 64 位注册表项删除失败，尝试 PowerShell 删除..." -ForegroundColor Red
    try {
        Remove-Item -Path $regPathPs -Recurse -Force -ErrorAction Stop
        Write-Host "✅ PowerShell 删除成功" -ForegroundColor Green
    } catch {
        Write-Host "❌ 删除彻底失败，请手动删除！路径：$regPathPs" -ForegroundColor Red
    }
} else {
    Write-Host "✅ 注册表项已彻底清除" -ForegroundColor Green
}

# 5. 删除安装目录中的所有文件（保留 Uninstall.exe 自身）
if (Test-Path $installDir) {
    Write-Host "正在清理安装目录..." -ForegroundColor Yellow
    Get-ChildItem -Path $installDir -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path $installDir -File | Where-Object { $_.Name -ne "Uninstall.exe" } | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "其他文件已删除" -ForegroundColor Green
}

# 6. 创建自删除批处理（删除 Uninstall.exe 和目录）
$batPath = "$env:TEMP\SelfDelete.bat"
$content = @"
@echo off
del /f /q "$installDir\Uninstall.exe" 2>nul
rmdir /s /q "$installDir" 2>nul
del /f /q "$batPath" 2>nul
"@
Set-Content -Path $batPath -Value $content -Encoding ASCII

Start-Process -FilePath $batPath -WindowStyle Hidden

Write-Host "卸载完成。3秒后自动清理残余。" -ForegroundColor Green
# 脚本结束，Uninstall.exe 退出
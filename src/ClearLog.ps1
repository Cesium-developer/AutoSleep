# ClearLog.ps1 - 清空日志和更新配置（独立运行，不依赖主脚本上下文）

$logFile = "C:\ProgramData\AutoSleep\AutoSleep.log"
$configPath = "C:\ProgramData\AutoSleep\settings.json"

# 结束其他 AutoSleep 进程（排除当前后台进程）
# ---- 检测运行模式 ----
$isExe = $MyInvocation.MyCommand.Path -match '\.exe$'
$processPattern = if ($isExe) { "*AutoSleep.exe*" } else { "*AutoSleep.ps1*" }

Get-CimInstance -ClassName Win32_Process | Where-Object {
    $_.CommandLine -like $processPattern -and $_.ProcessId -ne $PID
} | ForEach-Object {
    Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Milliseconds 500

# 删除日志文件
if (Test-Path $logFile) {
    Remove-Item -Path $logFile -Force -ErrorAction SilentlyContinue
}

# 更新配置中的轮转时间
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$config.LastRotationTime = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
$config | ConvertTo-Json | Set-Content -Path $configPath -Encoding UTF8

# 重启计划任务
schtasks /run /tn "AutoSleep" 2>&1 | Out-Null
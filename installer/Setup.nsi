; AutoSleep 安装程序（等效于原 WinRAR 自解压）
OutFile "AutoSleep_Setup.exe"
RequestExecutionLevel admin
SilentInstall normal   ; 正常显示解压进度，但不会阻挡后续窗口

Section
    ; 解压到临时目录（与原 WinRAR 的 TempMode 一致）
    SetOutPath "$TEMP\AutoSleepInstall"
    File "AutoSleep.ps1"
    File "Deploy-AutoSleep.ps1"
    File "Settings.ps1"
    File "README.txt"
    File "Uninstall.exe"
    File "ClearLog.ps1"

    ; 执行部署脚本（与原 WinRAR 的 Setup 命令一致，不加 -NoExit，因为 Deploy 脚本最后有 Read-Host）
    ExecWait 'powershell.exe -ExecutionPolicy Bypass -File "$TEMP\AutoSleepInstall\Deploy-AutoSleep.ps1"'

    ; 安装完成后删除临时文件（与原 WinRAR 的 DeleteAfter 对应）
    RMDir /r "$TEMP\AutoSleepInstall"
SectionEnd
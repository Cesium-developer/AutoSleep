; 根据宏设置输出文件名
!ifndef EXE_VERSION
  OutFile "AutoSleep_Setup_script.exe"
!else
  OutFile "AutoSleep_Setup_exe.exe"
!endif

RequestExecutionLevel admin
SilentInstall normal

Section
    SetOutPath "$TEMP\AutoSleepInstall"

    !ifndef EXE_VERSION
        ; 脚本版：复制 .ps1 文件
        File "..\src\AutoSleep.ps1"
        File "..\src\Deploy-AutoSleep.ps1"
        File "..\src\Settings.ps1"
        File "..\src\Uninstall_script.exe"
        File "..\src\ClearLog.ps1"
        File "..\docs\README.txt"
        ExecWait 'powershell.exe -ExecutionPolicy Bypass -File "$TEMP\AutoSleepInstall\Deploy-AutoSleep.ps1"'
    !else
        ; EXE 版：复制 .exe 文件
        File "..\src\AutoSleep.exe"
        File "..\src\Deploy-AutoSleep.exe"
        File "..\src\Settings.exe"
        File "..\src\Uninstall_exe.exe"
        File "..\src\ClearLog.exe"
        File "..\docs\README.txt"
        ExecWait '"$TEMP\AutoSleepInstall\Deploy-AutoSleep.exe"'
    !endif

    RMDir /r "$TEMP\AutoSleepInstall"
SectionEnd
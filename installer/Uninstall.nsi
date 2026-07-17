; 默认是脚本版输出文件名（如果未定义 EXE_VERSION）
!ifndef EXE_VERSION
  OutFile "Uninstall_script.exe"
!else
  OutFile "Uninstall_exe.exe"
!endif

SilentInstall silent
RequestExecutionLevel admin

Section
    SetOutPath "$TEMP"

    !ifndef EXE_VERSION
        ; 脚本版：打包 .ps1，用 powershell 执行
        File "..\src\Uninstall-AutoSleep.ps1"
        ExecWait '"powershell.exe" -ExecutionPolicy Bypass -File "$TEMP\Uninstall-AutoSleep.ps1"'
        Delete "$TEMP\Uninstall-AutoSleep.ps1"
    !else
        ; EXE 版：打包 .exe，直接执行
        File "..\src\Uninstall-AutoSleep.exe"
        ExecWait '"$TEMP\Uninstall-AutoSleep.exe"'
        Delete "$TEMP\Uninstall-AutoSleep.exe"
    !endif
SectionEnd
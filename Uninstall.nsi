OutFile "Uninstall.exe"
SilentInstall silent
RequestExecutionLevel admin

Section
    ; 解压卸载脚本到临时目录
    SetOutPath "$TEMP"
    File "Uninstall-AutoSleep.ps1"

    ; 执行卸载脚本（等待完成）
    ExecWait '"powershell.exe" -ExecutionPolicy Bypass -File "$TEMP\Uninstall-AutoSleep.ps1"'

    ; 删除临时脚本
    Delete "$TEMP\Uninstall-AutoSleep.ps1"
SectionEnd
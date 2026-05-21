@echo off
echo ============================================
echo   截图工具 - 编译
echo ============================================

set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo [ERROR] 未找到 csc.exe，请确认已安装 .NET Framework 4.0
    pause
    exit /b 1
)

echo 编译器：%CSC%
echo.
echo 编译中...

"%CSC%" /target:winexe /win32icon:截图.ico /out:截图工具.exe /optimize+ /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ScreenshotTool.cs

if exist 截图工具.exe (
    echo.
    echo ============================================
    echo   编译成功！双击 截图工具.exe 即可使用
    echo   热键：Ctrl+Q 或 F1 快速截图
    echo ============================================
) else (
    echo.
    echo [ERROR] 编译失败
)
pause

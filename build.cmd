@echo off
rem ---------------------------------------------------------------
rem  Deskrawl Mem Governor - one-click build
rem  Requires: .NET Framework 4.x (C# 5 compiler), no SDK needed
rem  Usage: build.cmd          (builds and waits for a key)
rem         build.cmd nopause (builds, no key wait)
rem ---------------------------------------------------------------
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" goto nocsc

echo [1/2] Building DeskrawlMemGovernor.exe ...
"%CSC%" /nologo /target:winexe /out:DeskrawlMemGovernor.exe ^
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll ^
  "src\Core.cs" "src\Ui.cs" "src\MainForm.cs" "src\Program.cs"
if errorlevel 1 goto fail

echo [2/2] Done: DeskrawlMemGovernor.exe
echo.
echo Run it by double-clicking DeskrawlMemGovernor.exe.
echo Admin rights are usually NOT required (same-user processes only).
echo Command line: DeskrawlMemGovernor.exe --cli help
goto done

:nocsc
echo [ERROR] csc.exe not found. Install .NET Framework 4.x first.
goto fail

:fail
echo.
echo Build failed.
if "%~1"=="" pause
exit /b 1

:done
if "%~1"=="" pause
endlocal

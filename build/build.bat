@ECHO OFF
SET Arbor.X.MSBuild.NuGetRestore.Enabled=true
SET Arbor.X.NuGet.Package.Artifacts.CreateOnAnyBranchEnabled=true
CALL "%~dp0\Build.exe"

EXIT /B %ERRORLEVEL%

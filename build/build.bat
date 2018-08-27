@ECHO OFF
SET Arbor.X.MSBuild.NuGetRestore.Enabled=true
SET Arbor.X.NuGet.Package.Artifacts.CreateOnAnyBranchEnabled=true
CALL dotnet arbor-build

EXIT /B %ERRORLEVEL%

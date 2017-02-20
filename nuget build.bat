@echo off

::find the file name of the spec
for /f "tokens=*" %%a in ('dir "*.nuspec" /b /s') do set p=%%~nxa

if defined p (
nuget pack "%p%"
pause
) else (
echo No NuGet Specification Found!
)
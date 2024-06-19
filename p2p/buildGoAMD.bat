@echo off

cd go-code

echo Building Go Code...

REM Compile Go code to generate a shared library (.dll) file
go build -ldflags="-s -w" -buildmode=c-shared -o ..\..\chat\libgo.dll .\chatp2p.go .\chatp2p_api.go

REM Check if compilation was successful
if %errorlevel% equ 0 (
    echo Compilation [32msuccessful[0m.
) else (
    echo Compilation [31mfailed[0m. Error code: %errorlevel%
)

cd ..

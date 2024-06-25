#!/bin/bash

cd go-code || exit

echo Building Go Code...

export CGO_ENABLED=1
export GOOS=linux
export GOARCH=arm64
export CC=aarch64-linux-gnu-gcc
export CXX=aarch64-linux-gnu-g++

# Clean Go build cache
go clean -cache -modcache -i -r > /dev/null 2>&1

# Compile Go code to generate a shared library (.so) file
go build -ldflags="-s -w" -buildmode=c-shared -o libgo.so chatp2p.go chatp2p_api.go > /dev/null 2>&1

# Check if compilation was successful
if [ $? -eq 0 ]; then
    echo -e "Compilation \033[32msuccessful\033[0m."
else
    echo -e "Compilation \033[31mfailed\033[0m. Error code: $?"
fi

cp libgo.so ../../chat/


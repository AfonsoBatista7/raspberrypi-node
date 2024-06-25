#!/bin/bash

echo Building DotNet code...

dotnet build > /dev/null 2>&1

dotnet publish --runtime linux-arm64 --self-contained -p:DefineConstants="LINUX" -o out > /dev/null 2>&1

# Check if compilation was successful
if [ $? -eq 0 ]; then
    echo -e "Compilation \033[32msuccessful\033[0m."
else
    echo -e "Compilation \033[31mfailed\033[0m. Error code: $?"
fi

cp libgo.so ./out


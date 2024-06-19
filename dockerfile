#BUILD Go Lib
FROM golang:1.22 AS builderGo

# Set the working directory inside the container
WORKDIR /ProjectGo

COPY ./p2p .

ENV CGO_ENABLED=1 && \
    GOOS=linux && \
    GOARCH=arm64 && \
    CC=aarch64-linux-gnu-gcc && \
    CXX=aarch64-linux-gnu-g++ 

# Clean Go build cache
# Compile Go code to generate a shared library (.so) file
RUN go clean -cache -modcache -i -r && \
    go build -ldflags="-s -w" -buildmode=c-shared -o libgo.so chatp2p.go chatp2p_api.go

#BUILD DotNet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builderDotNet

# Set the working directory inside the container
WORKDIR /ProjectDotNet

COPY ./chat .
COPY --from=builderGo /ProjectGo/libgo.so ./

RUN dotnet restore && \
    dotnet add package System.Device.Gpio --version 2.2.0-* && \
    dotnet publish --runtime linux-arm64 --self-contained -o out

RUN cp libgo.so ./out 


# Use the official .NET Docker image for ARM64 architecture
FROM mcr.microsoft.com/dotnet/aspnet:8.0.3-jammy-arm64v8 AS runtime

# Install necessary packages for GPIO access
RUN apt-get update && \
    apt-get install -y \
    libgpiod2 \
    libgpiod-dev

# Set the working directory inside the container
WORKDIR /Project

# Copy the compiled binary into the container
COPY --from=builderDotNet /ProjectDotNet/out ./

# Make the binary executable
RUN chmod +x chat

ENV LD_LIBRARY_PATH=/Project:$LD_LIBRARY_PATH

EXPOSE 4001/tcp
EXPOSE 4001/udp

# Command to run the executable
ENTRYPOINT ["./chat"]

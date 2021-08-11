#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && "pwd" )"

echo -e "\n## Building Crypto Agent"

echo -e "\nBuilding app"
echo ".NET Core version $(dotnet --version)"
echo "Restore"
dotnet restore "$DIR/CryptoAgent.csproj"
echo "Clean"
dotnet clean "$DIR/CryptoAgent.csproj" -c "Release" -o "$DIR/obj/build-output/publish"
echo "Publish"
dotnet publish "$DIR/CryptoAgent.csproj" -c "Release" -o "$DIR/obj/build-output/publish"

echo -e "\nBuilding docker image"
docker --version
docker build -t bitwarden/crypto-agent "$DIR/."

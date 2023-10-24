#!/bin/bash

cp /etc/bitwarden/ca-certificates/*.crt /usr/local/share/ca-certificates/ >/dev/null 2>&1 \
    && update-ca-certificates

dotnet /app/KeyConnector.dll
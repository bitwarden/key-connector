# Multi-stage build for hardened container
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS base

# Install build dependencies and YubiHSM2 SDK
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        ca-certificates \
        libc6-dev \
        opensc \
        dpkg-dev \
    && curl -fsSL -o yubihsm2-sdk.tar.gz \
        https://developers.yubico.com/YubiHSM2/Releases/yubihsm2-sdk-2023-11-debian12-amd64.tar.gz \
    && tar -xzf yubihsm2-sdk.tar.gz \
    && rm yubihsm2-sdk.tar.gz \
    && dpkg -i yubihsm2-sdk/libyubihsm-http1_*_amd64.deb \
    && dpkg -i yubihsm2-sdk/libyubihsm1_*_amd64.deb \
    && dpkg -i yubihsm2-sdk/yubihsm-pkcs11_*_amd64.deb \
    && apt-get install -y -f --no-install-recommends \
    && rm -rf /var/lib/apt/lists/* yubihsm2-sdk/ \
    && apt-get clean

# Runtime stage - hardened but maintains compatibility  
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime

LABEL com.bitwarden.product="bitwarden"

# Copy YubiHSM libraries from base stage
COPY --from=base /usr/lib/x86_64-linux-gnu/libyubihsm* /usr/lib/x86_64-linux-gnu/
COPY --from=base /usr/lib/x86_64-linux-gnu/pkcs11/ /usr/lib/x86_64-linux-gnu/pkcs11/
COPY --from=base /usr/lib/x86_64-linux-gnu/opensc-pkcs11.so /usr/lib/x86_64-linux-gnu/

# Install only essential runtime packages
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        gosu \
        curl \
        ca-certificates \
        passwd \
        libpam-modules \
    && rm -rf /var/lib/apt/lists/* \
    && apt-get clean \
    # Remove package manager to reduce attack surface
    && dpkg --remove --force-depends apt dpkg \
    && rm -rf /var/lib/dpkg/info/* \
    && rm -rf /usr/share/doc/* \
    && rm -rf /usr/share/man/* \
    && rm -rf /usr/share/locale/* \
    # Remove unnecessary binaries but keep essential ones for entrypoint
    && find /usr/bin /usr/sbin -type f ! -name 'gosu' ! -name 'groupadd' ! -name 'useradd' \
       ! -name 'groupmod' ! -name 'usermod' ! -name 'mkhomedir_helper' ! -name 'chown' \
       ! -name 'mkdir' ! -name 'curl' ! -name 'update-ca-certificates' ! -name 'cp' ! -name 'sh' \
       -delete 2>/dev/null || true

# Security configurations
ENV ASPNETCORE_URLS=http://+:5000
WORKDIR /app
EXPOSE 5000

# Copy application
COPY obj/build-output/publish .

# Copy and set up entrypoint script
COPY entrypoint.sh /
RUN chmod +x /entrypoint.sh

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Use entrypoint script to maintain dynamic user support
ENTRYPOINT ["/entrypoint.sh"]
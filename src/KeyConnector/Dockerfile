FROM mcr.microsoft.com/dotnet/aspnet:9.0

LABEL com.bitwarden.product="bitwarden"

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gosu=1.14* \
    curl=7.88.1* \
    libc6-dev=2.36* \
    opensc=0.23.0* \
    && rm -rf /var/lib/apt/lists/*

# Install YubiHSM2 SDK
RUN curl -O https://developers.yubico.com/YubiHSM2/Releases/yubihsm2-sdk-2023-11-debian12-amd64.tar.gz \
    && tar -xzf yubihsm2-sdk-*.tar.gz \
    && rm yubihsm2-sdk-*.tar.gz \
    && dpkg -i yubihsm2-sdk/libyubihsm-http1_*_amd64.deb \
    && dpkg -i yubihsm2-sdk/libyubihsm1_*_amd64.deb \
    && dpkg -i yubihsm2-sdk/yubihsm-pkcs11_*_amd64.deb \
    && apt-get install -y -f --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS http://+:5000
WORKDIR /app
EXPOSE 5000

COPY obj/build-output/publish .

COPY entrypoint.sh /
RUN chmod +x /entrypoint.sh

HEALTHCHECK CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["/entrypoint.sh"]

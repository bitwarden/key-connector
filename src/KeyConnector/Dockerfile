FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble

LABEL com.bitwarden.product="bitwarden"

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    gosu=1.17* \
    curl=8.5.0* \
    libc6-dev=2.39* \
    opensc=0.25.0* \
    && rm -rf /var/lib/apt/lists/*

# Install YubiHSM2 SDK, pinned by SHA-256 (taken from a tarball whose Yubico GPG signature verified)
ARG YUBIHSM_SDK_TARBALL=yubihsm2-sdk-2023-11-debian12-amd64.tar.gz
ARG YUBIHSM_SDK_SHA256=72c5159480cb518d1c391207b4d239661e0a40f297c08d43837f3fbf075223d4
RUN curl --proto "=https" -fsSLO "https://developers.yubico.com/YubiHSM2/Releases/${YUBIHSM_SDK_TARBALL}" \
    && echo "${YUBIHSM_SDK_SHA256}  ${YUBIHSM_SDK_TARBALL}" | sha256sum -c - \
    && tar -xzf "${YUBIHSM_SDK_TARBALL}" \
    && rm "${YUBIHSM_SDK_TARBALL}" \
    && dpkg -i yubihsm2-sdk/libyubihsm-http1_*_amd64.deb \
    && dpkg -i yubihsm2-sdk/libyubihsm1_*_amd64.deb \
    && dpkg -i yubihsm2-sdk/yubihsm-pkcs11_*_amd64.deb \
    && apt-get install -y -f --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5000
WORKDIR /app
EXPOSE 5000

COPY obj/build-output/publish .

COPY entrypoint.sh /
RUN chmod +x /entrypoint.sh

HEALTHCHECK CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["/entrypoint.sh"]
# ================= BUILD =================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY ISDSS.sln ./
COPY ISDSS.Application/ISDSS.Application.csproj ISDSS.Application/
COPY ISDSS.Domain/ISDSS.Domain.csproj ISDSS.Domain/
COPY ISDSS.Infrastructure/ISDSS.Infrastructure.csproj ISDSS.Infrastructure/
COPY ISDSS.Presentation.UI/ISDSS.Presentation.UI.csproj ISDSS.Presentation.UI/

RUN dotnet restore ISDSS.Presentation.UI/ISDSS.Presentation.UI.csproj

COPY . .

RUN dotnet publish ISDSS.Presentation.UI/ISDSS.Presentation.UI.csproj \
    -c Release \
    -r linux-x64 \
    --no-self-contained \
    -o /app/publish

# ================= RUNTIME =================
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# X11 + Avalonia deps
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libharfbuzz0b \
    libicu-dev \
    libx11-6 \
    libxcomposite1 \
    libxrandr2 \
    libxi6 \
    libxtst6 \
    libxinerama1 \
    libgl1 \
    libice6 \
    libsm6 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV DISPLAY=:0
ENV AVALONIA_USE_X11=1

ENTRYPOINT ["dotnet", "ISDSS.Presentation.UI.dll"]

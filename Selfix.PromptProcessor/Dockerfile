# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker cache for restore
COPY ["Selfix.PromptProcessor.EntryPoint/Selfix.PromptProcessor.EntryPoint.csproj", "Selfix.PromptProcessor.EntryPoint/"]
COPY ["Selfix.PromptProcessor.Application/Selfix.PromptProcessor.Application.csproj", "Selfix.PromptProcessor.Application/"]
COPY ["Selfix.PromptProcessor.Infrastructure/Selfix.PromptProcessor.Infrastructure.csproj", "Selfix.PromptProcessor.Infrastructure/"]
COPY ["Selfix.PromptProcessor.Shared/Selfix.PromptProcessor.Shared.csproj", "Selfix.PromptProcessor.Shared/"]
COPY ["Submodules/Selfix.Schema/Selfix.Schema/Selfix.Schema.csproj", "Submodules/Selfix.Schema/Selfix.Schema/"]

RUN dotnet restore "Selfix.PromptProcessor.EntryPoint/Selfix.PromptProcessor.EntryPoint.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/Selfix.PromptProcessor.EntryPoint"
RUN dotnet publish "Selfix.PromptProcessor.EntryPoint.csproj" -c Release -o /app/publish

# Runtime stage
FROM vllm/vllm-openai:v0.8.3 AS runtime

# Install .NET runtime
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    wget \
    apt-transport-https \
    software-properties-common && \
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y --no-install-recommends dotnet-runtime-9.0 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# User and workspace setup (same as before)
ENV PUID=${PUID:-1000}
ENV PGID=${PGID:-1000}
RUN groupadd -g "${PGID}" appuser && \
    useradd -m -s /bin/sh -u "${PUID}" -g "${PGID}" appuser

WORKDIR /app
RUN mkdir -p /workspace/models
RUN chown -R appuser:appuser /app /workspace

# Copy scripts and app
COPY ["Scripts/start-vllm.sh", "/app/start-vllm.sh"]
COPY ["Scripts/entrypoint.sh", "/app/entrypoint.sh"]
RUN chmod +x /app/start-vllm.sh /app/entrypoint.sh
COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app

USER appuser
ENTRYPOINT ["/app/entrypoint.sh"]
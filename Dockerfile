# Dev-style container: runs the app with `dotnet run` straight from source,
# using the full SDK image — NOT a multi-stage publish into a slim runtime
# image. This matches "run normally, not published."
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

# Copy only the csproj first so `dotnet restore` is cached in its own layer —
# it only re-runs when dependencies actually change, not on every code edit.
COPY Task_Management/Task_Management.csproj Task_Management/
RUN dotnet restore Task_Management/Task_Management.csproj

# Install the EF Core CLI tool so migrations can be applied from inside the
# container (docker compose exec api dotnet ef database update).
RUN dotnet tool install --global dotnet-ef --version 10.*
ENV PATH="${PATH}:/root/.dotnet/tools"

# Now copy the rest of the source. docker-compose additionally bind-mounts
# this same folder at runtime, so edits on your host are picked up without
# rebuilding the image.
COPY Task_Management/ Task_Management/
WORKDIR /src/Task_Management

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# --no-launch-profile: ignore launchSettings.json (its https profile/dev-cert
# assumptions don't apply inside a container).
ENTRYPOINT ["dotnet", "run", "--no-launch-profile"]
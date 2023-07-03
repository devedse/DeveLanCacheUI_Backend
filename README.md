# DeveLanCacheUI_Backend
A UI for Lan Cache

## How to run this

```
version: '3'

services:
  develancacheui_backend:
    image: devedse/develancacheui_backend:latest
    restart: unless-stopped
    ports:
      - '7301:80'
    environment:
      - LanCacheLogsDirectory=/var/develancacheui/lancachelogs
      - DepotFileDirectory=/var/develancacheui/depotdir
      - ConnectionStrings__DefaultConnection=Data Source=/var/develancacheui/database/develancacheui.db;
    volumes:
      - "/home/pi/dockercomposers/develancacheui/backend/depotdir:/var/develancacheui/depotdir"
      - "/home/pi/dockercomposers/develancacheui/backend/database:/var/develancacheui/database"
      - "/mnt/devenologynas/DockerComposers/lancache/logs:/var/develancacheui/lancachelogs"
  develancacheui_frontend:
    image: devedse/develancacheui_frontend:latest
    restart: unless-stopped
    ports:
      - '7302:80'
    environment:
      - BACKENDURL=https://develancacheui_api.devedse.duckdns.org
```

Steps:
1. Create/mount the relevant directories
2. Run the docker-compose file
3. Copy paste the app-depot-output.csv file in the mounted `depotdir`. This will automatically fill the database with all Depot => App mappings
4. Profit


## Build status

| GitHubActions Builds |
|:--------------------:|
| [![GitHubActions Builds](https://github.com/devedse/DeveLanCacheUI_Backend/workflows/GitHubActionsBuilds/badge.svg)](https://github.com/devedse/DeveLanCacheUI_Backend/actions/workflows/githubactionsbuilds.yml) |

## DockerHub

| Docker Hub |
|:----------:|
| [![Docker pulls](https://img.shields.io/docker/v/devedse/develancacheui_backend)](https://hub.docker.com/r/devedse/develancacheui_backend/) |

## Code Coverage Status

| CodeCov |
|:-------:|
| [![codecov](https://codecov.io/gh/devedse/DeveLanCacheUI_Backend/branch/master/graph/badge.svg)](https://codecov.io/gh/devedse/DeveLanCacheUI_Backend) |

## Code Quality Status

| SonarQube |
|:---------:|
| [![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=DeveLanCacheUI_Backend&metric=alert_status)](https://sonarcloud.io/dashboard?id=DeveLanCacheUI_Backend) |

## Package

| NuGet |
|:-----:|
| [![NuGet](https://img.shields.io/nuget/v/DeveLanCacheUI_Backend.svg)](https://www.nuget.org/packages/DeveLanCacheUI_Backend/) |

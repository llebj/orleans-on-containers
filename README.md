# Introduction
orleans-on-containers is a console-based instant messaging application that explores the features of [Microsoft Orleans](https://learn.microsoft.com/en-gb/dotnet/orleans/overview), in particular, the different methods that can be used for clustering.

# Overview
This project consists of two main parts: the [Client](src/OrleansOnContainers/Client) and the [Silo](src/OrleansOnContainers/Silo). The Client is an interactive .NET console application that sends messages to and receives messages from other connected Clients. The Silo is a .NET console application that hosts an Orleans silo. Clients send messages to each other via a grain managed by the Silo. Both the Client and the Silo contain Dockerfiles that allow images to be created for each application, enabling the use of containerisation tools to easily deploy a working instance of orleans-on-containers.

As stated earlier, the aim of this project is to explore the clustering features of Microsoft Orleans and to see how clustering behaves in different hosting configurations. There are currently two supported clustering providers:
1. Development (managed by a grain)
2. ADO.NET (PostgreSQL)

In addition to the Client and the Silo, a Docker file exists to create an [extended Postgres instance](src/OrleansOnContainers/Storage/PostgreSQL) that contains the database artifacts required to support Orleans clustering. As part of the creation of the Postgres clustering provider, a special database role called 'orleans_on_containers' is created with reduced access permissions. Further information on accessing the database is listed below. 

# Deployment
The simplest way to run orleans-on-containers is to use `docker compose` and the yaml files located in `src/OrleansOnContainers`. Instructions for running orleans-on-containers are broken down by clustering provider.

## Development Clustering
An instance of orleans-on-containers configured to use development clustering can be deployed by running `docker compose up -d`. The `docker attach` command can then be used to attach to a Client instance to interact with the application.

## ADO.NET Clustering (PostgreSQL)
An instance of orleans-on-containers configured to use ADO.NET clustering can be deployed using the [`compose.postgres.yaml`](src/OrleansOnContainers/compose.postgres.yaml) file. As well as the Silo and the Client, this compose file also builds and runs the extended Postgres instance.

Several options are required to configure ADO.NET clustering. The `compose.postgres.yaml` file makes use of the `POSTGRES_PASSWORD_FILE` environment variable used by the base Postgres image. The password should be stored in `/secrets/postgres_password.txt`. 

>[!IMPORTANT]
>The `/secrets` directory must be placed at the root of the repository.

The following environment variables are also required:

| Variable Name | Description |
| --- | --- |
| PostgresVersion | A valid [postgres Docker image](https://hub.docker.com/_/postgres) tag used to specify the base image for the postgres service. |
| NpgsqlVersion | A vaild [Npgsql nuget package](https://www.nuget.org/packages/Npgsql/) version. This package provides Orleans with the capability to interact with the postgres database. |
| ConnectionString | The connection string for the postgres database. |

>[!NOTE]
>The specialised orleans_on_containers user should be used to connect to the database instead of the postgres superuser. To do so, use the following connection string:
>
>`Host=postgres;Port=5432;Database=orleans_on_containers;Username=orleans_on_containers;Password=orleans_on_containers;`

To deploy orleans-on-containers with ADO.NET clustering, the above mentioned environment variables must be provided. By placing a file called `.env.postgres` in a directory called `/env` at the root of the repository, the following command can be used to deploy the services:

`docker compose -f compose.postgres.yaml --env-file ../../env/.env.postgres up -d`

Both the `client` and `silo` services depend on the `postgres` service being healthy, so you should allow time for that condition to be met.

# User Guide
Upon starting, the client generates a GUID which it uses to join a chat called 'test'. This GUID identifies the client when sending messages.

The client supports a basic feature set:

- Messages are composed by typing into the console.
- The client supports alphanumeric and special characters.
- Characters can be removed by using the backspace key.
- Pressing the Enter key will send a message.
- Pressing Ctrl + q, or Esc will terminate the application.

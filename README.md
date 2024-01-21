# orleans-on-containers
## Introduction
orleans-on-containers is a console-based instant messaging application that explores the features of Microsoft Orleans, in particular, the different methods that can be used for clustering.

## Overview
This project consists of two main parts: the `Client` and the `Silo`. The Client is an interactive .NET console application that sends messages to and receives messages from other connected Clients. The Silo is another .NET console application that hosts an Orleans silo. Clients send messages to each other via a grain managed by the Silo. Both the Client and the Silo contain Dockerfiles that allow images to be created for each application, enabling the use of containerisation tools to easily deploy a working instance of orleans-on-containers.

As stated earlier, the aim of this project is to explore the clustering features of Microsoft Orleans and to see how clustering behaves in different hosting configurations. There are currently two supported clustering providers:
1. Development (managed by a grain)
2. ADO.NET (PostgreSQL)

A Docker compose file exists for each clustering provider. Any required configuration for running orleans-on-containers with each provider will be documented below.

## User Guide
The simplest way to run orleans-on-containers is to use the existing Docker compose files. Instructions for running orleans-on-containers are broken down by clustering provider.

### Development Clustering
Coming soon...

### ADO.NET Clustering (PostgreSQL)
Coming soon...

# ProteusNet

Just like its namesake, ProteusNet is a highly versatile network framework for the Unity Engine. Unlike most network frameworks, ProteusNet is not limited to a single network model or the Unity Runtime alone. It allows developers to create their own custom network models, whether server authoritative, distributed authority, or client prediction, and synchronise data even within the Unity Editor without starting the Playmode or a build. By using either a MonoBehaviour or an EditorWindow Network Manager, it is possible to connect clients over the internet from either the Editor or Runtime. The customisable transport layer also supports the implementation of custom transports, allowing for TCP-based or dedicated solutions like Steamworks.Net, EpicOnlineServices, etc. </br>
While ProteusNet can be used for full-fledged games and other Unity applications, it is currently mainly geared towards use-cases, where more fine-grained control over the underlying netcode is required. ProteusNet has been used in multiple research projects for evaluating different network models and netcode optimisations or quick and easy prototyping.

## Features

- Client-server and client-hosted architecture
- Un-/reliable, un-/ordered UDP communication
- Extensible transport layer
- Transport implementation for Unity Gaming Services
- MonoBehaviour Network Manager for Unity scenes
- Static EditorWindow Network Manager for Unity Editor
- API for sending byte arrays or structs across the network
- Automatic serializer for classes and structs
- Server discovery module for LAN

## Roadmap

- Components for automatically synchronising Unity scenes
- Custom transport implementation
- SteamWorks and Epic Online Services transport implementation
- Improvements to the serializer and Unity transport implementation
- Automated testing suite
- Network simulator module for testing different network conditions
- Network statistics module

Wiki is in progress...

## About

This is a solo project, which I started as part of my master's thesis to learn netcode development. This means that the time I can spend on Proteus is limited, and new features are not guaranteed to arrive at regular intervals. Nonetheless, I'm happy about any and all feedback or feature suggestions.

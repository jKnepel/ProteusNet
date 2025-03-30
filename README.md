# ProteusNet

Just like its namesake, ProteusNet is a highly versatile network framework for the Unity Engine. Unlike most network frameworks, ProteusNet is not limited to the Unity Runtime alone. It allows developers to synchronize data even within the Unity Editor without starting the playmode or a build. By using either a MonoBehaviour or an EditorWindow Network Manager, it is possible to connect clients over the internet from either the Editor or Runtime. The customizable transport layer also supports the implementation of custom transports, allowing developers to implement their own transmittion concepts. </br>
While ProteusNet can be used for full-fledged games and other Unity applications, it is currently mainly geared towards use-cases, where more fine-grained control over the underlying netcode is required. ProteusNet has been used in multiple research projects for evaluating different network models and netcode optimisations or quick and easy prototyping.

## Features

- Client-server and client-hosted architecture
- Un-/reliable, un-/ordered UDP communication
- Extensible transport layer
- Transport implementation for Unity Gaming Services
- MonoBehaviour Network Manager for Unity scenes
- Static EditorWindow Network Manager for Unity Editor
- Components for automatically replicating GameObjects across the network
- API for sending byte arrays or structs across the network
- Automatic serializer for classes and structs
- Server discovery module for LAN
- Network Metrics and GUI Module

## Roadmap

- Automated Benchmark and testing suite
- RPCs and Replicatable Variables
- Input Prediction
- Custom transport implementation
- SteamWorks and Epic Online Services transport implementation
- Improvements to the Serializer
- Wiki for Developers

Or visit the [Trello Board](https://trello.com/b/Lgzz2o7C/proteusnet) with current and planned tasks

## Installation

Download the package through the Window > Package Manager in the Unity Editor with Add package from git URL.... Using the URL https://github.com/jKnepel/ProteusNet.git#upm for the newest version, or https://github.com/jKnepel/ProteusNet.git#VERSION_TAG for a specific one. The available version tags can be found on the [Tags](https://github.com/jKnepel/ProteusNet/tags) page.

## About

This is a solo project, which I started as part of my master's thesis to learn netcode development. This means that the time I can spend on Proteus is limited, and new features are not guaranteed to arrive at regular intervals. Nonetheless, I'm happy about any and all feedback or feature suggestions.

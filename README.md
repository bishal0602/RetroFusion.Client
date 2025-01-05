# Retrofusion.Client [ARCHIVED]

> [!WARNING] 
> This repository has been archived and moved to the [Retrofusion main repository](https://github.com/Fiesty-Cushion/Retro-Fusion). Please use the main repository for all updates, issues, and contributions.

## Historical Information

This was originally a client app for the Retrofusion project that:
1. Listened for UDP broadcast messages on the network containing information about available servers
2. Parsed UDP packets to extract host information (server IP address and WebSocket port)
3. Established WebSocket connections to servers using the extracted host information
4. Streamed mobile phone sensor data to the server over WebSocket for gameplay control

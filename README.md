## Under construction 🚧

Client app for Retrofusion project

Basically listens to UDP broadcasts on the network. Parses the packet for host information and then based on the extracted information, connects to a Web Socket port and sends device's Motion Sensor data.

### Main components
- [Main View Model](src/SensorStream.MAUI/ViewModels/MainPageViewModel.cs)
- [Upd Listener](src/SensorStream.MAUI/Services/UdpListener.cs)
- [Lobby View Model](src/SensorStream.MAUI/ViewModels/LobbyViewModel.cs)
- [Web Socket Client](src/SensorStream.MAUI/Services/WebSocketService.cs)
- [Demo Godot Server](demo_server.gd)


### [Download apk](https://1drv.ms/u/c/6f7b14e4559c6d8e/EcE6osX5FFNMoi7vSk3-_t4Bh2DP5tmYIALU5TYvVjZOUQ?e=ugfleG)
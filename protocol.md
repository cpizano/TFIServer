## Network Protocol

#1 Client Connects via TCP  (TCP::Connect()  -> GameLogic::Connect())
Server replies (TCP)
    ==== welcome ======================
	int32 ServerPackets.welcome (1)
	int32 ServerSend.version
	int32 ServerHandle.version
	int32 client id   (aka _fromClient)

    int32 map version   (MapHandler.mapVersion)
	int32 # of layers
	int32 # of rows
	int32 # of columns

	int32 byte count

Client replies (TCP) either
    ==== Session End =================
	int32 ClientPackets.sessionEnd (3)
	string reason
	int32 byte count

	=== Welcome Received ===========
	int32 ClientPackets.welcomeReceived (1)
	int32 client id  (aka server's _fromClient)
	string player name
	int32 map version
	int32 map # of layers
	int32 map # of rows
	int32 map # of columns

	int32 byte count

	also client connects via UDP Client.instance.udp.Connect()

Server Replies (TCP)
    === Map Layer Row ===========
	int32 layer
	int32 row
	int32 row lenght
	int16 first cell
	int16 second cell
	...
	...
	int16 last cell

	int32 byte count

	(repeats for # of columns)











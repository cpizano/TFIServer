## Network Protocol

#1 Client Connects via TCP  (TCP::Connect()  -> GameLogic::Connect())
Server replies (TCP)
    ==== welcome ======================
	int32 packet lenghth (bytes) 
	int32 ServerPackets.welcome (1)
	int32 ServerSend.version
	int32 ServerHandle.version
	int32 client id   (aka _fromClient)

    int32 map version   (MapHandler.mapVersion)
	int32 # of layers
	int32 # of rows
	int32 # of columns

Client replies (TCP) either
    ==== Session End =================
	int32 packet lenghth (bytes)
	int32 ClientPackets.sessionEnd (3)
	string reason

	=== Welcome Received ===========
	int32 packet lenghth (bytes)
	int32 ClientPackets.welcomeReceived (1)
	int32 client id  (aka server's _fromClient)
	string player name
	int32 map version
	int32 map # of layers
	int32 map # of rows
	int32 map # of columns

	also client connects via UDP Client.instance.udp.Connect()

Server Replies (TCP)
	==== Spawn Player =============
	int32 packet lenghth (bytes)
	int32 ServerPackets.spawnPlayer (2)
	int32 player id
	string user_name
	float position.x
	float position.y
	int32 z_level;
	float roation x 
	float rotation y
	float rotation z
	float rotation w
	int32 health

Server Sends (TCP)
    === Map Layer Row ===========
	int32 packet lenghth (bytes)
	int32 ServerPackets.mapLayerRow (6)
	int32 layer
	int32 row
	int32 row lenght
	int16 first cell
	int16 second cell
	...
	...
	int16 last cell  (repeats for # of columns)

(above packet repeats for # of rows)

if client presses WASD keys
Client sends (UDP)
    ==== Player Movement ===========
    int32 packet lenghth (bytes)
    int32 ClientPackets.playerMovement (2)
    int32 input key length (4)
	bool key on (W)
	bool key on (S)
	bool key on (A)
	bool key on (D)
	float roation x 
	float rotation y
	float rotation z
	float rotation w

Server will reply to each ClientPackets.playerMovement via  (UDP)
    ===== Player Position ===========
    int32 packet length (bytes)
    int32 ServerPackets.playerPosition (3)
	int32 client id
	float x
	float y
	int32 z_level

(above packet is sent to all clients)

Server sends (UDP)
	==== Player health ========
	int32 packet lenght (bytes)
	int32 ServerPackets.playerHealth (5)
	int32 health

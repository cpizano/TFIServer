using System.Drawing;
using System.Numerics;

namespace TFIServer
{
    // This object methods should only be called from the gamethread.
    class Player
    {
        public readonly int id;
        public readonly string user_name;

        private Point input_direction;
        private PlayerState state;

        public int move_speed;

        public Point Position { get => state.position; }
        public TransitState TransitState { get => state.transit_state; }
        public int ZLevel { get => state.z_level;  }
        public int Health { get => state.health; }


        public Player(int _id, string username, Point spawn_position, int _z_level)
        {
            id = _id;
            user_name = username;
            state = new PlayerState(spawn_position, _z_level, 100, TransitState.Ground);
            // FIXME
            move_speed = (int)(2.4f * 32) / Constants.TICKS_PER_SEC;
        }
        public void Update(GameLogic game)
        {
            if (input_direction == Point.Empty)
            {
                return;
            }

            // For 3D, Z is forward (towards screen) and +Y is up.
            // Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            // Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

            var forward = new Size(0, 1);
            var right = new Size(-1, 0);

            var move_direction = (right * input_direction.X) + (forward * input_direction.Y);
            var proposed_position = state.position + (move_direction * move_speed);

            (var ns, int tz_boost) = game.MovePlayer(state, proposed_position);
            if (ns is PlayerState new_state)
            {
                state = new_state;
                ServerSend.PlayerPosition(this, tz_boost);
            }
        }

        public void SetInput(bool[] inputs)
        {
            input_direction = Point.Empty;

            if (inputs[0])  // W
            {
                input_direction.Y += 1;
            }
            if (inputs[1])  // S
            {
                input_direction.Y -= 1;
            }
            if (inputs[2]) // A
            {
                input_direction.X += 1;
            }
            if (inputs[3]) // D
            {
                input_direction.X -= 1;
            }
        }
    }
}

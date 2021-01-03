using System;
using System.Collections;
using System.Drawing;
using System.Numerics;
using System.Text;

namespace TFIServer
{
    public enum TransitState
    {
        Frozen,     // Cannot move at all.
        Ground,     // Can move in the plane anywhere
        Threshold,  // Entering or exiting a special zone
        Stairs,     // Can move up and down.
        ClosedArea, // Restricted to an area.
    };

    public readonly struct PlayerState
    {
        public readonly Vector2 position;
        public readonly int z_level;
        public readonly int health;
        public readonly TransitState transit_state;

        public PlayerState(Vector2 _pos, int _z_level, int _health, TransitState _state)
        {
            position = _pos;
            z_level = _z_level;
            health = _health;
            transit_state = _state;
        }

        public PlayerState(in PlayerState o)
        {
            position = o.position;
            z_level = o.z_level;
            health = o.health;
            transit_state = o.transit_state;
        }

        public PlayerState(in PlayerState o, Vector2 _pos) : this(o)
        {
            position = _pos;
        }

        public PlayerState(in PlayerState o, int _z_level, Vector2 _pos) : this(o)
        {
            position = _pos;
            z_level = _z_level;
        }

        public PlayerState(in PlayerState o, TransitState ts, Vector2 _pos) : this(o)
        {
            transit_state = ts;
            position = _pos;
        }
    }

    // This object methods should only be called from the gamethread.
    class Player
    {
        public readonly int id;
        public readonly string user_name;

        private Point input_direction;
        private PlayerState state;

        public float move_speed = 2.5f / Constants.TICKS_PER_SEC;

        public Vector2 Position { get => state.position; }
        public TransitState TransitState { get => state.transit_state; }
        public int ZLevel { get => state.z_level;  }
        public int Health { get => state.health; }


        public Player(int _id, string username, Vector2 spawn_position, int _z_level)
        {
            id = _id;
            user_name = username;
            state = new PlayerState(spawn_position, _z_level, 100, TransitState.Ground);
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

            Vector2 forward = Vector2.UnitY;
            Vector2 right = -Vector2.UnitX;

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

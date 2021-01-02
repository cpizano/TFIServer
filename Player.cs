using System;
using System.Collections;
using System.Drawing;
using System.Numerics;
using System.Text;

namespace TFIServer
{
    // This object methods should only be called from the gamethread.
    class Player
    {
        public enum TransitState
        {
            Frozen,     // Cannot move at all.
            Ground,     // Can move in the plane anywhere
            Threshold,  // Entering or exiting a special zone
            Stairs,     // Can move up and down.
            ClosedArea, // Restricted to an area.
        };

        public readonly int id;
        public readonly string user_name;

        private Point input_direction;
        private Vector2 position;
        public int z_level;
        public int health;

        public TransitState transit_state;

        public float move_speed = 2.5f / Constants.TICKS_PER_SEC;

        public Vector2 Position { get => position; }

        public Player(int _id, string username, Vector2 spawn_position, int _z_level)
        {
            id = _id;
            user_name = username;
            position = spawn_position;
            z_level = _z_level;
            health = 100;
            transit_state = TransitState.Frozen;
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
            var proposed_position = position + (move_direction * move_speed);

            var res = game.MovePlayer(this, proposed_position);
            if (res is Vector2 new_position)
            {
                position = new_position;
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

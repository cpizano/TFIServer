using System;
using System.Collections;
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

        // Position of the player. The Z axis is discrete and corresponds to
        // the client sorting order.
        public Vector2 position;
        public int z_level;
        public int health;

        public Quaternion rotation;
        public TransitState transit_state;

        public float move_speed = 2.5f / Constants.TICKS_PER_SEC;
        private bool[] inputs_;

        public Player(int _id, string username, Vector2 spawn_position, int _z_level)
        {
            id = _id;
            user_name = username;
            position = spawn_position;
            z_level = _z_level;
            health = 100;

            rotation = Quaternion.CreateFromYawPitchRoll(0f, 0f, 0f);
            transit_state = TransitState.Frozen;

            inputs_ = new bool[4];
        }
        public void Update(GameLogic game)
        {
            Vector2 input_direction = Vector2.Zero;
            if (inputs_[0])  // W
            {
                input_direction.Y += 1;
            }
            if (inputs_[1])  // S
            {
                input_direction.Y -= 1;
            }
            if (inputs_[2]) // A
            {
                input_direction.X += 1;
            }
            if (inputs_[3]) // D
            {
                input_direction.X -= 1;
            }

            if (input_direction == Vector2.Zero)
            {
                return;
            }

            game.MovePlayer(this, input_direction);

            inputs_[0] = inputs_[1] = inputs_[2] = inputs_[3] = false;
        }

        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            inputs_ = _inputs;
            rotation = _rotation;
        }
    }
}

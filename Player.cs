using System;
using System.Numerics;
using System.Text;

namespace TFIServer
{
    // This object methods should only be called from the gamethread.
    class Player
    {
        public readonly int id;
        public readonly string user_name;

        public Vector3 position;
        public Quaternion rotation;

        public float move_speed = 5f / Constants.TICKS_PER_SEC;
        private bool[] inputs_;

        public Player(int _id, string username, Vector3 spawn_position)
        {
            id = _id;
            user_name = username;
            position = spawn_position;
            rotation = Quaternion.CreateFromYawPitchRoll(0f, 0f, 0f);

            inputs_ = new bool[4];
        }
        public void Update(GameLogic game)
        {
            Vector2 _inputDirection = Vector2.Zero;
            if (inputs_[0])  // W
            {
                _inputDirection.Y += 1;
            }
            if (inputs_[1])  // S
            {
                _inputDirection.Y -= 1;
            }
            if (inputs_[2]) // A
            {
                _inputDirection.X += 1;
            }
            if (inputs_[3]) // D
            {
                _inputDirection.X -= 1;
            }

            if (_inputDirection == Vector2.Zero)
            {
                return;
            }

            game.MovePlayer(this, _inputDirection);

            inputs_[0] = inputs_[1] = inputs_[2] = inputs_[3] = false;
        }

        public bool Hit(Vector3 point)
        {
            var _delta = position - point;
            return _delta.LengthSquared() < Constants.HIT_RADIUS_SQR;
        }

        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            inputs_ = _inputs;
            rotation = _rotation;
        }
    }
}

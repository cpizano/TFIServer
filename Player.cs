using System;
using System.Numerics;
using System.Text;

namespace TFIServer
{
    // This object methods should only be called from the gamethread.
    class Player
    {
        public readonly int id;
        public readonly string username;

        public Vector3 position;
        public Quaternion rotation;

        public float moveSpeed = 5f / Constants.TICKS_PER_SEC;
        private bool[] inputs;

        public Player(int _id, string _username, Vector3 _spawnPosition)
        {
            id = _id;
            username = _username;
            position = _spawnPosition;
            //rotation = Quaternion.Identity
            rotation = Quaternion.CreateFromYawPitchRoll(0f, 0f, 0f);

            inputs = new bool[4];
        }
        public void Update(GameLogic game)
        {
            Vector2 _inputDirection = Vector2.Zero;
            if (inputs[0])  // W
            {
                _inputDirection.Y += 1;
            }
            if (inputs[1])  // S
            {
                _inputDirection.Y -= 1;
            }
            if (inputs[2]) // A
            {
                _inputDirection.X += 1;
            }
            if (inputs[3]) // D
            {
                _inputDirection.X -= 1;
            }

            if (_inputDirection == Vector2.Zero)
            {
                return;
            }

            game.MovePlayer(this, _inputDirection);

            inputs[0] = inputs[1] = inputs[2] = inputs[3] = false;
        }

        public bool Hit(Vector3 _point)
        {
            var _delta = position - _point;
            return _delta.LengthSquared() < Constants.HIT_RADIUS_SQR;
        }

        
        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            inputs = _inputs;
            rotation = _rotation;
        }
    }
}

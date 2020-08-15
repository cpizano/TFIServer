using System;
using System.Numerics;


namespace TFIServer
{
    // This object methods should only be called from the gamethread.
    class Player
    {
        public readonly int id;
        public readonly string username;

        public Vector3 position;
        public Quaternion rotation;

        private float moveSpeed = 5f / Constants.TICKS_PER_SEC;
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

            Move(_inputDirection, game);
        }

        private void Move(Vector2 _inputDirection, GameLogic game)
        {
            // For 3D, Z is forward (towards screen) and +Y is up.
            // Vector3 _forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            // Vector3 _right = Vector3.Normalize(Vector3.Cross(_forward, new Vector3(0, 1, 0)));

            Vector3 _forward = Vector3.UnitY;
            Vector3 _right = -Vector3.UnitX;

            Vector3 _moveDirection = (_right * _inputDirection.X) + (_forward * _inputDirection.Y);
            position = game.UpdatePosition(this, position + _moveDirection * moveSpeed);

            ServerSend.PlayerPosition(this);
            // Client is authoritative for rotation: update not sent back to self.
            ServerSend.PlayerRotation(this);
        }

        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            inputs = _inputs;
            rotation = _rotation;
        }
    }
}

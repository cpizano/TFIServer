using System.Drawing;
using System.Numerics;

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
        public readonly Point position;
        public readonly int z_level;
        public readonly int health;
        public readonly TransitState transit_state;

        public PlayerState(Point _pos, int _z_level, int _health, TransitState _state)
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

        public PlayerState(in PlayerState o, Point _pos) : this(o)
        {
            position = _pos;
        }

        public PlayerState(in PlayerState o, int _z_level, Point _pos) : this(o)
        {
            position = _pos;
            z_level = _z_level;
        }

        public PlayerState(in PlayerState o, TransitState ts, Point _pos) : this(o)
        {
            transit_state = ts;
            position = _pos;
        }
    }
}

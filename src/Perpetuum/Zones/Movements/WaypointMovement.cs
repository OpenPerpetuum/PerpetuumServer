using System;
using System.Numerics;
using Perpetuum.Units;

namespace Perpetuum.Zones.Movements
{
    public class WaypointMovement : Movement
    {
        private readonly Vector2 _target;

        public WaypointMovement(Vector2 target)
        {
            _target = target;
        }

        public Vector2 Target
        {
            get { return _target; }
        }

        public override void Start(Unit unit)
        {
            Arrived = false;
            unit.Direction = unit.CurrentPosition.DirectionTo(_target);
            unit.CurrentSpeed = 1.0;

            // Vector to destination
            var path = Vector2.Subtract(_target, unit.CurrentPosition.ToVector2());
            // The absolute path along each coordinate
            _distance = Vector2.Abs(path);
            if (path.Length().IsApproximatelyEqual(0.0f))
            {
                // If the path is zero, then is already at the destination
                Arrived = true;
                // Velocity vector in the direction of the unit.
                _velocity = MathHelper.DirectionToVector(unit.Direction);
            }
            else
            {
                // Velocity vector in the direction of the path.
                _velocity = Vector2.Normalize(path);
            }

            base.Start(unit);
        }

        private Vector2 _velocity;
        private Vector2 _distance;

        public override void Update(Unit unit, TimeSpan elapsed)
        {
            if ( Arrived )
                return;
            
            var d = (float) (unit.Speed * elapsed.TotalSeconds);
            var v = Vector2.Multiply(_velocity,d);

            _distance -= Vector2.Abs(v);

            if (_distance.X <= 0.0f && _distance.Y <= 0.0f)
            {
                Arrived = true;
                unit.CurrentPosition = _target;
            }
            else
            {
                unit.CurrentPosition += v;
            }

//            unit.Zone.CreateAlignedDebugBeam(BeamType.orange_20sec, unit.CurrentPosition);
        }

        public bool Arrived { get; private set; }
    }
}
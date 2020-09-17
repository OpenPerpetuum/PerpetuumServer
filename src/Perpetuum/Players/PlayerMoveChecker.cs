using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.PathFinders;
using Perpetuum.Zones;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Players
{
    public class PosPair
    {
        public Position Start { private set; get; }
        public Position End { private set; get; }
        public PosPair(Position start, Position end)
        {
            Start = start;
            End = end;
        }
    }

    public class PlayerMoveCheckQueue
    {
        private Task _task;
        private CancellationTokenSource _tokenSrc;
        private CancellationToken _ct;

        private Player _player;
        private Checker checker;

        ConcurrentQueue<PosPair> q;
        public PlayerMoveCheckQueue(Player player)
        {
            Console.WriteLine("NEW PlayerMoveCheckQueue");
            _player = player;
            checker = new Checker(player);
            _tokenSrc = new CancellationTokenSource();
            q = new ConcurrentQueue<PosPair>();
            _ct = _tokenSrc.Token;
            _task = new Task(() => ThreadLoop(),
                    TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
            _task.ContinueWith((t) => Console.WriteLine("=======Task ended!============"));
        }

        public void Start()
        {
            Console.WriteLine("START PlayerMoveCheckQueue");
            if(_task.Status == TaskStatus.Created)
                _task.Start();
        }

        public void Stop()
        {
            Console.WriteLine("STOP PlayerMoveCheckQueue");
            _tokenSrc.Cancel();
        }

        public void EnqueueMove(Position prev, Position target)
        {
            q.Enqueue(new PosPair(prev, target));
        }

        private void ThreadLoop()
        {
            while (!_ct.IsCancellationRequested)
            {
                try
                {
                    while (q.IsEmpty)
                    {
                        Thread.Sleep(50);
                    }
                    while (!q.IsEmpty)
                    {
                        q.TryDequeue(out PosPair pair);
                        var moveGood = checker.IsUpdateValid(pair.Start, pair.End);
                        if (!moveGood)
                        {
                            q.Clear();
                            _player.CurrentPosition = pair.Start;
                            _player.SetLastValidPosition(pair.Start);
                            _player.SendForceUpdate();
                            break;
                        }
                        _player.SetLastValidPosition(pair.End);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }
    }




    public class Checker
    {
        private readonly Player _player;
        private readonly AStarLimited _aStar;
        private const int MAX_DIST = 10;

        public Checker(Player player)
        {
            _player = player;
            _aStar = new AStarLimited(Heuristic.Manhattan, _player.IsWalkable, MAX_DIST);
            _aStar.RegisterDebugHandler((node, type) =>
            {
                if (type == PathFinderNodeType.Neighbour)
                {
                    _player.Zone.CreateAlignedDebugBeam(BeamType.blue_5sec, node.Location.ToPosition());
                }
                else if (type == PathFinderNodeType.Current)
                {
                    _player.Zone.CreateAlignedDebugBeam(BeamType.orange_5sec, node.Location.ToPosition());
                }
                else if (type == PathFinderNodeType.Path)
                {
                    _player.Zone.CreateAlignedDebugBeam(BeamType.green_5sec, node.Location.ToPosition());
                }
            });
        }

        public bool IsUpdateValid(Position prev, Position pos)
        {
            var dx = Math.Abs(prev.intX - pos.intX);
            var dy = Math.Abs(prev.intY - pos.intY);
            if (dx < 2 && dy < 2)
            {
                return true;
            }
            else if (dx > MAX_DIST || dy > MAX_DIST)
            {
                return false;
            }
            else if (_player.Zone.CheckLinearPath(prev, pos, _player.Slope))
            {
                return true;
            }
            else if (_aStar.HasPath(prev.ToPoint(), pos.ToPoint()))
            {
                return true;
            }
            return false;
        }
    }






    public class PlayerMoveChecker
    {
        private Position _prev;
        private readonly object _lock = new object();
        public Position GetPrev()
        {
            lock (_lock)
                return _prev;
        }
        public void SetPrev(Position prev)
        {
            lock (_lock)
                _prev = prev;
        }

        private readonly Player _player;
        private readonly AStarLimited _aStar;
        private const int MAX_DIST = 10;

        public PlayerMoveChecker(Player player)
        {
            _player = player;
            _aStar = new AStarLimited(Heuristic.Manhattan, _player.IsWalkable, MAX_DIST);
        }

        public bool IsUpdateValid(Position pos)
        {
            var prev = GetPrev();
            var dx = Math.Abs(prev.intX - pos.intX);
            var dy = Math.Abs(prev.intY - pos.intY);
            if (dx < 2 && dy < 2)
            {
                return true;
            }
            else if (dx > MAX_DIST || dy > MAX_DIST)
            {
                return false;
            }
            else if (_player.Zone.CheckLinearPath(prev, pos, _player.Slope))
            {
                return true;
            }
            else if (_aStar.HasPath(prev.ToPoint(), pos.ToPoint()))
            {
                return true;
            }
            return false;
        }
    }
}

using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.PathFinders;
using Perpetuum.Zones;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Perpetuum.Players
{
    public class PlayerMoveCheckQueue
    {
        private readonly Task _task;
        private readonly CancellationTokenSource _tokenSrc;
        private CancellationToken _ct;

        private readonly Player _player;
        private readonly PlayerMoveChecker _moveChecker;
        private readonly ConcurrentQueue<Position> _movesToReview;

        private Position Prev { get; set; }

        public PlayerMoveCheckQueue(Player player, Position start)
        {
            Prev = start;
            _player = player;
            _moveChecker = new PlayerMoveChecker(player);
            _tokenSrc = new CancellationTokenSource();
            _movesToReview = new ConcurrentQueue<Position>();
            _ct = _tokenSrc.Token;
            _task = new Task(() => ProcessQueue(),
                    TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
            Start();
        }

        private void Start()
        {
            if (_task.Status == TaskStatus.Created)
                _task.Start();
        }

        public void Stop()
        {
            _tokenSrc.Cancel();
        }

        public void EnqueueMove(Position target)
        {
            _movesToReview.Enqueue(target);
        }

        private bool IsCanceled()
        {
            return _ct.IsCancellationRequested;
        }

        private void ProcessQueue()
        {
            while (!IsCanceled())
            {
                try
                {
                    while (_movesToReview.IsEmpty)
                    {
                        Thread.Sleep(50);
                        if (IsCanceled())
                            return;
                    }
                    while (!_movesToReview.IsEmpty)
                    {
                        if(_movesToReview.TryDequeue(out Position pos))
                        {
                            if (!_moveChecker.IsUpdateValid(Prev, pos))
                            {
                                _movesToReview.Clear();
                                _player.CurrentPosition = Prev;
                                _player.SendForceUpdate();
                                break;
                            }
                            Prev = pos;
                        }

                        if (IsCanceled())
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }
    }

    public class PlayerMoveChecker
    {
        private readonly Player _player;
        private readonly AStarLimited _aStar;
        private const int MAX_DIST = 10;

        public PlayerMoveChecker(Player player)
        {
            _player = player;
            _aStar = new AStarLimited(Heuristic.Manhattan, _player.IsWalkable, MAX_DIST);
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
}

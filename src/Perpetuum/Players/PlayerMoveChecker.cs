using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Log;
using Perpetuum.PathFinders;
using Perpetuum.Threading;
using Perpetuum.Zones;

namespace Perpetuum.Players
{
    public class PlayerMoveCheckQueue : Disposable
    {
        private readonly Task _task;
        private readonly CancellationTokenSource _tokenSrc;
        private CancellationToken _ct;

        private readonly Player _player;
        private readonly PlayerMoveChecker _moveChecker;
        private readonly BlockingCollection<Position> _movesToReview;

        private Position Prev { get; set; }
        private bool IsCompleted { get { return _movesToReview?.IsAddingCompleted ?? true; } }
        private bool IsCanceled { get { return _ct.IsCancellationRequested; } }
        private bool IsStopped { get { return IsCanceled || IsCompleted; } }

        public PlayerMoveCheckQueue(Player player, Position start)
        {
            Prev = start;
            _player = player;
            _moveChecker = new PlayerMoveChecker(player);
            _tokenSrc = new CancellationTokenSource();
            _movesToReview = new BlockingCollection<Position>();
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

        public void StopAndDispose()
        {
            Stop();
            Dispose();
        }

        private void Stop()
        {
            if (!IsCanceled)
                _tokenSrc.Cancel();

            if (!IsCompleted)
                _movesToReview?.CompleteAdding();
        }

        public void EnqueueMove(Position target)
        {
            try
            {
                if (IsCompleted)
                    return;

                _movesToReview?.Add(target, _ct);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException || ex is ObjectDisposedException)
                {
                    return;
                }
                Logger.Exception(ex);
            }
        }

        private void ProcessQueue()
        {
            while (!IsStopped)
            {
                try
                {
                    var position = _movesToReview?.Take(_ct); //This still gets null ref exceptions...
                    if (position is Position pos)
                    {
                        if (_moveChecker.IsUpdateValid(Prev, pos))
                        {
                            Prev = pos;
                        }
                        else
                        {
                            _movesToReview?.Clear();
                            _player.CurrentPosition = Prev;
                            _player.SendForceUpdate();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException || ex is ObjectDisposedException)
                    {
                        return;
                    }
                    Logger.Exception(ex);
                }
            }
        }

        #region DISPOSAL
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            try
            {
                if (!IsStopped)
                {
                    Stop();
                }
                _movesToReview?.Dispose();
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }
        #endregion
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

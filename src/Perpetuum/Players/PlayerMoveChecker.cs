using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Log;
using Perpetuum.PathFinders;
using Perpetuum.Zones;

namespace Perpetuum.Players
{
    public class PlayerMoveCheckQueue : IDisposable
    {
        private const int TIMEOUT_MS = 1000;
        private readonly Task _task;
        private readonly CancellationTokenSource _tokenSrc;
        private CancellationToken _ct;

        private readonly Player _player;
        private readonly PlayerMoveChecker _moveChecker;
        private readonly BlockingCollection<Position> _movesToReview;

        private Position Prev { get; set; }

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

        public void Stop()
        {
            if (!_tokenSrc.IsCancellationRequested)
                _tokenSrc.Cancel();

            if (!_movesToReview.IsAddingCompleted)
                _movesToReview.CompleteAdding();
        }

        public void EnqueueMove(Position target)
        {
            try
            {
                if (_movesToReview.IsAddingCompleted)
                    return;

                _movesToReview?.TryAdd(target, TIMEOUT_MS, _ct);
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
                    if (_movesToReview != null && _movesToReview.TryTake(out Position pos, TIMEOUT_MS, _ct))
                    {
                        if(_moveChecker.IsUpdateValid(Prev, pos))
                        {
                            Prev = pos;
                        }
                        else
                        {
                            _movesToReview.Clear();
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
        private bool _disposed = false;

        ~PlayerMoveCheckQueue() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    _movesToReview?.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Exception(e);
                }
                finally
                {
                    _disposed = true;
                }
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

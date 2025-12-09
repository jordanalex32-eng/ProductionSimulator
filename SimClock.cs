using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductionSimulator
{
    public static class SimClock_Production
    {
        private static readonly object _lock = new();
        private static long _displayedSimMs, _displayedTrueMs;
        private static long _remainingSimMs, _remainingTrueMs;

        private static System.Threading.Timer? _timer;
        private static readonly Stopwatch _sw = Stopwatch.StartNew();
        private static long _lastTick;

        private static Control? _ui;
        private static TextBox? _tbSim, _tbTrue;

        public static void Initialize(Control uiThreadControl, TextBox tbSimElapsed, TextBox tbTrueElapsed)
        {
            _ui = uiThreadControl ?? throw new ArgumentNullException(nameof(uiThreadControl));
            _tbSim = tbSimElapsed ?? throw new ArgumentNullException(nameof(tbSimElapsed));
            _tbTrue = tbTrueElapsed ?? throw new ArgumentNullException(nameof(tbTrueElapsed));

            _lastTick = _sw.ElapsedMilliseconds;
            _timer?.Dispose();
            _timer = new System.Threading.Timer(Tick, null, 0, 33); // ~30 FPS
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _displayedSimMs = _displayedTrueMs = 0;
                _remainingSimMs = _remainingTrueMs = 0;
            }
            TryUpdateUi();
        }

        /// <summary>
        /// Queue a new segment to animate. simMs is the actual Task.Delay ms (scaled).
        /// trueMs is the raw/unscaled ms for the same segment.
        /// </summary>
        public static void BeginSegment(int simMs, int trueMs)
        {
            if (simMs <= 0 && trueMs <= 0)
            {
                AddInstant(simMs, trueMs);
                return;
            }

            lock (_lock)
            {
                _remainingSimMs += simMs;
                _remainingTrueMs += trueMs;
            }
        }

        public static void AddInstant(int simMs, int trueMs)
        {
            lock (_lock)
            {
                _displayedSimMs += simMs;
                _displayedTrueMs += trueMs;
            }
            TryUpdateUi();
        }

        private static void Tick(object? _)
        {
            long now = _sw.ElapsedMilliseconds;
            long delta = now - Interlocked.Exchange(ref _lastTick, now);
            if (delta <= 0) return;

            long takeSim, takeTrue;

            lock (_lock)
            {
                if (_remainingSimMs <= 0 && _remainingTrueMs <= 0) return;

                // Consume a slice of the pending simulated time based on wall time
                takeSim = Math.Min(delta, _remainingSimMs);

                // Consume true time proportionally so totals end exactly right
                takeTrue = _remainingSimMs > 0
                    ? Math.Min(_remainingTrueMs, (long)Math.Round((double)_remainingTrueMs * takeSim / _remainingSimMs))
                    : _remainingTrueMs;

                _remainingSimMs -= takeSim;
                _remainingTrueMs -= takeTrue;
                _displayedSimMs += takeSim;
                _displayedTrueMs += takeTrue;
            }

            TryUpdateUi();
        }

        private static void TryUpdateUi()
        {
            var ui = _ui; var tbSim = _tbSim; var tbTrue = _tbTrue;
            if (ui == null || tbSim == null || tbTrue == null) return;

            var sim = _displayedSimMs;
            var tru = _displayedTrueMs;

            try
            {
                ui.BeginInvoke((Action)(() =>
                {
                    tbSim.Text = Format(sim);
                    tbTrue.Text = Format(tru);
                }));
            }
            catch { /* form closing, ignore */ }
        }

        private static string Format(long ms)
        {
            if (ms < 0) ms = 0;
            var ts = TimeSpan.FromMilliseconds(ms);
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }
    }
    public static class SimClock_Tester1
    {
        private static readonly object _lock = new();
        private static long _displayedSimMs, _displayedTrueMs;
        private static long _remainingSimMs, _remainingTrueMs;

        private static System.Threading.Timer? _timer;
        private static readonly Stopwatch _sw = Stopwatch.StartNew();
        private static long _lastTick;

        private static Control? _ui;
        private static TextBox? _tbSim, _tbTrue;

        public static void Initialize(Control uiThreadControl, TextBox tbSimElapsed, TextBox tbTrueElapsed)
        {
            _ui = uiThreadControl ?? throw new ArgumentNullException(nameof(uiThreadControl));
            _tbSim = tbSimElapsed ?? throw new ArgumentNullException(nameof(tbSimElapsed));
            _tbTrue = tbTrueElapsed ?? throw new ArgumentNullException(nameof(tbTrueElapsed));

            _lastTick = _sw.ElapsedMilliseconds;
            _timer?.Dispose();
            _timer = new System.Threading.Timer(Tick, null, 0, 33); // ~30 FPS
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _displayedSimMs = _displayedTrueMs = 0;
                _remainingSimMs = _remainingTrueMs = 0;
            }
            TryUpdateUi();
        }

        /// <summary>
        /// Queue a new segment to animate. simMs is the actual Task.Delay ms (scaled).
        /// trueMs is the raw/unscaled ms for the same segment.
        /// </summary>
        public static void BeginSegment(int simMs, int trueMs)
        {
            if (simMs <= 0 && trueMs <= 0)
            {
                AddInstant(simMs, trueMs);
                return;
            }

            lock (_lock)
            {
                _remainingSimMs += simMs;
                _remainingTrueMs += trueMs;
            }
        }

        public static void AddInstant(int simMs, int trueMs)
        {
            lock (_lock)
            {
                _displayedSimMs += simMs;
                _displayedTrueMs += trueMs;
            }
            TryUpdateUi();
        }

        private static void Tick(object? _)
        {
            long now = _sw.ElapsedMilliseconds;
            long delta = now - Interlocked.Exchange(ref _lastTick, now);
            if (delta <= 0) return;

            long takeSim, takeTrue;

            lock (_lock)
            {
                if (_remainingSimMs <= 0 && _remainingTrueMs <= 0) return;

                // Consume a slice of the pending simulated time based on wall time
                takeSim = Math.Min(delta, _remainingSimMs);

                // Consume true time proportionally so totals end exactly right
                takeTrue = _remainingSimMs > 0
                    ? Math.Min(_remainingTrueMs, (long)Math.Round((double)_remainingTrueMs * takeSim / _remainingSimMs))
                    : _remainingTrueMs;

                _remainingSimMs -= takeSim;
                _remainingTrueMs -= takeTrue;
                _displayedSimMs += takeSim;
                _displayedTrueMs += takeTrue;
            }

            TryUpdateUi();
        }

        private static void TryUpdateUi()
        {
            var ui = _ui; var tbSim = _tbSim; var tbTrue = _tbTrue;
            if (ui == null || tbSim == null || tbTrue == null) return;

            var sim = _displayedSimMs;
            var tru = _displayedTrueMs;

            try
            {
                ui.BeginInvoke((Action)(() =>
                {
                    tbSim.Text = Format(sim);
                    tbTrue.Text = Format(tru);
                }));
            }
            catch { /* form closing, ignore */ }
        }

        private static string Format(long ms)
        {
            if (ms < 0) ms = 0;
            var ts = TimeSpan.FromMilliseconds(ms);
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }
    }
    public static class SimClock_Tester2
    {
        private static readonly object _lock = new();
        private static long _displayedSimMs, _displayedTrueMs;
        private static long _remainingSimMs, _remainingTrueMs;

        private static System.Threading.Timer? _timer;
        private static readonly Stopwatch _sw = Stopwatch.StartNew();
        private static long _lastTick;

        private static Control? _ui;
        private static TextBox? _tbSim, _tbTrue;

        public static void Initialize(Control uiThreadControl, TextBox tbSimElapsed, TextBox tbTrueElapsed)
        {
            _ui = uiThreadControl ?? throw new ArgumentNullException(nameof(uiThreadControl));
            _tbSim = tbSimElapsed ?? throw new ArgumentNullException(nameof(tbSimElapsed));
            _tbTrue = tbTrueElapsed ?? throw new ArgumentNullException(nameof(tbTrueElapsed));

            _lastTick = _sw.ElapsedMilliseconds;
            _timer?.Dispose();
            _timer = new System.Threading.Timer(Tick, null, 0, 33); // ~30 FPS
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _displayedSimMs = _displayedTrueMs = 0;
                _remainingSimMs = _remainingTrueMs = 0;
            }
            TryUpdateUi();
        }

        /// <summary>
        /// Queue a new segment to animate. simMs is the actual Task.Delay ms (scaled).
        /// trueMs is the raw/unscaled ms for the same segment.
        /// </summary>
        public static void BeginSegment(int simMs, int trueMs)
        {
            if (simMs <= 0 && trueMs <= 0)
            {
                AddInstant(simMs, trueMs);
                return;
            }

            lock (_lock)
            {
                _remainingSimMs += simMs;
                _remainingTrueMs += trueMs;
            }
        }

        public static void AddInstant(int simMs, int trueMs)
        {
            lock (_lock)
            {
                _displayedSimMs += simMs;
                _displayedTrueMs += trueMs;
            }
            TryUpdateUi();
        }

        private static void Tick(object? _)
        {
            long now = _sw.ElapsedMilliseconds;
            long delta = now - Interlocked.Exchange(ref _lastTick, now);
            if (delta <= 0) return;

            long takeSim, takeTrue;

            lock (_lock)
            {
                if (_remainingSimMs <= 0 && _remainingTrueMs <= 0) return;

                // Consume a slice of the pending simulated time based on wall time
                takeSim = Math.Min(delta, _remainingSimMs);

                // Consume true time proportionally so totals end exactly right
                takeTrue = _remainingSimMs > 0
                    ? Math.Min(_remainingTrueMs, (long)Math.Round((double)_remainingTrueMs * takeSim / _remainingSimMs))
                    : _remainingTrueMs;

                _remainingSimMs -= takeSim;
                _remainingTrueMs -= takeTrue;
                _displayedSimMs += takeSim;
                _displayedTrueMs += takeTrue;
            }

            TryUpdateUi();
        }

        private static void TryUpdateUi()
        {
            var ui = _ui; var tbSim = _tbSim; var tbTrue = _tbTrue;
            if (ui == null || tbSim == null || tbTrue == null) return;

            var sim = _displayedSimMs;
            var tru = _displayedTrueMs;

            try
            {
                ui.BeginInvoke((Action)(() =>
                {
                    tbSim.Text = Format(sim);
                    tbTrue.Text = Format(tru);
                }));
            }
            catch { /* form closing, ignore */ }
        }

        private static string Format(long ms)
        {
            if (ms < 0) ms = 0;
            var ts = TimeSpan.FromMilliseconds(ms);
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }
    }
    public static class SimClock_Tester3
    {
        private static readonly object _lock = new();
        private static long _displayedSimMs, _displayedTrueMs;
        private static long _remainingSimMs, _remainingTrueMs;

        private static System.Threading.Timer? _timer;
        private static readonly Stopwatch _sw = Stopwatch.StartNew();
        private static long _lastTick;

        private static Control? _ui;
        private static TextBox? _tbSim, _tbTrue;

        public static void Initialize(Control uiThreadControl, TextBox tbSimElapsed, TextBox tbTrueElapsed)
        {
            _ui = uiThreadControl ?? throw new ArgumentNullException(nameof(uiThreadControl));
            _tbSim = tbSimElapsed ?? throw new ArgumentNullException(nameof(tbSimElapsed));
            _tbTrue = tbTrueElapsed ?? throw new ArgumentNullException(nameof(tbTrueElapsed));

            _lastTick = _sw.ElapsedMilliseconds;
            _timer?.Dispose();
            _timer = new System.Threading.Timer(Tick, null, 0, 33); // ~30 FPS
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _displayedSimMs = _displayedTrueMs = 0;
                _remainingSimMs = _remainingTrueMs = 0;
            }
            TryUpdateUi();
        }

        /// <summary>
        /// Queue a new segment to animate. simMs is the actual Task.Delay ms (scaled).
        /// trueMs is the raw/unscaled ms for the same segment.
        /// </summary>
        public static void BeginSegment(int simMs, int trueMs)
        {
            if (simMs <= 0 && trueMs <= 0)
            {
                AddInstant(simMs, trueMs);
                return;
            }

            lock (_lock)
            {
                _remainingSimMs += simMs;
                _remainingTrueMs += trueMs;
            }
        }

        public static void AddInstant(int simMs, int trueMs)
        {
            lock (_lock)
            {
                _displayedSimMs += simMs;
                _displayedTrueMs += trueMs;
            }
            TryUpdateUi();
        }

        private static void Tick(object? _)
        {
            long now = _sw.ElapsedMilliseconds;
            long delta = now - Interlocked.Exchange(ref _lastTick, now);
            if (delta <= 0) return;

            long takeSim, takeTrue;

            lock (_lock)
            {
                if (_remainingSimMs <= 0 && _remainingTrueMs <= 0) return;

                // Consume a slice of the pending simulated time based on wall time
                takeSim = Math.Min(delta, _remainingSimMs);

                // Consume true time proportionally so totals end exactly right
                takeTrue = _remainingSimMs > 0
                    ? Math.Min(_remainingTrueMs, (long)Math.Round((double)_remainingTrueMs * takeSim / _remainingSimMs))
                    : _remainingTrueMs;

                _remainingSimMs -= takeSim;
                _remainingTrueMs -= takeTrue;
                _displayedSimMs += takeSim;
                _displayedTrueMs += takeTrue;
            }

            TryUpdateUi();
        }

        private static void TryUpdateUi()
        {
            var ui = _ui; var tbSim = _tbSim; var tbTrue = _tbTrue;
            if (ui == null || tbSim == null || tbTrue == null) return;

            var sim = _displayedSimMs;
            var tru = _displayedTrueMs;

            try
            {
                ui.BeginInvoke((Action)(() =>
                {
                    tbSim.Text = Format(sim);
                    tbTrue.Text = Format(tru);
                }));
            }
            catch { /* form closing, ignore */ }
        }

        private static string Format(long ms)
        {
            if (ms < 0) ms = 0;
            var ts = TimeSpan.FromMilliseconds(ms);
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }
    }
    public static class SimClock_LoadStationWait
    {
        private static readonly object _lock = new();
        private static long _displayedSimMs, _displayedTrueMs;
        private static long _remainingSimMs, _remainingTrueMs;

        private static System.Threading.Timer? _timer;
        private static readonly Stopwatch _sw = Stopwatch.StartNew();
        private static long _lastTick;

        private static Control? _ui;
        private static TextBox? _tbSim, _tbTrue;

        public static void Initialize(Control uiThreadControl, TextBox tbSimElapsed, TextBox tbTrueElapsed)
        {
            _ui = uiThreadControl ?? throw new ArgumentNullException(nameof(uiThreadControl));
            _tbSim = tbSimElapsed ?? throw new ArgumentNullException(nameof(tbSimElapsed));
            _tbTrue = tbTrueElapsed ?? throw new ArgumentNullException(nameof(tbTrueElapsed));

            _lastTick = _sw.ElapsedMilliseconds;
            _timer?.Dispose();
            _timer = new System.Threading.Timer(Tick, null, 0, 33); // ~30 FPS
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _displayedSimMs = _displayedTrueMs = 0;
                _remainingSimMs = _remainingTrueMs = 0;
            }
            TryUpdateUi();
        }

        /// <summary>
        /// Queue a new segment to animate. simMs is the actual Task.Delay ms (scaled).
        /// trueMs is the raw/unscaled ms for the same segment.
        /// </summary>
        public static void BeginSegment(int simMs, int trueMs)
        {
            if (simMs <= 0 && trueMs <= 0)
            {
                AddInstant(simMs, trueMs);
                return;
            }

            lock (_lock)
            {
                _remainingSimMs += simMs;
                _remainingTrueMs += trueMs;
            }
        }

        public static void AddInstant(int simMs, int trueMs)
        {
            lock (_lock)
            {
                _displayedSimMs += simMs;
                _displayedTrueMs += trueMs;
            }
            TryUpdateUi();
        }

        private static void Tick(object? _)
        {
            long now = _sw.ElapsedMilliseconds;
            long delta = now - Interlocked.Exchange(ref _lastTick, now);
            if (delta <= 0) return;

            long takeSim, takeTrue;

            lock (_lock)
            {
                if (_remainingSimMs <= 0 && _remainingTrueMs <= 0) return;

                // Consume a slice of the pending simulated time based on wall time
                takeSim = Math.Min(delta, _remainingSimMs);

                // Consume true time proportionally so totals end exactly right
                takeTrue = _remainingSimMs > 0
                    ? Math.Min(_remainingTrueMs, (long)Math.Round((double)_remainingTrueMs * takeSim / _remainingSimMs))
                    : _remainingTrueMs;

                _remainingSimMs -= takeSim;
                _remainingTrueMs -= takeTrue;
                _displayedSimMs += takeSim;
                _displayedTrueMs += takeTrue;
            }

            TryUpdateUi();
        }

        private static void TryUpdateUi()
        {
            var ui = _ui; var tbSim = _tbSim; var tbTrue = _tbTrue;
            if (ui == null || tbSim == null || tbTrue == null) return;

            var sim = _displayedSimMs;
            var tru = _displayedTrueMs;

            try
            {
                ui.BeginInvoke((Action)(() =>
                {
                    tbSim.Text = Format(sim);
                    tbTrue.Text = Format(tru);
                }));
            }
            catch { /* form closing, ignore */ }
        }

        private static string Format(long ms)
        {
            if (ms < 0) ms = 0;
            var ts = TimeSpan.FromMilliseconds(ms);
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }
    }
    public static class SimClock_TotalProduction
    {
        private static readonly object _lock = new();
        private static long _displayedSimMs, _displayedTrueMs;
        private static long _remainingSimMs, _remainingTrueMs;

        private static System.Threading.Timer? _timer;
        private static readonly Stopwatch _sw = Stopwatch.StartNew();
        private static long _lastTick;

        private static Control? _ui;
        private static TextBox? _tbSim, _tbTrue;

        public static void Initialize(Control uiThreadControl, TextBox tbSimElapsed, TextBox tbTrueElapsed)
        {
            _ui = uiThreadControl ?? throw new ArgumentNullException(nameof(uiThreadControl));
            _tbSim = tbSimElapsed ?? throw new ArgumentNullException(nameof(tbSimElapsed));
            _tbTrue = tbTrueElapsed ?? throw new ArgumentNullException(nameof(tbTrueElapsed));

            _lastTick = _sw.ElapsedMilliseconds;
            _timer?.Dispose();
            _timer = new System.Threading.Timer(Tick, null, 0, 33); // ~30 FPS
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _displayedSimMs = _displayedTrueMs = 0;
                _remainingSimMs = _remainingTrueMs = 0;
            }
            TryUpdateUi();
        }

        /// <summary>
        /// Queue a new segment to animate. simMs is the actual Task.Delay ms (scaled).
        /// trueMs is the raw/unscaled ms for the same segment.
        /// </summary>
        public static void BeginSegment(int simMs, int trueMs)
        {
            if (simMs <= 0 && trueMs <= 0)
            {
                AddInstant(simMs, trueMs);
                return;
            }

            lock (_lock)
            {
                _remainingSimMs += simMs;
                _remainingTrueMs += trueMs;
            }
        }

        public static void AddInstant(int simMs, int trueMs)
        {
            lock (_lock)
            {
                _displayedSimMs += simMs;
                _displayedTrueMs += trueMs;
            }
            TryUpdateUi();
        }

        private static void Tick(object? _)
        {
            long now = _sw.ElapsedMilliseconds;
            long delta = now - Interlocked.Exchange(ref _lastTick, now);
            if (delta <= 0) return;

            long takeSim, takeTrue;

            lock (_lock)
            {
                if (_remainingSimMs <= 0 && _remainingTrueMs <= 0) return;

                // Consume a slice of the pending simulated time based on wall time
                takeSim = Math.Min(delta, _remainingSimMs);

                // Consume true time proportionally so totals end exactly right
                takeTrue = _remainingSimMs > 0
                    ? Math.Min(_remainingTrueMs, (long)Math.Round((double)_remainingTrueMs * takeSim / _remainingSimMs))
                    : _remainingTrueMs;

                _remainingSimMs -= takeSim;
                _remainingTrueMs -= takeTrue;
                _displayedSimMs += takeSim;
                _displayedTrueMs += takeTrue;
            }

            TryUpdateUi();
        }

        private static void TryUpdateUi()
        {
            var ui = _ui; var tbSim = _tbSim; var tbTrue = _tbTrue;
            if (ui == null || tbSim == null || tbTrue == null) return;

            var sim = _displayedSimMs;
            var tru = _displayedTrueMs;

            try
            {
                ui.BeginInvoke((Action)(() =>
                {
                    tbSim.Text = Format(sim);
                    tbTrue.Text = Format(tru);
                }));
            }
            catch { /* form closing, ignore */ }
        }

        private static string Format(long ms)
        {
            if (ms < 0) ms = 0;
            var ts = TimeSpan.FromMilliseconds(ms);
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }
    }
}

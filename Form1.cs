using System;
using System.Diagnostics.Metrics;

namespace ProductionSimulator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var outputBox = tBox_Seats;  // e.g. a TextBox you added
            var okBox = tBox_OKSeats;      // e.g. another TextBox

            EvenFailureGate.OutputCountChanged += n =>
            {
                try
                {
                    if (outputBox.IsHandleCreated)
                        outputBox.BeginInvoke((Action)(() => outputBox.Text = n.ToString()));
                }
                catch { /* swallow if form is closing */ }
            };

            EvenFailureGate.OkCountChanged += n =>
            {
                try
                {
                    if (okBox.IsHandleCreated)
                        okBox.BeginInvoke((Action)(() => okBox.Text = n.ToString()));
                }
                catch { /* swallow if form is closing */ }
            };
        }
        //Properties
        private Dictionary<string, int> TimeCodeMaps = new Dictionary<string, int>()
        {
            {"NN",0 }, //Pallet Index with no Stop
            {"NP",0 }, //Pallet Index with A Stopper
            {"TT",0 }, //Transfer Conveyor Pallet Index
            {"B1",0 }, //BreakIn Station 1 Index Time
            {"B2",0 }, //BreakIn Station 2 Index Time
            {"B3",0 }, //BreakIn Station 3 Index Time
            {"T1",0 }, //Test Station 1 Index Time
            {"T2",0 }, //Test Station 2 Index Time
            {"T3",0 }, //Test Station 3 Index Time
            {"UL",0 }, //Unload station Time code dictionary map
            {"LD",0 }  //load station Time code dictionary map
        };


        public static class EvenFailureGate
        {
            private const int N = 100;        // denominator (percent)
            private static long s_counter;     // shared test counter (0,1,2,...)
            private static long b_counter;     // shared test counter (0,1,2,...)
            private static long o_counter;     // shared test counter (0,1,2,...)
            private static long ok_counter;
            public static (long countBefore, bool shouldFail) FlagAndEvaluateTester(int passPercent)
            {
                long countBefore = Interlocked.Increment(ref s_counter) - 1; // capture BEFORE this test
                bool shouldFail = ShouldFail(passPercent, countBefore);
                return (countBefore, shouldFail);
            }
            public static (long countBefore, bool shouldFail) FlagAndEvaluateBreakin(int passPercent)
            {
                long countBefore = Interlocked.Increment(ref b_counter) - 1; // capture BEFORE this test
                bool shouldFail = ShouldFail(passPercent, countBefore);
                return (countBefore, shouldFail);
            }
            public static (long countBefore, bool shouldFail) FlagAndEvaluateOutput(int passPercent)
            {
                long countBefore = Interlocked.Increment(ref o_counter) - 1; // capture BEFORE this test
                OutputCountChanged?.Invoke(Interlocked.Read(ref o_counter)); // notify subscribers
                bool shouldFail = ShouldFail(passPercent, countBefore);
                return (countBefore, shouldFail);
            }
            public static long IncrementOk()
            {
                long after = Interlocked.Increment(ref ok_counter); // capture BEFORE this test
                OkCountChanged?.Invoke(after); // notify subscribers
                return after;
            }
            public static bool ShouldFail(int passPercent, long countBefore)
            {
                // Bounds
                if (passPercent >= 100) return false; // never fail
                if (passPercent <= 0) return true;  // always fail

                int F = N - passPercent;              // fails per 100
                long k = countBefore % N;             // 0..99
                                                      // Emit when quota crosses an integer boundary:
                return ((k + 1) * F) / N > (k * F) / N;
            }

            public static void ResetTester() => Interlocked.Exchange(ref s_counter, 0);
            public static long ReadTesterCount() => Interlocked.Read(ref s_counter);
            public static void ResetBreakin() => Interlocked.Exchange(ref b_counter, 0);
            public static long ReadBreakInCount() => Interlocked.Read(ref b_counter);
            public static void ResetOutput() => Interlocked.Exchange(ref o_counter, 0);
            public static long ReadOutputCount() => Interlocked.Read(ref o_counter);
            public static void ResetOk() => Interlocked.Exchange(ref ok_counter, 0);
            public static long ReadOk() => Interlocked.Read(ref ok_counter);

            public static event Action<long>? OutputCountChanged;
            public static event Action<long>? OkCountChanged;
        }

        private volatile  bool OnBreak = false;
        private long Output;
        private long ActualRan;

        private CancellationTokenSource _cts;
        private bool StopCommand = true;
        private bool CommandStopped = true;
        private int FinalPalletReady = 0;

        private List<Task> thList = new List<Task>();
        private Task th;
        private Task Test1Th, Test2Th, Test3Th, BreakIn1Th, BreakIn2Th, BreakIn3Th;

        private int _currentSpeed;
        private int AllPalletsAdded = 0;
        private int MaxPalletsAdded = 0;
        private Color DefaultColorBreakin = Color.Yellow;
        private Color DefaultColorTester = Color.Green;
        private Color DefaultColorLoading = Color.Orange;
        private Color DefaultColorOthers = Color.White;

        private SemaphoreSlim MovingPallet;

        public string[] Errors;
        //Methods 
        private List<(string xy_coord, string name)> GetControlCoords()
        {
            var pallet_coords = new List<(string xy_coord, string name)>();
            foreach (Control c in gBox_SimulationVisual.Controls)
            {
                if (c.Name.Length == 17)
                {
                    if (c.Name.Where(Underscore => Underscore == '_').Count() == 5)
                    {
                        string name = c.Name;
                        string coord = name.Substring(6, 5); //Get the integer coordinates for the control name
                        pallet_coords.Add((coord, name));

                    }


                }

            }
            return pallet_coords.OrderBy(o => o.xy_coord).ToList();
        }
        private void SetText(Control control, string text)
        {
            if (control.InvokeRequired)
            {
                // Use an Action delegate to pass the operation to the UI thread
                control.Invoke((Action)(() => control.Text = text));
            }
            else
            {
                control.Text = text;
            }
        }
        private string GetText(Control control)
        {
            if (control.InvokeRequired)
            {
                // Use a Func delegate to pass the operation to the UI thread and return a value
                return (string)control.Invoke((Func<string>)(() => control.Text));
            }
            else
            {
                return control.Text;
            }
        }
        private void MyMultiThreadedMethod(string startAddress)
        {
            // Find the control using your existing logic
            Control foundControl = gBox_SimulationVisual.Controls.Find(startAddress, true)!.FirstOrDefault()!;

            if (foundControl is TextBox textBox)
            {
                // Use the thread-safe GetText method
                string textFromControl = GetText(textBox);

                if (textFromControl != "x")
                {
                    // Do other work here with the retrieved text
                    // You can also use SetText to update the control safely
                    SetText(textBox, "New Value");
                }
            }
        }
        //Main Pallet Move Method
        private async Task ProductionTotalClock(ThreadData data, CancellationToken ct)
        {
            await Task.Run(async () =>
            {
                while (!StopCommand && !ct.IsCancellationRequested)
                {

                    try { SimClock_TotalProduction.BeginSegment( 100, _currentSpeed * 100); } catch { }
                    try { await Task.Delay(100, ct).ConfigureAwait(false); } catch { }
                }
            });
        }
        private async Task PalletMove(ThreadData data, CancellationToken ct)
        {
            // The starting address should be managed by the Pallet object or passed in.
            string currentAddress = "LL_LD_14_10_UU_UU";
            Color prevColor = Color.White;

            await Task.Run(async () =>
            {
                while (!StopCommand && !ct.IsCancellationRequested)
                {
                    // Pause gate at top of the iteration
                    if (currentAddress == "LL_LD_14_10_UU_UU")
                        await WaitForResumeAsync(() => System.Threading.Volatile.Read(ref OnBreak), ct).ConfigureAwait(false);

                    gBox_SimulationVisual.Invoke((Action)(() =>
                    {
                        Control actionControl = gBox_SimulationVisual.Controls.Find(currentAddress, true).FirstOrDefault()!;
                        prevColor = actionControl.BackColor;
                    }));

                    // ---- INITIAL SEED of 'x' at the loader cell (pause-aware, no deadlocks) ----
                    if (currentAddress == "LL_LD_14_10_UU_UU")
                    {
                        while (!StopCommand && !ct.IsCancellationRequested)
                        {
                            // Respect pause while waiting to seed
                            await WaitForResumeAsync(() => System.Threading.Volatile.Read(ref OnBreak), ct).ConfigureAwait(false);

                            bool loadedThisTurn = false;

                            gBox_SimulationVisual.Invoke((Action)(() =>
                            {
                                Control actionControl = gBox_SimulationVisual.Controls.Find(currentAddress, true).FirstOrDefault()!;
                                if (actionControl is TextBox actionTextBox && actionTextBox.Text != "x")
                                {
                                    actionTextBox.Text = "x";
                                    AllPalletsAdded++;
                                    loadedThisTurn = true;
                                }
                            }));

                            // If we placed our 'x', proceed; also keep your original "near max" escape
                            if (loadedThisTurn || (AllPalletsAdded >= (MaxPalletsAdded - 1)))
                                break;

                            try { await Task.Delay(12, ct).ConfigureAwait(false); } catch { }
                        }
                    }

                    // First, move the pallet and get the sleep amount.
                    var action = data.pallet.Move(currentAddress, false, _currentSpeed);
                    var altAction = data.pallet.Move(currentAddress, true, _currentSpeed);


                //These are the stop watch time threads to be managed by each thread of pallet move
                    switch (action.TimeStatus)
                    {
                        //Production Time 
                        case "LD":
                
                        //Reset the Clock time before doing the next one.
                        SimClock_Production.Reset();

                        //Simulate the Clock time for a production stopwatch simulation
                        if (action.SleepAmount <= 0)
                        {
                            SimClock_Production.AddInstant(0, action.RawAmount);
                        }
                        else
                        {
                            SimClock_Production.BeginSegment(action.SleepAmount, action.RawAmount);
                        }
                            break;
                        case "T1":

                            //Reset the Clock time before doing the next one.
                            SimClock_Tester1.Reset();

                            //Simulate the Clock time for a production stopwatch simulation
                            if (action.SleepAmount <= 0)
                            {
                                SimClock_Tester1.AddInstant(0, action.RawAmount);
                            }
                            else
                            {
                                SimClock_Tester1.BeginSegment(action.SleepAmount, action.RawAmount);
                            }
                            break;
                        case "T2":

                            //Reset the Clock time before doing the next one.
                            SimClock_Tester2.Reset();

                            //Simulate the Clock time for a production stopwatch simulation
                            if (action.SleepAmount <= 0)
                            {
                                SimClock_Tester2.AddInstant(0, action.RawAmount);
                            }
                            else
                            {
                                SimClock_Tester2.BeginSegment(action.SleepAmount, action.RawAmount);
                            }
                            break;
                        case "T3":

                            //Reset the Clock time before doing the next one.
                            SimClock_Tester3.Reset();

                            //Simulate the Clock time for a production stopwatch simulation
                            if (action.SleepAmount <= 0)
                            {
                                SimClock_Tester3.AddInstant(0, action.RawAmount);
                            }
                            else
                            {
                                SimClock_Tester3.BeginSegment(action.SleepAmount, action.RawAmount);
                            }
                            break;
                    }

                // Wait for the specified time calculated by p.Move (pause-respect happens on next loop checkpoints)
                try { await Task.Delay(action.SleepAmount, ct).ConfigureAwait(false); } catch { }
                    if(action.TimeStatus == "LD")
                    {
                        data.pallet.RateData.FailedFromSRFT = false;
                    }
                    // Now, check and wait for the target text box to be free.
                    while (true && !StopCommand && !ct.IsCancellationRequested)
                    {
                        // Respect pause while waiting
                        if(currentAddress == "LL_LD_14_10_UU_UU")
                            await WaitForResumeAsync(() => System.Threading.Volatile.Read(ref OnBreak), ct).ConfigureAwait(false);

                        // UI check needs to be in an Invoke to prevent cross-thread issues.
                        bool targetIsFree = false;
                        bool altTargetIsFree = false;

                        gBox_SimulationVisual.Invoke((Action)(() =>
                        {
                            Control currentControl = gBox_SimulationVisual.Controls.Find(currentAddress, true).FirstOrDefault()!;
                            Control actionControl = gBox_SimulationVisual.Controls.Find(action.NextAddress!, true).FirstOrDefault()!;
                            Control altActionControl = gBox_SimulationVisual.Controls.Find(altAction.NextAddress!, true).FirstOrDefault()!;

                            if (actionControl is TextBox actionTextBox) targetIsFree = actionTextBox.Text != "x";
                            if (altActionControl is TextBox altActionTextBox) altTargetIsFree = altActionTextBox.Text != "x";

                            // keep your existing color feedback exactly as-is
                            if (!targetIsFree && !altTargetIsFree)
                            {
                                if (currentControl.BackColor != Color.Red)
                                    prevColor = currentControl.BackColor;
                                currentControl.BackColor = Color.Red;
                            }
                            if (targetIsFree || altTargetIsFree)
                            {
                                if (currentControl.BackColor == Color.Red)
                                    currentControl.BackColor = prevColor!;
                            }
                        }));

                        // If a path is free, break the inner waiting loop.
                        if (targetIsFree || altTargetIsFree)
                            break;



                        switch (action.TimeStatus)
                        {
                            //Production Time 
                            case "LD":
                                SimClock_Production.BeginSegment(100, _currentSpeed * 100);
                                SimClock_LoadStationWait.BeginSegment(100, _currentSpeed * 100);
                                break;
                        }
                        // If both paths are blocked, wait and check again.
                        try { await Task.Delay(100, ct).ConfigureAwait(false); } catch { }
                    }

                    // Acquire the semaphore to perform the resource update (writing the 'x')
                    try
                    {
                        // Respect pause right before trying to mutate shared/UI state
                        if (currentAddress == "LL_LD_14_10_UU_UU")
                            await WaitForResumeAsync(() => System.Threading.Volatile.Read(ref OnBreak), ct).ConfigureAwait(false);
                        await MovingPallet.WaitAsync(ct).ConfigureAwait(false);
                    }
                    catch { }

                    try
                    {
                        // Perform the UI update inside an Invoke
                        gBox_SimulationVisual.Invoke((Action)(() =>
                        {
                            Control actionControl = gBox_SimulationVisual.Controls.Find(action.NextAddress!, true).FirstOrDefault();
                            Control altActionControl = gBox_SimulationVisual.Controls.Find(altAction.NextAddress!, true).FirstOrDefault();
                            Control currentActionControl = gBox_SimulationVisual.Controls.Find(currentAddress, true).FirstOrDefault();

                            if (actionControl is TextBox actionTextBox &&
                                altActionControl is TextBox altActionTextBox &&
                                currentActionControl is TextBox currentTextBox)
                            {
                                // Re-check occupancy atomically (UI thread + semaphore held)
                                bool nextBusy = actionTextBox.Text == "x";
                                bool altBusy = altActionTextBox.Text == "x";

                                // If both targets are occupied, do nothing (prevents disappearing 'x')
                                if (nextBusy && altBusy)
                                    return;

                                // Prefer primary if free; otherwise use alternate
                                bool useAlt = nextBusy && !altBusy;
                                TextBox destTb = useAlt ? altActionTextBox : actionTextBox;
                                var chosen = useAlt ? altAction : action; // (NextAddress, TimeStatus, ...)

                                // ---- Decide fail/pass for the chosen hop (no color mutations here) ----
                                bool? shouldFailThisTest = null;
                                string timeStatus = chosen.TimeStatus; // "T1","T2","T3","B1","B2","B3","UL", etc.

                                if (!data.pallet.RateData.FailedBreakIn1 &&
                                    !data.pallet.RateData.FailedBreakIn2 &&
                                    !data.pallet.RateData.FailedBreakIn3)
                                {
                                    if (timeStatus is "T1" or "T2" or "T3")
                                    {
                                        var (countBefore, failStation) = EvenFailureGate.FlagAndEvaluateTester(data.pallet.RateData.Station_Rate_Perc);
                                        shouldFailThisTest = failStation;
                                        data.pallet.RateData.FailedStation = failStation;

                                        // Correct assignments
                                        data.pallet.RateData.FailedLV = EvenFailureGate.ShouldFail(data.pallet.RateData.LV_Rate_perc, countBefore);
                                        data.pallet.RateData.FailedHV = EvenFailureGate.ShouldFail(data.pallet.RateData.HV_Rate_perc, countBefore);
                                        data.pallet.RateData.FailedNV = EvenFailureGate.ShouldFail(data.pallet.RateData.NV_Rate_perc, countBefore);
                                    }
                                    else if (timeStatus is "B1" or "B2" or "B3")
                                    {
                                        var (countBefore, failBI) = EvenFailureGate.FlagAndEvaluateBreakin(data.pallet.RateData.BreakIn_Rate_perc);
                                        shouldFailThisTest = failBI;
                                        if (timeStatus == "B1") data.pallet.RateData.FailedBreakIn1 = failBI;
                                        if (timeStatus == "B2") data.pallet.RateData.FailedBreakIn2 = failBI;
                                        if (timeStatus == "B3") data.pallet.RateData.FailedBreakIn3 = failBI;
                                    }
                                    else if (timeStatus is "UL")
                                    {
                                        var (countBefore, failOut) = EvenFailureGate.FlagAndEvaluateOutput(data.pallet.RateData.SRFT_Rate_perc);
                                        shouldFailThisTest = failOut;
                                        if (!failOut)
                                        {
                                            EvenFailureGate.IncrementOk();
                                        }
                                        else
                                        {
                                            data.pallet.RateData.FailedFromSRFT = true;
                                        }
                                    }
                                }
                                else
                                {
                                    // Break-in skip handled once; then clear flags
                                    data.pallet.RateData.FailedBreakIn1 = false;
                                    data.pallet.RateData.FailedBreakIn2 = false;
                                    data.pallet.RateData.FailedBreakIn3 = false;
                                }

                                // ---- Atomic move: write destination first, then clear current ----
                                destTb.Text = "x";
                                currentTextBox.Text = "";

                                // Advance cursor
                                currentAddress = chosen.NextAddress!;
                            }
                            else
                            {
                                // Controls missing; do nothing to avoid losing the 'x'
                                return;
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        Errors[data.Id - 1] = ex.Message;
                    }
                    finally
                    {
                        // Release the semaphore after the UI update
                        try { MovingPallet.Release(); } catch { }
                    }
                }
            });
        }

        private static async Task WaitForResumeAsync(Func<bool> isPaused, CancellationToken ct)
        {
            while (isPaused() && !ct.IsCancellationRequested)
                await Task.Delay(50, ct).ConfigureAwait(false);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            var pallet_Coords = GetControlCoords();
            _currentSpeed = tBar_Speed.Value;
            DefaultColorOthers = UU_TT_15_10_UU_UU.BackColor;

            SimClock_Production.Initialize(this, tBox_ProductionTimer_Sim, tBox_ProductionTimer_Act);
            SimClock_LoadStationWait.Initialize(this, tBox_LoadstationWait_Timer_Sim, tBox_LoadstationWait_Timer_Act);
            SimClock_Tester1.Initialize(this, tBox_Tester1Timer_Sim, tBox_Tester1Timer_Act);
            SimClock_Tester2.Initialize(this, tBox_Tester2Timer_Sim, tBox_Tester2Timer_Act);
            SimClock_Tester3.Initialize(this, tBox_Tester3Timer_Sim, tBox_Tester3Timer_Act);
            SimClock_TotalProduction.Initialize(this, tBox_TotalProduction_Timer_Sim, tBox_TotalProduction_Timer_Act);
        }

        private async void btn_Stop_Click(object sender, EventArgs e)
        {
            btn_Stop.Enabled = false;
            CommandStopped = false;
            StopCommand = true;
            _cts?.Cancel();

            try { MovingPallet?.Release(); } catch { /* ignore if already full */ }


            if (thList != null && thList.Count > 0)
            {
                try { await Task.WhenAll(thList); }
                catch (OperationCanceledException) { }
                catch (AggregateException ae) { ae.Handle(ex => ex is OperationCanceledException); }
            }

            foreach (Control c in gBox_SimulationVisual.Controls)
            {
                if (c.Name.Length == 17)
                {
                    if (c.Name.Where(Underscore => Underscore == '_').Count() == 5)
                    {

                        c.Text = "";
                        var namesplit = c.Name.Split("_");
                        var TimeStatus = namesplit[1];

                        switch(TimeStatus)
                        {
                            case "T1":
                            case "T2":
                            case "T3":
                                c.BackColor = DefaultColorTester;
                                break;
                            case "B1":
                            case "B2":
                            case "B3":
                                c.BackColor = DefaultColorBreakin;
                                break;
                            case "LD":
                            case "UL":
                                c.BackColor = DefaultColorLoading;
                                break;
                            default:
                                c.BackColor = DefaultColorOthers;
                                break;
                                
                        }
                    }


                }

            }

            CommandStopped = true;

            // Optionally reset counters
            EvenFailureGate.ResetTester();
            EvenFailureGate.ResetBreakin();
            EvenFailureGate.ResetOutput();
            EvenFailureGate.ResetOk();
  
            btn_Stop.Enabled = true;
        }

        private async void btn_Start_Click(object sender, EventArgs e)
        {
            StopCommand = false;
            if (CommandStopped)
            {


                //private Dictionary<string, int> TimeCodeMaps = new Dictionary<string, int>()
                //{
                //    {"NN",0 }, //Pallet Index with no Stop
                //    {"NP",0 }, //Pallet Index with A Stopper
                //    {"TT",0 }, //Transfer Conveyor Pallet Index
                //    {"B1",0 }, //BreakIn Station 1 Index Time
                //    {"B2",0 }, //BreakIn Station 2 Index Time
                //    {"B3",0 }, //BreakIn Station 3 Index Time
                //    {"T1",0 }, //Test Station 1 Index Time
                //    {"T2",0 }, //Test Station 2 Index Time
                //    {"T3",0 }, //Test Station 3 Index Time
                //    {"UL",0 }, //Unload station Time code dictionary map
                //    {"LD",0 }  //load station Time code dictionary map
                //};
                FinalPalletReady = 0;
                var LV_pass_perc = 0;
                var HV_pass_perc = 0;
                var NV_pass_perc = 0;
                var Station_pass_perc = 0;
                var BreakIn_pass_perc = 0;
                var SRFT_perc = 0;
                var TimeToRetry_Unlaod = 0;
                var TimeToRetry_Load = 0;
                try
                {


                    TimeCodeMaps["NN"] = Convert.ToInt32(tBox_PalletIndexTime.Text);
                    TimeCodeMaps["NP"] = Convert.ToInt32(tBox_PalletWithStopIndexTime.Text);
                    TimeCodeMaps["TT"] = Convert.ToInt32(tBox_TransferIndexTime.Text);
                    TimeCodeMaps["B1"] = Convert.ToInt32(tBox_BreakInTime_B1.Text);
                    TimeCodeMaps["B2"] = Convert.ToInt32(tBox_BreakInTime_B1.Text);
                    TimeCodeMaps["B3"] = Convert.ToInt32(tBox_BreakInTime_B1.Text);
                    TimeCodeMaps["T1"] = Convert.ToInt32(tBox_StationTime_T1.Text) + Convert.ToInt32(tBox_LVTime_T1.Text) + Convert.ToInt32(tBox_HVTime_T1.Text) + Convert.ToInt32(tBox_NVTime_T1.Text);
                    TimeCodeMaps["T2"] = Convert.ToInt32(tBox_StationTime_T1.Text) + Convert.ToInt32(tBox_LVTime_T1.Text) + Convert.ToInt32(tBox_HVTime_T1.Text) + Convert.ToInt32(tBox_NVTime_T1.Text);
                    TimeCodeMaps["T3"] = Convert.ToInt32(tBox_StationTime_T1.Text) + Convert.ToInt32(tBox_LVTime_T1.Text) + Convert.ToInt32(tBox_HVTime_T1.Text) + Convert.ToInt32(tBox_NVTime_T1.Text);
                    TimeCodeMaps["UL"] = Convert.ToInt32(tBox_UnloadTime.Text);
                    TimeCodeMaps["LD"] = Convert.ToInt32(tBox_LoadTime.Text);





                    MaxPalletsAdded = Convert.ToInt32(tBox_Pallets.Text);

                    LV_pass_perc = Convert.ToInt32(tBox_LV_Rate.Text);
                    HV_pass_perc = Convert.ToInt32(tBox_HV_Rate.Text);
                    NV_pass_perc = Convert.ToInt32(tBox_NV_Rate.Text);
                    Station_pass_perc = Convert.ToInt32(tBox_Station_Rate.Text);
                    BreakIn_pass_perc = Convert.ToInt32(tBox_BreakIn_Rate.Text);
                    SRFT_perc = Convert.ToInt32(tBox_SRT_Rate.Text); ;



                    TimeToRetry_Unlaod = Convert.ToInt32(tBox_UnloadRetry_Time.Text);
                    TimeToRetry_Load = Convert.ToInt32(tBox_LoadRetry_Time.Text);

                    if (LV_pass_perc > 100 || HV_pass_perc > 100 || NV_pass_perc > 100 || Station_pass_perc > 100 || BreakIn_pass_perc > 100 || SRFT_perc > 100 ||
                        LV_pass_perc < 1 || HV_pass_perc < 1 || NV_pass_perc < 1 || Station_pass_perc < 1 || BreakIn_pass_perc < 1 || SRFT_perc < 1)
                    {
                        MessageBox.Show("Make sure the percentages are all no less than or equal 100% and greater than 1%");
                        return;
                    }

                    if (MaxPalletsAdded > 40)
                    {
                        MessageBox.Show("Keep the Max Pallets at 40 please....");
                        return;
                    }

                }
                catch
                {
                    MessageBox.Show("Issue with the parameter input times. Make sure the variables are digits only.");
                    return;
                }

                EvenFailureGate.ResetBreakin();
                EvenFailureGate.ResetTester();
                EvenFailureGate.ResetOutput();
                EvenFailureGate.ResetOk();

                // Seed UI to 0 so the boxes match
                tBox_Seats.Text = "0";
                tBox_OKSeats.Text = "0";
                FinalPalletReady = 0;
                CommandStopped = false;
                _cts = new CancellationTokenSource();
                MovingPallet = new SemaphoreSlim(1, 1);
                Errors = new string[MaxPalletsAdded];
                thList = new List<Task>();
                SimClock_LoadStationWait.Reset();
                SimClock_TotalProduction.Reset();
                var pallet = new ProductionPallet(GetControlCoords(), TimeCodeMaps)
                {
                    RateData = new TimeFailureData
                    {
                        LV_Rate_perc = LV_pass_perc,
                        HV_Rate_perc = HV_pass_perc,
                        NV_Rate_perc = NV_pass_perc,
                        Station_Rate_Perc = Station_pass_perc, // generic station pass %
                        BreakIn_Rate_perc = BreakIn_pass_perc, // break-in pass %
                        SRFT_Rate_perc = SRFT_perc,
                        StationTestTime = Convert.ToInt32(tBox_StationTime_T1.Text),
                        LVTestTime = Convert.ToInt32(tBox_LVTime_T1.Text),
                        HVTestTime = Convert.ToInt32(tBox_HVTime_T1.Text),
                        NVTestTime = Convert.ToInt32(tBox_NVTime_T1.Text),
                        TimeToRetry_Load = TimeToRetry_Load,
                        TimeToRetry_Unload = TimeToRetry_Unlaod

                    }
                };
                thList.Add(Task.Run(() => ProductionTotalClock(new ThreadData { Id = 0, Name = Name + "_" + (0).ToString(), pallet = pallet }, _cts.Token)));
                await Task.Delay(10, _cts.Token);
                for (int i = 1; i <= MaxPalletsAdded; i++)
                {

                    pallet = new ProductionPallet(GetControlCoords(), TimeCodeMaps)
                    {
                        RateData = new TimeFailureData
                        {
                            LV_Rate_perc = LV_pass_perc,
                            HV_Rate_perc = HV_pass_perc,
                            NV_Rate_perc = NV_pass_perc,
                            Station_Rate_Perc = Station_pass_perc, // generic station pass %
                            BreakIn_Rate_perc = BreakIn_pass_perc, // break-in pass %
                            SRFT_Rate_perc = SRFT_perc,
                            StationTestTime = Convert.ToInt32(tBox_StationTime_T1.Text),
                            LVTestTime = Convert.ToInt32(tBox_LVTime_T1.Text),
                            HVTestTime = Convert.ToInt32(tBox_HVTime_T1.Text),
                            NVTestTime = Convert.ToInt32(tBox_NVTime_T1.Text),
                            TimeToRetry_Load = TimeToRetry_Load,
                            TimeToRetry_Unload = TimeToRetry_Unlaod

                        }
                    };
                    thList.Add(Task.Run(() => PalletMove(new ThreadData { Id = i, Name = Name + "_" + i.ToString(), pallet = pallet }, _cts.Token)));
                    try
                    {
                        await Task.Delay(10, _cts.Token);
                    }
                    catch { }
                    if (StopCommand)
                        break;

                }


                try { await Task.WhenAll(thList); } catch { }
            }
            else
            {
                MessageBox.Show("Waiting For All threads to stop!");
            }
            CommandStopped = true;

        }

        private void tBar_Speed_ValueChanged(object sender, EventArgs e)
        {
            _currentSpeed = tBar_Speed.Value;
        }

        private void btn_Reset_Click(object sender, EventArgs e)
        {
            if (CommandStopped)
            {


                EvenFailureGate.ResetBreakin();
                EvenFailureGate.ResetTester();
                EvenFailureGate.ResetOutput();
                EvenFailureGate.ResetOk();

                // Seed UI to 0 so the boxes match
                tBox_Seats.Text = "0";
                tBox_OKSeats.Text = "0";
                SimClock_Production.Initialize(this, tBox_ProductionTimer_Sim, tBox_ProductionTimer_Act);
                SimClock_LoadStationWait.Initialize(this, tBox_LoadstationWait_Timer_Sim, tBox_LoadstationWait_Timer_Act);
                SimClock_Tester1.Initialize(this, tBox_Tester1Timer_Sim, tBox_Tester1Timer_Act);
                SimClock_Tester2.Initialize(this, tBox_Tester2Timer_Sim, tBox_Tester2Timer_Act);
                SimClock_Tester3.Initialize(this, tBox_Tester3Timer_Sim, tBox_Tester3Timer_Act);
                SimClock_TotalProduction.Initialize(this, tBox_TotalProduction_Timer_Sim, tBox_TotalProduction_Timer_Act);

                SimClock_Production.Reset();
                SimClock_LoadStationWait.Reset();
                SimClock_Tester1.Reset();
                SimClock_Tester2.Reset();
                SimClock_Tester3.Reset();
                SimClock_TotalProduction.Reset();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            EvenFailureGate.OutputCountChanged -= null; // if you kept references, unsubscribe them explicitly
            EvenFailureGate.OkCountChanged -= null;
            base.OnFormClosed(e);
        }

        private void btn_BreakTime_Click(object sender, EventArgs e)
        {
            if (OnBreak)
            {
                OnBreak = false;
                btn_BreakTime.BackColor = Color.Gray;
            }
            else
            {
                OnBreak = true;
                btn_BreakTime.BackColor = Color.Yellow;
            }
        }
    }
}

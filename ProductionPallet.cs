using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductionSimulator
{
    public class ProductionPallet
    {
        // Properties 
        private List<(string coord, string name)> pallet_coords;
        private Dictionary<string, int> pallet_timeCodes;

        public TimeFailureData RateData = new TimeFailureData();
        public string PreviousAddress { get; set; } = "";
        public ProductionPallet(List<(string coord, string name)> p_coords, Dictionary<string, int> p_timeCodes) 
        {
            pallet_coords = new List<(string coord, string name)>();
            pallet_coords = p_coords;
            pallet_timeCodes = p_timeCodes;
        }
        //Methods
     
        public (string NextAddress ,int SleepAmount, string nextcoord, string TimeStatus, int RawAmount) Move(string controlName, bool useAlt, int Scalar)
        {
            (string nextAddress, string TimeStatus, string nextcoord) nextItem = getNextAddress(controlName, useAlt);
            var times = SleepTimes(nextItem.TimeStatus, Scalar);
            return (nextItem.nextAddress, times.ScaledMs, nextItem.nextcoord,nextItem.TimeStatus,times.RawMs);
        }

        public (string nextAddress,string TimeStatus, string nextcoord) getNextAddress(string currentAddress,bool useAlt)
        {
            (string nextAddress, string TimeStatus, string nextcoord) nextAddress = ("","","");

            var args = currentAddress.Split("_");
            if (args.Length == 6)
            {
                var CurrentDirection = args[0];
                var TimeStatus = args[1];
                var X = args[2];
                var Y = args[3];
                var AltDirection1 = args[4];
                var AltDirection2 = args[5];

                var newX = 0;
                var newY = 0;
                switch (CurrentDirection)
                {
                    case "UU":
                        newX = Convert.ToInt32(X);
                        newY = Convert.ToInt32(Y) + 1;
                        break;
                    case "UR":
                        if (useAlt)
                        {
                            switch (AltDirection2)
                            {
                                case "UU":
                                    newX = Convert.ToInt32(X);
                                    newY = Convert.ToInt32(Y) + 1;
                                    break;
                                case "RR":
                                    if(TimeStatus == "T1" || TimeStatus == "T2" || TimeStatus == "T3")
                                        newX = Convert.ToInt32(X) - 2;
                                    else
                                        newX = Convert.ToInt32(X) - 1;
                                    newY = Convert.ToInt32(Y);
                                    break;
                                default:
                                    return ("","","");
                            }
           
                        }
                        else
                        {
                            switch (AltDirection1)
                            {
                                case "UU":
                                    newX = Convert.ToInt32(X);
                                    newY = Convert.ToInt32(Y) + 1;
                                    break;
                                case "RR":
                                    if (TimeStatus == "T1" || TimeStatus == "T2" || TimeStatus == "T3")
                                        newX = Convert.ToInt32(X) - 2;
                                    else
                                        newX = Convert.ToInt32(X) - 1;
                                    newY = Convert.ToInt32(Y);
                                    break;
                                default:
                                    return ("","","");
                            }
                        }
                        break;
                    case "RR":
                        if (TimeStatus == "T1" || TimeStatus == "T2" || TimeStatus == "T3")
                            newX = Convert.ToInt32(X) - 2;
                        else
                            newX = Convert.ToInt32(X) - 1;
                        newY = Convert.ToInt32(Y);
                        break;
                    case "LL":
                        newX = Convert.ToInt32(X) + 1;
                        newY = Convert.ToInt32(Y);
                        break;
                    case "DD":
                        newX = Convert.ToInt32(X);
                        newY = Convert.ToInt32(Y)-1;
                        break;
                    default:
                        return ("", "","");
                }
                var nextcoord = newX.ToString() + "_" + newY.ToString();
                nextAddress = ("", TimeStatus, nextcoord);
                if (pallet_coords.Count > 0) 
                {
                    nextAddress = (pallet_coords.Where(w=>w.coord == nextcoord).FirstOrDefault().name, TimeStatus,nextcoord);
                    PreviousAddress = currentAddress;
                }
            if(pallet_coords.Where(w => w.coord == nextcoord).FirstOrDefault().name == null)
                {
                    string coord = pallet_coords.Where(w => w.coord == nextcoord).FirstOrDefault().name;
                }

            }
            return nextAddress;

        }

        public (int ScaledMs,int RawMs) SleepTimes(string status, int Scalar)
        {
            if (Scalar == 0)
            {
                return (0,0);
            }

            var scaled = 0;
            var sumInitial = pallet_timeCodes[status];
            var totalTimeAdd = 0;
            switch (status)
            {
                case "T1":
                    if (RateData.FailedBreakIn1)
                    {
                        sumInitial = pallet_timeCodes["NP"];
                       
                    }
                    else
                    {
                        if (RateData.FailedStation)
                        {
                            totalTimeAdd += RateData.StationTestTime;
                            RateData.FailedStation = false;
                        }
                        if (RateData.FailedLV)
                        {
                            totalTimeAdd += RateData.LVTestTime;
                            RateData.FailedLV = false;
                        }
                        if (RateData.FailedHV)
                        {
                            totalTimeAdd += RateData.HVTestTime;
                            RateData.FailedHV = false;
                        }
                        if (RateData.FailedNV)
                        {
                            totalTimeAdd += RateData.NVTestTime;
                            RateData.FailedNV = false;
                        }

                    }


                        break;
                case "T2":
                    if (RateData.FailedBreakIn2)
                    {
                        sumInitial = pallet_timeCodes["NP"];
                      
                    }
                    else
                    {
                        if (RateData.FailedStation)
                        {
                            totalTimeAdd += RateData.StationTestTime;
                            RateData.FailedStation = false;
                        }
                        if (RateData.FailedLV)
                        {
                            totalTimeAdd += RateData.LVTestTime;
                            RateData.FailedLV = false;
                        }
                        if (RateData.FailedHV)
                        {
                            totalTimeAdd += RateData.HVTestTime;
                            RateData.FailedHV = false;
                        }
                        if (RateData.FailedNV)
                        {
                            totalTimeAdd += RateData.NVTestTime;
                            RateData.FailedNV = false;
                        }

                    }
                    break;
                case "T3":
                    if (RateData.FailedBreakIn3)
                    {
                        sumInitial = pallet_timeCodes["NP"];
                     
                    }
                    else
                    {
                        if (RateData.FailedStation)
                        {
                            totalTimeAdd += RateData.StationTestTime;
                            RateData.FailedStation = false;
                        }
                        if (RateData.FailedLV)
                        {
                            totalTimeAdd += RateData.LVTestTime;
                            RateData.FailedLV = false;
                        }
                        if (RateData.FailedHV)
                        {
                            totalTimeAdd += RateData.HVTestTime;
                            RateData.FailedHV = false;
                        }
                        if (RateData.FailedNV)
                        {
                            totalTimeAdd += RateData.NVTestTime;
                            RateData.FailedNV = false;
                        }

                    }
                    break;
                case "LD":
                    if (RateData.FailedFromSRFT)
                        sumInitial = RateData.TimeToRetry_Load;
                    break;
                case "UL":
                    if (RateData.FailedFromSRFT)
                        sumInitial = RateData.TimeToRetry_Unload;
                    break;

            }
            int raw = sumInitial;
            sumInitial = sumInitial + totalTimeAdd;
            raw = sumInitial;
            if (raw < 0)
            {
                raw = pallet_timeCodes["NP"];
            }

            var ScaledInitial = Convert.ToDecimal(Scalar) / 1m;
            var newSum = Convert.ToDecimal(raw) / ScaledInitial;

            scaled = Convert.ToInt32(Math.Round(newSum, MidpointRounding.AwayFromZero));
            return (scaled,raw);

        }
        public int SleepTime(string status, int Scalar) => SleepTimes(status, Scalar).ScaledMs;
    }
    public class TimeFailureData
    {
       
        public int StationTestTime {  get; set; }
        public int LVTestTime { get; set; }
        public int HVTestTime { get; set; }
        public int NVTestTime { get; set; }


        public int BreakIn_Rate_perc {  get; set; }
        public int LV_Rate_perc { get; set; }
        public int HV_Rate_perc { get; set; }
        public int NV_Rate_perc { get; set; }
        public int Station_Rate_Perc { get; set; }
        public int SRFT_Rate_perc { get; set; }

        public bool FailedBreakIn1 { get; set; }
        public bool FailedBreakIn2 { get; set; }
        public bool FailedBreakIn3 { get; set; }
        public bool FailedLV {  get; set; }
        public bool FailedHV { get; set; }
        public bool FailedNV { get; set; }
        public bool FailedStation { get; set; }

        public bool FailedFromSRFT { get; set; }


        public int TimeToRetry_Unload { get; set; }
        public int TimeToRetry_Load { get; set; }

    }
}

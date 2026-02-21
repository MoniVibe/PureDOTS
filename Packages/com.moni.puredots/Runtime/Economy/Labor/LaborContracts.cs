using System;

namespace PureDOTS.Runtime.Economy.Labor
{
    [Flags]
    public enum WorkDayMask : byte
    {
        None = 0,
        Day1 = 1 << 0,
        Day2 = 1 << 1,
        Day3 = 1 << 2,
        Day4 = 1 << 3,
        Day5 = 1 << 4,
        Day6 = 1 << 5,
        Day7 = 1 << 6,
        All = Day1 | Day2 | Day3 | Day4 | Day5 | Day6 | Day7
    }

    public enum WorkSiteType : byte
    {
        Any = 0,
        Station = 1,
        Colony = 2,
        Ship = 3,
        Facility = 4
    }

    public enum WorkSeatType : byte
    {
        Production = 0,
        Operations = 1,
        Command = 2,
        Support = 3,
        Security = 4
    }

    public enum WorkRotationPolicy : byte
    {
        Fixed = 0,
        Rotating = 1,
        Adaptive = 2
    }

    public enum RegimenActivityKind : byte
    {
        Sleep = 0,
        Work = 1,
        FreeTime = 2,
        Leisure = 3,
        Social = 4,
        Pray = 5,
        Training = 6,
        Security = 7
    }

    public enum RegimenTargetKind : byte
    {
        Global = 0,
        Site = 1,
        Role = 2,
        Seat = 3,
        Entity = 4
    }

    public enum RegimenResolutionMode : byte
    {
        WeightedBlend = 0,
        PriorityWins = 1
    }

    public static class WorkforceContractDefaults
    {
        public const float DefaultDayLengthHours = 24f;
        public const int DefaultCycleLengthDays = 7;
        public const WorkDayMask DefaultWeekendOff = WorkDayMask.Day6 | WorkDayMask.Day7;
    }

    [Serializable]
    public struct WorkCalendarDefinition
    {
        public string Id;
        public string DisplayName;
        public float DayLengthHours;
        public int CycleLengthDays;
        public WorkDayMask OffDayMask;
        public float CycleStartOffsetHours;
        public string Notes;
    }

    [Serializable]
    public struct WorkShiftDefinition
    {
        public string Id;
        public string DisplayName;
        public float ShiftLengthHours;
        public float RestHours;
        public int MaxConsecutiveShifts;
        public float MaxOvertimeHours;
        public bool AllowOvertime;
        public float WageMultiplier;
        public float PowerMultiplier;
        public float FatigueGainPerHour;
        public float FatigueRecoveryPerHour;
    }

    [Serializable]
    public struct WorkDowntimeWindow
    {
        public WorkDayMask DayMask;
        public float StartHour;
        public float DurationHours;
        public bool IsMaintenance;
        public string Notes;
    }

    [Serializable]
    public struct WorkShiftScheduleDefinition
    {
        public string Id;
        public string CalendarId;
        public string ShiftId;
        public int ShiftsPerDay;
        public float ShiftOffsetHours;
        public float ShiftSpacingHours;
        public float CoverageTarget01;
        public WorkDowntimeWindow[] DowntimeWindows;
    }

    [Serializable]
    public struct WorkSeatDefinition
    {
        public string Id;
        public WorkSeatType SeatType;
        public string RoleId;
        public int SeatCount;
        public int CrewPerSeat;
        public float WagePerHour;
        public float PowerCostPerHour;
        public float MinSkill01;
    }

    [Serializable]
    public struct WorkTeamDefinition
    {
        public string Id;
        public string DisplayName;
        public WorkSiteType SiteType;
        public string ScheduleId;
        public string[] SeatIds;
        public WorkRotationPolicy RotationPolicy;
        public int MinCrew;
        public int MaxCrew;
        public float CoverageTarget01;
        public float OvertimeBias01;
    }

    [Serializable]
    public struct StaffingProfileDefinition
    {
        public string Id;
        public string DisplayName;
        public WorkSiteType SiteType;
        public string CalendarId;
        public string ScheduleId;
        public WorkSeatDefinition[] Seats;
        public WorkTeamDefinition[] Teams;
        public float PowerBudgetMultiplier;
        public float PayrollBudgetMultiplier;
        public float DowntimeTargetHoursPerCycle;
    }

    [Serializable]
    public struct RegimenSlotDefinition
    {
        public int SlotIndex;
        public RegimenActivityKind Activity;
    }

    [Serializable]
    public struct RegimenDayPatternDefinition
    {
        public string Id;
        public WorkDayMask DayMask;
        public int SlotCount;
        public float SlotLengthHours;
        public RegimenSlotDefinition[] Slots;
        public string Notes;
    }

    [Serializable]
    public struct RegimenScheduleDefinition
    {
        public string Id;
        public string DisplayName;
        public string CalendarId;
        public string DefaultPatternId;
        public RegimenDayPatternDefinition[] Patterns;
        public float ComplianceTarget01;
        public float OverrideTolerance01;
        public string Notes;
    }

    [Serializable]
    public struct RegimenProfileDefinition
    {
        public string Id;
        public string DisplayName;
        public string ScheduleId;
        public float Priority;
        public float FatigueBias01;
        public float MoraleBias01;
        public float DisciplineBias01;
        public float Strictness01;
        public float DeviationBias01;
        public float NightOwlShiftHours;
        public float AfterHoursSocialBias01;
        public float ForcedComplianceMoodPenalty01;
        public float ForcedComplianceSleepPenalty01;
        public string Notes;
    }

    [Serializable]
    public struct RegimenAssignmentDefinition
    {
        public string Id;
        public RegimenTargetKind TargetKind;
        public string TargetId;
        public string ProfileId;
        public string ResolverId;
        public float Priority;
        public float Weight;
    }

    [Serializable]
    public struct RegimenTargetWeightDefinition
    {
        public RegimenTargetKind TargetKind;
        public float Weight;
        public float PriorityBonus;
    }

    [Serializable]
    public struct RegimenResolverDefinition
    {
        public string Id;
        public string DisplayName;
        public RegimenResolutionMode Mode;
        public float ScheduleWeight01;
        public float NeedsWeight01;
        public float SocialWeight01;
        public float HabitWeight01;
        public float StrictnessThreshold01;
        public float DeviationPenalty01;
        public float CompliancePenaltyMultiplier01;
        public RegimenTargetWeightDefinition[] TargetWeights;
        public string Notes;
    }
}
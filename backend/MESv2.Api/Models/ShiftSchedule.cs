namespace MESv2.Api.Models;

public class ShiftSchedule
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public DateOnly EffectiveDate { get; set; }

    public decimal MondayHours { get; set; }
    public int MondayBreakMinutes { get; set; }
    public decimal TuesdayHours { get; set; }
    public int TuesdayBreakMinutes { get; set; }
    public decimal WednesdayHours { get; set; }
    public int WednesdayBreakMinutes { get; set; }
    public decimal ThursdayHours { get; set; }
    public int ThursdayBreakMinutes { get; set; }
    public decimal FridayHours { get; set; }
    public int FridayBreakMinutes { get; set; }
    public decimal SaturdayHours { get; set; }
    public int SaturdayBreakMinutes { get; set; }
    public decimal SundayHours { get; set; }
    public int SundayBreakMinutes { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public Plant Plant { get; set; } = null!;
    public User? CreatedByUser { get; set; }

    public decimal GetPlannedMinutes(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => MondayHours * 60 - MondayBreakMinutes,
        DayOfWeek.Tuesday => TuesdayHours * 60 - TuesdayBreakMinutes,
        DayOfWeek.Wednesday => WednesdayHours * 60 - WednesdayBreakMinutes,
        DayOfWeek.Thursday => ThursdayHours * 60 - ThursdayBreakMinutes,
        DayOfWeek.Friday => FridayHours * 60 - FridayBreakMinutes,
        DayOfWeek.Saturday => SaturdayHours * 60 - SaturdayBreakMinutes,
        DayOfWeek.Sunday => SundayHours * 60 - SundayBreakMinutes,
        _ => 0m
    };
}

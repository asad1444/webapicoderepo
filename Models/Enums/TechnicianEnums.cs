namespace SmartProManWebAPI.Models.Enums
{
    public enum DutyStatus
    {
        OffDuty = 0,
        OnDuty = 1
    }

    public enum JobStatus
    {
        Assigned = 1,
        InProgress = 2,
        Completed = 3,
        Stuck = 4
    }

    public enum Priority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Emergency = 4
    }

    public enum ServiceType
    {
        Complaint = 1,
        PM = 2,
        Installation = 3
    }

    public enum MediaType
    {
        Video = 1,
        ImageWM = 2,
        ImageFridge = 3,
        ImageGeneral = 4
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Online = 2
    }
}

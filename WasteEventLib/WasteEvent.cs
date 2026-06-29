
namespace WasteEvent;

public enum WasteEventType
{
    ServiceUpdate,
    RequestCreated,
    RoutePositionUpdate
}

public enum ServiceStatus
{
    Possible = 1,
    Await = 2,
    Complete = 3,
    Canceled = 4,
    CantComplete = 5
}

public class WasteEvent
{
    public DateTime HappenedAt { get; set; }
    
    public WasteEventType EventType { get; set; }
    
    public object? Context { get; set; }
}

public class Coordinate
{
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }

    public double Altitude { get; set; } = 0;
    
    public int Srid { get; set; } = 4326;
}

public class ServiceUpdateEventContext
{
    public int ServiceId { get; set; }
    
    public int LocationId { get; set; }
    
    public ServiceStatus Status { get; set; }
    
    public int? RouteId { get; set; }
    
    public int? HandledByDeviceId { get; set; }
    
    public int? HandledByCarId { get; set; }
    
    public DateTime Time { get; set; }
}

public class RequestCreatedEventContext
{
    public int RequestId { get; set; }
    
    public int LocationId { get; set; }
    
    public int ServiceId { get; set; }
    
    public Coordinate? Position { get; set; }
    
    public DateTime Time { get; set; }
}

public class RouteUpdateEventContext
{
    public DateTime Time { get; set; }
    
    public Coordinate? Position { get; set; }
    
    public int DeviceId { get; set; }
    
    public int CarId { get; set; }
    
    public int RouteId { get; set; }
    
    public float Speed { get; set; }
    
    public float Heading { get; set; }
}
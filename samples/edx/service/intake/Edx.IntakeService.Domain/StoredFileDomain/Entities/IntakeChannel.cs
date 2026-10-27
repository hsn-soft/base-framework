namespace Edx.IntakeService.Domain.StoredFileDomain.Entities;

/// <summary>How the customer sent a file: the service speaks all three, a customer uses the one it can.</summary>
public enum IntakeChannel
{
    Rest = 0,
    Grpc = 1,
    Wcf = 2,
}

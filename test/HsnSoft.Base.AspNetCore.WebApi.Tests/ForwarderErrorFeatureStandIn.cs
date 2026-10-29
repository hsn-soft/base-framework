#pragma warning disable IDE0130 // the name must be the one of YARP: the middleware finds the feature by it
namespace Yarp.ReverseProxy.Forwarder;

// what YARP leaves in the features of a request it could not forward: the middleware finds it by name, so the tests need no reference to YARP
public enum ForwarderError
{
    None,
    Request,
    NoAvailableDestinations,
}

#pragma warning disable CA1716 // the member is named by YARP
public interface IForwarderErrorFeature
{
    ForwarderError Error { get; }

    Exception? Exception { get; }
}
#pragma warning restore CA1716

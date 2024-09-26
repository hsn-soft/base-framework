using System;
using PuppeteerSharp;

namespace HsnSoft.Base.PuppeTeer.Connection;

public interface IPuppeTeerPersistentConnection : IDisposable
{
    IBrowser GetBrowser();

    public string InitResult { get; set; }
}
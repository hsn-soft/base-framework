﻿namespace HsnSoft.Base.Clients;

public interface ICurrentClient
{
    string Id { get; }

    bool IsAuthenticated { get; }
}

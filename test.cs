using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using WalletAPI.Data;
using System.Data;

var services = new ServiceCollection();
services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<WalletDbContext>(o =>
    {
        o.IsolationLevel = IsolationLevel.Serializable;
    });
});

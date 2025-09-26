using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;

class AddressResolver
{
    public AddressResolver(IServer server)
    {
        Address = GetAddress(server);
    }

    public string Address { get; }

    private static string GetAddress(IServer server)
    {
        var port = (from a in server.Features.Get<IServerAddressesFeature>()?.Addresses ?? []
                    let binding = BindingAddress.Parse(a)
                    where binding.Scheme == "http"
                    select binding.Port)
                        .First();
        return $"http://localhost:{port}/bingohub";
    }
}
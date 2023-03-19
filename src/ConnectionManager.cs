using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using gspro_r10.OpenConnect;
using gspro_r10.R10;
using Microsoft.Extensions.Configuration;

namespace gspro_r10
{
  public class ConnectionManager
  {
    private R10ConnectionServer? R10Server;
    private OpenConnectClient OpenConnectClient;
    private BluetoothConnection? BluetoothConnection { get; }
    private PuttingConnectionServer? PuttingServer { get; }

    private JsonSerializerOptions serializerSettings = new JsonSerializerOptions()
    {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private int shotNumber = 0;
    private PlayerInformation previousInfo = new PlayerInformation()
    {
      Club = Club.Unknown,
      DistanceToTarget = 0.0,
      Handed = Handed.Unknown
    };

    public ConnectionManager(IConfigurationRoot configuration)
    {
      if (bool.Parse(configuration.GetSection("r10E6Server")["enabled"] ?? "false"))
      {
        R10Server = new R10ConnectionServer(this, configuration.GetSection("r10E6Server"));
        R10Server.Start();
      }

      OpenConnectClient = new OpenConnectClient(this, configuration.GetSection("openConnect"));
      OpenConnectClient.ConnectAsync();

      if (bool.Parse(configuration.GetSection("bluetooth")["enabled"] ?? "false"))
        BluetoothConnection = new BluetoothConnection(this, configuration.GetSection("bluetooth"));

      if (bool.Parse(configuration.GetSection("putting")["enabled"] ?? "false"))
      {
        PuttingServer = new PuttingConnectionServer(this, configuration.GetSection("putting"));
      }
    }


    internal void SendShot(OpenConnect.BallData? ballData, OpenConnect.ClubData? clubData)
    {
      string openConnectMessage = JsonSerializer.Serialize(OpenConnectApiMessage.CreateShotData(
        shotNumber++,
        ballData,
        clubData
      ), serializerSettings);

      OpenConnectClient.SendAsync(openConnectMessage);
    }

    internal void SendLaunchMonitorReadyUpdate(bool deviceReady)
    {
      OpenConnectClient.SetDeviceReady(deviceReady);
    }

    internal void PlayerInformationUpdated(PlayerInformation newInfo)
    {
      if (previousInfo.Club != newInfo.Club)
      {
        if (previousInfo.Club == Club.PT && newInfo.Club != Club.PT)
        {
          PuttingServer?.Stop();
        } else if (newInfo.Club == Club.PT)
        {
          PuttingServer?.Start();
        }
      }

      previousInfo = newInfo;
    }
  }
}
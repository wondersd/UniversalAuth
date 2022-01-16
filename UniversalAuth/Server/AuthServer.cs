using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace UniversalAuth.Server
{
    using Data;
    using Network;

    public delegate void OnEvent(Client c);

    public abstract class AuthServer
    {
        public OnEvent OnConnect;
        public OnEvent OnDisconnect;

        public LengthedSocket Socket { get; private set; }
        public Random Random { get; private set; }

        public List<Client> Clients = new List<Client>();

        private ManualResetEvent acceptConnection;

        private bool continueAcceptConnection;

        protected AuthServer()
        {
            Socket = null;
            Random = new Random();
            acceptConnection = new ManualResetEvent(false);
            continueAcceptConnection = true;
        }

        ~AuthServer()
        {
            if (Socket != null)
                Socket.Close();
        }

        // https://docs.microsoft.com/en-us/dotnet/framework/network-programming/using-an-asynchronous-server-socket
        public virtual void Start(IPAddress bindAddress, Int32 port)
        {
            Socket = new LengthedSocket(LengthedSocket.SizeType.Word);
            Socket.Bind(new IPEndPoint(bindAddress, port));
            Socket.Listen(100);

            while(continueAcceptConnection) {
                acceptConnection.Reset();

                Socket.BeginAccept(EndAccept);

                acceptConnection.WaitOne();
            }
        }

        public virtual void Stop()
        {
            continueAcceptConnection = false;
            if (Socket != null) {
                Socket.Close();
                Socket = null;
            }
        }

        private void EndAccept(IAsyncResult result)
        {
            acceptConnection.Set();
            Clients.Add(new Client(Socket.EndAccept(result), this));
        }

        public virtual void Remove(Client client)
        {
            if (Clients.Contains(client))
                Clients.Remove(client);
        }

        public abstract Boolean ValidateServer(Client client, Byte serverId);
        public abstract Boolean ValidateLogin(Client client, String user, String password, UInt32 subscription, UInt16 cdkey);
        public abstract Boolean GetServerInfos(Client client, out List<ServerInfoEx> servers);

        public virtual void GenerateData(out UInt32 oneTimeKey, out UInt32 sessionId1, out UInt32 sessionId2)
        {
            var buff = new Byte[12];
            Random.NextBytes(buff);

            oneTimeKey = BitConverter.ToUInt32(buff, 0);
            sessionId1 = BitConverter.ToUInt32(buff, 4);
            sessionId2 = BitConverter.ToUInt32(buff, 8);
        }
    }
}

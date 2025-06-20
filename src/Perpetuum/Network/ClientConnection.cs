using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Network
{
    public class ClientConnection : EncryptedTcpConnection
    {
        private readonly Rc4 _rc4;

        private ClientConnection(Socket socket) : base(socket)
        {
            _rc4 = new Rc4(FastRandom.NextBytes(40));
            ObjectHelper.Swap(ref inIncrement, ref outIncrement);
            ObjectHelper.Swap(ref inDecodingByte, ref outEncodingByte);
        }

        protected override void OnReceived(byte[] data)
        {
            _rc4.Decrypt(data, 1, data.Length - 1);

            int dataOffset = 4;

            byte compressionLevel = data[0];
            if (compressionLevel == 2)
            {
                data = GZip.Decompress(data, 5);
                dataOffset--;
            }

            string messageText = Encoding.UTF8.GetString(data, dataOffset, data.Length - dataOffset);
            OnReceived(messageText);

            base.OnReceived(data);
        }

        public new event Action<string> Received;

        private readonly ConcurrentDictionary<Command, ConcurrentQueue<TaskCompletionSource<IMessage>>> _commandQueue = new ConcurrentDictionary<Command, ConcurrentQueue<TaskCompletionSource<IMessage>>>();

        public IMessage Send(Command command)
        {
            Task<IMessage> t = SendAsync(command);
            return t.Result;
        }

        public Task<IMessage> SendAsync(Command command)
        {
            return SendAsync(new Message(command));
        }

        public IMessage Send(string messageText)
        {
            Task<IMessage> t = SendAsync(messageText);
            return t.Result;
        }

        public Task<IMessage> SendAsync(string messageText)
        {
            Message m = Message.Parse(messageText);
            return SendAsync(m);
        }

        public IMessage Send(IMessage clientMessage)
        {
            Task<IMessage> t = SendAsync(clientMessage);
            return t.Result;
        }

        public Task<IMessage> SendAsync(IMessage clientMessage)
        {
            TaskCompletionSource<IMessage> source = new TaskCompletionSource<IMessage>();
            EnqueueCompletionSource(clientMessage.Command, source);
            Send(clientMessage.ToBytes());
            return source.Task;
        }

        private void EnqueueCompletionSource(Command command, TaskCompletionSource<IMessage> source)
        {
            ConcurrentQueue<TaskCompletionSource<IMessage>> q = _commandQueue.GetOrAdd(command, () => new ConcurrentQueue<TaskCompletionSource<IMessage>>());
            q.Enqueue(source);
        }

        public override void Send(byte[] data)
        {
            byte[] t = new byte[data.Length + 4];
            Array.Copy(data, 0, t, 4, data.Length);
            _rc4.Encrypt(t, 1, t.Length - 1);
            base.Send(t);
        }

        public static ClientConnection Connect(IPEndPoint endPoint)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            ClientConnection connect = new ClientConnection(socket);
            connect.Receive();
            return connect;
        }

        public void SendHandshake()
        {
            SendHandshakeAsync().Wait();
        }

        public Task SendHandshakeAsync()
        {
            byte[] e = Rsa.Encrypt(_rc4.streamKey);
            byte[] data = new byte[e.Length + 4];
            Array.Copy(e, 0, data, 4, e.Length);

            TaskCompletionSource<IMessage> source = new TaskCompletionSource<IMessage>();
            EnqueueCompletionSource(Commands.Welcome, source);

            base.Send(data);
            return source.Task;
        }

        public event TcpConnectionEventHandler<IMessage> MessageReceived;
        protected virtual void OnReceived(string messageText)
        {
            Message message = Message.Parse(messageText);
            MessageReceived?.Invoke(this, message);


            ConcurrentQueue<TaskCompletionSource<IMessage>> q = _commandQueue.GetOrDefault(message.Command);
            if (q != null)
            {
                if (q.TryDequeue(out TaskCompletionSource<IMessage> source))
                {
                    source.SetResult(message);
                }
            }

            Received?.Invoke(messageText);
        }
    }
}
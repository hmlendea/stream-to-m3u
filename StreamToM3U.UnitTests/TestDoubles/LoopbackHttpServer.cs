using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace StreamToM3U.UnitTests.TestDoubles
{
    internal sealed class LoopbackHttpServer : IDisposable
    {
        static string HeaderTerminator => "\r\n\r\n";

        static int MaximumRequestLength => 8192;

        static int ConnectionTimeoutMilliseconds => 10000;

        readonly CancellationTokenSource cancellationTokenSource;
        readonly ConcurrentQueue<string> requests;
        readonly TcpListener listener;
        readonly byte[] responseHeaders;
        readonly byte[] responseBody;
        readonly int port;

        public LoopbackHttpServer(int statusCode, string responseBody)
        {
            cancellationTokenSource = new CancellationTokenSource();
            requests = new ConcurrentQueue<string>();
            this.responseBody = Encoding.UTF8.GetBytes(responseBody);

            responseHeaders = BuildResponseHeaders(statusCode, this.responseBody.Length);

            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            port = ((IPEndPoint)listener.LocalEndpoint).Port;

            Thread serverThread = new(AcceptRequests);
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        public string Url => $"http://127.0.0.1:{port}/playlist.m3u8";

        public IEnumerable<string> Requests => requests;

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            listener.Stop();
            cancellationTokenSource.Dispose();
        }

        static byte[] BuildResponseHeaders(int statusCode, int contentLength)
        {
            HttpStatusCode status = (HttpStatusCode)statusCode;
            string headers =
                $"HTTP/1.1 {statusCode} {status}\r\n" +
                "Content-Type: application/vnd.apple.mpegurl; charset=utf-8\r\n" +
                $"Content-Length: {contentLength}\r\n" +
                "Connection: close" +
                HeaderTerminator;

            return Encoding.ASCII.GetBytes(headers);
        }

        static string ReadRequest(NetworkStream stream)
        {
            StringBuilder request = new();
            byte[] singleByte = new byte[1];

            while (request.Length < MaximumRequestLength)
            {
                int bytesRead = stream.Read(singleByte, 0, 1);

                if (bytesRead == 0)
                {
                    break;
                }

                request.Append((char)singleByte[0]);

                if (request.ToString().EndsWith(HeaderTerminator, StringComparison.Ordinal))
                {
                    break;
                }
            }

            return request.ToString();
        }

        void AcceptRequests()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }

                ServeClient(client);
            }
        }

        void ServeClient(TcpClient client)
        {
            using (client)
            {
                client.ReceiveTimeout = ConnectionTimeoutMilliseconds;
                client.SendTimeout = ConnectionTimeoutMilliseconds;

                NetworkStream stream = client.GetStream();

                try
                {
                    requests.Enqueue(ReadRequest(stream));

                    stream.Write(responseHeaders, 0, responseHeaders.Length);
                    stream.Write(responseBody, 0, responseBody.Length);
                    stream.Flush();
                }
                catch (IOException)
                {
                    // The client closed the connection before the response could be written.
                }
            }
        }
    }
}

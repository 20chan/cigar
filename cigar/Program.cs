using CreamRoll.Routing;
using System;
using System.Threading;

namespace cigar {
    class Program {
        static void Main(string[] args) {
            var cigar = new CigarServer("papers.db");
            var frontend = new FrontEndServer("FrontEnd", "index.html");

            var server = new RouteServer<CigarServer>(cigar, port: 4004);
            server.AppendRoutes(frontend);
            var waiter = new ManualResetEvent(false);

            Console.CancelKeyPress += (o, e) => {
                e.Cancel = true;
                waiter.Set();
            };

            server.StartAsync();
            Console.WriteLine("tart started.. press ctrl+c to stop");

            waiter.WaitOne();
            server.Stop();
        }
    }
}

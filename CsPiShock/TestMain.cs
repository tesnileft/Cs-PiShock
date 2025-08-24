using CsPiShock;

namespace TestMain
{
    class TestMain
    {
        private static string _user = "Tesni";
        private static string _apiKey = "a72962a9-05b8-4e81-bd62-8a02f31f4125";
        /// <summary>
        /// Main function for debugging
        /// </summary>
        static void Main(string[] args)
        {
            SocketTest();
        }

        static void SocketTest()
        {
            var socket = new WebSocketAPI(_apiKey, _user );
            Console.WriteLine("Ping...");
            if (socket.Ping().Result)
            {
                Console.WriteLine("Pong!");
            }
        }
    

        static void SerialTest()
        {
                PiShockSerialApi pishock = new PiShockSerialApi();
                pishock.DebugEnabled = true;
                SerialShocker shockerA = pishock.CreateShocker("8619");
                SerialShocker shockerB = pishock.CreateShocker("9509");

                bool running = true;
                while (running)
                {
                    switch (Console.ReadLine())
                    {
                        case "Info":
                            Console.WriteLine(pishock.Info);
                            break;
                        case "Stop":
                        case "stop":
                            pishock.Dispose();
                            running = false;
                            break;
                        case "sa":
                            shockerA.Vibrate(100, 100);
                            break;
                        case "sb":
                            shockerB.Vibrate(100, 100);
                            break;
                        case "aandb":
                            shockerA.Vibrate(100, 20);
                            Thread.Sleep(100);
                            shockerB.Vibrate(100, 20);
                            break;
                        case "shocka":
                            shockerA.Shock(100, 10);
                            break;
                        case "addnet":
                            pishock.AddNetwork("TesPhone", "lollollol");
                            break;
                    }
                }
        }
    }
}

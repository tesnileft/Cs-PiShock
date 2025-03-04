using CsPiShock;

namespace TestMain
{
    class TestMain
    {
        /// <summary>
        /// Main function for debugging
        /// </summary>
        static void Main(string[] args)
        {
            SerialTest();
        }

        static void HttpTest()
        {
            PiShockHttpApi http = new PiShockHttpApi("Tesni","6aaa08de-6151-4094-96eb-20a39d82cda8");
            HttpShocker a = http.CreateShocker("2ABD4353099");
            a.Beep(1000);
            a.Vibrate(10, 100);
            
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

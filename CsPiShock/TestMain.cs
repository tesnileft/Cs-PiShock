using CsPiShock;

namespace TestMain
{
    class TestMain
    {
        /// <summary>
        /// Main function for debugging
        /// </summary>
        static void Main()
        {
            PiShockSerialApi pishock = new PiShockSerialApi();
            SerialShocker shockerA = pishock.CreateShocker(8619);
            SerialShocker shockerB = pishock.CreateShocker(9509);

            bool running = true;
            while (running)
            {
                switch (Console.ReadLine())
                {
                    case "Info":
                        Console.WriteLine(pishock.Info());
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
                }
            }
        }
    }
}

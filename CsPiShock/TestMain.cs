
namespace CsPiShock
{
    internal class TestMain
    {
        /// <summary>
        /// Main function for debugging
        /// </summary>
        static void Main()
        {
            PiShockSerialApi pishock = new PiShockSerialApi();
            //SerialShocker s = new SerialShocker(8619, pishock);
            //Console.WriteLine(pishock.Info()); //Print the info of the shocker
            SerialShocker shockerA = new SerialShocker(8619, pishock);
            SerialShocker shockerB = new SerialShocker(9509, pishock);

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

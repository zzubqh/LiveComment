using Fleck;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveCommentServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var arguments = CommandLineArgumentParser.Parse(args);
            string bindIP = string.Empty;
            string recordPath = string.Empty;
            if (arguments.Has("-h"))
                bindIP = arguments.Get("-h").Next;
            else
                Console.WriteLine("请输入IP，格式 LiveCommentServer.exe -h IP -o record_path");
            if (arguments.Has("-o"))
                recordPath = arguments.Get("-o").Next;
            else
                Console.WriteLine("请输入记录文件地址，格式 LiveCommentServer.exe -h IP -o record_path");

            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer(string.Format("ws://{0}:7181", bindIP));
            //List<string> writeBuffer = new List<string>();
            List<String> readBuffer = new List<string>();
            //bool writFlag = false;
            try
            {
                StreamReader sr = new StreamReader(recordPath, Encoding.Default);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    readBuffer.Add(line);
                }
                sr.Close();
            }
            catch(IOException ex)
            {
                readBuffer.Clear();
            }

            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Open!");
                    foreach(string msg in readBuffer)
                    {
                        socket.Send(msg);
                    }
                    allSockets.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    allSockets.Remove(socket);
                    if (allSockets.Count == 0)
                    {
                        StreamWriter sw = new StreamWriter(recordPath);
                        foreach (string msg in readBuffer)
                            sw.WriteLine(msg);
                        sw.Close();
                    }
                };
                socket.OnMessage = message =>
                {
                    Console.WriteLine(message);
                    readBuffer.Add(message);
                    allSockets.ToList().ForEach(s => s.Send(message.Substring(0,message.LastIndexOf(','))));
                    //writeBuffer.Add(message);                   
                    //if(writeBuffer.Count > 999)
                    //{
                    //    StreamWriter sw = new StreamWriter(recordPath, true);
                    //    foreach (string msg in writeBuffer)
                    //    {
                    //        sw.WriteLine(msg);                            
                    //    }

                    //    sw.Close();
                    //    //writFlag = true;
                    //}
                    //writeBuffer.Clear();
                };
                socket.OnBinary = file => {

                    string path = ("D:\\test.txt");
                    //创建一个文件流
                    FileStream fs = new FileStream(path, FileMode.Create);

                    //将byte数组写入文件中
                    fs.Write(file, 0, file.Length);
                    //所有流类型都要关闭流，否则会出现内存泄露问题
                    fs.Close();
                };
            });


            var input = Console.ReadLine();
            while (input != "exit")
            {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }
        }
        
    }
}

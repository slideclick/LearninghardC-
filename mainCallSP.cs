using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Data;
using System.Data.SqlClient;

using System.Threading;



namespace mainCallSP
{
    class mainCallSP
    {
        static bool go = true;
        static int RequestPerSecond = 5;
        static int DelayBeforeDBRequest = 7;
        static bool useTran = false;
        static bool usePoolthread = false;

        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                
                RequestPerSecond = Int32.Parse(args[0]);
                DelayBeforeDBRequest = Int32.Parse(args[1]);
                useTran = (Int32.Parse(args[2]) == 1) ? true : false;
                usePoolthread = (args.Length > 3) ? true : false;
                
            }
            Console.WriteLine("RequestPerSecond: {0}; DelayBeforeDBRequest:{1} seconds,useTran: {2},usePoolthread:{3}", RequestPerSecond, DelayBeforeDBRequest, useTran, usePoolthread);
            //Worker(null); return;
            Thread backThread = new Thread(Enginee); // 创建一个线程
            backThread.IsBackground = true;
            backThread.Start();
            //backThread.Join();


            Console.WriteLine("按下回车键来取消创建DB请求");
            Console.ReadLine();
            go = false; // 取消请求
            Console.WriteLine("已经取消;等线程数降下来（DB请求执行完毕）按下回车键来退出");
            Console.ReadKey();
            Console.WriteLine("从主线程中退出");


        }

        public static void Enginee() {
            Console.WriteLine("Enginee线程运行");
            int count = 0;
            while (true) {
                if (go)
                {
                    if (!usePoolthread)
                    {
                        Thread backThread = new Thread(Worker); // 创建一个线程
                        backThread.IsBackground = false;// all db request will run to complete when exiting
                        backThread.Start();
                    }
                    else
                    {

                        ThreadPool.QueueUserWorkItem(Worker);
                        continue;

                        ThreadPool.QueueUserWorkItem((object state) =>
                        {
                            Thread.Sleep(TimeSpan.FromSeconds(DelayBeforeDBRequest));
                            if (state != null)
                            {
                                Console.WriteLine("线程池线程ID = {0} 传入的参数为 {1} 第{2}个线程", Thread.CurrentThread.ManagedThreadId, state.ToString(), (count++).ToString());
                            }
                        }, "work");
                    }

                    //Task task = Task. Run(() => { Thread. Sleep( TimeSpan. FromSeconds( 2)); });


                }
                else {
                    break;
                }
                if (RequestPerSecond > 0)
                    Thread.Sleep(1000 / RequestPerSecond);
            }
            Console.WriteLine("Enginee线程中退出");
        
        }



        public static void Worker(object state)
        {
            if (DelayBeforeDBRequest > 0) Thread.Sleep(1000 * DelayBeforeDBRequest);
            SqlDataReader reader;
            var _connectionString = "Server=x;Database=x;User Id=sa;Password=x;Connect Timeout=12";
            for (int i = 0; i < 1; i++)
            {               
                var sqlConn = new SqlConnection(_connectionString);
                if (!useTran)                
                {                    
                    try
                    {
                        sqlConn.Open();
                        for (int j = 0; j < 1; j++)
                        {

                            using (SqlCommand sqlCmd = new SqlCommand())
                            {
                                sqlCmd.Connection = sqlConn;  
                                //sqlCmd.CommandTimeout = 3;
                                sqlCmd.CommandType = CommandType.StoredProcedure;
                                sqlCmd.CommandText = "x";
      
                                sqlCmd.Parameters.Add(new SqlParameter("@@threadID", Thread.CurrentThread.ManagedThreadId));      
                                reader = sqlCmd.ExecuteReader(CommandBehavior.CloseConnection);
                                reader.Close();

                            }
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally {
                        sqlConn.Close();
                    }
                }
                else
                {
                    SqlCommand sqlCmd = sqlConn.CreateCommand();
                    sqlConn.Open();
                    SqlTransaction tx = sqlConn.BeginTransaction();
                    try
                    {
                        sqlCmd.Transaction = tx;

                        sqlCmd.CommandType = CommandType.StoredProcedure;
                        sqlCmd.CommandText = "x";

                        sqlCmd.Parameters.Add(new SqlParameter("@@threadID", Thread.CurrentThread.ManagedThreadId));
                        reader = sqlCmd.ExecuteReader();//CommandBehavior.CloseConnection
                        reader.Close();
                        tx.Commit();
                    }
                    finally
                    {
                        sqlConn.Close();
                    }
                }
               
            }
 
        }
    }
}



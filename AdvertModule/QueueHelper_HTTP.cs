using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.Collections;
using System.Threading;
using MiscUtil.Threading;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Configuration;


namespace AdvertModule
{
    public class QueueHelper_HTTP
    {
        private static volatile QueueHelper_HTTP instance;
        private static readonly ILog logger = LogManager.GetLogger(typeof(QueueHelper_HTTP));

        private Queue<HttpWebRequest> itemList = new Queue<HttpWebRequest>();
        private CustomThreadPool workerThreadPool;
        private AutoResetEvent resourceLockOut = new AutoResetEvent(true);

        public delegate void httpDelegate(HttpWebRequest req);

        private QueueHelper_HTTP()
        {
        }

        public static QueueHelper_HTTP Instance
        {
            get
            {
                if (instance == null)
                {
                    if (instance == null)
                        instance = new QueueHelper_HTTP();
                }

                return instance;
            }
        }

        public void sendHTTPRequest(HttpWebRequest req) {
            using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader streamIn = new StreamReader(responseStream))
                    {
                        String strResponse = streamIn.ReadToEnd();
                        streamIn.Close();
                    }
                    responseStream.Flush();
                    responseStream.Close();
                }
                response.Close();
            }
        }

        public void addHTTPRequestDelegateToThreadPool(HttpWebRequest req)
        {
            //add to the thread pool
            workerThreadPool.AddWorkItem(new httpDelegate(sendHTTPRequest), req);
        }

        private void QueueHandler()
        {
            //this will receive a notification when there is a new HttpRequest in the thread.
            workerThreadPool = new CustomThreadPool();

            int minThreads = 1;
            int maxThreads = 20;

            workerThreadPool.SetMinMaxThreads(minThreads, maxThreads);

            logger.Info("[" + MethodBase.GetCurrentMethod().Name + "()] - HTTP Web Request thread started");

            while (1 == 1)
            {
                resourceLockOut.WaitOne();

                while (itemList.Count > 0)
                {
                    if ((itemList.Count > 100) && (itemList.Count % 1000 == 0))
                    {
                        logger.Error("[" + MethodBase.GetCurrentMethod().Name + "()] - http queue size: " + itemList.Count);
                        
                        if (itemList.Count > 5000) { 
                            //we're in trouble if we get to this point
                            //better to dump queue and start over?                            
                            itemList.Clear();
                            logger.Fatal("[" + MethodBase.GetCurrentMethod().Name + "()] - queue size exceeds max threshold: " + itemList.Count + ", clearing queue!");
                        }
                    }

                    lock (itemList)
                    {
                        try
                        {
                            //Fetch item from front of queue and request a thread from thread pool to process it
                            HttpWebRequest req = itemList.Dequeue();
                            addHTTPRequestDelegateToThreadPool(req);
                        }
                        catch (Exception e)
                        {
                            logger.Error("[" + MethodBase.GetCurrentMethod().Name + "()] - System Exception doing QUEUE Request: " + e.ToString());
                        }

                    }
                }
            }
        }


        public void StartQueueHandlers()
        {
            ThreadStart job = new ThreadStart(QueueHandler);
            Thread thread = new Thread(job);
            thread.Start();
        }

        public void QueueItem(HttpWebRequest httpWebRequest)
        {
            logger.Debug("[" + MethodBase.GetCurrentMethod().Name + "()] - START");

            lock (itemList)
            {
                itemList.Enqueue(httpWebRequest);
            }

            try
            {
                //ResourceLockOut.Release(1);
                resourceLockOut.Set();
            }
            catch (Exception e)
            {
                logger.Error("[" + MethodBase.GetCurrentMethod().Name + "()] - ResourceLock exception (Out)", e);
            }
        }

    }
}

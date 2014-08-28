using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScotSoft.PattySaver;
using ScotSoft.PattySaver.LaunchManager;
using ScotSoft.PattySaver.DebugUtils;

namespace ScotSoft.PattySaver.DebugUtils
{
    public interface IDebugOutputConsumer
    {
        void ConsumeDebugOutputBuffer(string SomeText);
        void ConsumeDebugOutput(string SomeText);
    }

    class Logging
    {
        static private List<LogDestination> Destinations = new List<LogDestination>();

        static public void AddLogDestination(LogDestination destination)
        {
            Destinations.Add(destination);
        }

        static public void RemoveLogDestination(LogDestination destination)
        {
            Destinations.Remove(destination);
        }

        static public void ClearLogDestinations()
        {
            Destinations.Clear();
        }

        static public IReadOnlyList<LogDestination> LogDestinations
        {
            get
            {
                return (IReadOnlyList<LogDestination>)Destinations;
            }
        }

        static public bool DestinationsContains(LogDestination destination)
        {
            return Destinations.Contains(destination);
        }

        static private List<IDebugOutputConsumer> Consumers = new List<IDebugOutputConsumer>();

        static public void AddConsumer(IDebugOutputConsumer consumer)
        {
            if (consumer != null)
            {
                Consumers.Add(consumer);

                if (strBuffer != null)
                {
                    // send the consumer what is in the buffer now
                    consumer.ConsumeDebugOutputBuffer(strBuffer);
                }
            }
        }

        static public void RemoveConsumer(IDebugOutputConsumer consumer)
        {
            if (consumer != null)
            {
                if (!Consumers.Remove(consumer))
                {
                    System.Diagnostics.Debug.WriteLineIf(true, " *** RemoveConsumer():  FAILED to remove desired consumer from Consumers!");
                }
            }
        }

        static public void ClearConsumers()
        {
            Consumers.Clear();
        }

        static public bool ConsumersContains(IDebugOutputConsumer consumer)
        {
            return Consumers.Contains(consumer);
        }


        static private string strBuffer = "";

        public enum LogDestination
        {
            Default,
            Buffer
        };

        /// <summary>
        /// The only method that actually logs. All other LogX statements call this statement.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="AddCrLf"></param>
        static public void Log(int level, string category, string message, bool AddCrLf = false)
        {
            if (AddCrLf) message = message + Environment.NewLine;

            if (DestinationsContains(LogDestination.Default))
            {
                System.Diagnostics.Debugger.Log(level, category, message);
            }

            if (DestinationsContains(LogDestination.Buffer))
            {
                // if strBuffer.Length gets too larege clear it
                if (strBuffer.Length > Int32.MaxValue / 3)
                {
                    strBuffer = "";
                    GC.Collect();
                    strBuffer += "<< Cleared strBuffer as its length became greater than " + (Int32.MaxValue / 3) + " >>" + Environment.NewLine;
                }
                strBuffer += message;

                // Now send the message to any IDebugOutputConsumers in Consumers
                try
                {
                    foreach (IDebugOutputConsumer idoc in Consumers)
                    {
                        if (idoc != null)
                        {
                            idoc.ConsumeDebugOutput(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // not much we can do here. Logging the exception would just throw this again. 
                    // TODO: #define DEBUG, then show message box
                    System.Diagnostics.Debug.WriteLine("Log(): Exception thrown feeding one of the Consumers. Exception: " + ex.Message);
                }
            }
        }

        static public void Log(string message)
        {
            Log(0, null, message);
        }

        static public void LogIf(bool YesNo, string message)
        {
            if (YesNo)
            {
                Log(message);
            }
        }

        static public void LogIf(bool YesNo, int level, string category, string message)
        {
            if (YesNo)
            {
                Log(level, category, message);
            }
        }

        static public void LogLine(int level, string category, string message)
        {
            Log(level, category, message, true);
        }

        static public void LogLine(string message)
        {
            Log(0, null, message, true);
        }

        static public void LogLineIf(bool YesNo, string message)
        {
            if (YesNo)
            {
                LogLine(message);
            }
        }

        static public void LogLineIf(bool YesNo, int level, string category, string message)
        {
            if (YesNo)
            {
                LogLine(level, category, message);
            }
        }

        static public bool CannotLog()
        {
            if (System.Diagnostics.Debugger.IsAttached && System.Diagnostics.Debugger.IsLogging()) return false;
            return true;
        }

    }
}
